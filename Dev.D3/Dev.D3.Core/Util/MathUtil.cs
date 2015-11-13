using Enigma.D3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dev.D3.Core.Util
{
    public static class MathUtil
    {

        public static double Distance(this ActorCommonData acd)
        {
            if (Engine.Current == null)
                return 0;

            return (Math.Pow(Engine.Current.LocalData.x24_WorldPosX - acd.x0D0_WorldPosX, 2) + Math.Pow(Engine.Current.LocalData.x28_WorldPosY - acd.x0D4_WorldPosY, 2));
        }

        public static double Distance(this SharpDX.Vector3 vec3)
        {
            if (Engine.Current == null)
                return 0;

            return (Math.Pow(Engine.Current.LocalData.x24_WorldPosX - vec3.X, 2) + Math.Pow(Engine.Current.LocalData.x28_WorldPosY - vec3.Y, 2));
        }

        public static SharpDX.Vector3 ToSharpDXVector3(this ActorCommonData acd)
        {
            return new SharpDX.Vector3(acd.x0D0_WorldPosX, acd.x0D4_WorldPosY, acd.x0D8_WorldPosZ);
        }

        public static SharpDX.Vector2 ToSharpDXVector2(this ActorCommonData acd)
        {
            return new SharpDX.Vector2(acd.x0D0_WorldPosX, acd.x0D4_WorldPosY);
        }

        public static Nav.Vec3 ToNavVec3(this ActorCommonData acd)
        {
            return new Nav.Vec3(acd.x0D0_WorldPosX, acd.x0D4_WorldPosY, acd.x0D8_WorldPosZ);
        }



    }
}
