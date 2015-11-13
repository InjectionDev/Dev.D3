using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace Nav
{
    [Flags]
    public enum MovementFlag
    {
        None = 0x0000,
        Walk = 0x0001,
        Fly = 0x0002,
        All = Walk | Fly,
    }

    public class Cell : IEquatable<Cell>
    {
        public Cell()
        {
            InitCell(-1, new AABB(0, 0, 0, 0, 0, 0), MovementFlag.None, 1);
        }

        public Cell(float min_x, float min_y, float min_z, float max_x, float max_y, float max_z, MovementFlag flags, int id = -1)
        {
            InitCell(id, new AABB(min_x, min_y, min_z, max_x, max_y, max_z), flags, 1);
        }

        public Cell(Vec3 min, Vec3 max, MovementFlag flags, int id = -1)
        {
            InitCell(id, new AABB(min, max), flags, 1);
        }

        public Cell(AABB aabb, MovementFlag flags, float movement_cost_mult = 1, int id = -1)
        {
            InitCell(id, new AABB(aabb), flags, movement_cost_mult);
        }

        private void InitCell(int id, AABB aabb, MovementFlag flags, float movement_cost_mult)
        {
            Id = id;
            Flags = flags;
            MovementCostMult = movement_cost_mult;
            Replacement = false;
            Disabled = false;
            AABB = aabb;
            Neighbours = new List<Neighbour>();
            GlobalId = LastCellGlobalId++;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            Cell cell = obj as Cell;

            return Equals(cell);
        }

        public bool Equals(Cell cell)
        {
            if (cell == null)
                return false;

            return GlobalId.Equals(cell.GlobalId);
        }

        public override int GetHashCode()
        {
            return GlobalId.GetHashCode();
        }

        public bool Contains(Vec3 p, float z_tolerance = 0)
        {
            return AABB.Contains(p, z_tolerance);
        }

        public bool Contains2D(Vec3 p)
        {
            return AABB.Contains2D(p);
        }

        public bool Overlaps(Vec3 circle_center, float radius, bool tangential_ok = false)
        {
            return AABB.Overlaps(circle_center, radius, tangential_ok);
        }

        public bool Overlaps2D(Vec3 circle_center, float radius, bool tangential_ok = false)
        {
            return AABB.Overlaps2D(circle_center, radius, tangential_ok);
        }

        public bool Overlaps2D(Cell cell, bool tangential_ok = false)
        {
            return AABB.Overlaps2D(cell.AABB, tangential_ok);
        }

        private void AddNeighbour(Cell cell, Vec3 border_point)
        {
            Neighbours.Add(new Neighbour(cell, border_point, Flags & cell.Flags));
        }

        // Try to add given cell as neighbour. Returns true when actually added.
        public bool AddNeighbour(Cell cell)
        {
            if (cell.Equals(this))
                return false;

            AABB intersection = AABB.Intersect(cell.AABB, true);

            if (intersection != null)
            {
                if (Neighbours.Exists(x => x.cell.GlobalId == cell.GlobalId))
                    return false;

                AddNeighbour(cell, intersection.Center);
                cell.AddNeighbour(this, intersection.Center);
                
                return true;
            }

            return false;
        }

        public void Detach()
        {
            foreach (Neighbour neighbour in Neighbours)
            {
                Cell other_cell = neighbour.cell;
                for (int i = 0; i < other_cell.Neighbours.Count; ++i)
                {
                    if (other_cell.Neighbours[i].cell == this)
                    {
                        other_cell.Neighbours.RemoveAt(i);
                        break;
                    }
                }
            }

            Neighbours.Clear();
        }

        public float Distance(Cell cell)
        {
            return Center.Distance(cell.Center);
        }

        public float Distance(Vec3 p)
        {
            return AABB.Distance(p);
        }

        public float Distance2D(Vec3 p)
        {
            return AABB.Distance2D(p);
        }

        public bool HasFlags(MovementFlag flags)
        {
            return (Flags & flags) == flags;
        }

        internal int GlobalId { get; set; }

        public int Id { get; private set; }
        public MovementFlag Flags { get; private set; }
        public bool Replacement { get; set; }
        public bool Disabled { get; set; }
        public float MovementCostMult { get; set; }
        public Int64 UserData { get; set; }
        public AABB AABB { get; private set; }
        public Vec3 Center { get { return AABB.Center; } }
        public Vec3 Min { get { return AABB.Min; } }
        public Vec3 Max { get { return AABB.Max; } }

        public class Neighbour
        {
            public Neighbour(Cell cell, Vec3 border_point, MovementFlag connection_flags)
            {
                this.cell = cell;
                this.border_point = border_point;
                this.connection_flags = connection_flags;
            }

            public Cell cell;
            public Vec3 border_point;
            public MovementFlag connection_flags;
        }

        public List<Neighbour> Neighbours { get; private set; }

        //public List<Cell> Neighbours { get; private set; }
        //public List<Vec3> NeighbourBorderPoints { get; protected set; }

        public class CompareByGlobalId : IComparer<Cell>
        {
            public int Compare(Cell x, Cell y)
            {
                return x.GlobalId.CompareTo(y.GlobalId);
            }

        }

        internal virtual void Serialize(BinaryWriter w)
        {
            w.Write(GlobalId);
            w.Write(Id);
            AABB.Serialize(w);
            w.Write((int)Flags);
            w.Write(Replacement);
            w.Write(Disabled);
            w.Write(MovementCostMult);

            w.Write(Neighbours.Count);
            foreach (Neighbour neighbour in Neighbours)
            {
                w.Write(neighbour.cell.GlobalId);
                w.Write(neighbour.border_point != null);

                if (neighbour.border_point != null)
                    neighbour.border_point.Serialize(w);

                w.Write((int)neighbour.connection_flags);
            }
        }

        internal void Deserialize<T>(List<T> all_cells, BinaryReader r) where T : Cell, new()
        {
            GlobalId = r.ReadInt32();
            Id = r.ReadInt32();
            AABB.Deserialize(r);
            Flags = (MovementFlag)r.ReadInt32();
            Replacement = r.ReadBoolean();
            Disabled = r.ReadBoolean();
            MovementCostMult = r.ReadSingle();

            T temp_cell = new T();
            Cell.CompareByGlobalId comp_by_global_id = new Cell.CompareByGlobalId();

            int neighbours_num = r.ReadInt32();
            for (int i = 0; i < neighbours_num; ++i)
            {
                Neighbour neighbour = new Neighbour(null, null, MovementFlag.None);

                int neighbour_global_id = r.ReadInt32();
                temp_cell.GlobalId = neighbour_global_id;
                neighbour.cell = all_cells.ElementAt(all_cells.BinarySearch((T)temp_cell, comp_by_global_id));
                
                if (r.ReadBoolean())
                    neighbour.border_point = new Vec3(r);

                neighbour.connection_flags = (MovementFlag)r.ReadInt32();

                Neighbours.Add(neighbour);
            }
        }

        internal static int LastCellGlobalId = 0;
    }
}
