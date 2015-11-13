using System;
using System.Collections.Generic;
using System.Threading;

namespace Nav.ExploreEngine
{
    // This algorithm is attempt to solve Traveling Salesman Problem by generating path leading through all unexplored cells.
    public class TSP : ExplorationEngine
    {
        public TSP()
        {
            ExplorePathThread = new Thread(UpdateExplorePathThread);
            ExplorePathThread.Name = "ExplorePathThread";
            ExplorePathThread.Start();

            m_UpdateExplorationInterval = 500;
        }

        public override void Clear()
        {
            base.Clear();
            m_ExplorePathCalculated = false;
        }

        public override void Dispose()
        {
            base.Dispose();
            ExplorePathThread.Abort();
        }

        public bool TryGetExplorePath(ref List<Vec3> p)
        {
            if (ExplorePathDataLock.TryEnterReadLock(0))
            {
                p = new List<Vec3>(ExplorePath);
                ExplorePathDataLock.ExitReadLock();
                return true;
            }

            return false;
        }

        public override void OnHugeCurrentPosChange()
        {
            base.OnHugeCurrentPosChange();
            RequestExplorePathUpdate();
        }

        public override void OnNavDataChange()
        {
            base.OnNavDataChange();
            RequestExplorePathUpdate();
        }

        internal void RequestExplorePathUpdate()
        {
            force_explore_path_update = true;
        }

        internal override bool IsDataAvailable
        {
            get { return m_ExplorePathCalculated; }
        }

        internal override Vec3 GetDestinationCellPosition()
        {
            ExploreCell current_explore_cell = m_ExploreCells.Find(x => x.CellsContains2D(Navmesh.Navigator.CurrentPos));

            // visit 'unexplored' cells on the way
            if (current_explore_cell != null && !current_explore_cell.Explored)
                return current_explore_cell.Position;
            else
                using (new ReadLock(ExplorePathDataLock, true))
                    return ExplorePath.Count > 0 ? ExplorePath[0] : Vec3.Empty;
        }

        protected override void OnCellExplored(ExploreCell cell)
        {
            base.OnCellExplored(cell);

            using (new WriteLock(ExplorePathDataLock))
                ExplorePath.Remove(cell.Position);
        }

        public override bool IsExplored()
        {
            using (new ReadLock(ExplorePathDataLock, true))
                return m_ExplorePathCalculated && ExplorePath.Count == 0;
        }

        private void UpdateExplorePathThread()
        {
            while (true)
            {
                if (force_explore_path_update)
                {
                    force_explore_path_update = false;
                    //using (new Profiler("[Nav] Explore path updated [{t}]"))
                        UpdateExplorePath();
                }
                else
                    Thread.Sleep(50);
            }
        }

        private void UpdateExplorePath()
        {
            if (Navmesh == null)
                return;

            List<Vec3> new_explore_path = new List<Vec3>();

            using (new ReadLock(DataLock))
            {
                ExploreCell start_cell = null;

                Vec3 current_pos_copy = Navmesh.Navigator.CurrentPos;

                if (!current_pos_copy.IsEmpty)
                {
                    List<ExploreCell> containing_cells = m_ExploreCells.FindAll(c => c.Contains2D(current_pos_copy) && c.Neighbours.Count > 0);

                    float min_dist = float.MaxValue;

                    foreach (ExploreCell e_cell in containing_cells)
                    {
                        foreach (Cell cell in e_cell.Cells)
                        {
                            float dist = cell.AABB.Distance2D(current_pos_copy);
                            if (dist < min_dist)
                            {
                                start_cell = e_cell;
                                min_dist = dist;
                            }
                        }
                    }
                }

                if (start_cell == null)
                    return;

                Algorihms.FindExplorePath2Opt(start_cell, m_ExploreCells, m_ExploreCellsDistancer, ref new_explore_path);
            }

            using (new WriteLock(ExplorePathDataLock))
            {
                ExplorePath = new_explore_path;
                m_ExplorePathCalculated = true;
            }

            RequestExplorationUpdate();
        }

        private bool m_ExplorePathCalculated = false;

        private volatile bool force_explore_path_update = false;

        private Thread ExplorePathThread = null;

        protected List<Vec3> ExplorePath = new List<Vec3>(); //@ explore_path_data_lock

        internal ReaderWriterLockSlim ExplorePathDataLock = new ReaderWriterLockSlim();

    }
}
