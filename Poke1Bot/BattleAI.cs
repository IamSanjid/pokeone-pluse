using Poke1Protocol;
using System.Collections.Generic;
using System.Linq;

namespace Poke1Bot
{
    public class BattleAI
    {
        private const int DoubleEdge = 38;
        private const int DragonRage = 82;
        private const int DreamEater = 138;
        private const int Explosion = 153;
        private const int FalseSwipe = 206;
        private const int NightShade = 101;
        private const int Psywave = 149;
        private const int SeismicToss = 69;
        private const int Selfdestruct = 120;
        private const int Synchronoise = 485;

        private readonly GameClient _client;

        public bool IsBusy { get; private set; }

        public BattleAI(GameClient client)
        {
            _client = client;
            _client.BattleUpdated += Client_BattleUpdated;
        }

        private void Client_BattleUpdated()
        {
            IsBusy = false;
        }

        public int UsablePokemonsCount
        {
            get
            {
                int usablePokemons = 0;
                foreach (Pokemon pokemon in _client.Team)
                {
                    if (IsPokemonUsable(pokemon))
                    {
                        usablePokemons += 1;
                    }
                }
                return usablePokemons;
            }
        }

        public Pokemon ActivePokemon => _client.Team[_client.ActiveBattle.SelectedPokemonIndex];
        public SwitchedPokemon[] ActivePokemons => _client.ActiveBattle.PlayerAcivePokemon != null ? _client.ActiveBattle.PlayerAcivePokemon.ToArray() : null;
        public SwitchedPokemon[] ActiveOpponentPokemons => _client.ActiveBattle.OpponentActivePokemon != null ? _client.ActiveBattle.OpponentActivePokemon.ToArray() : null;
        public PSXAPI.Response.Payload.BattleSide Side => _client.ActiveBattle.PlayerBattleSide;

        public bool UseMandatoryAction()
        {
            return RepeatAttack();
        }

        public bool Attack()
        {
            if (!IsPokemonUsable(ActivePokemon)) return false;
            if (ActivePokemons != null && ActiveOpponentPokemons != null)
            {
                if (ActiveOpponentPokemons.Length > 1)
                {
                    // double battle...
                    var result = false;
                    var i = 0;
                    foreach(var poke in ActivePokemons)
                    {
                        if (poke.Health > 0)
                            result = UseAttack(true, _client.Team.FindIndex(p => p.PokemonData.Pokemon.Payload.Personality == poke.Personality));
                        else
                            _client.UseAttack(0, i + 1);
                        i++;
                    }
                    return result;
                }
            }
            return UseAttack(true);
        }

        public bool WeakAttack()
        {
            if (!IsPokemonUsable(ActivePokemon)) return false;
            if (ActivePokemons != null && ActiveOpponentPokemons != null)
            {
                if (ActiveOpponentPokemons.Length > 1)
                {
                    // double battle...
                    var result = false;
                    foreach (var poke in ActivePokemons)
                    {
                        result = UseAttack(false, _client.Team.FindIndex(p => p.PokemonData.Pokemon.Payload.Personality == poke.Personality));
                    }
                    return result;
                }
            }
            return UseAttack(false);
        }

        public bool SendPokemon(int index, int changeWith = 0)
        {
            if (_client.ActiveBattle.IsTrapped) return false;
            if (index < 1 || index > _client.Team.Count) return false;
            Pokemon pokemon = _client.Team[index - 1];
            if (pokemon.BattleCurrentHealth > 0)
            {
                var uid = Side.pokemon.ToList().FindIndex(x => x.personality == pokemon.PokemonData.Pokemon.Payload.Personality) + 1;
                if (ActivePokemons != null && ActivePokemons.Length > 1 && changeWith > 0)
                {
                    if (ActivePokemons.Any(p => p.Personality == pokemon.PokemonData.Pokemon.Payload.Personality))
                        return false;
                    return _client.ChangePokemon(uid, changeWith);
                }
                else
                {
                    if (pokemon != ActivePokemon)
                        return _client.ChangePokemon(uid);
                }
            }
            return false;
        }

        public bool SendUsablePokemon(int changeWith = 0)
        {
            if (_client.ActiveBattle.IsTrapped) return false;
            if (Side != null)
            {
                for (int i = 0; i < Side.pokemon.Length; ++i)
                {
                    var pokemon = Battle.GetSwitchedPokemon(Side.pokemon[i]);
                    if (pokemon.Trainer.ToLowerInvariant() == _client.PlayerName.ToLowerInvariant())
                    {
                        if (IsPokemonUsable(pokemon) && !Side.pokemon[i].active)
                        {
                            return _client.ChangePokemon(i + 1, changeWith);
                        }
                    }
                }
            }
            foreach (Pokemon pokemon in _client.Team)
            {
                if (IsPokemonUsable(pokemon))
                {
                    var uid = Side.pokemon.ToList().FindIndex(x => x.personality == pokemon.PokemonData.Pokemon.Payload.Personality) + 1;
                    if (ActivePokemons != null && ActivePokemons.Length > 1)
                    {
                        if (ActivePokemons.Any(p => p.Personality == pokemon.PokemonData.Pokemon.Payload.Personality))
                            continue;
                        if (changeWith > 0)
                            _client.ChangePokemon(uid, changeWith);
                        else
                        {
                            changeWith = ActivePokemons.ToList().FindIndex(x => x.Health <= 1 || x.Health != x.MaxHealth) < 0 ? 1 
                                : ActivePokemons.ToList().FindIndex(x => x.Health <= 1 || x.Health != x.MaxHealth);
                            return _client.ChangePokemon(uid, changeWith + 1);
                        }
                    }
                    else
                    {
                        if (ActivePokemon == pokemon)
                            continue;
                        return _client.ChangePokemon(uid, changeWith);
                    }
                }
            }
            return false;
        }

        public bool SendAnyPokemon(int changeWith = 0)
        {
            if (_client.ActiveBattle.IsTrapped) return false;
            if (Side != null)
            {
                for (int i = 0; i < Side.pokemon.Length; ++i)
                {
                    var p1 = Battle.GetSwitchedPokemon(Side.pokemon[i]);
                    if (p1.Trainer.ToLowerInvariant() == _client.PlayerName.ToLowerInvariant())
                    {
                        if (IsPokemonUsable(p1) && !Side.pokemon[i].active)
                        {
                            return _client.ChangePokemon(i + 1);
                        }
                    }
                }
            }
            Pokemon pokemon = _client.Team.FirstOrDefault(p => p != ActivePokemon && p.BattleCurrentHealth > 0);
            if (pokemon != null)
            {
                var uid = Side.pokemon.ToList().FindIndex(x => x.personality == pokemon.PokemonData.Pokemon.Payload.Personality) + 1;
                if (ActivePokemons != null && ActivePokemons.Length > 1)
                {
                    if (changeWith >= 0)
                        _client.ChangePokemon(uid, changeWith);
                    else
                    {
                        return _client.ChangePokemon(uid, changeWith);
                    }
                }
                else
                {
                    return _client.ChangePokemon(uid);
                }
            }
            return false;
        }

        public bool Run()
        {
            if (ActivePokemon.BattleCurrentHealth == 0) return false;
            if (!_client.ActiveBattle.IsWild) return false;
            if (_client.ActiveBattle.IsTrapped) return false;
            _client.RunFromBattle();
            return true;
        }

        public bool UseMove(string moveName)
        {
            if (ActivePokemon.BattleCurrentHealth == 0) return false;

            moveName = moveName.ToUpperInvariant();
            for (int i = 0; i < ActivePokemon.Moves.Length; ++i)
            {
                PokemonMove move = ActivePokemon.Moves[i];
                if (move.CurrentPoints > 0)
                {
                    MovesManager.MoveData moveData = MovesManager.Instance.GetMoveData(move.Id);
                    if (moveData.Name.ToUpperInvariant() == moveName)
                    {
                        _client.UseAttack(i + 1);
                        return true;
                    }
                }
            }
            return false;
        }
        public bool UseAnyMove()
        {
            if (ActivePokemon.BattleCurrentHealth == 0) return false;

            for (int i = 0; i < ActivePokemon.Moves.Length; ++i)
            {
                PokemonMove move = ActivePokemon.Moves[i];
                if (move.CurrentPoints > 0)
                {
                    _client.UseAttack(i + 1);
                    return true;
                }
            }

            // Struggle
            _client.UseAttack(1);
            return true;
        }
        public bool UseItem(int itemId, int pokemonUid = 0)
        {
            if (ActivePokemon.BattleCurrentHealth == 0) return false;
            _client.UseItem(itemId, pokemonUid);
            return true;
        }

        public bool UseItemOnMove(int itemId, int pokemonUid = 0, int moveId = 0)
        {
            if (ActivePokemon.BattleCurrentHealth == 0) return false;
            _client.UseItem(itemId, pokemonUid, moveId);
            return true;
        }

        private bool RepeatAttack()
        {
            if (ActiveOpponentPokemons != null && ActiveOpponentPokemons.Length > 1)
            {
                var repeats = ActivePokemons.ToList().FindAll(x => x.Moves.Length == 1);
                if (repeats.Count > 0)
                {
                    for (int i = 0; i < ActivePokemons.Length; ++i)
                    {
                        if (ActivePokemons[i].Moves.Length == 1)
                        {
                            _client.UseAttack(1, i + 1);
                            _client.ActiveBattle.RepeatAttack = false;
                            return true;
                        }
                    }
                }
            }
            if (ActivePokemon.BattleCurrentHealth > 0 && _client.ActiveBattle.RepeatAttack)
            {
                _client.UseAttack(1);
                _client.ActiveBattle.RepeatAttack = false;
                return true;
            }
            return false;
        }

        private bool UseAttack(bool useBestAttack, int activePoke = 0)
        {
            PokemonMove bestMove = null;
            int bestIndex = 0;
            double bestPower = 0;

            PokemonMove worstMove = null;
            int worstIndex = 0;
            double worstPower = 0;

            int opponentIndex = 0;

            if (activePoke < 0)
                return false;

            for (int j = 0; j < ActiveOpponentPokemons.Length; ++j)
            {
                if (ActiveOpponentPokemons[j].Health <= 0)
                    continue;
                var activePokemon = _client.Team[activePoke];
                for (int i = 0; i < activePokemon.Moves.Length; ++i)
                {
                    PokemonMove move = activePokemon.Moves[i];
                    if (move.CurrentPoints == 0)
                        continue;

                    MovesManager.MoveData moveData = MovesManager.Instance.GetMoveData(move.Id);

                    if (move.Id + 1 == DreamEater && ActiveOpponentPokemons[j].Status != "slp")
                    {
                        continue;
                    }

                    if (move.Id + 1 == Explosion || move.Id + 1 == Selfdestruct ||
                        (move.Id + 1 == DoubleEdge && activePokemon.BattleCurrentHealth < ActiveOpponentPokemons[j].Health / 3))
                    {
                        continue;
                    }

                    if (!IsMoveOffensive(move, moveData))
                        continue;

                    PokemonType attackType = PokemonTypeExtensions.FromName(moveData.Type);

                    PokemonType playerType1 = TypesManager.Instance.Type1[activePokemon.Id];
                    PokemonType playerType2 = TypesManager.Instance.Type2[activePokemon.Id];

                    PokemonType opponentType1 = TypesManager.Instance.Type1[ActiveOpponentPokemons[j].ID];
                    PokemonType opponentType2 = TypesManager.Instance.Type2[ActiveOpponentPokemons[j].ID];

                    double accuracy = (moveData.Accuracy < 0 ? 101.0 : moveData.Accuracy);

                    double power = moveData.RealPower * accuracy;

                    if (attackType == playerType1 || attackType == playerType2)
                    {
                        power *= 1.5;
                    }

                    power *= TypesManager.Instance.GetMultiplier(attackType, opponentType1);
                    power *= TypesManager.Instance.GetMultiplier(attackType, opponentType2);

                    if (attackType == PokemonType.Ground && PokemonManager.Instance.IsLavitatingPokemon(ActiveOpponentPokemons[j].ID))
                    {
                        power = 0;
                    }

                    power = ApplySpecialEffects(move, power);

                    if (move.Id + 1 == Synchronoise)
                    {
                        if (playerType1 != opponentType1 && playerType1 != opponentType2 &&
                            (playerType2 == PokemonType.None || playerType2 != opponentType1) &&
                            (playerType2 == PokemonType.None || playerType2 != opponentType2))
                        {
                            power = 0;
                        }
                    }

                    if (power < 0.01)
                        continue;

                    if (bestMove == null || power > bestPower)
                    {
                        bestMove = move;
                        bestPower = power;
                        bestIndex = i;
                    }

                    if (worstMove == null || power < worstPower)
                    {
                        worstMove = move;
                        worstPower = power;
                        worstIndex = i;
                    }
                }
                opponentIndex = j;
            }
            if (ActivePokemons.Length <= 1)
                IsBusy = true;

            activePoke = ActivePokemons.ToList().FindIndex(p => p.Personality == _client.Team[activePoke].PokemonData.Pokemon.Payload.Personality);

            if (useBestAttack && bestMove != null)
            {
                _client.UseAttack(bestIndex + 1, activePoke + 1, opponentIndex + 1);
                return true;
            }
            if (!useBestAttack && worstMove != null)
            {
                _client.UseAttack(worstIndex + 1, activePoke + 1, opponentIndex + 1);
                return true;
            }

            return false;
        }

        private bool UseAttack(bool useBestAttack)
        {
            PokemonMove bestMove = null;
            int bestIndex = 0;
            double bestPower = 0;

            PokemonMove worstMove = null;
            int worstIndex = 0;
            double worstPower = 0;

            for (int i = 0; i < ActivePokemon.Moves.Length; ++i)
            {
                PokemonMove move = ActivePokemon.Moves[i];
                if (move.CurrentPoints == 0)
                    continue;

                MovesManager.MoveData moveData = MovesManager.Instance.GetMoveData(move.Id);

                if (move.Id + 1 == DreamEater && _client.ActiveBattle.OpponentStatus != "slp")
                {
                    continue;
                }

                if (move.Id + 1 == Explosion || move.Id + 1 == Selfdestruct ||
                    (move.Id + 1 == DoubleEdge && ActivePokemon.BattleCurrentHealth < _client.ActiveBattle.CurrentHealth / 3))
                {
                    continue;
                }

                if (!IsMoveOffensive(move, moveData))
                    continue;

                PokemonType attackType = PokemonTypeExtensions.FromName(moveData.Type);

                PokemonType playerType1 = TypesManager.Instance.Type1[ActivePokemon.Id];
                PokemonType playerType2 = TypesManager.Instance.Type2[ActivePokemon.Id];

                PokemonType opponentType1 = TypesManager.Instance.Type1[_client.ActiveBattle.OpponentId];
                PokemonType opponentType2 = TypesManager.Instance.Type2[_client.ActiveBattle.OpponentId];

                double accuracy = (moveData.Accuracy < 0 ? 101.0 : moveData.Accuracy);

                double power = moveData.RealPower * accuracy;

                if (attackType == playerType1 || attackType == playerType2)
                {
                    power *= 1.5;
                }

                power *= TypesManager.Instance.GetMultiplier(attackType, opponentType1);
                power *= TypesManager.Instance.GetMultiplier(attackType, opponentType2);

                if (attackType == PokemonType.Ground && PokemonManager.Instance.IsLavitatingPokemon(_client.ActiveBattle.OpponentId))
                {
                    power = 0;
                }

                power = ApplySpecialEffects(move, power);

                if (move.Id + 1 == Synchronoise)
                {
                    if (playerType1 != opponentType1 && playerType1 != opponentType2 &&
                        (playerType2 == PokemonType.None || playerType2 != opponentType1) &&
                        (playerType2 == PokemonType.None || playerType2 != opponentType2))
                    {
                        power = 0;
                    }
                }

                if (power < 0.01)
                    continue;

                if (bestMove == null || power > bestPower)
                {
                    bestMove = move;
                    bestPower = power;
                    bestIndex = i;
                }

                if (worstMove == null || power < worstPower)
                {
                    worstMove = move;
                    worstPower = power;
                    worstIndex = i;
                }
            }

            if (useBestAttack && bestMove != null)
            {
                _client.UseAttack(bestIndex + 1);
                return true;
            }
            if (!useBestAttack && worstMove != null)
            {
                _client.UseAttack(worstIndex + 1);
                return true;
            }
            return false;
        }

        public bool IsPokemonUsable(Pokemon pokemon)
        {
            if ((pokemon.CurrentHealth > 0 && !_client.IsInBattle) || (pokemon.BattleCurrentHealth > 0 && _client.IsInBattle))
            {
                foreach (PokemonMove move in pokemon.Moves)
                {
                    MovesManager.MoveData moveData = MovesManager.Instance.GetMoveData(move.Id);
                    if (move.CurrentPoints > 0 && IsMoveOffensive(move, moveData) &&
                        move.Id + 1 != DreamEater && move.Id + 1 != Synchronoise && move.Id + 1 != DoubleEdge)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsPokemonUsable(SwitchedPokemon pokemon)
        {
            if (pokemon.Health > 0 && _client.IsInBattle)
            {
                foreach (var move in pokemon.Moves)
                {
                    MovesManager.MoveData moveData = MovesManager.Instance.GetMoveData(move.id);
                    if (move.pp > 0 && IsMoveOffensive(moveData) && !move.disabled &&
                        moveData.ID != DreamEater && moveData.ID != Synchronoise && moveData.ID != DoubleEdge)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool IsMoveOffensive(PokemonMove move, MovesManager.MoveData moveData)
        {
            if (move.Data is null is false)
                return moveData.RealPower > 0 || move.Data.ID == DragonRage || move.Data.ID == SeismicToss || move.Data.ID == NightShade || move.Data.ID == Psywave;
            return moveData.RealPower > 0 || move.Id + 1 == DragonRage || move.Id + 1 == SeismicToss || move.Id + 1 == NightShade || move.Id + 1 == Psywave;
        }

        private bool IsMoveOffensive(MovesManager.MoveData moveData)
        {
            return moveData != null && (moveData.RealPower > 0 || moveData.ID == DragonRage || moveData.ID == SeismicToss || moveData.ID == NightShade || moveData.ID == Psywave);
        }

        private double ApplySpecialEffects(PokemonMove move, double power)
        {
            if (move.Id + 1 == DragonRage)
            {
                return _client.ActiveBattle.CurrentHealth <= 40 ? 10000.0 : 1.0;
            }

            if (move.Id + 1 == SeismicToss || move.Id + 1 == NightShade)
            {
                return _client.ActiveBattle.CurrentHealth <= ActivePokemon.Level ? 10000.0 : 1.0;
            }

            if (move.Id + 1 == Psywave)
            {
                return _client.ActiveBattle.CurrentHealth <= (ActivePokemon.Level / 2) ? 10000.0 : 1.0;
            }

            if (move.Id + 1 == FalseSwipe)
            {
                return 0.1;
            }

            return power;
        }
    }
}
