using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class Shop
    {
        private List<ShopItem> _items = new List<ShopItem>();
        public ReadOnlyCollection<ShopItem> Items => _items.AsReadOnly();
        public Guid ScriptId { get; private set; }
        internal Shop(string[] data, Guid id)
        {
            foreach(var d in data)
            {
                _items.Add(new ShopItem(d.Split(',')));
            }
            ScriptId = id;
        }

    }
}
