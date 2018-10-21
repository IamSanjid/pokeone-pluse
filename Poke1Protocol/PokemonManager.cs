using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Poke1Protocol.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Poke1Protocol
{
    public class PokemonManager
    {
        private static PokemonManager _instance;

        public DexInfo AllPokemonInfos;

        public static PokemonManager Instance
        {
            get
            {
                return _instance ?? (_instance = new PokemonManager());
            }
        }

        public bool IsLavitatingPokemon(int id)
        {
            var result = false;
            if (id <= Names.Length)
            {
                var foundPok = AllPokemonInfos.Pokemon.ToList().Find(x => x.ID == id);
                if (foundPok != null)
                    result = foundPok.Ability1.ToLowerInvariant() == "levitate"
                        || foundPok.Ability2.ToLowerInvariant() == "levitate"
                        || foundPok.Ability3.ToLowerInvariant() == "levitate";
            }
            return result;
        }

        public string[] Names { get; private set; }
        public int GetIdByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return -1;
            var result = -1;
            if (Names.Length > 0)
            {
                name = name.ToUpperInvariant();
                var foundName = Names.ToList().FirstOrDefault(x => x.ToUpperInvariant() == name);
                
                result = string.IsNullOrEmpty(foundName) ? -1 : Names.ToList().IndexOf(foundName);
            }
            return result;
        }

        public string GetNameFromEnum(PSXAPI.Response.Payload.PokemonID id)
        {
            string result = null;
            if (!string.IsNullOrEmpty(id.ToString()))
            {
                var foundName = Names.ToList().Find(x => x.ToLowerInvariant() == id.ToString().ToLowerInvariant());

                result = string.IsNullOrEmpty(foundName) ? null : foundName;
            }
            return result;
        }

        public ExpGroup GetExpGroup(string name)
        {
            var group = ExpGroup.Erratic;
            if (Names.Length > 0)
            {
                name = name.ToUpperInvariant();
                var foundPokeInfo = AllPokemonInfos.Pokemon.FirstOrDefault(x => x.Name.ToUpperInvariant() == name);

                group = foundPokeInfo is null ? ExpGroup.Unset : ExpGroupExtention.FromName(foundPokeInfo.ExpRate);
            }
            return group;
        }

        public PokemonManager()
        {
            try
            {
                List<string> name = new List<string>();
                var json = JsonConvert.DeserializeObject(Encoding.UTF8.GetString(Resources.pokemons)) as JObject;
                AllPokemonInfos = JsonConvert.DeserializeObject<DexInfo>(json.ToString());
                int i = 0;
                foreach (var st in AllPokemonInfos.Pokemon)
                {
                    name.Add(st.Name);
                    i++;
                }
                Names = new string[i];
                Names = name.ToArray();
            }
            catch (Exception)
            {

            }
        }

        public class DexInfo
        {
            public PokedexInfo[] Pokemon;
        }

        public class PokedexInfo
        {

            public int ID { get; set; }

            public string Name { get; set; }
            public string ExpRate { get; set; }
            public string Description { get; set; }

            public string Type { get; set; }

            public string Type2 { get; set; }

            public int HP { get; set; }

            public int ATK { get; set; }

            public int DEF { get; set; }

            public int SPATK { get; set; }

            public int SPDEF { get; set; }

            public int SPD { get; set; }
            public string Species { get; set; }

            public string Height { get; set; }

            public string Weight { get; set; }

            public double RatioM { get; set; }
            public string Ability1 { get; set; }
            public string Ability2 { get; set; }
            public string Ability3 { get; set; }

            public int EVATK { get; set; }

            public int EVDEF { get; set; }

            public int EVSPD { get; set; }

            public int EVSPDEF { get; set; }

            public int EVSPATK { get; set; }

            public int EVHP { get; set; }
        }
    }
}
