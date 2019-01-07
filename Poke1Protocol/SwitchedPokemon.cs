namespace Poke1Protocol
{
    public class SwitchedPokemon
    {
        public string Species = "";

        public bool Shiny = false;

        public int Level = 0;

        public string Gender = "";

        public int Health = 1;

        public int MaxHealth = 1;

        public string Status;

        public int ID;

        public string Forme = null;

        public PSXAPI.Response.Payload.BattleMove[] Moves = null;

        public string Trainer = null;

        public int Personality = -1;

        public bool Sent = false;

        public bool RepeatAttack = false;
    }
}
