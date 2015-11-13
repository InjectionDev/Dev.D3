using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeSharp;

namespace Dev.D3.Core.Bot.Behaviors
{
    public class FindTargetDecorator
    {

        public Sequence this[IBot.IBot bot]
        {
            get
            {
                var seq = new Sequence(

                    new Decorator(ret => bot.isRunning, new TreeSharp.Action(ret => RunStatus.Success)),

                    //first we must not already have a target.
                    new Decorator(ret => !Util.Targeting.HasValidTarget(bot.CurrentTarget), new TreeSharp.Action(ret => RunStatus.Success)),

                    //go grab a target
                    new TreeSharp.Action(ret =>
                    {
                        bot.CurrentTarget = Util.Targeting.GetTarget();
                    })

                );

                return seq;

            }

        }
    }
}
