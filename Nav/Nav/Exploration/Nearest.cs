using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Nav.ExploreEngine
{
    // This algorithm chooses nearest unexplored neighbour but prefer those with many visited neighbours to not leave unexplored islands
    public class Nearest : ExplorationEngine
    {
        internal override Vec3 GetDestinationCellPosition()
        {
            Vec3 dest = base.GetDestinationCellPosition();

            if (!dest.IsEmpty)
                return dest;

            ExploreCell current_explore_cell = GetCurrentExploreCell();

            if (current_explore_cell == null)
                return Vec3.Empty;

            if (!current_explore_cell.Explored)
                return current_explore_cell.Position;

            List<int> unexplored_cells_id = GetUnexploredCellsId(current_explore_cell);

            ExploreCell dest_cell = null;
            float dest_cell_distance = float.MaxValue;

            //const float DISTANCE_REDUCTION_PER_EXPLORED_NEIGHBOUR = 15;
            //const float DISTANCE_REDUCTION_PER_MISSING_NEIGHBOUR = 10;
            const float DISTANCE_REDUCTION_EXPLORE_PCT = 500;
            //const float DISTANCE_PCT_REDUCTION_PER_EXPLORED_NEIGHBOUR = 0.06f;
            //const float DISTANCE_PCT_REDUCTION_PER_MISSING_NEIGHBOUR = 0.05f;
            //const int AVG_NEIGHBOURS_COUNT = 8;

            foreach (int cell_id in unexplored_cells_id)
            {
                float base_dist = m_ExploreCellsDistancer.GetDistance(current_explore_cell.GlobalId, cell_id);

                //base_dist *= base_dist; // increase distance significance

                ExploreCell cell = m_ExploreCells.Find(x => x.GlobalId == cell_id);

                // decrease distance based on number of explored neighbours (do not leave small unexplored fragments)


                //int explored_neighbours_count = GetExploredNeighbours(cell, 2);
                //int missing_neighbours_count = Math.Max(0, AVG_NEIGHBOURS_COUNT - cell.Neighbours.Count);

                float dist = base_dist;

                dist -= DISTANCE_REDUCTION_EXPLORE_PCT * GetExploredNeighboursPct(cell, 1);

                //dist -= DISTANCE_REDUCTION_PER_EXPLORED_NEIGHBOUR * (float)explored_neighbours_count;
                //dist -= DISTANCE_REDUCTION_PER_MISSING_NEIGHBOUR * Math.Max(0, AVG_NEIGHBOURS_COUNT - cell.Neighbours.Count);

                //dist -= base_dist * DISTANCE_PCT_REDUCTION_PER_EXPLORED_NEIGHBOUR * (float)explored_neighbours_count;
                //dist -= base_dist * DISTANCE_PCT_REDUCTION_PER_MISSING_NEIGHBOUR * (float)missing_neighbours_count;

                //cell.UserData = (Int64)(GetExploredNeighboursPct(cell, 1) * 100);

                if (dist < dest_cell_distance)
                {
                    dest_cell = cell;
                    dest_cell_distance = dist;
                }
            }

            if (dest_cell != null)
                return dest_cell.Position;

            return Vec3.Empty;
        }

        private float GetExploredNeighboursPct(ExploreCell cell, int max_depth)
        {
            List<ExploreCell> cells_group = new List<ExploreCell>();

            Algorihms.Visit(cell, ref cells_group, Navmesh.Navigator.MovementFlags, true, 0, max_depth, m_ExploreCells);

            //treat missing cells as explored thus explore edges to possibly load new navmesh data
            int max_cells_num = (int)Math.Pow(9, max_depth);
            int missing_cells = Math.Max(0, max_cells_num - cells_group.Count);

            return (float)(cells_group.Count(x => ((ExploreCell)x).Explored) + missing_cells) / (float)max_cells_num;
        }
    }
}
