using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.D3;
using Enigma.D3.DataTypes;
using Enigma.D3.Helpers;

namespace Dev.D3.Core.Util
{
    public static class Attack
    {

        public static async Task AttackAcdAsync(ActorCommonData acd)
        {

            var minHitsReached = acd.x188_Hitpoints;
            var dtHitReached = DateTime.Now;

            DateTime dtTimeout = DateTime.Now;
            while (acd.x188_Hitpoints > 00000.1)
            {
                if (DateTime.Now > dtTimeout.AddSeconds(30) || DateTime.Now > dtHitReached.AddSeconds(5))
                    return;

                var acdVector3 = new SharpDX.Vector3() { X = acd.x0D0_WorldPosX, Y = acd.x0D4_WorldPosY, Z = acd.x0D8_WorldPosZ };
                await MoveTo.MoveToPosAsync(acdVector3);

                var screenPos = D3ToScreen.FromD3toScreenCoords(acdVector3);

                MouseEvents.LeftClick(screenPos.X, screenPos.Y);
                await Task.Delay(new Random().Next(100, 250));

                if (acd.x188_Hitpoints < minHitsReached)
                {
                    minHitsReached = acd.x188_Hitpoints;
                    dtHitReached = DateTime.Now;
                }
            }
        }


    }
}
