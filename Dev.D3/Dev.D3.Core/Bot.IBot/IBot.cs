using Enigma.D3;
using Nav;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dev.D3.Core.Bot.IBot
{
    public class IBot
    {
        public bool isRunning { get; set; }

        public ActorCommonData CurrentTarget { get; set; }
        public bool isAttacking { get; set; }
        public bool isMoving { get; set; }


        public virtual void Attack(ActorCommonData acd)
        {
            
        }



        public bool isInAction
        {
            get
            {
                if (!isMoving && !Util.Targeting.HasValidTarget(CurrentTarget))
                {
                    isAttacking = false;
                    return false;
                }

                if (isMoving)
                {
                    //Vec3 pos = Data.GetCurrentPos();

                    //                 float xd = _vMovingTo.x - pos.x;
                    //                 float yd = _vMovingTo.y - pos.y;
                    //                 float zd = _vMovingTo.z - pos.z;
                    //                 double distance = Math.Sqrt(pos.x * pos.x + pos.y * pos.y + pos.z * pos.z);
                    // 
                    //                 if (distance < 2)
                    //                 {
                    //                     _bMoving = false;
                    //                     return false;
                    //                 }

                    return true;
                }
                else if (isAttacking)
                {
                    if (!Util.Targeting.HasValidTarget(CurrentTarget))
                    {
                        isAttacking = false;
                        return false;
                    }

                    //Vec3 pos = Data.GetCurrentPos();
                    //double distance = Math.Sqrt(pos.x * pos.x + pos.y * pos.y + pos.z * pos.z);
                    //if (distance < 2 || Globals.mem.ReadMemoryAsFloat(Offsets.clickToMoveToggle) == 0)
                    //{
                    //    owner.isAttacking = false;
                    //    return false;
                    //}

                    return true;
                }

                return false;
            }
        }

    }
}
