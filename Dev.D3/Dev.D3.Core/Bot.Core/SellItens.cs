using Enigma.D3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dev.D3.Core.Bot.Core
{
    public class SellItens
    {

        private const int QtMaxItensOnInventory = 15;

        private Engine engine;

        public SellItens(Engine pEngine)
        {
            this.engine = pEngine;
        }

        public void Start()
        {
            if (this.MustSellItens())
            {
                
            }

        }


        private bool MustSellItens()
        {
            var InventoryItems = Enigma.D3.Helpers.ActorCommonDataHelper.EnumerateInventoryItems();

            return InventoryItems.Count() > QtMaxItensOnInventory;
        }



    }
}
