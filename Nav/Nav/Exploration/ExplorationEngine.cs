using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Linq;

namespace Nav
{
    public abstract class ExplorationEngine : IDisposable, NavigationObserver
    {
        public ExplorationEngine()
        {
            UpdatesThread = new Thread(Updates);
            UpdatesThread.Name = "Explorator-UpdatesThread";
            UpdatesThread.Start();

            EXPLORE_CELL_SIZE = 100;
            MAX_AREA_TO_MARK_AS_SMALL = 400;
        }

        public void AttachToNavmesh(Navmesh nav_mesh)
        {
            Navmesh = nav_mesh;
            Navmesh.Navigator.AddListener(this);
        }

        public int EXPLORE_CELL_SIZE { get; protected set; }
        public float MAX_AREA_TO_MARK_AS_SMALL { get; protected set; }

        public virtual void Clear()
        {
            using (new WriteLock(DataLock))
            using (new WriteLock(InputLock))
            {
                m_ExploreCells.Clear();
                m_ExploreCellsDistancer.Clear();

                m_LastExploreCellId = 0;

                m_HintPos = Vec3.Empty;
            }
        }

        public virtual float GetExploredPercent()
        {
            using (new ReadLock(DataLock))
            {
                if (m_ExploreCells.Count == 0)
                    return 0;

                int explored_cells_count = m_ExploreCells.Count(x => x.Explored);

                return (float)Math.Round(explored_cells_count / (float)m_ExploreCells.Count * 100, 1);
            }
        }

        public virtual void Dispose()
        {
            UpdatesThread.Abort();
        }

        internal virtual void Serialize(BinaryWriter w)
        {
            using (new ReadLock(DataLock))
            using (new ReadLock(InputLock))
            {
                w.Write(m_Enabled);

                // write all cells global IDs
                w.Write(m_ExploreCells.Count);
                foreach (ExploreCell explore_cell in m_ExploreCells)
                    w.Write(explore_cell.GlobalId);

                foreach (ExploreCell explore_cell in m_ExploreCells)
                    explore_cell.Serialize(w);

                w.Write(ExploreCell.LastExploreCellGlobalId);

                m_ExploreCellsDistancer.Serialize(w);

                m_HintPos.Serialize(w);
            }
        }

        internal virtual void Deserialize(List<Cell> all_cells, BinaryReader r)
        {
            using (new WriteLock(DataLock))
            using (new WriteLock(InputLock))
            {
                m_ExploreCells.Clear();
                m_ExploreCellsDistancer.Clear();

                m_Enabled = r.ReadBoolean();

                int explore_cells_count = r.ReadInt32();

                // pre-allocate explore cells
                for (int i = 0; i < explore_cells_count; ++i)
                {
                    ExploreCell explore_cell = new ExploreCell();
                    explore_cell.GlobalId = r.ReadInt32();
                    m_ExploreCells.Add(explore_cell);
                }

                m_ExploreCells.Sort(new Cell.CompareByGlobalId());

                foreach (ExploreCell explore_cell in m_ExploreCells)
                    explore_cell.Deserialize(m_ExploreCells, all_cells, r);

                ExploreCell.LastExploreCellGlobalId = r.ReadInt32();

                m_ExploreCellsDistancer.Deserialize(r);

                m_HintPos = new Vec3(r);
            }
        }

        public virtual void OnHugeCurrentPosChange()
        {
            RequestExplorationUpdate();
        }

        public virtual void OnNavDataChange()
        {
            RequestExplorationUpdate();
        }

        internal virtual Vec3 GetDestinationCellPosition()
        {
            Vec3 hint_pos = HintPos;

            if (!hint_pos.IsEmpty)
            {
                // find nearest unexplored cell to the hint position
                float min_dist = float.MaxValue;
                ExploreCell nearest_explore_cell = null;

                foreach (ExploreCell explore_cell in m_ExploreCells)
                {
                    if (explore_cell.Explored)
                        continue;

                    float dist = explore_cell.Position.DistanceSqr(hint_pos);

                    if (dist < min_dist)
                    {
                        nearest_explore_cell = explore_cell;
                        min_dist = dist;
                    }
                }

                return nearest_explore_cell != null ? nearest_explore_cell.Position : Vec3.Empty;
            }

            return Vec3.Empty;
        }

        internal virtual bool IsDataAvailable
        {
            get { return m_ExploreCells.Count > 0; }
        }
        
        public bool Enabled
        {
            get
            {
                return m_Enabled;
            }

            set
            {
                if (m_Enabled == value)
                    return;

                m_Enabled = CanBeEnabled() && value;

                if (value)
                    RequestExplorationUpdate();
                else
                    Navmesh.Navigator.ClearDestination(DestType.Explore);
            }
        }

        // Operations on this list are not thread safe! Use dbg_ReadLockGridCells to make it thread safe
        public List<ExploreCell> dbg_GetExploreCells()
        {
            return m_ExploreCells;
        }

        public ReadLock AquireReadDataLock()
        {
            return new ReadLock(DataLock);
        }

        public float ExploreDistance(ExploreCell cell_1, ExploreCell cell_2)
        {
            using (new ReadLock(DataLock))
                return m_ExploreCellsDistancer.GetDistance(cell_1.GlobalId, cell_2.GlobalId);
        }

        protected virtual bool CanBeEnabled() { return true; }

        protected Navmesh Navmesh;

        internal void RequestExplorationUpdate()
        {
            m_ForceNavUpdate = true;
        }

        public void OnDestinationReached(DestType type, Vec3 dest)
        {
            if (type != DestType.Explore)
                return;

            ExploreCell dest_cell = m_ExploreCells.Find(x => x.Position.Equals(dest));

            if (dest_cell != null)
                OnCellExplored(dest_cell);

            Vec3 next_dest = GetDestinationCellPosition();
            Navmesh.Navigator.SetDestination(next_dest, DestType.Explore);
        }

        public void OnDestinationReachFailed(DestType type, Vec3 dest)
        {
            OnDestinationReached(type, dest);
        }

        protected virtual void OnCellExplored(ExploreCell cell)
        {
            cell.Explored = true; // this is safe as explore cells cannot be added/removed now
            Navmesh.Log("[Nav] Explored cell " + cell.GlobalId + " [progress: " + GetExploredPercent() + "%]!");
        }

        public virtual bool IsExplored()
        {
            using (new ReadLock(DataLock))
            {
                if (!IsDataAvailable)
                    return false;

                ExploreCell current_explore_cell = GetCurrentExploreCell();

                if (current_explore_cell == null)
                    return true;

                if (!current_explore_cell.Explored)
                    return false;

                return GetUnexploredCellsId(current_explore_cell).Count == 0;
            }
        }

        protected ExploreCell GetCurrentExploreCell()
        {
            Vec3 curr_pos = Navmesh.Navigator.CurrentPos;

            ExploreCell current_explore_cell = m_ExploreCells.Find(x => x.CellsContains2D(curr_pos));

            if (current_explore_cell == null)
                current_explore_cell = m_ExploreCells.Find(x => x.Contains2D(curr_pos));

            // find nearest explore cell
            if (current_explore_cell == null)
            {
                float min_dist = float.MaxValue;

                foreach (ExploreCell explore_cell in m_ExploreCells)
                {
                    float dist = explore_cell.Position.DistanceSqr(curr_pos);

                    if (dist < min_dist)
                    {
                        current_explore_cell = explore_cell;
                        min_dist = dist;
                    }
                }
            }

            return current_explore_cell;
        }

        protected List<int> GetUnexploredCellsId(ExploreCell origin_cell)
        {
            using (new ReadLock(DataLock))
            {
                List<ExploreCell> unexplored_cells = m_ExploreCells.FindAll(x => !x.Explored);
                List<ExploreCell> big_unexplored_cells = unexplored_cells.FindAll(x => !x.Small);

                List<int> unexplored_cells_id = null;

                // explore small cells only when all other cells were explored
                if (big_unexplored_cells.Count == 0)
                    unexplored_cells_id = unexplored_cells.Select(x => x.GlobalId).ToList();
                else
                    unexplored_cells_id = big_unexplored_cells.Select(x => x.GlobalId).ToList();

                List<int> potential_cells_id = m_ExploreCellsDistancer.GetConnectedTo(origin_cell.GlobalId);

                return potential_cells_id.Intersect(unexplored_cells_id).ToList(); // consider only unexplored
            }
        }

        private void Updates()
        {
            long last_update_time = 0;

            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (true)
            {
                long time = timer.ElapsedMilliseconds;

                if (m_ForceNavUpdate || (m_UpdateExplorationInterval > 0 && (time - last_update_time) > m_UpdateExplorationInterval))
                {
                    last_update_time = time;
                    m_ForceNavUpdate = false;

                    //using (new Profiler("[Nav] Nav updated [{t}]"))
                        UpdateExploration();
                }
                else
                    Thread.Sleep(50);
            }
        }

        private void UpdateExploration()
        {
            Vec3 current_pos = Navmesh.Navigator.CurrentPos;

            if (!Enabled || current_pos.IsEmpty)
                return;

            if (!IsDataAvailable)
            {
                Navmesh.Log("[Nav] Exploration data unavailable!");
                return;
            }

            if (IsExplored())
            {
                Navmesh.Log("[Nav] Exploration finished!");
                Navmesh.Navigator.SetDestination(Vec3.Empty, DestType.None);
                return;
            }

            using (new ReadLock(DataLock, true))
            {
                if (Navmesh.Navigator.GetDestinationType() < DestType.Explore)
                {
                    Vec3 dest = GetDestinationCellPosition();
                    Navmesh.Navigator.SetDestination(dest, DestType.Explore);
                    Navmesh.Log("[Nav] Explore dest changed.");
                }
                
                {
                    // mark cells as explored when passing by close enough
                    ExploreCell current_explore_cell = m_ExploreCells.Find(x => !x.Explored && x.Position.Distance2D(current_pos) < Navmesh.Navigator.ExploreCellPrecision);

                    if (current_explore_cell != null)
                        OnCellExplored(current_explore_cell);
                }
            }
        }

        private void Add(ExploreCell explore_cell)
        {
            using (new WriteLock(DataLock))
            {
                foreach (ExploreCell e_cell in m_ExploreCells)
                {
                    if (e_cell.AddNeighbour(explore_cell))
                        m_ExploreCellsDistancer.Connect(e_cell.GlobalId, explore_cell.GlobalId, e_cell.Position.Distance2D(explore_cell.Position));
                }

                m_ExploreCells.Add(explore_cell);
            }
        }

        internal void OnGridCellAdded(GridCell grid_cell, bool trigger_nav_data_change = true)
        {
            //using (new Profiler("[Nav] Nav data updated [{t}]"))
            using (new ReadLock(DataLock, true))
            {
                // remove explore cells overlapping with grid cell
                List<ExploreCell> cells_to_validate = m_ExploreCells.FindAll(x => x.Overlaps2D(grid_cell));

                using (new WriteLock(DataLock))
                {
                    foreach (ExploreCell explore_cell in cells_to_validate)
                    {
                        explore_cell.Detach();
                        m_ExploreCells.RemoveAll(x => x.GlobalId == explore_cell.GlobalId);
                        m_ExploreCellsDistancer.Disconnect(explore_cell.GlobalId);
                    }
                }

                // check if new explore cells should be added
                int x_min = (int)Math.Floor(grid_cell.Min.X / EXPLORE_CELL_SIZE);
                int y_min = (int)Math.Floor(grid_cell.Min.Y / EXPLORE_CELL_SIZE);

                int x_max = (int)Math.Ceiling(grid_cell.Max.X / EXPLORE_CELL_SIZE);
                int y_max = (int)Math.Ceiling(grid_cell.Max.Y / EXPLORE_CELL_SIZE);

                int explore_cells_generated = 0;

                for (int y_index = y_min; y_index < y_max; ++y_index)
                {
                    for (int x_index = x_min; x_index < x_max; ++x_index)
                    {
                        AABB cell_aabb = new AABB(x_index * EXPLORE_CELL_SIZE, y_index * EXPLORE_CELL_SIZE, -10000,
                                                  (x_index + 1) * EXPLORE_CELL_SIZE, (y_index + 1) * EXPLORE_CELL_SIZE, 10000);

                        explore_cells_generated += GenerateExploreCells(cell_aabb);
                    }
                }

                if (explore_cells_generated > 0)
                    Navmesh.Log("[Nav] " + explore_cells_generated + " explore cell(s) generated");
            }

            //due to performance reasons OnNavDataChange is not automatically called after adding each grid cell
            if (trigger_nav_data_change)
                OnNavDataChange();
        }

        private int GenerateExploreCells(AABB cell_aabb)
        {
            // should not happen
            //if (m_ExploreCells.Exists(x => x.AABB.Equals(cell_aabb)))
            //    return;

            MovementFlag movement_flags = Navmesh.Navigator.MovementFlags;

            //using (new Profiler("[Nav] Nav data generated [{t}]"))
            using (Navmesh.AquireReadDataLock())
            {
                List<Cell> cells = new List<Cell>();

                // find all cells inside cell_aabb
                foreach (GridCell grid_cell in Navmesh.m_GridCells)
                {
                    if (!cell_aabb.Overlaps2D(grid_cell.AABB))
                        continue;

                    cells.AddRange(grid_cell.Cells.FindAll(x => !x.Replacement && x.HasFlags(movement_flags) && cell_aabb.Overlaps2D(x.AABB)));
                }

                // add all replaced cells overlapped by explore cell
                //cells.AddRange(Navmesh.GetReplacedCells().FindAll(x => x.HasFlags(movement_flags) && cell_aabb.Overlaps2D(x.AABB)));

                //create ExploreCell for each interconnected group of cells
                List<Cell> cells_copy = new List<Cell>(cells);
                int last_explore_cells_count = m_ExploreCells.Count;

                while (cells_copy.Count > 0)
                {
                    List<Cell> visited = new List<Cell>();

                    Algorihms.Visit(cells_copy[0], ref visited, movement_flags, true, 1, -1, cells_copy);

                    List<AABB> intersections = new List<AABB>();
                    AABB intersections_aabb = new AABB();

                    foreach (Cell c in visited)
                    {
                        AABB intersection = cell_aabb.Intersect(c.AABB);

                        intersections.Add(intersection);
                        intersections_aabb.Extend(intersection);
                    }

                    Vec3 nearest_intersection_center = Vec3.Empty;
                    float nearest_intersection_dist = float.MaxValue;

                    foreach (AABB inter_aabb in intersections)
                    {
                        float dist = inter_aabb.Center.Distance2D(intersections_aabb.Center);

                        if (dist < nearest_intersection_dist)
                        {
                            nearest_intersection_center = inter_aabb.Center;
                            nearest_intersection_dist = dist;
                        }
                    }

                    ExploreCell ex_cell = new ExploreCell(cell_aabb, visited, nearest_intersection_center, m_LastExploreCellId++);
                    Add(ex_cell);

                    ex_cell.Small = (ex_cell.CellsArea() < MAX_AREA_TO_MARK_AS_SMALL);

                    cells_copy.RemoveAll(x => visited.Contains(x));
                }

                return m_ExploreCells.Count - last_explore_cells_count;
            }
        }

        public Vec3 HintPos
        {
            get
            {
                using (new ReadLock(InputLock))
                    return new Vec3(m_HintPos);
            }

            set
            {
                using (new WriteLock(InputLock))
                    m_HintPos = value;
            }
        }

        private bool m_Enabled = true;
        private int m_LastExploreCellId = 0;

        protected List<ExploreCell> m_ExploreCells = new List<ExploreCell>(); //@ DataLock
        protected CellsDistancer m_ExploreCellsDistancer = new CellsDistancer(); //@ DataLock
        
        private Thread UpdatesThread = null;        

        private volatile bool m_ForceNavUpdate = false;
        protected int m_UpdateExplorationInterval = 500;

        private ReaderWriterLockSlim InputLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        protected ReaderWriterLockSlim DataLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private Vec3 m_HintPos = Vec3.Empty; //@ InputLock
    }
}
