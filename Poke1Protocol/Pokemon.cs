using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSXAPI.Response;

namespace Poke1Protocol
{
    public class Pokemon
    {
        public int Uid { get; private set; }
        public int Id { get; private set; }
        public InventoryPokemon PokemonData { get; private set; }
        public int Level {
            get;
            private set;
        }
        public int MaxHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public PokemonMove[] Moves { get; private set; }
        public PokemonExperience Experience { get; private set; }
        public bool IsShiny { get; private set; }
        public string Gender { get; private set; }
        public string Nature
        {
            get;
            private set;
        }
        public PokemonAbility Ability { get; private set; }
        public int Happiness { get; private set; }
        public string ItemHeld { get; private set; }
        public PokemonStats Stats { get; private set; }
        public PokemonStats IV { get; private set; }
        public PokemonStats EV { get; private set; }
        public PokemonStats EVsCollected { get; private set; }
        public string OriginalTrainer { get; private set; }
        public PokemonType Type1 { get; private set; }
        public PokemonType Type2 { get; private set; }
        public string Types { get => Type2 == PokemonType.None ? Type1.ToString() : Type1.ToString() + "/" + Type2.ToString(); private set { } }

        private string _status;
        public string Status
        {
            get => CurrentHealth == 0 ? "KO" : _status;
            set => _status = value;
        }
        public string Name { get; private set; }
        public string Health => CurrentHealth + "/" + MaxHealth;
        public int BattleCurrentHealth { get; private set; }
        public int BattleMaxHealth { get; private set; }
        public PSXAPI.Response.Payload.PokemonMoveID[] LearnableMoves { get; private set; }
        public PSXAPI.Response.Payload.PokemonID CanEvolveTo { get; private set; }
        public Guid UniqueID { get; private set; }
        public string Forme { get; private set; }
        internal Pokemon(InventoryPokemon data)
        {
            if (data != null)
            {
                PokemonData = data;
                UniqueID = data.Pokemon.UniqueID;
                Ability = new PokemonAbility(data.Pokemon.Ability, data.Pokemon.Payload.AbilitySlot);
                Stats = new PokemonStats(data.Pokemon.Stats);
                IV = new PokemonStats(data.Pokemon.Payload.IVs);
                EV = new PokemonStats(data.Pokemon.Payload.EVs);
                EVsCollected = new PokemonStats(data.Pokemon.Payload.EVsCollected);
                Id = PokemonManager.Instance.GetIdByName(data.Pokemon.Payload.PokemonID.ToString());
                Type1 = TypesManager.Instance.Type1[Id];
                Type2 = TypesManager.Instance.Type2[Id];
                Experience = new PokemonExperience
                    (data.Pokemon.Payload.Level, data.Pokemon.Payload.Exp,
                    data.Pokemon.ExpNext, data.Pokemon.ExpStart, 
                    PokemonManager.Instance.GetExpGroup(data.Pokemon.Payload.PokemonID.ToString()));
                OriginalTrainer = data.Pokemon.OriginalTrainer;
                _status = data.Pokemon.Payload.Condition.ToString();
                BattleMaxHealth = data.Pokemon.Stats.HP;
                BattleCurrentHealth = data.Pokemon.Payload.HP;
                LearnableMoves = data.CanLearnMove;
                CanEvolveTo = data.CanEvolve;
                if (data.Pokemon.Payload.Moves != null && data.Pokemon.Payload.Moves.Length > 0)
                {
                    Moves = new PokemonMove[data.Pokemon.Payload.Moves.Length];
                    var i = 0;
                    foreach(var move in data.Pokemon.Payload.Moves)
                    {
                        Moves[i] = new PokemonMove(i + 1, MovesManager.Instance.GetMoveId(move.Move.ToString()), move.MaxPP, move.PP);
                        i++;
                    }
                }
                if (data.Position > 0)
                    UpdatePosition(data.Position);
                Level = Experience.CurrentLevel;
                MaxHealth = data.Pokemon.Stats.HP;
                CurrentHealth = data.Pokemon.Payload.HP;
                Happiness = data.Pokemon.Payload.Happiness;
                ItemHeld = data.Pokemon.Payload.HoldItem <= 0 ? "" : ItemsManager.Instance.ItemClass.items.ToList().Find(x => x.ID == data.Pokemon.Payload.HoldItem)?.Name;
                IsShiny = data.Pokemon.Payload.Shiny;
                Gender = data.Pokemon.Payload.Gender.ToString();
                Nature = data.Pokemon.Payload.Nature.ToString().FirstOrDefault().ToString().ToUpperInvariant() + data.Pokemon.Payload.Nature.ToString().Substring(1);
                Name = PokemonManager.Instance.Names[Id];
            }
            Forme = "Normal";
        }

        public void UpdatePokemonData(InventoryPokemon data)
        {
            if (data != null)
            {
                PokemonData = data;
                UniqueID = data.Pokemon.UniqueID;
                Ability = new PokemonAbility(data.Pokemon.Ability, data.Pokemon.Payload.AbilitySlot);
                Stats = new PokemonStats(data.Pokemon.Stats);
                IV = new PokemonStats(data.Pokemon.Payload.IVs);
                EV = new PokemonStats(data.Pokemon.Payload.EVs);
                EVsCollected = new PokemonStats(data.Pokemon.Payload.EVsCollected);
                Id = PokemonManager.Instance.GetIdByName(data.Pokemon.Payload.PokemonID.ToString());
                Type1 = TypesManager.Instance.Type1[Id];
                Type2 = TypesManager.Instance.Type2[Id];
                Experience = new PokemonExperience
                    (data.Pokemon.Payload.Level, data.Pokemon.Payload.Exp,
                    data.Pokemon.ExpNext, data.Pokemon.ExpStart,
                    PokemonManager.Instance.GetExpGroup(data.Pokemon.Payload.PokemonID.ToString()));
                OriginalTrainer = data.Pokemon.OriginalTrainer;
                _status = data.Pokemon.Payload.Condition.ToString();
                BattleMaxHealth = data.Pokemon.Stats.HP;
                BattleCurrentHealth = data.Pokemon.Payload.HP;
                LearnableMoves = data.CanLearnMove;
                CanEvolveTo = data.CanEvolve;
                if (data.Pokemon.Payload.Moves != null && data.Pokemon.Payload.Moves.Length > 0)
                {
                    Moves = new PokemonMove[data.Pokemon.Payload.Moves.Length];
                    var i = 0;
                    foreach (var move in data.Pokemon.Payload.Moves)
                    {
                        Moves[i] = new PokemonMove(i + 1, MovesManager.Instance.GetMoveId(move.Move.ToString()), move.MaxPP, move.PP);
                        i++;
                    }
                }
                if (data.Position > 0)
                    UpdatePosition(data.Position);
                Level = Experience.CurrentLevel;
                MaxHealth = data.Pokemon.Stats.HP;
                CurrentHealth = data.Pokemon.Payload.HP;
                Happiness = data.Pokemon.Payload.Happiness;
                ItemHeld = data.Pokemon.Payload.HoldItem <= 0 ? "" : ItemsManager.Instance.ItemClass.items.ToList().Find(x => x.ID == data.Pokemon.Payload.HoldItem)?.Name;
                IsShiny = data.Pokemon.Payload.Shiny;
                Gender = data.Pokemon.Payload.Gender.ToString();
                Nature = data.Pokemon.Payload.Nature.ToString().FirstOrDefault().ToString().ToUpperInvariant() + data.Pokemon.Payload.Nature.ToString().Substring(1);
                Name = PokemonManager.Instance.Names[Id];
            }
        }

        public void UpdateHealth(int current, int max)
        {
            BattleCurrentHealth = current;
            BattleMaxHealth = max;
        }

        public void UpdatePosition(int uid)
        {
            Uid = uid;
        }

        public void UpdateStatus(string status)
        {
            if (!string.IsNullOrEmpty(status) && status.ToLowerInvariant() != "none")
            {
                
                _status = GameClient.GetStatus(status);
            }
        }

        public void UpdateMovePoints(int moveId, int currnetpp, int maxPP)
        {
            Moves[moveId].CurrentPoints = currnetpp;
            Moves[moveId].MaxPoints = maxPP;
        }

        public void UpdateForme(string forme)
        {
            if (!string.IsNullOrEmpty(forme))
            {
                switch (forme)
                {
                    case "-mega-x":
                        forme = "Mega X";
                        break;
                    case "-mega-y":
                        forme = "Mega Y";
                        break;
                    case "-mega":
                        forme = "Mega";
                        break;
                    case "-primal":
                        forme = "Primal";
                        break;
                    case "mimikyubusted":
                        forme = "Mimikyu Busted";
                        break;
                    case "wishiwashischool":
                        forme = "Wishi Washi School";
                        break;
                    default:
                        forme = "";
                        break;
                }
                Forme = forme;
            }
        }
    }
}
