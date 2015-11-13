using Dev.D3.Core.Hook;
using Enigma.D3;
using Enigma.D3.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dev.D3.Core.Util
{
    public static class Targeting
    {

        public static ActorCommonData GetTarget()
        {
            ActorCommonData acdTarget = null;

            var queryMonster = ActorCommonDataHelper.EnumerateMonsters().Where(x => x.x0D0_WorldPosX > 0 && x.x188_Hitpoints > 00001 && x.x190_TeamId == 10 && x.x184_ActorType == Enigma.D3.Enums.ActorType.Monster && !x.x004_Name.Contains("sandWasp"));

            var queryMonsterQuality = queryMonster
                .Where(x => x.x0B8_MonsterQuality > Enigma.D3.Enums.MonsterQuality.Normal)
                .OrderBy(x => x.x0B8_MonsterQuality);
            if (queryMonsterQuality.Any())
            {
                var acd = queryMonsterQuality.First();
                WindowHook.SetD3WindowText(string.Format("Target {0} x098_MonsterSnoId:{1}", acd.x004_Name, acd.x098_MonsterSnoId));
                return acd;
            }
                

            var queryMonsterLeft = queryMonster
                .OrderBy(x => x.x188_Hitpoints);
            if (queryMonsterLeft.Any())
            {
                var acd = queryMonsterLeft.First();
                WindowHook.SetD3WindowText(string.Format("Target {0} x098_MonsterSnoId:{1}", acd.x004_Name, acd.x184_ActorType == Enigma.D3.Enums.ActorType.Monster));
                return acd;
            }

            WindowHook.SetD3WindowText("Target NULL");
            return acdTarget;
        }


        public static bool HasValidTarget(ActorCommonData acd)
        {
            if (acd == null || acd.x188_Hitpoints < 00001)
                return false;

            return true;

        }


    }
}
