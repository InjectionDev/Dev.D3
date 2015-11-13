using Enigma.D3.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dev.D3.Core.Util
{
    public static class Geometry
    {

        public static SharpDX.Vector2 Extend(this SharpDX.Vector2 v, SharpDX.Vector2 to, float distance)
        {
            return v + distance * (to - v).Normalized();
        }

        public static SharpDX.Vector3 Extend(this SharpDX.Vector3 v, SharpDX.Vector3 to, float distance)
        {
            return v + distance * (to - v).Normalized();
        }

        public static SharpDX.Vector2 Normalized(this SharpDX.Vector2 v)
        {
            v.Normalize();
            return v;
        }

        public static SharpDX.Vector3 Normalized(this SharpDX.Vector3 v)
        {
            v.Normalize();
            return v;
        }

        public static SharpDX.Vector3 To3D(this SharpDX.Vector2 v)
        {
            return new SharpDX.Vector3(v.X, v.Y, ActorCommonDataHelper.GetLocalAcd().x0D8_WorldPosZ);
        }


    }
}
