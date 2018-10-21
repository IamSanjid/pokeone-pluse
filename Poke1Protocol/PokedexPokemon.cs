using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class PokedexPokemon
    {
        public string Name { get; }
        public int Id { get; }
        public bool Caught { get; private set; }
        public bool Seen { get; private set; }
        internal PokedexPokemon(PSXAPI.Response.Payload.PokedexEntry data)
        {
            Name = PokemonManager.Instance.GetNameFromEnum(data.Pokemon);
            Id = PokemonManager.Instance.GetIdByName(Name);
            Seen = data.State == PSXAPI.Response.Payload.PokedexEntryState.Seen;
            Caught = data.State == PSXAPI.Response.Payload.PokedexEntryState.Caught;
            if (Caught)
                Seen = true;
        }

        public void UpdateStatus(PSXAPI.Response.Payload.PokedexEntryState state)
        {
            Caught = state == PSXAPI.Response.Payload.PokedexEntryState.Caught;
            Seen = state == PSXAPI.Response.Payload.PokedexEntryState.Seen;
            if (Caught)
                Seen = true;
        }
    }
}
