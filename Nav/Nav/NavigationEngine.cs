using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using System.Diagnostics;

namespace Nav
{
    // the higher value the more important destination
    public enum DestType
    {
        None = 0x0000,
        Explore = 0x0001,
        Waypoint = 0x0002,
        Grid = 0x0004,
        Custom = 0x0008, // same as user but not cleared automatically
        User = 0x0010,
        BackTrack = 0x0020, // used for moving along historical destinations
        RunAway = 0x0040, // not used yet
        All = 0xFFFF,
    }

    public class NavigationEngine : IDisposable
    {
        public NavigationEngine(Navmesh navmesh)
        {
            m_Navmesh = navmesh;

            UpdatesThread = new Thread(Updates);
            UpdatesThread.Name = "Navigator-UpdatesThread";
            UpdatesThread.Start();

            Precision = 8;
            GridCellPrecision = 40;
            ExploreCellPrecision = 25;
            PathRandomCoeff = 0;
            PathNodesShiftDist = 5;
            CurrentPosDiffRecalcThreshold = 20;
            UpdatePathInterval = -1;
            EnableAntiStuck = false;
            IsStandingOnPurpose = true;
            MovementFlags = MovementFlag.Walk;            
        }

        public void AddListener(NavigationObserver listener)
        {
            if (m_Listeners.Contains(listener))
                return;

            m_Listeners.Add(listener);
        }

        public void RemoveListener(NavigationObserver listener)
        {
            m_Listeners.Remove(listener);
        }

        // defines how user can move through navmesh
        public MovementFlag MovementFlags { get; set; }

        // precision with each path node will be accepted as reached
        public float Precision { get; set; }

        // precision with grid cell will be accepted as reached
        public float GridCellPrecision { get; set; }

        // precision with grid cell will be accepted as reached
        public float ExploreCellPrecision { get; set; }

        // precision with each path node will be accepted as reached
        public float PathRandomCoeff { get; set; }

        // each point on path will be offseted in direction from previous point so bot will move along path more precisely even with high precision parameter
        public float PathNodesShiftDist { get; set; }

        // when new CurrentPos differ from last one by more than this value path update will be immediately requested
        public float CurrentPosDiffRecalcThreshold { set; get; }

        // path will be automatically recalculated with this interval (miliseconds)
        public int UpdatePathInterval { get; set; }

        public bool EnableAntiStuck { get; set; }

        // should be used when EnableAntiStuck is true to notify navigator that actor is not blocked by some obstacle but just standing
        public bool IsStandingOnPurpose { get; set; }

        public List<int> DestinationGridsId
        {
            get
            {
                using (new ReadLock(InputLock))
                    return new List<int>(m_DestinationGridsId);
            }

            set
            {
                using (new WriteLock(InputLock))
                {
                    if (value == null)
                        m_DestinationGridsId.Clear();
                    else
                        m_DestinationGridsId = new List<int>(value);
                }
            }
        }

        public bool FindPath(Vec3 from, Vec3 to, ref List<Vec3> path, bool as_close_as_possible)
        {
            return FindPath(from, to, MovementFlags, ref path, PATH_NODES_MERGE_DISTANCE, as_close_as_possible, false, m_PathRandomCoeffOverride > 0 ? m_PathRandomCoeffOverride : PathRandomCoeff, m_PathBounce, PathNodesShiftDist);
        }

        public bool FindPath(Vec3 from, Vec3 to, MovementFlag flags, ref List<Vec3> path, float merge_distance = -1, bool as_close_as_possible = false, bool include_from = false, float random_coeff = 0, bool bounce = false, float shift_nodes_distance = 0, bool straighten = true)
        {
            using (m_Navmesh.AquireReadDataLock())
            {
                List<path_pos> tmp_path = new List<path_pos>();

                if (from.IsEmpty || to.IsEmpty)
                    return false;

                Cell start = null;
                Cell end = null;

                bool start_on_nav_mesh = m_Navmesh.GetCellContaining(from, out start, null, as_close_as_possible, flags, false, 2);
                bool end_on_nav_mesh = m_Navmesh.GetCellContaining(to, out end, null, as_close_as_possible, flags, false, 2);

                if (bounce)
                {
                    Vec3 bounce_dir = start.AABB.GetBounceDir2D(from);
                    Vec3 new_from = from + bounce_dir * 10;
                    m_Navmesh.GetCellContaining(new_from, out start, null, as_close_as_possible, flags, false, 2);

                    if (!Algorihms.FindPath<Cell>(start, ref end, new_from, to, flags, ref tmp_path, random_coeff, true))
                        return false;

                    tmp_path.Insert(0, new path_pos(start.AABB.Align(from), start));
                }
                else
                {
                    if (!Algorihms.FindPath<Cell>(start, ref end, from, to, flags, ref tmp_path, random_coeff, true))
                        return false;
                }

                if (straighten && random_coeff == 0)
                    StraightenPath(ref tmp_path, flags, bounce ? 1 : 0);

                path = tmp_path.Select(x => x.pos).ToList();

                PostProcessPath(ref path, merge_distance, shift_nodes_distance);

                if (!include_from && start_on_nav_mesh)
                    path.RemoveAt(0);

                return true;
            }
        }

        private void StraightenPath(ref List<path_pos> path, MovementFlag flags, int skip_first_count = 0)
        {
            int ray_start_index = skip_first_count;

            while (ray_start_index + 2 < path.Count)
            {
                path_pos ray_start_data = path[ray_start_index];
                path_pos intermediate_data = path[ray_start_index + 1];
                path_pos ray_end_data = path[ray_start_index + 2];

                if (m_Navmesh.RayCast2D(ray_start_data.pos, ray_end_data.pos, flags, false))
                    path.RemoveAt(ray_start_index + 1);
                else
                    ++ray_start_index;
            }
        }

        private void PostProcessPath(ref List<Vec3> path, float merge_distance, float shift_nodes_distance)
        {
            if (merge_distance > 0)
            {
                for (int i = 0; i < path.Count - 1; ++i)
                {
                    int start_count = path.Count;
                    Vec3 merge_point = path[i];

                    while (i < path.Count - 1 && path[i].Distance2D(path[i + 1]) < merge_distance)
                    {
                        merge_point = merge_point + path[i + 1];
                        path.RemoveAt(i + 1);
                    }

                    if (path.Count != start_count)
                        path[i] = merge_point / (float)(start_count - path.Count + 1);
                }
            }

            // shift points to increase movement accuracy
            if (shift_nodes_distance > 0)
            {
                for (int i = path.Count - 2; i > 0; --i)
                {
                    Vec3 dir_to_next = path[i] - path[i - 1];
                    dir_to_next.Normalize();

                    path[i] += dir_to_next * shift_nodes_distance;
                }
            }
        }

        public bool IsPositionReachable(Vec3 from, Vec3 to, MovementFlag flags, float tolerance)
        {
            List<Vec3> tmp_path = new List<Vec3>();
            FindPath(from, to, flags, ref tmp_path, -1, true, true, 0, false, 0, false);
            return tmp_path.Count > 0 ? tmp_path.Last().Distance(to) <= tolerance : false;
        }

        public Vec3 Destination
        {
            get
            {
                using (new ReadLock(InputLock))
                    return new Vec3(m_Destination);
            }

            set
            {
                SetDestination(value, DestType.User);
            }
        }

        public void ClearAllDestinations()
        {
            ClearDestination(DestType.All);
        }

        public void ClearGridDestination()
        {
            ClearDestination(DestType.Grid);
        }

        public void SetCustomDestination(Vec3 pos)
        {
            SetDestination(pos, DestType.Custom);
        }

        public void ClearCustomDestination()
        {
            ClearDestination(DestType.Custom);
        }

        public bool TryGetPath(ref List<Vec3> p, ref DestType p_dest_type)
        {
            if (PathLock.TryEnterReadLock(0))
            {
                p = new List<Vec3>(m_Path);
                p_dest_type = m_PathDestType;
                PathLock.ExitReadLock();
                return true;
            }

            return false;
        }

        public bool TryGetBackTrackPath(ref List<Vec3> p)
        {
            if (InputLock.TryEnterReadLock(0))
            {
                p = new List<Vec3>(m_DestinationsHistory);
                p.Reverse();
                InputLock.ExitReadLock();
                return true;
            }

            return false;
        }

        public bool TryGetDebugPositionsHistory(ref List<Vec3> p)
        {
            if (InputLock.TryEnterReadLock(0))
            {
                p = new List<Vec3>(m_DebugPositionsHistory);
                InputLock.ExitReadLock();
                return true;
            }

            return false;
        }

        public List<Vec3> GetPath()
        {
            using (new ReadLock(PathLock))
                return new List<Vec3>(m_Path);
        }

        public DestType GetDestinationType()
        {
            return m_DestinationType;
        }

        public Vec3 GoToPosition
        {
            get
            {
                using (new ReadLock(PathLock))
                    return m_Path.Count > 0 ? new Vec3(m_Path[0]) : Vec3.Empty;
            }
        }

        public Vec3 CurrentPos
        {
            get
            {
                using (new ReadLock(InputLock))
                    return new Vec3(m_CurrentPos);
            }

            set
            {
                bool was_empty = false;
                float diff = 0;

                using (new WriteLock(InputLock))
                {
                    if (!m_CurrentPos.Equals(value))
                    {
                        was_empty = m_CurrentPos.IsEmpty;
                        diff = m_CurrentPos.Distance2D(value);
                        m_CurrentPos = value;

                        // add initial position as history destination
                        if (was_empty && m_DestinationsHistory.Count == 0)
                            m_DestinationsHistory.Add(value);
                    }
                }

                if (was_empty)
                    ReorganizeWaypoints();

                bool path_empty = false;
                bool huge_current_pos_change = was_empty || diff > CurrentPosDiffRecalcThreshold;

                // reduce locking when not necessary
                if (!huge_current_pos_change)
                {
                    using (new ReadLock(PathLock))
                        path_empty = m_Path.Count == 0;
                }

                if (huge_current_pos_change || path_empty)
                {
                    if (huge_current_pos_change && m_Navmesh.Explorator != null)
                        m_Navmesh.Explorator.OnHugeCurrentPosChange();

                    RequestPathUpdate();
                }

                if (!path_empty)
                {
                    UpdateAntiStuck();
                    UpdateDestReachFailed();
                    UpdatePathProgression();
                }
            }
        }

        public List<Vec3> Waypoints
        {
            get
            {
                using (new ReadLock(InputLock))
                    return new List<Vec3>(m_Waypoints);
            }

            set
            {
                using (new WriteLock(InputLock))
                    m_Waypoints = value;

                ReorganizeWaypoints();
            }
        }

        public bool BackTrackEnabled
        {
            get
            {
                return m_HistoryDestId >= 0;
            }

            set
            {
                using (new ReadLock(InputLock))
                {
                    // already enabled
                    if (value && m_HistoryDestId >= 0)
                        return;

                    m_HistoryDestId = (value ? m_DestinationsHistory.Count - 1 : -1);
                }

                if (!value)
                {
                    ClearDestination(DestType.BackTrack);

                    // all other "destination" updates will be handled by navigator
                    if (m_Navmesh.Explorator != null)
                        m_Navmesh.Explorator.RequestExplorationUpdate();
                }
            }
        }

        //public bool RunAwayEnabled
        //{
        //    get
        //    {
        //        return m_RunAway;
        //    }

        //    set
        //    {
        //        m_RunAway = value;
                
        //        if (!value)
        //        {
        //            ClearDestination(DestType.RunAway);

        //            // all other "destination" updates will be handled by navigator
        //            if (m_Navmesh.Explorator != null)
        //                m_Navmesh.Explorator.RequestExplorationUpdate();
        //        }
        //    }
        //}

        public GridCell GetCurrentGridCell()
        {
            using (new ReadLock(InputLock))
                return m_Navmesh.GetGridCell(m_CurrentPos);
        }

        //public Vec3 GetNearestGridCellOutsideAvoidAreas()
        //{
        //    using (new ReadLock(m_Navmesh.DataLock))
        //    using (new ReadLock(RegionsDataLock))
        //    {
        //        return Algorihms.GetRunAwayPosition(m_Navmesh.m_GridCells, m_Regions, m_CurrentPos, 200, MovementFlags);
        //    }
        //}

        public Int64 dbg_GetAntiStuckPrecisionTimerTime()
        {
            return m_AntiStuckPrecisionTimer.ElapsedMilliseconds;
        }

        public Int64 dbg_GetAntiStuckPathingTimerTime()
        {
            return m_AntiStuckPathingTimer.ElapsedMilliseconds;
        }

        public Int64 dbg_GetDestReachFailedTimerTime()
        {
            return m_DestReachFailedTimer.ElapsedMilliseconds;
        }
        
        public void dbg_StressTestPathing()
        {
            int timeout = 6000 * 1000;
            m_Navmesh.Log("[Nav] Stress test started [" + timeout / 1000 + "sec long]...");

            Stopwatch timeout_watch = new Stopwatch();
            timeout_watch.Start();

            while (timeout_watch.ElapsedMilliseconds < timeout)
            {
                CurrentPos = m_Navmesh.GetRandomPos();
                Destination = m_Navmesh.GetRandomPos();
                m_Navmesh.RayCast2D(m_Navmesh.GetRandomPos(), m_Navmesh.GetRandomPos(), MovementFlag.Fly);
                m_Navmesh.Log("[Nav] Ray cast done!");
            }

            m_Navmesh.Log("[Nav] Stress test ended!");
        }

        public void Dispose()
        {
            UpdatesThread.Abort();
        }

        // Aquires InputLock (read -> write)
        internal void ClearDestination(DestType type)
        {
            using (new ReadLock(InputLock, true))
            {
                if ((m_DestinationType & type) == 0)
                    return;

                using (new WriteLock(InputLock))
                {
                    m_Navmesh.Log("[Nav] Dest [" + m_DestinationType + "] cleared using [" + type + "] flags!");

                    m_Destination = Vec3.Empty;
                    m_DestinationType = DestType.None;
                }
            }
        }

        internal void Clear()
        {
            using (new WriteLock(InputLock))
            using (new WriteLock(PathLock))
            {
                m_CurrentPos = Vec3.Empty;
                m_Destination = Vec3.Empty;
                m_DestinationType = DestType.None;
                m_Waypoints.Clear();
                m_DestinationsHistory.Clear();
                m_DebugPositionsHistory.Clear();
                m_HistoryDestId = -1;
                m_DestinationGridsId.Clear();
                m_Path.Clear();
                m_PathDestination = Vec3.Empty;
                m_PathDestType = DestType.None;
            }
            ResetAntiStuckPrecition();
            ResetAntiStuckPathing();            
        }

        // May enter InputLock (read -> write)
        internal void SetDestination(Vec3 pos, DestType type)
        {
            if (pos.IsEmpty)
            {
                ClearDestination(type);
                return;
            }

            using (new ReadLock(InputLock, true))
            {
                if ((m_Destination.Equals(pos) && m_DestinationType == type) || (!m_Destination.IsEmpty && m_DestinationType > type))
                    return;

                using (new WriteLock(InputLock))
                {
                    m_Destination = pos;
                    m_DestinationType = type;
                }

                m_Navmesh.Log("[Nav] Dest changed to " + pos + " [" + type + "]");

                ResetDestReachFailed();
            }

            RequestPathUpdate();
        }

        internal bool IsDestinationReached(DestType type_filter)
        {
            Vec3 temp = Vec3.Empty;
            return IsDestinationReached(type_filter, ref temp);
        }

        internal bool IsDestinationReached(DestType type_filter, ref Vec3 destination)
        {
            using (new ReadLock(InputLock))
            using (new ReadLock(PathLock))
            {
                if ((m_DestinationType & type_filter) == 0)
                    return false;

                destination = new Vec3(m_Destination);

                return (m_CurrentPos.IsEmpty || m_Destination.IsEmpty || !m_Destination.Equals(m_PathDestination)) ? false : (m_Path.Count == 0);
            }
        }

        public void RequestPathUpdate()
        {
            //m_Navmesh.Log("Path update requested!");
            ForcePathUpdate = true;
        }

        private void Updates()
        {
            long last_path_update_time = 0;

            Stopwatch timer = new Stopwatch();
            timer.Start();

            while (true)
            {
                using (new ReadLock(InputLock, true))
                {
                    UpdateWaypointDestination();
                    UpdateGridDestination();
                    UpdateBackTrackDestination();
                }

                long time = timer.ElapsedMilliseconds;
                int update_path_interval = m_UpdatePathIntervalOverride > 0 ? m_UpdatePathIntervalOverride : UpdatePathInterval;

                if (ForcePathUpdate || (update_path_interval > 0 && (time - last_path_update_time) > update_path_interval))
                {
                    ForcePathUpdate = false;
                    last_path_update_time = time;
                    //using (new Profiler("[Nav] Path updated [{t}]"))
                        UpdatePath();
                }
                else
                    Thread.Sleep(50);
            }
        }

        private void UpdatePath()
        {
            if (!m_Navmesh.IsNavDataAvailable)
                return;

            Vec3 destination = Vec3.Empty;
            DestType dest_type = DestType.None;

            // make sure destination and its type are in sync
            using (new ReadLock(InputLock))
            {
                destination = m_Destination;
                dest_type = m_DestinationType;
            }

            Vec3 current_pos = CurrentPos;

            if (current_pos.IsEmpty || destination.IsEmpty)
                return;

            List<Vec3> new_path = new List<Vec3>();

            FindPath(current_pos, destination, MovementFlags, ref new_path, PATH_NODES_MERGE_DISTANCE, true, false, m_PathRandomCoeffOverride > 0 ? m_PathRandomCoeffOverride : PathRandomCoeff, m_PathBounce, PathNodesShiftDist);

            // verify whenever some point of path was not already passed during its calculation (this may take place when path calculations took long time)
            // this is done by finding first path segment current position can be casted on and removing all points preceding this segment including segment origin
            current_pos = CurrentPos;

            while (new_path.Count > 1)
            {
                Vec3 segment = new_path[1] - new_path[0];
                Vec3 segment_origin_to_current_pos = current_pos - new_path[0];

                float segment_len = segment.Length2D();
                float projection_len = segment.Dot2D(segment_origin_to_current_pos) / segment_len;

                // current position is already 'after' segment origin so remove it from path
                if (projection_len > 0)
                {
                    float distance_from_segment = -1;

                    // additionally verify if current pos is close enough to segment
                    if (projection_len > segment_len)
                        distance_from_segment = current_pos.Distance2D(new_path[1]);
                    else
                        distance_from_segment = current_pos.Distance2D(segment.Normalized2D() * projection_len);

                    if (distance_from_segment < Precision)
                        new_path.RemoveAt(0);
                    else
                        break;
                }
                else
                    break;
            }

            using (new WriteLock(PathLock))
            {
                // reset override when first destination from path changed
                if (m_Path.Count == 0 || (new_path.Count > 0 && !m_Path[0].Equals(new_path[0])))
                    ResetAntiStuckPrecition();

                m_Path = new_path;
                m_PathDestination = destination;
                m_PathDestType = dest_type;
            }
        }

        private void UpdateGridDestination()
        {
            Vec3 destination = Vec3.Empty;

            if (m_DestinationGridsId.Count > 0)
            {
                using (m_Navmesh.AquireReadDataLock())
                {
                    GridCell current_grid = m_Navmesh.m_GridCells.Find(g => g.Contains2D(CurrentPos));

                    GridCell destination_grid = m_Navmesh.m_GridCells.Find(g => m_DestinationGridsId.Contains(g.Id) && Algorihms.AreConnected(current_grid, ref g, MovementFlag.None));

                    if (destination_grid != null)
                        destination = m_Navmesh.GetNearestCell(destination_grid.Cells, destination_grid.Center).Center;
                }
            }
            
            if (!destination.IsEmpty)
                SetDestination(destination, DestType.Grid);
        }

        private void UpdateBackTrackDestination()
        {
            if (BackTrackEnabled)
                SetDestination(m_DestinationsHistory[m_HistoryDestId], DestType.BackTrack);
        }

        private void UpdateWaypointDestination()
        {
            
            if (m_Waypoints.Count > 0)
                SetDestination(m_Waypoints[0], DestType.Waypoint);
        }

        private float GetPrecision()
        {
            if (m_PrecisionOverride > 0)
                return m_PrecisionOverride;

            float precision = Precision;

            if (m_Path.Count == 1)
            {
                switch (m_DestinationType)
                {
                    case DestType.Grid:
                        precision = GridCellPrecision;
                        break;
                    case DestType.Explore:
                        precision = ExploreCellPrecision;
                        break;
                    case DestType.BackTrack:
                        // sometimes when grid or explore cell was just reached, back trace might lead us forward instead of backwards
                        precision = Math.Max(GridCellPrecision, ExploreCellPrecision) + 5;
                        break;
                }
            }

            return precision;
        }

        private void UpdatePathProgression()
        {
            bool any_node_reached = false;

            using (new ReadLock(InputLock, true))
            {
                Vec3 reached_pos = Vec3.Empty;

                using (new WriteLock(InputLock))
                {
                    if (m_DebugPositionsHistory.Count == 0 || m_CurrentPos.Distance2D(m_DebugPositionsHistory.Last()) > 15)
                        m_DebugPositionsHistory.Add(m_CurrentPos);
                }

                using (new ReadLock(PathLock, true))
                {
                    while (m_Path.Count > 0)
                    {
                        float precision = GetPrecision();

                        if (m_CurrentPos.Distance2D(m_Path[0]) > precision)
                            break;

                        reached_pos = m_Path[0];

                        using (new WriteLock(PathLock))
                            m_Path.RemoveAt(0);

                        any_node_reached = true;

                        ResetAntiStuckPrecition();
                    }
                }

                if (any_node_reached && !m_Destination.IsEmpty)
                {
                    if (IsDestinationReached(DestType.All))
                    {
                        if (m_DestinationType != DestType.BackTrack &&
                            (m_DestinationsHistory.Count == 0 || (!m_DestinationsHistory[m_DestinationsHistory.Count - 1].Equals(reached_pos) &&
                                                                  m_DestinationsHistory[m_DestinationsHistory.Count - 1].Distance(reached_pos) > MIN_DEST_DIST_TO_ADD_TO_HISTORY)))
                        {
                            m_DestinationsHistory.Add(reached_pos);
                        }

                        foreach (NavigationObserver listener in m_Listeners)
                            listener.OnDestinationReached(m_DestinationType, m_Destination);

                        m_Navmesh.Log("[Nav] Dest " + m_Destination + " [" + m_DestinationType + "] reached!");
                    }

                    if (IsDestinationReached(DestType.Waypoint))
                    {
                        using (new WriteLock(InputLock))
                            m_Waypoints.RemoveAt(0);

                        if (m_Waypoints.Count == 0)
                            ClearDestination(DestType.Waypoint);
                    }

                    if (IsDestinationReached(DestType.User))
                    {
                        ClearDestination(DestType.User);
                    }

                    if (IsDestinationReached(DestType.BackTrack))
                    {
                        --m_HistoryDestId;

                        if (!BackTrackEnabled)
                            ClearDestination(DestType.BackTrack);
                    }

                    if (IsDestinationReached(DestType.Explore) && m_Navmesh.Explorator != null)
                        m_Navmesh.Explorator.RequestExplorationUpdate();
                }
            }
        }

        private void UpdateDestReachFailed()
        {
            const float MIN_DIST_TO_RESET = 90;
            const float MIN_TIME_TO_FAIL_DESTINATION_REACH = 20000;

            if (m_DestReachFailedTestPos.IsEmpty || m_DestReachFailedTestPos.Distance(m_CurrentPos) > MIN_DIST_TO_RESET)
                ResetDestReachFailed();
            else if (IsStandingOnPurpose)
                m_DestReachFailedTimer.Stop();
            else
                m_DestReachFailedTimer.Start();

            if (m_DestReachFailedTimer.ElapsedMilliseconds > MIN_TIME_TO_FAIL_DESTINATION_REACH)
            {
                DestType dest_type = DestType.None;
                Vec3 dest = null;

                using (new ReadLock(InputLock))
                {
                    dest_type = m_DestinationType;
                    dest = m_Destination;
                }

                m_Navmesh.Log("[Nav] Dest " + dest + " [" + dest_type + "] reach failed!");

                foreach (NavigationObserver listener in m_Listeners)
                    listener.OnDestinationReachFailed(dest_type, dest);

                ResetDestReachFailed();
            }
        }

        private void UpdateAntiStuck()
        {
            if (!EnableAntiStuck)
                return;

            const float MIN_DIST_TO_RESET_ANTI_STUCK_PRECISION = 10;
            const float MIN_DIST_TO_RESET_ANTI_STUCK_PATHING = 25;

            const float MIN_TIME_TO_RECALCULATE_PATH = 2000;
            const float MIN_TIME_TO_OVERRIDE_PRECISION = 4000;
            const float MIN_TIME_TO_BOUNCE = 6000;
            const float MIN_TIME_TO_OVERRIDE_PATH_RANDOM_COEFF = 9000;

            using (new ReadLock(AntiStuckLock, true))
            {
                if (m_AntiStuckPrecisionTestPos.IsEmpty || m_AntiStuckPrecisionTestPos.Distance(m_CurrentPos) > MIN_DIST_TO_RESET_ANTI_STUCK_PRECISION)
                    ResetAntiStuckPrecition();
                else if (IsStandingOnPurpose)
                    m_AntiStuckPrecisionTimer.Stop();
                else
                    m_AntiStuckPrecisionTimer.Start();

                if (m_AntiStuckPathingTestPos.IsEmpty || m_AntiStuckPathingTestPos.Distance(m_CurrentPos) > MIN_DIST_TO_RESET_ANTI_STUCK_PATHING)
                    ResetAntiStuckPathing();
                else if (IsStandingOnPurpose)
                    m_AntiStuckPathingTimer.Stop();
                else
                    m_AntiStuckPathingTimer.Start();

                // handle anti stuck precision management features
                if (m_AntiStuckPrecisionTimer.ElapsedMilliseconds > MIN_TIME_TO_OVERRIDE_PRECISION)
                    m_PrecisionOverride = 60;

                // handle anti stuck path management features

                // level 1
                if (m_AntiStuckPathingTimer.ElapsedMilliseconds > MIN_TIME_TO_RECALCULATE_PATH &&
                    m_AntiStuckPathingLevel == 0)
                {
                    m_AntiStuckPathingLevel = 1;
                    RequestPathUpdate();
                }
                // level 2
                else if (m_AntiStuckPathingTimer.ElapsedMilliseconds > MIN_TIME_TO_BOUNCE &&
                         m_AntiStuckPathingLevel == 1)
                {
                    ResetAntiStuckPrecition();
                    m_AntiStuckPathingLevel = 2;
                    m_PathBounce = true;
                    RequestPathUpdate();
                }
                // level 3
                else if (m_AntiStuckPathingTimer.ElapsedMilliseconds > MIN_TIME_TO_OVERRIDE_PATH_RANDOM_COEFF &&
                         m_AntiStuckPathingLevel == 2)
                {
                    ResetAntiStuckPrecition();
                    m_PathBounce = false;
                    m_AntiStuckPathingLevel = 3;
                    m_PathRandomCoeffOverride = 1.5f;
                    m_UpdatePathIntervalOverride = 3000;
                    RequestPathUpdate();
                }                
            }
        }

        private void ResetAntiStuckPrecition()
        {
            using (new WriteLock(AntiStuckLock))
            {
                m_PrecisionOverride = -1;
                m_AntiStuckPrecisionTestPos = CurrentPos;
                m_AntiStuckPrecisionTimer.Reset();
            }
        }

        private void ResetAntiStuckPathing()
        {
            using (new WriteLock(AntiStuckLock))
            {
                m_PathRandomCoeffOverride = -1;
                m_UpdatePathIntervalOverride = -1;
                m_PathBounce = false;
                m_AntiStuckPathingTestPos = CurrentPos;
                m_AntiStuckPathingTimer.Reset();
                m_AntiStuckPathingLevel = 0;
            }
        }

        private void ResetDestReachFailed()
        {
            m_DestReachFailedTestPos = CurrentPos;
            m_DestReachFailedTimer.Reset();
        }

        private void ReorganizeWaypoints()
        {
            Vec3 pos = CurrentPos;

            if (pos.IsEmpty)
                return;

            using (new ReadLock(InputLock, true))
            {
                if (m_Waypoints.Count == 0)
                    return;

                int nearest_waypoint_index = -1;
                float nearest_waypoint_dist = float.MaxValue;

                for (int i = 0; i < m_Waypoints.Count; ++i)
                {
                    float dist = m_Waypoints[i].Distance(pos);

                    if (dist < nearest_waypoint_dist)
                    {
                        nearest_waypoint_index = i;
                        nearest_waypoint_dist = dist;
                    }
                }

                using (new WriteLock(InputLock))
                {
                    for (int i = 0; i < nearest_waypoint_index; ++i)
                    {
                        m_Waypoints.Add(new Vec3(m_Waypoints[0]));
                        m_Waypoints.RemoveAt(0);
                    }
                }
            }
        }

        internal void Serialize(BinaryWriter w)
        {
            using (new ReadLock(InputLock))
            using (new ReadLock(PathLock))
            {
                w.Write(m_Waypoints.Count);
                foreach (Vec3 p in m_Waypoints)
                    p.Serialize(w);

                w.Write(m_Path.Count);
                foreach (Vec3 p in m_Path)
                    p.Serialize(w);
                m_PathDestination.Serialize(w);
                w.Write((int)m_PathDestType);

                w.Write(m_DestinationsHistory.Count);
                foreach (Vec3 p in m_DestinationsHistory)
                    p.Serialize(w);
                w.Write(m_HistoryDestId);

                w.Write(m_DebugPositionsHistory.Count);
                foreach (Vec3 p in m_DebugPositionsHistory)
                    p.Serialize(w);

                w.Write(m_DestinationGridsId.Count);
                foreach (int d in m_DestinationGridsId)
                    w.Write(d);

                m_CurrentPos.Serialize(w);
                m_Destination.Serialize(w);
                w.Write((int)m_DestinationType);

                w.Write(UpdatePathInterval);
                w.Write(CurrentPosDiffRecalcThreshold);
                w.Write(PathNodesShiftDist);
                w.Write(PathRandomCoeff);
                w.Write(Precision);
            }
        }

        internal void Deserialize(List<Cell> all_cells, BinaryReader r)
        {
            using (new WriteLock(InputLock))
            using (new WriteLock(PathLock))
            {
                m_Waypoints.Clear();
                m_DestinationGridsId.Clear();
                m_Path.Clear();
                m_DestinationsHistory.Clear();
                m_DebugPositionsHistory.Clear();

                int waypoints_count = r.ReadInt32();
                for (int i = 0; i < waypoints_count; ++i)
                    m_Waypoints.Add(new Vec3(r));

                int path_count = r.ReadInt32();
                for (int i = 0; i < path_count; ++i)
                    m_Path.Add(new Vec3(r));
                m_PathDestination = new Vec3(r);
                m_PathDestType = (DestType)r.ReadInt32();

                int destination_history_count = r.ReadInt32();
                for (int i = 0; i < destination_history_count; ++i)
                    m_DestinationsHistory.Add(new Vec3(r));
                m_HistoryDestId = r.ReadInt32();

                int debug_positions_history_count = r.ReadInt32();
                for (int i = 0; i < debug_positions_history_count; ++i)
                    m_DebugPositionsHistory.Add(new Vec3(r));

                int destination_grid_cells_id_count = r.ReadInt32();
                for (int i = 0; i < destination_grid_cells_id_count; ++i)
                    m_DestinationGridsId.Add(r.ReadInt32());

                m_CurrentPos = new Vec3(r);
                m_Destination = new Vec3(r);
                m_DestinationType = (DestType)r.ReadInt32();

                UpdatePathInterval = r.ReadInt32();
                CurrentPosDiffRecalcThreshold = r.ReadSingle();
                PathNodesShiftDist = r.ReadSingle();
                PathRandomCoeff = r.ReadSingle();
                Precision = r.ReadSingle();
            }

            ResetAntiStuckPrecition();
            ResetAntiStuckPathing();
        }

        private float PATH_NODES_MERGE_DISTANCE = -1;
        private float MIN_DEST_DIST_TO_ADD_TO_HISTORY = 75;

        private Thread UpdatesThread = null;
        private volatile bool ForcePathUpdate = false;

        private ReaderWriterLockSlim PathLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private ReaderWriterLockSlim InputLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        private ReaderWriterLockSlim AntiStuckLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        
        private List<Vec3> m_Path = new List<Vec3>(); //@ PathLock
        private Vec3 m_PathDestination = Vec3.Empty; //@ PathLock
        private DestType m_PathDestType = DestType.None; //@ PathLock
        private List<Vec3> m_Waypoints = new List<Vec3>(); //@ InputLock
        private List<Vec3> m_DestinationsHistory = new List<Vec3>(); //@ InputLock
        private List<Vec3> m_DebugPositionsHistory = new List<Vec3>(); //@ InputLock
        private int m_HistoryDestId = -1; //@ InputLock
        private List<int> m_DestinationGridsId = new List<int>(); //@ InputLock
        
        private float m_PrecisionOverride = -1;
        private Stopwatch m_AntiStuckPrecisionTimer = new Stopwatch();
        private float m_PathRandomCoeffOverride = -1;
        private int m_UpdatePathIntervalOverride = -1;
        private Stopwatch m_AntiStuckPathingTimer = new Stopwatch();
        private int m_AntiStuckPathingLevel = 0;
        private bool m_PathBounce = false;
        private Stopwatch m_DestReachFailedTimer = new Stopwatch();

        private Vec3 m_AntiStuckPrecisionTestPos = Vec3.Empty; //@ AntiStuckLock
        private Vec3 m_AntiStuckPathingTestPos = Vec3.Empty; //@ AntiStuckLock
        private Vec3 m_DestReachFailedTestPos = Vec3.Empty;

        private Vec3 m_CurrentPos = Vec3.Empty; //@ InputLock        
        private Vec3 m_Destination = Vec3.Empty; //@ InputLock
        private DestType m_DestinationType = DestType.None; //@ InputLock

        private List<NavigationObserver> m_Listeners = new List<NavigationObserver>();
        
        private Navmesh m_Navmesh = null;
    }
}
