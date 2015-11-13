using Dev.D3.Core.Util;
using Enigma.D3.DataTypes;
using Enigma.D3.Helpers;
using Nav;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dev.D3.Core.Util
{
    public static class MoveTo
    {


        public static async Task<bool> MoveToPosAsync(SharpDX.Vector3 vecDest, int NearDistance = 50)
        {
            var localAcd = ActorCommonDataHelper.GetLocalAcd();
            var distance = (Math.Pow(localAcd.x0D0_WorldPosX - vecDest.X, 2) + Math.Pow(localAcd.x0D4_WorldPosY - vecDest.Y, 2));

            var minDistanceReached = distance;
            var dtDistanceReached = DateTime.Now;

            DateTime dtTimeout = DateTime.Now;
            while (distance > NearDistance)
            {
                if (DateTime.Now > dtTimeout.AddSeconds(30) || DateTime.Now > dtDistanceReached.AddSeconds(10))
                    return false;

                SharpDX.Vector2 curVector = new SharpDX.Vector2(localAcd.x0D0_WorldPosX, localAcd.x0D4_WorldPosY);
                SharpDX.Vector2 destVector = new SharpDX.Vector2(vecDest.X, vecDest.Y);

                distance = (Math.Pow(localAcd.x0D0_WorldPosX - vecDest.X, 2) + Math.Pow(localAcd.x0D4_WorldPosY - vecDest.Y, 2));
                var minExtendValue = Math.Min(10f, float.Parse(distance.ToString(), CultureInfo.InvariantCulture.NumberFormat));

                var vecNormalized = curVector.Extend(destVector, minExtendValue).To3D(); 

                System.Drawing.Point screenPoint = D3ToScreen.FromD3toScreenCoords(vecNormalized);

                MouseEvents.RightClick(screenPoint.X, screenPoint.Y);
                await Task.Delay(new Random().Next(100, 250));

                if (distance < minDistanceReached)
                {
                    minDistanceReached = distance;
                    dtDistanceReached = DateTime.Now;
                }
            }

            return true;
        }

        public static async Task<bool> MoveToPosWithNavMeshAsync(SharpDX.Vector3 vecDest, int NearDistance = 50)
        {
            if (Nav.D3.Navmesh.Current == null)
                Nav.D3.Navmesh.Create(Enigma.D3.Engine.Current, new Nav.ExploreEngine.Nearest());

            var localAcd = ActorCommonDataHelper.GetLocalAcd();
            var distance = vecDest.Distance(); // (Math.Pow(localAcd.x0D0_WorldPosX - vecDest.X, 2) + Math.Pow(localAcd.x0D4_WorldPosY - vecDest.Y, 2));

            var minDistanceReached = distance;
            var dtDistanceReached = DateTime.Now;

            DateTime dtTimeout = DateTime.Now;
            while (distance > NearDistance)
            {
                if (DateTime.Now > dtTimeout.AddSeconds(30) || DateTime.Now > dtDistanceReached.AddSeconds(10))
                    return false;

                SharpDX.Vector2 curVector = localAcd.ToSharpDXVector2(); // new SharpDX.Vector2(localAcd.x0D0_WorldPosX, localAcd.x0D4_WorldPosY);
                SharpDX.Vector2 destVector = new SharpDX.Vector2(vecDest.X, vecDest.Y);

                // Update current player position.
                Nav.D3.Navmesh.Current.Navigator.CurrentPos = localAcd.ToNavVec3(); // new Nav.Vec3(localAcd.x0D0_WorldPosX, localAcd.x0D4_WorldPosY, localAcd.x0D8_WorldPosZ);

                // Update destination. You can keep setting the same value there is internal check if destination has actually changed. 
                // This destination overrides any internal destinations (including exploration). When You just want to explore You do 
                // not need to set any destination. It will be set automatically.
                Nav.D3.Navmesh.Current.Navigator.Destination = new Nav.Vec3(vecDest.X, vecDest.Y, vecDest.Z);

                // Get current destination.
                Nav.Vec3 goToPosition = Nav.D3.Navmesh.Current.Navigator.GoToPosition;
                while (goToPosition.IsEmpty)
                    await Task.Delay(10);

                SharpDX.Vector3 goToPositionVector = new SharpDX.Vector3(goToPosition.X, goToPosition.Y, goToPosition.Z);
                await MoveToPosAsync(goToPositionVector);



            }

            return true;
        }

        public static bool MoveToPosWithNavmesh(SharpDX.Vector3 vecDest, int NearDistance = 50)
        {
            return true;
        }

    }
}
