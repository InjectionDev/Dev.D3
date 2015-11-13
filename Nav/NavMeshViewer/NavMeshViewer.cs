using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using Enigma.D3;
using Enigma.D3.Memory;
using Enigma.D3.Helpers;
using Enigma.D3.Collections;
using Enigma.D3.Enums;
using Nav;
using Nav.D3;

namespace NavMeshViewer
{
    public partial class NavMeshViewer : Form
    {
        public NavMeshViewer(string[] args)
        {
            InitializeComponent();

            engine = Engine.Create();

            BackColor = Color.LightGray;
            
            m_Navmesh = Nav.D3.Navmesh.Create(engine, new Nav.ExploreEngine.Nearest(), true);
            m_Navmesh.RegionsMoveCostMode = Nav.Navmesh.RegionsMode.Mult;
            //m_Navmesh = Nav.D3.Navmesh.Create(engine, null, false);
            //m_Navmesh.Navigator.UpdatePathInterval = 500;
            //m_Navmesh.Navigator.MovementFlags = MovementFlag.Fly;
            //m_Navmesh.Navigator.PathNodesShiftDist = 0;
            
            //m_Navmesh.AllowedAreasSnoId = new List<int>() { 19837 };

            LoadDebugConfig();

            if (engine == null)
            {
                if (args.Length == 1)
                {
                    string[] files = args[0].Split('#');

                    foreach (string file in files)
                        LoadData(file, files.Length == 1);
                }

                m_Navmesh.Explorator.Enabled = false;
            }
            else
            {
                m_Navmesh.Explorator.Enabled = false;
                auto_clear_navmesh = true;
            }
        }

        private void LoadDebugConfig()
        {
            Ini.IniFile debug_ini = new Ini.IniFile("./debug.ini");

            string[] allowed_areas_sno_id_str = debug_ini.IniReadValue("Navmesh", "allowed_areas_sno_id").Split(',');
            List<int> allowed_areas_sno_id = new List<int>();
            foreach (string id in allowed_areas_sno_id_str)
            {
                if (id.Length > 0)
                    allowed_areas_sno_id.Add(int.Parse(id));
            }
            m_Navmesh.AllowedAreasSnoId = allowed_areas_sno_id;

            string[] allowed_grid_cells_id_str = debug_ini.IniReadValue("Navmesh", "allowed_grid_cells_id").Split(',');
            List<int> allowed_grid_cells_id = new List<int>();
            foreach (string id in allowed_grid_cells_id_str)
            {
                if (id.Length > 0)
                    allowed_grid_cells_id.Add(int.Parse(id));
            }
            m_Navmesh.AllowedGridCellsId = allowed_grid_cells_id;

            m_Navmesh.Navigator.UpdatePathInterval = int.Parse(debug_ini.IniReadValue("Navigator", "update_path_interval"));
            m_Navmesh.Navigator.MovementFlags = (MovementFlag)Enum.Parse(typeof(MovementFlag), debug_ini.IniReadValue("Navigator", "movement_flags"));
            m_Navmesh.Navigator.PathNodesShiftDist = float.Parse(debug_ini.IniReadValue("Navigator", "path_nodes_shift_dist"));
            
        }

        private void OnLoad(object sender, EventArgs e)
        {
            DoubleBuffered = true;
            this.Paint += new PaintEventHandler(Render);
        }

        private void OnClosing(object sender, FormClosingEventArgs e)
        {
            m_Navmesh.Dispose();
        }

        private float m_LastMaxMoveCostMult = 1;

        private void Render(object sender, PaintEventArgs e)
        {
            try
            {
                int location = -1;

                if (engine != null)
                {
                    LevelArea level_area = Engine.Current.LevelArea;
                    if (level_area != null)
                        location = level_area.x044_SnoId;
                }

                if (last_location != location)
                {
                    if (auto_clear_navmesh)
                    {
                        m_Navmesh.Clear();
                        LoadDebugConfig();
                    }
                    last_location = location;
                }

                Matrix m = new Matrix();
                m.Scale(render_scale, render_scale);
                m.Translate((Width - 16) / (2 * render_scale), (Height - 30) / (2 * render_scale));

                // when Diablo is running display navmesh in the same manner as Diablo does
                if (engine != null)
                {
                    m.Rotate(135);
                    Matrix flip_x_m = new Matrix(1, 0, 0, -1, 0, 0);
                    m.Multiply(flip_x_m);
                }

                e.Graphics.Transform = m;
                e.Graphics.CompositingQuality = CompositingQuality.GammaCorrected;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                int cells_count = 0;
                int grid_cells_count = 0;

                if (render_grids || render_cells)
                {
                    using (m_Navmesh.AquireReadDataLock())
                    {
                        List<GridCell> grid_cells = m_Navmesh.dbg_GetGridCells();

                        if (render_grids)
                        {
                            foreach (Nav.GridCell grid_cell in grid_cells)
                                RenderHelper.Render(grid_cell, render_center, e, render_connections, render_id);

                            grid_cells_count = grid_cells.Count;
                        }

                        if (render_cells)
                        {
                            float max_move_cost_mult = 1;

                            foreach (Nav.GridCell grid_cell in grid_cells)
                            {
                                foreach (Nav.Cell cell in grid_cell.Cells)
                                {
                                    RenderHelper.Render(cell, render_center, e, render_connections, render_id, m_LastMaxMoveCostMult);
                                    max_move_cost_mult = Math.Max(max_move_cost_mult, cell.MovementCostMult);
                                }

                                cells_count += grid_cell.Cells.Count;
                            }

                            m_LastMaxMoveCostMult = max_move_cost_mult;
                        }
                    }
                }

                if (render_explore_cells || render_explore_dist)
                {
                    using (m_Navmesh.Explorator.AquireReadDataLock())
                    {
                        List<ExploreCell> explore_cells = m_Navmesh.Explorator.dbg_GetExploreCells();

                        if (render_explore_cells)
                        {
                            foreach (Nav.ExploreCell explore_cell in explore_cells)
                                RenderHelper.Render(explore_cell, m_Navmesh.Navigator.ExploreCellPrecision, render_center, e, render_connections, render_id);
                        }

                        if (render_explore_dist)
                        {
                            if (explore_cells.Exists(c => c.Id == explore_cell_id_to_render_dists))
                                RenderHelper.Render(m_Navmesh, explore_cells.Find(c => c.Id == explore_cell_id_to_render_dists), render_center, e, render_id);
                        }
                    }
                }

                if (render_regions)
                {
                    var regions = m_Navmesh.Regions;

                    foreach (var region in regions)
                        RenderHelper.DrawRectangle(e.Graphics, Pens.Black, render_center, region.area.Min, region.area.Max);

                    //Vec3 safe_point = m_Navmesh.Navigator.GetNearestGridCellOutsideAvoidAreas();

                    //if (!safe_point.IsEmpty)
                    //    RenderHelper.DrawPoint(e.Graphics, Pens.Green, render_center, safe_point);
                }

                if (render_axis)
                {
                    e.Graphics.DrawString("X", new Font("Arial", 6 / render_scale), Brushes.Black, 25 / render_scale, 0);
                    e.Graphics.DrawLine(RenderHelper.AXIS_PEN, -25 / render_scale, 0, 25 / render_scale, 0);
                    e.Graphics.DrawString("Y", new Font("Arial", 6 / render_scale), Brushes.Black, 0, 25 / render_scale);
                    e.Graphics.DrawLine(RenderHelper.AXIS_PEN, 0, -25 / render_scale, 0, 25 / render_scale);
                }

                if (render_explore_cells && m_Navmesh.Explorator is Nav.ExploreEngine.TSP)
                {
                    ((Nav.ExploreEngine.TSP)m_Navmesh.Explorator).TryGetExplorePath(ref last_explore_path);
                    RenderHelper.DrawLines(e.Graphics, RenderHelper.EXPLORE_PATH_PEN, render_center, last_explore_path, 1);
                }

                if (!render_original_path && render_path)
                {
                    DestType last_path_dest_type = DestType.None;
                    if (m_Navmesh.Navigator.TryGetPath(ref last_path, ref last_path_dest_type))
                        last_path.Insert(0, m_Navmesh.Navigator.CurrentPos);
                    RenderHelper.DrawLines(e.Graphics, RenderHelper.PATH_PEN, render_center, last_path, 1);
                }

                if (render_backtrack_path)
                {
                    if (m_Navmesh.Navigator.TryGetBackTrackPath(ref last_back_track_path))
                        last_back_track_path.Insert(0, m_Navmesh.Navigator.CurrentPos);
                    RenderHelper.DrawLines(e.Graphics, Pens.Blue, render_center, last_back_track_path, 1);
                }

                if (render_positions_history)
                {
                    m_Navmesh.Navigator.TryGetDebugPositionsHistory(ref last_positions_history);
                    RenderHelper.DrawLines(e.Graphics, Pens.Green, render_center, last_positions_history, 1);
                }

                if (!m_Navmesh.Navigator.CurrentPos.IsEmpty)
                    RenderHelper.DrawPoint(e.Graphics, Pens.Blue, render_center, m_Navmesh.Navigator.CurrentPos);
                if (!m_Navmesh.Navigator.Destination.IsEmpty)
                    RenderHelper.DrawPoint(e.Graphics, Pens.LightBlue, render_center, m_Navmesh.Navigator.Destination);

                {
                    Vec3 curr = m_Navmesh.Navigator.CurrentPos;
                    Vec3 dest = m_Navmesh.Navigator.Destination;

                    if (!curr.IsEmpty && !dest.IsEmpty)
                    {
                        if (render_original_path)
                        {
                            List<Vec3> path = new List<Vec3>();
                            m_Navmesh.Navigator.FindPath(curr, dest, MovementFlag.Walk, ref path, -1, false, false, 0, false, 0, false);
                            path.Insert(0, curr);
                            RenderHelper.DrawLines(e.Graphics, Pens.Black, render_center, path, 1);
                        }

                        if (render_ray_cast)
                            RenderHelper.DrawLine(e.Graphics, m_Navmesh.RayCast2D(curr, dest, MovementFlag.Walk) ? Pens.Green : Pens.Red, render_center, curr, dest);
                    }
                }

                if (waypoints_paths.Count > 0)
                {
                    int waypoint_id = 1;
                    foreach (List<Vec3> p in waypoints_paths)
                    {
                        if (p.Count > 0)
                        {
                            RenderHelper.DrawCircle(e.Graphics, Pens.Black, render_center, p[0], 3);
                            RenderHelper.DrawString(e.Graphics, Brushes.Black, render_center, p[0], waypoint_id.ToString(), 10);
                        }
                        RenderHelper.DrawLines(e.Graphics, Pens.Red, render_center, p, 1);
                        ++waypoint_id;
                    }
                }

                if (bot != null)
                {
                    if (!bot.Paused && center_on_bot)
                        render_center = new PointF(bot.Position.X, bot.Position.Y);
                    bot.Render(e.Graphics, render_center);
                }

                e.Graphics.ResetTransform();

                Font legend_font = new Font("Arial", 8, FontStyle.Bold);
                Font stats_font = new Font("Arial", 8);

                TextRenderer.DrawText(e.Graphics, "L: Toggle render legend", legend_font, new Point(10, 10), render_legend ? Color.White : Color.Black, render_legend ? Color.Black : Color.Transparent);

                if (render_legend)
                {
                    e.Graphics.DrawString("F1: Reload waypoints", legend_font, Brushes.Black, 10, 25);
                    e.Graphics.DrawString("F2: Reload nav data", legend_font, Brushes.Black, 10, 40);
                    e.Graphics.DrawString("F3: Dump nav data", legend_font, Brushes.Black, 10, 55);
                    e.Graphics.DrawString("F4: Clear nav data", legend_font, Brushes.Black, 10, 70);
                    e.Graphics.DrawString("F5: Serialize nav data", legend_font, Brushes.Black, 10, 85);
                    e.Graphics.DrawString("F6: Deserialize nav data", legend_font, Brushes.Black, 10, 100);
                    e.Graphics.DrawString("F10: Activate some test", legend_font, Brushes.Black, 10, 115);
                    TextRenderer.DrawText(e.Graphics, "1: Toggle render grid cells", legend_font, new Point(10, 130), render_grids ? Color.White : Color.Black, render_grids ? Color.Black : Color.Transparent);                    
                    TextRenderer.DrawText(e.Graphics, "2: Toggle render cells", legend_font, new Point(10, 145), render_cells ? Color.White : Color.Black, render_cells ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "3: Toggle render explore cells", legend_font, new Point(10, 160), render_explore_cells ? Color.White : Color.Black, render_explore_cells ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "4: Toggle render connections", legend_font, new Point(10, 175), render_connections ? Color.White : Color.Black, render_connections ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "5: Toggle render IDs", legend_font, new Point(10, 190), render_id ? Color.White : Color.Black, render_id ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "6: Toggle render axis", legend_font, new Point(10, 205), render_axis ? Color.White : Color.Black, render_axis ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "7: Toggle render regions", legend_font, new Point(10, 220), render_regions ? Color.White : Color.Black, render_regions ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "8: Toggle render original path", legend_font, new Point(10, 235), render_original_path ? Color.White : Color.Black, render_original_path ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "9: Toggle render ray cast", legend_font, new Point(10, 250), render_ray_cast ? Color.White : Color.Black, render_ray_cast ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "0: Toggle render back track path", legend_font, new Point(10, 265), render_backtrack_path ? Color.White : Color.Black, render_backtrack_path ? Color.Black : Color.Transparent);
                    e.Graphics.DrawString("S: Set current pos", legend_font, Brushes.Black, 10, 280);
                    e.Graphics.DrawString("E: Set destination pos", legend_font, Brushes.Black, 10, 295);
                    e.Graphics.DrawString("B: Run bot", legend_font, Brushes.Black, 10, 310);
                    TextRenderer.DrawText(e.Graphics, "A: Toggle auto clear navmesh", legend_font, new Point(10, 325), auto_clear_navmesh ? Color.White : Color.Black, auto_clear_navmesh ? Color.Black : Color.Transparent);
                    e.Graphics.DrawString("F7: Reload debug.ini", legend_font, Brushes.Black, 10, 340);
                    TextRenderer.DrawText(e.Graphics, "Ctrl+1: Toggle render path", legend_font, new Point(10, 355), render_path ? Color.White : Color.Black, render_path ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "Ctrl+2: Toggle regions", legend_font, new Point(10, 370), m_Navmesh.RegionsEnabled ? Color.White : Color.Black, m_Navmesh.RegionsEnabled ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "Ctrl+3: Toggle danger regions", legend_font, new Point(10, 385), m_Navmesh.DangerRegionsEnabled ? Color.White : Color.Black, m_Navmesh.DangerRegionsEnabled ? Color.Black : Color.Transparent);
                    TextRenderer.DrawText(e.Graphics, "Ctrl+4: Toggle render positions history", legend_font, new Point(10, 400), render_positions_history ? Color.White : Color.Black, render_positions_history ? Color.Black : Color.Transparent);
                }

                e.Graphics.DrawString("Cells count: " + cells_count, stats_font, Brushes.Black, 10, Height - 55);
            }
            catch (Exception)
            {
            }
        }

        private void LoadWaypoints(string filename)
        {
            last_waypoints_file = filename;
            waypoints_paths.Clear();

            if (!File.Exists(filename))
                return;

            using (var reader = File.OpenText(filename))
            {
                string line;
                Vec3 last_wp = Vec3.Empty;

                while ((line = reader.ReadLine()) != null)
                {
                    String[] coords = line.Split(';');

                    if (coords.Length >= 3)
                    {
                        Vec3 wp = new Vec3(float.Parse(coords[0], CultureInfo.InvariantCulture),
                                           float.Parse(coords[1], CultureInfo.InvariantCulture),
                                           float.Parse(coords[2], CultureInfo.InvariantCulture));
                        
                        if (!last_wp.IsEmpty)
                        {
                            List<Vec3> path = new List<Vec3>();
                            m_Navmesh.Navigator.FindPath(last_wp, wp, MovementFlag.Walk, ref path, -1, true, true);
                            waypoints_paths.Add(path);
                        }

                        last_wp = wp;
                    }
                }
            }
        }

        private void LoadData(string filename, bool clear = true)
        {
            last_data_file = filename;

            if (m_Navmesh.Load(filename, clear))
            {
                Vec3 initial_pos = m_Navmesh.Navigator.CurrentPos;
                if (initial_pos.IsEmpty)
                    initial_pos = m_Navmesh.GetCenter();

                render_center.X = initial_pos.X;
                render_center.Y = initial_pos.Y;
            }
        }

        private void dbg_ContiniousSerialize()
        {
            while (true)
            {
                m_Navmesh.Serialize("test.dat");
                Thread.Sleep(500);
            }
        }

        private void dbg_MoveRegions()
        {
            Random rng = new Random();
            HashSet<region_data> regions = new HashSet<region_data>();

            for (int i = 0; i < 80; ++i)
            {
                Vec3 pos = m_Navmesh.GetRandomPos();
                float size = 20 + (float)rng.NextDouble() * 10;
                regions.Add(new region_data(new AABB(pos - new Vec3(size * 0.5f, size * 0.5f, 0), pos + new Vec3(size * 0.5f, size * 0.5f, 0)), 2));
            }

            const int dt = 100;

            while (true)
            {

                foreach (var region in regions)
                {
                    Vec3 dir = new Vec3((float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble() * 2 - 1, 0);

                    region.area.Translate(dir * 30 * ((float)dt / 1000));
                }

                m_Navmesh.Regions = regions;

                Thread.Sleep(dt);
            }
        }

        private void refresh_timer_Tick(object sender, EventArgs e)
        {
            if (bot != null)
                bot.Update(refresh_timer.Interval * 0.001f);

            if (m_Navmesh.IsUpdating)
            {
                Actor local_actor = ActorHelper.GetLocalActor();

                if (local_actor == null)
                    return;

                render_center.X = local_actor.x0A8_WorldPosX;
                render_center.Y = local_actor.x0AC_WorldPosY;

                m_Navmesh.Navigator.CurrentPos = new Vec3(local_actor.x0A8_WorldPosX, local_actor.x0AC_WorldPosY, local_actor.x0B0_WorldPosZ);
            }

            Refresh();
        }

        private void NavMeshViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Control.MouseButtons.HasFlag(MouseButtons.Left))
                return;

            if (!last_drag_mouse_pos.IsEmpty)
            {
                render_center.X += last_drag_mouse_pos.X - e.X;
                render_center.Y += last_drag_mouse_pos.Y - e.Y;
            }

            last_drag_mouse_pos = new PointF(e.X, e.Y);
        }

        private void NavMeshViewer_MouseUp(object sender, MouseEventArgs e)
        {
            last_drag_mouse_pos = PointF.Empty;
        }

        private void NavMeshViewer_MouseWheel(object sender, MouseEventArgs e)
        {
            render_scale += e.Delta * 0.002f;

            render_scale = Math.Max(0.01f, Math.Min(100.0f, render_scale));
        }

        private void NavMeshViewer_KeyPress(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                if (e.KeyCode == System.Windows.Forms.Keys.D1)
                {
                    render_path = !render_path;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D2)
                {
                    m_Navmesh.RegionsEnabled = !m_Navmesh.RegionsEnabled;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D3)
                {
                    m_Navmesh.DangerRegionsEnabled = !m_Navmesh.DangerRegionsEnabled;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D4)
                {
                    render_positions_history = !render_positions_history;
                    e.Handled = true;
                }
            }
            else
            {
                if (e.KeyCode == System.Windows.Forms.Keys.A)
                {
                    auto_clear_navmesh = !auto_clear_navmesh;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.S)
                {
                    Vec3 result = null;
                    m_Navmesh.RayTrace(new Vec3(render_center.X, render_center.Y, 1000),
                                       new Vec3(render_center.X, render_center.Y, -1000),
                                       MovementFlag.Walk,
                                       out result);

                    if (result.IsEmpty)
                        result = new Vec3(render_center.X, render_center.Y, 0);

                    m_Navmesh.Navigator.CurrentPos = result;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.E)
                {
                    Vec3 result = null;
                    m_Navmesh.RayTrace(new Vec3(render_center.X, render_center.Y, 1000),
                                       new Vec3(render_center.X, render_center.Y, -1000),
                                       MovementFlag.Walk,
                                       out result);

                    if (result.IsEmpty)
                        result = new Vec3(render_center.X, render_center.Y, 0);

                    m_Navmesh.Navigator.Destination = result;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.L)
                {
                    render_legend = !render_legend;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D1)
                {
                    render_grids = !render_grids;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D2)
                {
                    render_cells = !render_cells;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D3)
                {
                    render_explore_cells = !render_explore_cells;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D4)
                {
                    render_connections = !render_connections;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D5)
                {
                    render_id = !render_id;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D6)
                {
                    render_axis = !render_axis;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D7)
                {
                    //render_explore_dist = !render_explore_dist;
                    render_regions = !render_regions;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D8)
                {
                    render_original_path = !render_original_path;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D9)
                {
                    render_ray_cast = !render_ray_cast;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D0)
                {
                    render_backtrack_path = !render_backtrack_path;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.F1)
                {
                    LoadWaypoints(last_waypoints_file);
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.F2)
                {
                    LoadData(last_data_file);
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.F3)
                {
                    m_Navmesh.Dump("nav_dump.txt");
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.F4)
                {
                    m_Navmesh.Clear();
                    LoadDebugConfig();
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.F5)
                {
                    m_Navmesh.Serialize("nav_save.dat");
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.F6)
                {
                    m_Navmesh.Deserialize("nav_save.dat");

                    Vec3 initial_pos = m_Navmesh.Navigator.CurrentPos;
                    if (initial_pos.IsEmpty)
                        initial_pos = m_Navmesh.GetCenter();
                    render_center.X = initial_pos.X;
                    render_center.Y = initial_pos.Y;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.F7)
                {
                    LoadDebugConfig();
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.F10)
                {
                    //Thread t = new Thread(dbg_ContiniousSerialize);
                    //t.Start();

                    Thread t = new Thread(dbg_MoveRegions);
                    t.Start();

                    //m_Navmesh.dbg_GenerateRandomAvoidAreas();

                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.B)
                {
                    Vec3 result = null;
                    m_Navmesh.RayTrace(new Vec3(render_center.X, render_center.Y, 1000),
                                       new Vec3(render_center.X, render_center.Y, -1000),
                                       MovementFlag.Walk,
                                       out result);

                    bot = new TestBot(m_Navmesh, result, m_Navmesh.Navigator.Destination, true, false);
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.C)
                {
                    center_on_bot = !center_on_bot;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.D)
                {
                    if (bot != null)
                        bot.Destination = new Vec3(render_center.X, render_center.Y, 0);
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.H)
                {
                    if (m_Navmesh.Explorator != null)
                        m_Navmesh.Explorator.HintPos = new Vec3(render_center.X, render_center.Y, 0);
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.X)
                {
                    m_Navmesh.Navigator.CurrentPos = new Vec3(render_center.X, render_center.Y, 0);
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.Space)
                {
                    if (bot != null)
                        bot.Paused = !bot.Paused;
                    e.Handled = true;
                }
                else if (e.KeyCode == System.Windows.Forms.Keys.V)
                {
                    if (bot != null)
                        bot.BackTrace = !bot.BackTrace;
                    e.Handled = true;
                }
            }
        }

        private Nav.D3.Navmesh m_Navmesh = null;
        private Enigma.D3.Engine engine = null;
        private int last_location = -1;
        private bool auto_clear_navmesh = false;
        private PointF render_center = new PointF(200, 350);
        private float render_scale = 1.5f;//0.75f;
        private bool render_id = false;
        private bool render_axis = true;
        private bool render_connections = false;
        private bool render_original_path = false;
        private bool render_backtrack_path = false;
        private bool render_positions_history = false;
        private bool render_ray_cast = false;
        private bool render_explore_cells = false;
        private bool render_regions = false;
        private bool render_explore_dist = false;
        private bool render_cells = true;
        private bool render_legend = true;
        private bool render_grids = false;
        private bool render_path = true;
        private bool center_on_bot = true;
        private PointF last_drag_mouse_pos = PointF.Empty;
        private TestBot bot = null;
        private string last_waypoints_file;
        private string last_data_file;
        private int explore_cell_id_to_render_dists = -1;
        private List<List<Vec3>> waypoints_paths = new List<List<Vec3>>();
        private List<Vec3> last_path = new List<Vec3>();
        private List<Vec3> last_back_track_path = new List<Vec3>();
        private List<Vec3> last_positions_history = new List<Vec3>();
        private List<Vec3> last_explore_path = new List<Vec3>();
    }

    class RenderHelper
    {
        private static float GetProportional(float value, float min, float max, float new_min, float new_max)
        {
            if (min == max)
                return new_max;

            float value_progress = (value - min) / (max - min);
            return new_min + (new_max - new_min) * value_progress;
        }

        public static void Render(Nav.Cell cell, PointF trans, PaintEventArgs e, bool draw_connections, bool draw_id, float max_move_cost_mult)
        {
            if (cell.Disabled)
                return;

            DrawRectangle(e.Graphics, cell.Replacement ? REPLACEMENT_CELL_BORDER_PEN : CELL_BORDER_PEN, trans, cell.Min, cell.Max);

            Color cell_color = Color.White;
            int move_cost_level = 255;

            if (cell.MovementCostMult > 1)
            {
                move_cost_level = 255 - (int)Math.Min(GetProportional(cell.MovementCostMult, 1, 100, 20, 255), 255);
                cell_color = Color.FromArgb(255, 255, move_cost_level, move_cost_level);
            }
            else if (cell.MovementCostMult < 1)
            {
                move_cost_level = 255 - (int)Math.Min(GetProportional(cell.MovementCostMult, 0, 1, 20, 255), 255);
                cell_color = Color.FromArgb(255, move_cost_level, 255, move_cost_level);
            }

            FillRectangle(e.Graphics, cell.Flags == MovementFlag.Fly ? Brushes.Gray : new SolidBrush(cell_color), trans, cell.Min, cell.Max);
            
            if (draw_connections)
            {
                foreach (Nav.Cell.Neighbour neighbour in cell.Neighbours)
                    DrawLine(e.Graphics, CELL_CONNECTION_PEN, trans, cell.Center, neighbour.border_point);
            }

            if (draw_id)
                DrawString(e.Graphics, Brushes.Black, trans, cell.Min, cell.Id.ToString(), 2);
        }

        public static void Render(Nav.GridCell cell, PointF trans, PaintEventArgs e, bool draw_connections, bool draw_id)
        {
            DrawRectangle(e.Graphics, Pens.Black, trans, cell.Min, cell.Max);

            if (draw_connections)
                foreach (Nav.Cell.Neighbour neighbour in cell.Neighbours)
                    DrawLine(e.Graphics, GRID_CELL_CONNECTION_PEN, trans, cell.Center, neighbour.cell.Center);

            if (draw_id)
                DrawString(e.Graphics, Brushes.Black, trans, cell.Min, cell.Id.ToString(), 15);
        }

        public static void Render(Nav.ExploreCell cell, float radius, PointF trans, PaintEventArgs e, bool draw_connections, bool draw_id)
        {
            DrawRectangle(e.Graphics, Pens.Magenta, trans, cell.Min, cell.Max);

            //DrawString(e.Graphics, Brushes.Black, trans, cell.Position, Math.Round(cell.CellsArea()).ToString(), 14);
            
            if (cell.Explored)
            {
                //DrawLine(e.Graphics, explored_pen, trans, cell.Min, cell.Max);
                //DrawLine(e.Graphics, explored_pen, trans, new Vec3(cell.Min.X, cell.Max.Y), new Vec3(cell.Max.X, cell.Min.Y));
                FillRectangle(e.Graphics, explored_brush, trans, cell.Min, cell.Max);
            }
            else
            {
                //DrawCircle(e.Graphics, Pens.Red, trans, cell.Position, radius);
                //DrawString(e.Graphics, Brushes.Black, trans, cell.Position, cell.UserData.ToString(), 10);

                if (draw_connections)
                    foreach (Nav.Cell.Neighbour neighbour in cell.Neighbours)
                    {
                        ExploreCell neighbour_cell = (ExploreCell)neighbour.cell;

                        DrawLine(e.Graphics, EXPLORE_CELL_CONNECTION_PEN, trans, cell.Position, neighbour_cell.Position);
                    }

                if (draw_id)
                    DrawString(e.Graphics, Brushes.Black, trans, cell.Position, cell.Id.ToString(), 10);
            }
        }

        public static void Render(Nav.Navmesh navmesh, Nav.ExploreCell cell, PointF trans, PaintEventArgs e, bool draw_id)
        {
            List<Nav.ExploreCell> all_cells = navmesh.Explorator.dbg_GetExploreCells();

            foreach (ExploreCell other_cell in all_cells)
            {
                if (cell.Id == other_cell.Id)
                    continue;

                DrawLine(e.Graphics, Pens.Gray, trans, cell.Position, other_cell.Position);
                DrawString(e.Graphics, Brushes.Black, trans, (cell.Position + other_cell.Position) * 0.5f, Math.Round(navmesh.Explorator.ExploreDistance(cell, other_cell)).ToString(), 8);
            }
        }

        public static void DrawString(Graphics g, Brush b, PointF trans, Vec3 pos, string text, int font_size)
        {
            g.DrawString(text, new Font("Arial", font_size), b, pos.X - trans.X, pos.Y - trans.Y);
        }

        public static void DrawRectangle(Graphics g, Pen p, PointF trans, Vec3 min, Vec3 max)
        {
            g.DrawRectangle(p, min.X - trans.X, min.Y - trans.Y, max.X - min.X, max.Y - min.Y);
        }

        public static void FillRectangle(Graphics g, Brush b, PointF trans, Vec3 min, Vec3 max)
        {
            g.FillRectangle(b, min.X - trans.X, min.Y - trans.Y, max.X - min.X, max.Y - min.Y);
        }

        public static void DrawLine(Graphics g, Pen p, PointF trans, Vec3 start, Vec3 end)
        {
            g.DrawLine(p, start.X - trans.X, start.Y - trans.Y, end.X - trans.X, end.Y - trans.Y);
        }

        public static void DrawPoint(Graphics g, Pen p, PointF trans, Vec3 pos)
        {
            DrawCircle(g, p, trans, pos, 1);
        }

        public static void DrawCircle(Graphics g, Pen p, PointF trans, Vec3 pos, float radius)
        {
            g.DrawEllipse(p, pos.X - trans.X - radius, pos.Y - trans.Y - radius, 2 * radius, 2 * radius);
        }

        public static void DrawLines(Graphics g, Pen p, PointF trans, List<Vec3> points, float point_radius)
        {
            if (points.Count < 2)
                return;

            if (point_radius > 0)
                DrawCircle(g, Pens.Black, trans, points[0], point_radius);

            for (int i = 1; i < points.Count; ++i)
            {
                DrawLine(g, p, trans, points[i - 1], points[i]);

                if (point_radius > 0)
                    DrawCircle(g, Pens.Black, trans, points[i], point_radius);
            }
        }

        private static Pen EXPLORE_CELL_CONNECTION_PEN = new Pen(Color.FromArgb(255, 50, 50, 50), 3f);
        private static Pen GRID_CELL_CONNECTION_PEN = new Pen(Color.FromArgb(255, 50, 50, 50), 4);
        private static Pen CELL_CONNECTION_PEN = new Pen(Color.Black, 0.3f);
        private static Pen CELL_BORDER_PEN = new Pen(Color.Blue, 0.3f);
        private static Pen REPLACEMENT_CELL_BORDER_PEN = new Pen(Color.LightGray, 0.3f);
        public static readonly Pen AXIS_PEN = new Pen(Color.SaddleBrown, 0.3f);
        public static readonly Pen EXPLORE_PATH_PEN = new Pen(Color.Black, 5);
        public static readonly Pen PATH_PEN = new Pen(Color.Black, 1.5f);
        private static Brush explored_brush = new SolidBrush(Color.FromArgb(128, 50, 50, 50));
    }
}
