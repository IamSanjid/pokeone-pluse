using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class PokemonAbility
    {
        public string Name { get; }
        public PSXAPI.Response.Payload.AbilitySlotType AbilityType { get; }
        public PokemonAbility(string name, PSXAPI.Response.Payload.AbilitySlotType abilityType)
        {
            Name = name;
            AbilityType = abilityType;
        }
    }
}
