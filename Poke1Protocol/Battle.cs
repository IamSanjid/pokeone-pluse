using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class Battle
    {
        //public Action<string> BattleMessage;

        public int OpponentId { get; private set; }
        public int OpponentHealth { get; private set; }
        public int CurrentHealth { get; private set; }
        public int OpponentLevel { get; private set; }
        public bool IsShiny { get; private set; }
        public bool IsWild { get; private set; }
        public string OpponentGender { get; private set; }
        public string OpponentStatus { get; private set; }

        public string OpponentForme { get; private set; }
        public string TrainerName { get; private set; }
        public bool IsPvp { get; }
        public int PokemonCount { get; private set; }
        public string BattleText { get; }
        public bool AlreadyCaught { get; set; }
        public int SelectedPokemonIndex { get; private set; }
        public bool IsFinished { get; private set; }
        public string _playerName { get; set; }

        public bool OnlyInfo { get; private set; } = false;

        public PokemonStats OpponentStats { get; private set; }
        public int ResponseID { get; private set; }

        public int Turn { get; private set; } = 1;

        public bool IsTrapped { get; private set; }

        public bool RepeatAttack { get; set; }

        public PSXAPI.Response.Payload.BattleSide PlayerBattleSide { get; private set; }

        public int PlayerSide { get; private set; } = 1;

        public PSXAPI.Response.Battle Data { get; private set; }

        public bool IsUpdated { get; private set; }

        public int CurrentBattlingPokemonIndex { get; private set; } // different than SelectedPokemonIndex

        public List<SwitchedPokemon> OpponentActivePokemon { get; private set; }

        public List<SwitchedPokemon> PlayerAcivePokemon { get; private set; }

        private List<Pokemon> _team { get; set; }

        public PSXAPI.Response.Payload.BattleActive GetActivePokemon
        {
            get
            {
                var playerReq = PlayerSide == 1 ? Data.Request1 : Data.Request2;
                if (playerReq is null || playerReq.RequestInfo.active is null) return null;
                var pok = playerReq.RequestInfo.active?.FirstOrDefault(s => s?.trainer?.ToLowerInvariant() == _playerName?.ToLowerInvariant());
                if (pok is null)
                    return playerReq.RequestInfo.active[0] ?? null;
                return pok;
            }
        }

        public string AttackTargetType(int uid, int pokeUid)
        {
            if (string.IsNullOrEmpty(_playerName) || PlayerAcivePokemon is null)
                return "normal";
            var targetMove = PlayerAcivePokemon[pokeUid - 1].Moves[uid - 1];
            if (targetMove is null || string.IsNullOrEmpty(targetMove.target))
                return "normal";
            return targetMove.target;
        }

        public Battle(string playerName, PSXAPI.Response.Battle data, List<Pokemon> team)
        {
            IsUpdated = false;

            Data = data;

            _playerName = playerName;

            IsWild = data.CanCatch;

            IsFinished = data.Ended;

            _team = team;

            if (data.Mapping1 != null && !string.IsNullOrEmpty(playerName))
            {
                if (data.Mapping1.ToList().Any(name => name.ToLowerInvariant() == _playerName.ToLowerInvariant()))
                    PlayerSide = 1;
            }

            if (data.Mapping2 != null && !string.IsNullOrEmpty(playerName))
            {
                if (data.Mapping2.ToList().Any(name => name.ToLowerInvariant() == _playerName.ToLowerInvariant()))
                    PlayerSide = 2;
            }

            

            if (data.Request1 != null)
            {
                HandleBattleRequest(data.Request1, PlayerSide == 1);
            }
            if (data.Request2 != null)
            {
                HandleBattleRequest(data.Request2, PlayerSide == 2);
            }

            if (data.Log != null && data.Log.Length > 0)
            {
                var switchTxt = data.Log.ToList().Find(m => m.Contains("switch") && m.Split('|')[1] == "switch");
                if (!string.IsNullOrEmpty(switchTxt))
                {
                    string[] array = switchTxt.Split(new char[]
                    {
                        '|'
                    });
                    if (array.Length > 1)
                    {
                        SwitchedPokemon pokemon = null;
                        var pokeName = "";
                        var side = Convert.ToInt32(array[2].Substring(0, 2).Replace("p", "").Replace("a", "").Replace("b", "").Replace("c", ""));
                        var personality = 0;
                        if (array.Length >= 7)
                        {
                            pokemon = GetSwitchedPokemon(array[3], array[4]);
                            pokeName = array[5];
                            int.TryParse(array[6], out personality);
                        }
                        else
                        {
                            pokemon = GetSwitchedPokemon(array[3], string.Empty);
                            pokeName = array[4];
                            int.TryParse(array[5], out personality);
                        }

                        if (pokemon is null) return;

                        if (side == PlayerSide)
                        {
                            //player
                            var req = PlayerSide == 1 ? Data.Request1 : Data.Request2;
                            var index = PlayerBattleSide.pokemon.ToList().IndexOf(req.RequestInfo.side.pokemon.FirstOrDefault(x => x.personality == personality));
                            SelectedPokemonIndex = index < 0 ? SelectedPokemonIndex : index;
                            team[index].UpdateHealth(pokemon.Health, pokemon.MaxHealth);
                        }
                        else
                        {
                            //oppoenent
                            var req = PlayerSide == 1 ? Data.Request2 : Data.Request1;
                            var index = req.RequestInfo.side.pokemon.ToList().IndexOf(req.RequestInfo.side.pokemon.FirstOrDefault(x => x.personality == personality));
                            CurrentHealth = pokemon.Health;
                            OpponentHealth = pokemon.MaxHealth;
                            OpponentGender = pokemon.Gender;
                            OpponentId = pokemon.ID;
                            OpponentLevel = pokemon.Level;
                            OpponentStatus = pokemon.Status;
                            IsShiny = pokemon.Shiny;
                            if (index >= 0)
                                OpponentStats = new PokemonStats(req.RequestInfo.side.pokemon[index].stats, pokemon.MaxHealth);
                        }
                    }
                }
            }
        }

        public void UpdateBattle(string playerName, PSXAPI.Response.Battle data, List<Pokemon> team)
        {
            _playerName = playerName;

            IsUpdated = true;

            IsWild = data.CanCatch;

            IsFinished = data.Ended;

            _team = team;

            if (data.Mapping1 != null && !string.IsNullOrEmpty(_playerName))
            {
                if (data.Mapping1.ToList().Any(name => name.ToLowerInvariant() == _playerName.ToLowerInvariant()))
                    PlayerSide = 1;

                if (SelectedPokemonIndex > data.Mapping1.Length - 1)
                    SelectedPokemonIndex = data.Mapping1.Length - 1;
            }

            if (data.Mapping2 != null && !string.IsNullOrEmpty(_playerName))
            {
                if (data.Mapping2.ToList().Any(name => name.ToLowerInvariant() == _playerName.ToLowerInvariant()))
                    PlayerSide = 2;

                if (SelectedPokemonIndex > data.Mapping2.Length - 1)
                    SelectedPokemonIndex = data.Mapping2.Length - 1;
            }

            if (data.Request1 != null)
            {
                Data = data;
                HandleBattleRequest(data.Request1, PlayerSide == 1);
            }
            if (data.Request2 != null)
            {
                Data = data;
                HandleBattleRequest(data.Request2, PlayerSide == 2);
            }

            if (data.Log != null && data.Log.Length > 0)
            {
                var switchTxt = data.Log.ToList().Find(m => m.Contains("switch") && m.Split('|')[1] == "switch");
                if (!string.IsNullOrEmpty(switchTxt))
                {
                    string[] array = switchTxt.Split(new char[]
                    {
                        '|'
                    });
                    if (array.Length > 1)
                    {
                        SwitchedPokemon pokemon = null;
                        var pokeName = "";
                        var side = Convert.ToInt32(array[2].Substring(0, 2).Replace("p", "").Replace("a", "").Replace("b", "").Replace("c", ""));
                        var personality = 0;
                        if (array.Length >= 7)
                        {
                            pokemon = GetSwitchedPokemon(array[3], array[4]);
                            pokeName = array[5];
                            int.TryParse(array[6], out personality);
                        }
                        else
                        {
                            pokemon = GetSwitchedPokemon(array[3], string.Empty);
                            pokeName = array[4];
                            int.TryParse(array[5], out personality);
                        }

                        if (pokemon is null) return;

                        if (side == PlayerSide)
                        {
                            //player
                            var req = PlayerSide == 1 ? Data.Request1 : Data.Request2;
                            var index = PlayerBattleSide.pokemon.ToList().IndexOf(req.RequestInfo.side.pokemon.FirstOrDefault(x => x.personality == personality));
                            SelectedPokemonIndex = index < 0 ? SelectedPokemonIndex : index;
                            team[index].UpdateHealth(pokemon.Health, pokemon.MaxHealth);
                        }
                        else
                        {
                            //oppoenent
                            var req = PlayerSide == 1 ? Data.Request2 : Data.Request1;
                            var index = req.RequestInfo.side.pokemon.ToList().IndexOf(req.RequestInfo.side.pokemon.FirstOrDefault(x => x.personality == personality));
                            CurrentHealth = pokemon.Health;
                            OpponentHealth = pokemon.MaxHealth;
                            OpponentGender = pokemon.Gender;
                            OpponentId = pokemon.ID;
                            OpponentLevel = pokemon.Level;
                            OpponentStatus = pokemon.Status;
                            IsShiny = pokemon.Shiny;
                            if (index >= 0)
                                OpponentStats = new PokemonStats(req.RequestInfo.side.pokemon[index].stats, pokemon.MaxHealth);
                        }
                    }
                }
            }
        }

        public void UpdateSelectedPokemon() // While switching to another Pokemon.
        {
            UpdateSelectedPokemonIndex();
        }

        private void UpdateSelectedPokemonIndex()
        {
            if (GetActivePokemon != null)
            {
                SelectedPokemonIndex = _team.FindIndex(p => p.PokemonData.Pokemon.Payload.Personality == GetActivePokemon.personality);
            }
        }

        private void UpdateBattlePokemon(PSXAPI.Response.Payload.BattlePokemon[] pokemon)
        {
            for (var i = 0; i < pokemon.Length; i++)
            {
                var condition = pokemon[i].condition;
                var details = pokemon[i].details;
                var newPoke = GetSwitchedPokemon(details, condition);
                var index = _team.FindIndex(p => p.PokemonData.Pokemon.Payload.Personality == pokemon[i].personality); // find the correct index...
                if (index >= 0)
                {
                    _team[index].UpdateHealth(newPoke.Health, newPoke.MaxHealth);
                    _team[index].UpdateStatus(newPoke.Status);
                    for (var j = 0; j < pokemon[i].moveData.Length; ++j) {
                        var move = pokemon[i].moveData[j];
                        _team[index].UpdateMovePoints(j, move.pp, move.maxpp);
                    }
                }
            }
        }

        private void HandleBattleRequest(PSXAPI.Response.Payload.BattleRequest request, bool isPlayerSide)
        {
            if (isPlayerSide)
            {
                var p1 = request;
                var active = p1.RequestInfo.active;
                var activePokemon = p1.RequestInfo.side.pokemon.ToList().Find(x => x.active);

                CurrentBattlingPokemonIndex = p1.RequestInfo.side.pokemon.ToList().FindIndex(x => x.active) + 1;

                UpdateBattlePokemon(p1.RequestInfo.side.pokemon);

                ResponseID = request.RequestID;
                PlayerBattleSide = p1.RequestInfo.side;

                if (_team is null || _team.Count <= 0)
                    SelectedPokemonIndex = p1.RequestInfo.side.pokemon.ToList().IndexOf(activePokemon);
                else
                    SelectedPokemonIndex = _team.IndexOf(_team.Find(p => p.PokemonData.Pokemon.Payload.Personality == activePokemon.personality));
                UpdateSelectedPokemonIndex();
                var pokemons = p1.RequestInfo.side.pokemon;
                if (p1.RequestInfo is null) return;
                if (p1.RequestInfo.active != null)
                {
                    PlayerAcivePokemon = new List<SwitchedPokemon>();
                    for (var i = 0; i < p1.RequestInfo.active.Length; i++)
                    {
                        if (p1.RequestInfo.active[i].trainer.ToLowerInvariant() != _playerName.ToLowerInvariant())
                            continue;
                        var newPoke = GetSwitchedPokemon(pokemons[i]);
                        if (pokemons[i].active && pokemons[i].moveData.Length == 1)
                            RepeatAttack = true;
                        PlayerAcivePokemon.Add(newPoke);
                    }
                }
                else
                {
                    PlayerAcivePokemon = null;
                }
            }
            else
            {
                var p2 = request;
                PokemonCount = p2.RequestInfo.side.pokemon.Length;
                var opponent = p2.RequestInfo.side.pokemon.ToList().Find(x => x.active);

                var Poke = GetSwitchedPokemon(opponent.details, opponent.condition);

                OpponentGender = Poke.Gender;
                OpponentId = Poke.ID;
                OpponentStats = new PokemonStats(opponent.stats, Poke.MaxHealth);
                IsShiny = Poke.Shiny;
                OpponentLevel = Poke.Level;
                OpponentStatus = Poke.Status;
                OpponentHealth = Poke.MaxHealth;
                CurrentHealth = Poke.Health;
                OpponentForme = Poke.Forme;
                if (!IsWild && opponent.trainer != null)
                    TrainerName = opponent.trainer;
                ResponseID = p2.RequestID;               
                var opAbility = opponent.baseAbility.ToLowerInvariant().Replace(" ", "");
                var pokemons = p2.RequestInfo.side.pokemon;
                if (p2.RequestInfo is null) return;
                if (p2.RequestInfo.active != null)
                {
                    OpponentActivePokemon = new List<SwitchedPokemon>();
                    for (var i = 0; i < p2.RequestInfo.active.Length; i++)
                    {
                        var newPoke = GetSwitchedPokemon(pokemons[i]);
                        OpponentActivePokemon.Add(newPoke);
                    }
                }
                else
                {
                    OpponentActivePokemon = null;
                }
            }
            IsTrapped = GetActivePokemon?.maybeTrapped == true || GetActivePokemon?.maybeDisabled == true;
        }

        public void ProcessLog(string[] logs, List<Pokemon> team, Action<string> BattleMessage)
        {
            if (logs is null) return;

            foreach(var log in logs)
            {
                string[] info = log.Split(new char[]
                {
                    '|'
                });
                if (info.Length > 0)
                {
                    var type = info[1];
                    if (type == "--online")
                        OnlyInfo = true;
                    else
                        OnlyInfo = false;

                    var ranAway = logs.Any(sf => sf.Contains("--run"));
                    var caught = logs.Any(sf => sf.Contains("--catch"));

                    switch (type)
                    {
                        case "turn":
                            var s = int.TryParse(info[2], out int turn);
                            if (s)
                                Turn = turn;
                            break;
                        case "-damage":
                            var damageTaker = info[2].Split(new string[]
                                {
                                    ": "
                                }, StringSplitOptions.None);
                            if (((damageTaker[0].Contains("p1") && PlayerSide == 1)
                                || (damageTaker[0].Contains("p2") && PlayerSide == 2)) 
                                && !info[3].Contains("fnt"))
                            {
                                var st = Regex.Replace(info[3], @"[a-zA-Z]+", "");
                                int currentHp = Convert.ToInt32(st.Split('/')[0]);
                                int maxHp = Convert.ToInt32(st.Split('/')[1]);
                                if (SelectedPokemonIndex >= 0)
                                    team[SelectedPokemonIndex].UpdateHealth(currentHp, maxHp);
                            }
                            else if (((damageTaker[0].Contains("p1") && PlayerSide != 1)
                                || (damageTaker[0].Contains("p2") && PlayerSide != 2)))
                            {
                                CurrentHealth = 0;
                            }
                            break;
                        case "move":

                            if (caught) break;

                            //Attacker
                            var attacker = info[2].Split(new string[]
                                {
                                    ": "
                                }, StringSplitOptions.None);
                            //Move
                            var move = info[3];
                            // Attack Taker
                            var attackTaker = info[4].Split(new string[]
                            {
                                ": "
                            }, StringSplitOptions.None);
                            

                            BattleMessage?.Invoke($"{attacker[1]} used {move}!");
                            if (info.Length > 5)
                            {
                                var happened = info[5];
                                if (happened == "[miss]")
                                    BattleMessage?.Invoke($"{attacker[1]}'s attack missed!");
                            }
                            break;
                        case "faint":
                            if (caught) break;
                            var died = info[2].Split(new string[]
                            {
                                ": "
                            }, StringSplitOptions.None);
                            BattleMessage?.Invoke(!died[0].Contains("p1") ? IsWild ? $"Wild {died[1]} fainted!" : $"Opponent's {died[1]} fainted!" : $"Your {died[1]} fainted!");
                            break;
                        case "--run":
                            if (info[3] == "0")
                            {
                                BattleMessage?.Invoke($"{info[4]} failed to run away!");
                            }
                            else
                            {
                                BattleMessage?.Invoke("You got away safely!");
                            }
                            break;
                        case "win":                           
                            IsFinished = true;
                            if (caught) break;
                            var winner = info[2];
                            if (!ranAway)
                                BattleMessage?.Invoke((winner == "p1" && PlayerSide == 1) || (winner == "p2" && PlayerSide == 2)
                                    ? "You have won the battle!" : "You have lost the battle!");                         
                            break;
                        case "nothing":
                            BattleMessage?.Invoke("But nothing happened!");
                            break;
                        case "-notarget":
                            BattleMessage?.Invoke("But it failed!");
                            break;
                        case "-ohko":
                            BattleMessage?.Invoke("It's a one-hit KO!");
                            break;
                        case "--catch":
                            var isMySide = Convert.ToInt32(info[2].Substring(0, 2).Replace("p", "")) == PlayerSide;
                            if (isMySide)
                            {
                                BattleMessage?.Invoke($"{info[8]} threw a " +
                                    $"{ItemsManager.Instance.ItemClass.items.ToList().Find(itm => itm.BattleID == info[4] || itm.Name == info[4]).Name}!");
                            }
                            else
                            {
                                var enemyName = PlayerSide == 1 && Data.Mapping2 != null ? 
                                    Data.Mapping2[0] : Data.Mapping1 != null && 
                                    Data.Mapping2 != null ? Data.Mapping1[0] : "Enemy"; 

                                BattleMessage?.Invoke($"{info[8]} threw a " +
                                    $"{ItemsManager.Instance.ItemClass.items.ToList().Find(itm => itm.BattleID == info[4] || itm.Name == info[4]).Name}!");                                
                            }
                            int pokeID = Convert.ToInt32(info[3]);
                            int success = Convert.ToInt32(info[7]);
                            int shakes = Convert.ToInt32(info[5]);

                            if (shakes < 0)
                            {
                                BattleMessage?.Invoke("But it failed.");
                            }
                            else
                            {
                                if (success == 0)
                                {
                                    BattleMessage?.Invoke($"Gotcha! {PokemonManager.Instance.Names[OpponentId]} was caught!");
                                }
                                else if (shakes == 0)
                                {
                                    BattleMessage?.Invoke($"Oh no! The Pokémon broke free!");
                                }
                                else if (shakes < 2)
                                {
                                    BattleMessage?.Invoke("Aww! It appeared to be caught!");
                                }
                                else
                                {
                                    BattleMessage?.Invoke("Aargh! Almost had it!");
                                }
                            }
                            break;
                    }
                }
            }
        }

        public static SwitchedPokemon GetSwitchedPokemon(string text, string hpstatus)
        {
            SwitchedPokemon switchPkmn = new SwitchedPokemon();
            switchPkmn.Shiny = text.ToLowerInvariant().Contains("shiny");

            string[] array = text.Split(new string[]
            {
            ", "
            }, StringSplitOptions.None);
            foreach (string text2 in array)
            {
                if (text2 == "F")
                {
                    switchPkmn.Gender = "F";
                }
                if (text2 == "M")
                {
                    switchPkmn.Gender = "M";
                }
                if (text2 != array[0] && text2.Length > 1 && text2.Substring(0, 1) == "L")
                {
                    switchPkmn.Level = Convert.ToInt32(text2.Substring(1));
                }
            }
            if (array[0].ToLower().Contains("-mega-x"))
            {
                switchPkmn.Forme = "-mega-x";
                array[0].Replace("-mega-x", "");
            }
            else if (array[0].ToLower().Contains("-mega-y"))
            {
                switchPkmn.Forme = "-mega-y";
                array[0].Replace("-mega-y", "");
            }
            else if (array[0].ToLower().Contains("-mega"))
            {
                switchPkmn.Forme = "-mega";
                array[0].Replace("-mega", "");
            }
            else if (array[0].ToLower().Contains("-primal"))
            {
                switchPkmn.Forme = "-primal";
                array[0].Replace("-primal", "");
            }
            else if (array[0].ToLowerInvariant().Contains("mimikyubusted"))
            {
                switchPkmn.Forme = "mimikyubusted";
                array[0].Replace("mimikyubusted", "");
            }
            else if (array[0].ToLowerInvariant().Contains("wishiwashischool"))
            {
                switchPkmn.Forme = "wishiwashischool";
                array[0].Replace("wishiwashischool", "");
            }
            switchPkmn.ID = PokemonManager.Instance.GetIdByName(array[0]);
            if (switchPkmn.Level == 0)
            {
                switchPkmn.Level = 100;
            }
            if (hpstatus == "")
            {
                hpstatus = "50/50";
            }
            array = hpstatus.Split(new string[]
            {
            " "
            }, StringSplitOptions.None);
            string[] array3 = array[0].Split(new string[]
            {
            "/"
            }, StringSplitOptions.None);
            if (array3.Length == 1)
            {
                switchPkmn.Health = Convert.ToInt32(Regex.Replace(array3[0], "[^0-9.-]", string.Empty));
                switchPkmn.MaxHealth = 100;
            }
            else
            {
                switchPkmn.Health = Convert.ToInt32(Regex.Replace(array3[0], "[^0-9.-]", string.Empty));
                switchPkmn.MaxHealth = Convert.ToInt32(Regex.Replace(array3[1], "[^0-9.-]", string.Empty));
            }
            if (array.Length > 1)
            {
                switchPkmn.Status = string.IsNullOrEmpty(array[1]) ? "OK" : array[1];
            }
            return switchPkmn;
        }

        public static SwitchedPokemon GetSwitchedPokemon(PSXAPI.Response.Payload.BattlePokemon side)
        {
            var switchedPoke = GetSwitchedPokemon(side.details, side.condition);
            switchedPoke.Moves = side.moveData;
            switchedPoke.Personality = side.personality;
            switchedPoke.Trainer = side.trainer;
            return switchedPoke;
        }
    }
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
    }
}
