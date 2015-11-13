using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TreeSharp;

namespace Dev.D3.Core.Bot.Behaviors
{
    public class AttackTargetDecorator
    {
        public Sequence this[IBot.IBot bot]
        {
            get
            {
                var seq = new Sequence(

                    new Decorator(ret => bot.isRunning, new TreeSharp.Action(ret => RunStatus.Success)),
                    
                    //first we must have a valid target
                    new Decorator(ret => Util.Targeting.HasValidTarget(bot.CurrentTarget), new TreeSharp.Action(ret => RunStatus.Success)),

                    //if we are already busy, don't continue.
                    new Decorator(ret => !bot.isInAction, new TreeSharp.Action(ret => RunStatus.Success)),


                    //send the attack action.
                    new TreeSharp.Action(ret =>
                    {
                        bot.Attack(bot.CurrentTarget);
                    }),

                    //wait for the action to complete (or we time out).
                    new Wait(1, ret => bot.isInAction, new TreeSharp.Action(ret => RunStatus.Success))
            );

                return seq;

            }

        }

    }
}
