using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using Nav;

namespace NavMeshViewer
{
    public class TestBot : NavigationObserver
    {
        public TestBot(Nav.D3.Navmesh navmesh, Vec3 pos, Vec3 dest, bool explore = false, bool simulate_stuck = false, int dest_grid_id = -1, List<Vec3> waypoints = null)
        {
            m_Navmesh = navmesh;
            m_Navmesh.Navigator.AddListener(this);
            m_Navmesh.Navigator.CurrentPos = pos;

            m_Navmesh.Navigator.Precision = 2;
            m_Navmesh.Explorator.Enabled = explore;
            m_Navmesh.Navigator.DestinationGridsId = dest_grid_id != -1 ? new List<int>(new int[]{dest_grid_id}) : null;

            if (waypoints != null)
                m_Navmesh.Navigator.Waypoints = waypoints;  

            Destination = dest;
            m_Navmesh.Navigator.EnableAntiStuck = true;
            m_GotoPosUpdateTimer.Start();

            Paused = false;
            SimulateStuck = simulate_stuck;
        }

        public static float SPEED = 60;//25; //approximated movement speed with 25% move speed bonus

        public void OnDestinationReached(DestType type, Vec3 dest)
        {
            if (dest.Equals(m_Destination))
                m_Destination = Vec3.Empty;
        }

        public void OnDestinationReachFailed(DestType type, Vec3 dest)
        {
        }
        
        public void Update(float dt)
        {
            if (m_GotoPosUpdateTimer.ElapsedMilliseconds > GOTO_POS_UPDATE_INTERVAL)
            {
                m_LastGotoPos = m_Navmesh.Navigator.GoToPosition;
                m_GotoPosUpdateTimer.Restart();
            }

            if (!m_Destination.IsEmpty)
                m_Navmesh.Navigator.Destination = m_Destination;

            if (m_Navmesh.Explorator.IsExplored() || m_LastGotoPos.IsEmpty)
                return;

            Vec3 dir = Vec3.Empty;
            float dist = 0;

            if (!Paused && !SimulateStuck && !m_LastGotoPos.Equals(m_Navmesh.Navigator.CurrentPos))
            {
                dir = m_LastGotoPos - m_Navmesh.Navigator.CurrentPos;
                dist = dir.Length();
                dir.Normalize();
            }

            m_Navmesh.Navigator.IsStandingOnPurpose = false;
            m_Navmesh.Navigator.CurrentPos = m_Navmesh.Navigator.CurrentPos + dir * Math.Min(SPEED * dt, dist);
        }

        public void Render(Graphics g, PointF trans)
        {
            RenderHelper.DrawCircle(g, Pens.Blue, trans, m_Navmesh.Navigator.CurrentPos, 4);
        }

        public bool Paused { get; set; }
        
        public bool SimulateStuck { get; set; }
        
        public bool BackTrace
        {
            get { return m_Navmesh.Navigator.BackTrackEnabled; }
            set { m_Navmesh.Navigator.BackTrackEnabled = value; }
        }

        public Vec3 Position
        {
            get { return m_Navmesh.Navigator.CurrentPos; }
        }

        public Vec3 Destination
        {
            set { m_Destination = value; m_Navmesh.Navigator.Destination = value; }
        }

        private Nav.D3.Navmesh m_Navmesh = null;
        private Vec3 m_LastGotoPos = Vec3.Empty;
        private Vec3 m_Destination = Vec3.Empty;
        private Stopwatch m_GotoPosUpdateTimer = new Stopwatch();
        private const int GOTO_POS_UPDATE_INTERVAL = 25;
    }
}
