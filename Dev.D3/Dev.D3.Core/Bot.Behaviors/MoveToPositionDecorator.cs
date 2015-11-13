using Dev.D3.Core.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeSharp;

namespace Dev.D3.Core.Bot.Behaviors
{
    public class MoveToPositionDecorator
    {
        public Sequence this[IBot.IBot bot]
        {
            get
            {
                var seq = new Sequence(

                    new Decorator(ret => bot.isRunning, new TreeSharp.Action(ret => RunStatus.Success)),


                    new Decorator(ret => bot.CurrentTarget.Distance() > 50, new TreeSharp.Action(ret => RunStatus.Success)),

                    new TreeSharp.Action(ret =>
                    {
                        Util.MoveTo.MoveToPosWithNavMeshAsync(bot.CurrentTarget.ToSharpDXVector3());
                    }),

                    new Wait(1, ret => bot.isInAction, new TreeSharp.Action(ret => RunStatus.Success))
                );

                return seq;

            }

        }
    }
}
