using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Enigma.D3;
using TreeSharp;
using Dev.D3.Core.Bot.Behaviors;

namespace Dev.D3.Core.Bot.SimpleRun
{
    public class SimpleRun : IBot.IBot
    {

        public SimpleRun()
        {

            BuildTree();
        }

        public Composite BuildTree()
        {
            return new PrioritySelector(

                //we first try and find a target
                new FindTargetDecorator()[this],

                //then we will attack it.
                new AttackTargetDecorator()[this]

            );
        }

    }
}
