using System;
using System.Collections.Generic;
using System.Linq;

namespace Poke1Protocol
{
    public class TypesManager
    {
        private static TypesManager _instance;
        public static TypesManager Instance => _instance ?? (_instance = new TypesManager());

        public PokemonType[] Type1 = new PokemonType[808];
        public PokemonType[] Type2 = new PokemonType[808];

        private Dictionary<PokemonType, Dictionary<PokemonType, double>> _typeChart;



        private TypesManager()
        {
            LoadTypes();

            var allTypes = (PokemonType[])Enum.GetValues(typeof(PokemonType));

            _typeChart = new Dictionary<PokemonType, Dictionary<PokemonType, double>>();
            foreach (PokemonType attacker in allTypes)
            {
                _typeChart[attacker] = new Dictionary<PokemonType, double>();
                foreach (PokemonType defender in allTypes)
                {
                    _typeChart[attacker][defender] = 1.0;
                }
            }

            _typeChart[PokemonType.Normal][PokemonType.Rock] = 0.5;
            _typeChart[PokemonType.Normal][PokemonType.Ghost] = 0;
            _typeChart[PokemonType.Normal][PokemonType.Steel] = 0.5;

            _typeChart[PokemonType.Fighting][PokemonType.Normal] = 2.0;
            _typeChart[PokemonType.Fighting][PokemonType.Flying] = 0.5;
            _typeChart[PokemonType.Fighting][PokemonType.Poison] = 0.5;
            _typeChart[PokemonType.Fighting][PokemonType.Rock] = 2.0;
            _typeChart[PokemonType.Fighting][PokemonType.Bug] = 0.5;
            _typeChart[PokemonType.Fighting][PokemonType.Ghost] = 0;
            _typeChart[PokemonType.Fighting][PokemonType.Steel] = 2.0;
            _typeChart[PokemonType.Fighting][PokemonType.Psychic] = 0.5;
            _typeChart[PokemonType.Fighting][PokemonType.Ice] = 2.0;
            _typeChart[PokemonType.Fighting][PokemonType.Dark] = 2.0;
            _typeChart[PokemonType.Fighting][PokemonType.Fairy] = 0.5;

            _typeChart[PokemonType.Flying][PokemonType.Fighting] = 2.0;
            _typeChart[PokemonType.Flying][PokemonType.Rock] = 0.5;
            _typeChart[PokemonType.Flying][PokemonType.Bug] = 2.0;
            _typeChart[PokemonType.Flying][PokemonType.Steel] = 0.5;
            _typeChart[PokemonType.Flying][PokemonType.Grass] = 2.0;
            _typeChart[PokemonType.Flying][PokemonType.Electric] = 0.5;

            _typeChart[PokemonType.Poison][PokemonType.Poison] = 0.5;
            _typeChart[PokemonType.Poison][PokemonType.Ground] = 0.5;
            _typeChart[PokemonType.Poison][PokemonType.Rock] = 0.5;
            _typeChart[PokemonType.Poison][PokemonType.Ghost] = 0.5;
            _typeChart[PokemonType.Poison][PokemonType.Steel] = 0;
            _typeChart[PokemonType.Poison][PokemonType.Grass] = 2.0;
            _typeChart[PokemonType.Poison][PokemonType.Fairy] = 2.0;

            _typeChart[PokemonType.Ground][PokemonType.Flying] = 0;
            _typeChart[PokemonType.Ground][PokemonType.Poison] = 2.0;
            _typeChart[PokemonType.Ground][PokemonType.Rock] = 2.0;
            _typeChart[PokemonType.Ground][PokemonType.Bug] = 0.5;
            _typeChart[PokemonType.Ground][PokemonType.Steel] = 2.0;
            _typeChart[PokemonType.Ground][PokemonType.Fire] = 2.0;
            _typeChart[PokemonType.Ground][PokemonType.Grass] = 0.5;
            _typeChart[PokemonType.Ground][PokemonType.Electric] = 2.0;

            _typeChart[PokemonType.Rock][PokemonType.Fighting] = 0.5;
            _typeChart[PokemonType.Rock][PokemonType.Flying] = 2.0;
            _typeChart[PokemonType.Rock][PokemonType.Ground] = 0.5;
            _typeChart[PokemonType.Rock][PokemonType.Bug] = 2.0;
            _typeChart[PokemonType.Rock][PokemonType.Steel] = 0.5;
            _typeChart[PokemonType.Rock][PokemonType.Fire] = 2.0;
            _typeChart[PokemonType.Rock][PokemonType.Ice] = 2.0;

            _typeChart[PokemonType.Bug][PokemonType.Fighting] = 0.5;
            _typeChart[PokemonType.Bug][PokemonType.Flying] = 0.5;
            _typeChart[PokemonType.Bug][PokemonType.Poison] = 0.5;
            _typeChart[PokemonType.Bug][PokemonType.Ghost] = 0.5;
            _typeChart[PokemonType.Bug][PokemonType.Steel] = 0.5;
            _typeChart[PokemonType.Bug][PokemonType.Fire] = 0.5;
            _typeChart[PokemonType.Bug][PokemonType.Grass] = 2.0;
            _typeChart[PokemonType.Bug][PokemonType.Psychic] = 2.0;
            _typeChart[PokemonType.Bug][PokemonType.Dark] = 2.0;
            _typeChart[PokemonType.Bug][PokemonType.Fairy] = 0.5;

            _typeChart[PokemonType.Ghost][PokemonType.Normal] = 0;
            _typeChart[PokemonType.Ghost][PokemonType.Ghost] = 2.0;
            _typeChart[PokemonType.Ghost][PokemonType.Psychic] = 2.0;
            _typeChart[PokemonType.Ghost][PokemonType.Dark] = 0.5;

            _typeChart[PokemonType.Steel][PokemonType.Rock] = 2.0;
            _typeChart[PokemonType.Steel][PokemonType.Steel] = 0.5;
            _typeChart[PokemonType.Steel][PokemonType.Fire] = 0.5;
            _typeChart[PokemonType.Steel][PokemonType.Water] = 0.5;
            _typeChart[PokemonType.Steel][PokemonType.Electric] = 0.5;
            _typeChart[PokemonType.Steel][PokemonType.Ice] = 2.0;
            _typeChart[PokemonType.Steel][PokemonType.Fairy] = 2.0;

            _typeChart[PokemonType.Fire][PokemonType.Rock] = 0.5;
            _typeChart[PokemonType.Fire][PokemonType.Bug] = 2.0;
            _typeChart[PokemonType.Fire][PokemonType.Steel] = 2.0;
            _typeChart[PokemonType.Fire][PokemonType.Fire] = 0.5;
            _typeChart[PokemonType.Fire][PokemonType.Water] = 0.5;
            _typeChart[PokemonType.Fire][PokemonType.Grass] = 2.0;
            _typeChart[PokemonType.Fire][PokemonType.Ice] = 2.0;
            _typeChart[PokemonType.Fire][PokemonType.Dragon] = 0.5;

            _typeChart[PokemonType.Water][PokemonType.Ground] = 2.0;
            _typeChart[PokemonType.Water][PokemonType.Rock] = 2.0;
            _typeChart[PokemonType.Water][PokemonType.Fire] = 2.0;
            _typeChart[PokemonType.Water][PokemonType.Water] = 0.5;
            _typeChart[PokemonType.Water][PokemonType.Grass] = 0.5;
            _typeChart[PokemonType.Water][PokemonType.Dragon] = 0.5;

            _typeChart[PokemonType.Grass][PokemonType.Flying] = 0.5;
            _typeChart[PokemonType.Grass][PokemonType.Poison] = 0.5;
            _typeChart[PokemonType.Grass][PokemonType.Ground] = 2.0;
            _typeChart[PokemonType.Grass][PokemonType.Rock] = 2.0;
            _typeChart[PokemonType.Grass][PokemonType.Bug] = 0.5;
            _typeChart[PokemonType.Grass][PokemonType.Steel] = 0.5;
            _typeChart[PokemonType.Grass][PokemonType.Fire] = 0.5;
            _typeChart[PokemonType.Grass][PokemonType.Water] = 2.0;
            _typeChart[PokemonType.Grass][PokemonType.Grass] = 0.5;
            _typeChart[PokemonType.Grass][PokemonType.Dragon] = 0.5;

            _typeChart[PokemonType.Electric][PokemonType.Flying] = 2.0;
            _typeChart[PokemonType.Electric][PokemonType.Ground] = 0;
            _typeChart[PokemonType.Electric][PokemonType.Water] = 2.0;
            _typeChart[PokemonType.Electric][PokemonType.Grass] = 0.5;
            _typeChart[PokemonType.Electric][PokemonType.Electric] = 0.5;
            _typeChart[PokemonType.Electric][PokemonType.Dragon] = 0.5;

            _typeChart[PokemonType.Psychic][PokemonType.Fighting] = 2.0;
            _typeChart[PokemonType.Psychic][PokemonType.Poison] = 2.0;
            _typeChart[PokemonType.Psychic][PokemonType.Steel] = 0.5;
            _typeChart[PokemonType.Psychic][PokemonType.Psychic] = 0.5;
            _typeChart[PokemonType.Psychic][PokemonType.Dark] = 0;

            _typeChart[PokemonType.Ice][PokemonType.Flying] = 2.0;
            _typeChart[PokemonType.Ice][PokemonType.Ground] = 2.0;
            _typeChart[PokemonType.Ice][PokemonType.Steel] = 0.5;
            _typeChart[PokemonType.Ice][PokemonType.Fire] = 0.5;
            _typeChart[PokemonType.Ice][PokemonType.Water] = 0.5;
            _typeChart[PokemonType.Ice][PokemonType.Grass] = 2.0;
            _typeChart[PokemonType.Ice][PokemonType.Ice] = 0.5;
            _typeChart[PokemonType.Ice][PokemonType.Dragon] = 2.0;

            _typeChart[PokemonType.Dragon][PokemonType.Steel] = 0.5;
            _typeChart[PokemonType.Dragon][PokemonType.Dragon] = 2.0;
            _typeChart[PokemonType.Dragon][PokemonType.Fairy] = 0;

            _typeChart[PokemonType.Dark][PokemonType.Fighting] = 0.5;
            _typeChart[PokemonType.Dark][PokemonType.Ghost] = 2.0;
            _typeChart[PokemonType.Dark][PokemonType.Psychic] = 2.0;
            _typeChart[PokemonType.Dark][PokemonType.Dark] = 0.5;
            _typeChart[PokemonType.Dark][PokemonType.Fairy] = 0.5;

            _typeChart[PokemonType.Fairy][PokemonType.Fighting] = 2.0;
            _typeChart[PokemonType.Fairy][PokemonType.Poison] = 0.5;
            _typeChart[PokemonType.Fairy][PokemonType.Steel] = 0.5;
            _typeChart[PokemonType.Fairy][PokemonType.Fire] = 0.5;
            _typeChart[PokemonType.Fairy][PokemonType.Dragon] = 2.0;
            _typeChart[PokemonType.Fairy][PokemonType.Dark] = 2.0;
        }

        public double GetMultiplier(PokemonType attacker, PokemonType defender)
        {
            if (attacker == PokemonType.None || defender == PokemonType.None)
            {
                return 1.0;
            }
            return _typeChart[attacker][defender];
        }

        public void LoadTypes()
        {
            foreach(var pokemon in PokemonManager.Instance.AllPokemonInfos.Pokemon)
            {
                if (PokemonManager.Instance.AllPokemonInfos.Pokemon.ToList().LastOrDefault() == pokemon) break;
                Type1[pokemon.ID] = PokemonTypeExtensions.FromName(pokemon.Type);
                Type2[pokemon.ID] = PokemonTypeExtensions.FromName(pokemon.Type2);
            }
        }
    }
}
