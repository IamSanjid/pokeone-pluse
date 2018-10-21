using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Poke1Protocol.Properties;

namespace Poke1Protocol
{
    public class ItemsManager
    {
        private static ItemsManager _instance;

        public string[] Names { get; }

        public ItemsClass ItemClass;

        public static ItemsManager Instance
        {
            get
            {
                return _instance ?? (_instance = new ItemsManager());
            }
        }
        public ItemsManager()
        {
            try
            {
                List<string> lst = new List<string>();
                var json = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Resources.items)) as JObject;
                ItemClass = JsonConvert.DeserializeObject<ItemsClass>(json.ToString());
                int i = 0;
                var items = ItemClass.items.ToList();
                foreach (var item in items)
                {
                    if (item.ID != i)
                    {

                    }
                    i++;
                }
                Names = new string[lst.Count];
                Names = lst.ToArray();
            }
            catch (Exception)
            {
                //ignore
            }
        }
        public class ItemsClass
        {
            public Item[] items;
        }
        public class Item
        {
            public int ID { get; set; }
            public string Name { get; set; }
            public string BattleID { get; set; }
            public string Description { get; set; }
            public int Pocket { get; set; }
            public int Usage { get; set; }
            public int ItemImage { get; set; }
        }
    }
}
