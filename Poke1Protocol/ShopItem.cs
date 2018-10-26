using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class ShopItem
    {
        public int Id { get; }

        public int Max { get; }

        public int Cost { get; }

        public int TokenCost { get; }

        public string Name => ItemsManager.Instance.ItemClass.items.FirstOrDefault(x => x.ID == Id).Name;


        internal ShopItem(string[] data)
        {
            Id = Convert.ToInt32(data[0]);
            Cost = Convert.ToInt32(data[1]);
            TokenCost = Convert.ToInt32(data[2]);
            Max = Convert.ToInt32(data[3]);
        }
    }
}
