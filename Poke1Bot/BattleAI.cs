using Poke1Protocol;
using System.Collections.Generic;
using System.Linq;

namespace Poke1Bot
{
    public class BattleAI
    {
        private enum ResultUsingMove
        {
            None,
            Fainted,
            NoLongerUsable,
            Success
        }

        private enum SwitchingResult
        {
            None,
            Success,
            NotPossible
        }

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
        public List<SwitchedPokemon> Pokemons => _client.ActiveBattle.PlayerAllPokemon;

        public bool UseMandatoryAction()
        {
            return RepeatAttack();
        }

        public bool Attack()
        {
            if (Side is null) return false;
            var req = _client.ActiveBattle.PlayerRequest;
            if (req.forceSwitch != null && req.forceSwitch.Length > 0)
                return false;
            if (ActivePokemons != null && ActiveOpponentPokemons != null)
            {
                if (ActiveOpponentPokemons.Length > 0)
                {
                    var results = new List<ResultUsingMove>();

                    for (int i = 0; i < ActivePokemons.Length; ++i)
                    {
                        for (int j = 0; j < ActiveOpponentPokemons.Length; ++j)
                        {
                            var result = UseAttack(true, i, j);
                            if (result == ResultUsingMove.Success)
                                results.Add(result);
                            else if (j == ActiveOpponentPokemons.Length - 1)
                                results.Add(result);
                        }
                    }

                    return results.All(r => r == ResultUsingMove.Success);
                }
            }
            return false;
        }

        public bool WeakAttack()
        {
            if (Side is null) return false;
            var req = _client.ActiveBattle.PlayerRequest;
            if (req.forceSwitch != null && req.forceSwitch.Length > 0)
                return false;
            if (ActivePokemons != null && ActiveOpponentPokemons != null)
            {
                if (ActiveOpponentPokemons.Length > 0)
                {
                    var results = new List<ResultUsingMove>();

                    for (int i = 0; i < ActivePokemons.Length; ++i)
                    {
                        for (int j = 0; j < ActiveOpponentPokemons.Length; ++j)
                        {
                            var result = UseAttack(false, i, j);
                            if (result == ResultUsingMove.Success)
                                results.Add(result);
                            else if (j == ActiveOpponentPokemons.Length - 1)
                                results.Add(result);
                        }
                    }

                    return results.All(r => r == ResultUsingMove.Success);
                }
            }
            return false;
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
            var req = _client.ActiveBattle.PlayerRequest;
            var results = new List<SwitchingResult>();

            if (req is null || _client.ActiveBattle.IsTrapped) return false;

            if (req.forceSwitch?.Length > 0)
            {
                var switchAblePokes = req.side.pokemon.Count(IsPokemonUsable);
                var switched = 0;

                for (var j = 0; j < req.forceSwitch.Length; ++j)
                {
                    if (req.forceSwitch[j] && switched < switchAblePokes)
                    {
                        for (var i = 0; i < Side.pokemon.Length; ++i)
                        {
                            var pokemon = Battle.GetSwitchedPokemon(Side.pokemon[i]);

                            if (!pokemon.Sent && IsPokemonUsable(pokemon))
                            {
                                switched++;
                                var result = _client.ChangePokemon(i + 1, j + 1);
                                pokemon.Sent = true;
                                results.Add(result ? SwitchingResult.Success : SwitchingResult.NotPossible);
                                if (switched >= req.forceSwitch.Count(t => t))
                                    break;
                            }
                            else if (i == Side.pokemon.Length - 1)
                                results.Add(SwitchingResult.NotPossible);
                        }
                        continue;
                    }

                    if (req.side.pokemon.Count(p => Battle.GetSwitchedPokemon(p).Health > 0) > 0)
                    {
                        results.Add(SwitchingResult.NotPossible);
                        continue;
                    }
                    _client.UseAttack(0, j + 1, 0);
                    results.Add(SwitchingResult.Success);
                }
            }
            else
            {
                var needToChange = ActivePokemons.Count(p => !IsPokemonUsable(p));
                if (needToChange == 0)
                {
                    needToChange = ActivePokemons.Length;
                }
                var switched = 0;
                for (var j = 0; j < ActivePokemons.Length; ++j)
                {
                    var active = ActivePokemons[j];
                    if (!IsPokemonUsable(active))
                    {
                        changeWith = j + 1;
                    }

                    for (var i = 0; i < Side.pokemon.Length; ++i)
                    {
                        var pokemon = Battle.GetSwitchedPokemon(Side.pokemon[i]);
                        if (!pokemon.Sent && IsPokemonUsable(pokemon))
                        {
                            switched++;
                            if (switched <= needToChange)
                            {
                                var result = _client.ChangePokemon(i + 1, changeWith);
                                results.Add(result ? SwitchingResult.Success : SwitchingResult.NotPossible);
                                continue;
                            }
                            _client.UseAttack(0, j + 1, 0);
                            results.Add(SwitchingResult.Success);
                        }
                        else if (i == Side.pokemon.Length - 1)
                            results.Add(SwitchingResult.NotPossible);
                    }
                }
            }

            return results.All(r => r == SwitchingResult.Success);
        }

        public bool SendAnyPokemon(int changeWith = 0)
        {
            var req = _client.ActiveBattle.PlayerRequest;
            var results = new List<SwitchingResult>();

            if (req is null || _client.ActiveBattle.IsTrapped) return false;

            if (req.forceSwitch != null && req.forceSwitch.Length > 0)
            {
                var switchAblePokes = req.side.pokemon.ToList().FindAll(p => Battle.GetSwitchedPokemon(p).Health > 0).Count;
                var switched = 0;

                for (int j = 0; j < req.forceSwitch.Length; ++j)
                {
                    if (req.forceSwitch[j])
                    {
                        if (switched < switchAblePokes)
                        {
                            for (int i = 0; i < Side.pokemon.Length; ++i)
                            {
                                var pokemon = Battle.GetSwitchedPokemon(Side.pokemon[i]);

                                if (!pokemon.Sent && pokemon.Health > 0)
                                {
                                    switched++;
                                    var result = _client.ChangePokemon(i + 1, j + 1);
                                    pokemon.Sent = true;
                                    results.Add(result ? SwitchingResult.Success : SwitchingResult.NotPossible);
                                    if (switched >= req.forceSwitch.ToList().FindAll(t => t).Count)
                                        break;
                                }
                                else if (i == Side.pokemon.Length - 1)
                                    results.Add(SwitchingResult.NotPossible);
                            }
                            continue;
                        }
                    }
                    _client.UseAttack(0, j + 1, 0);
                    results.Add(SwitchingResult.Success);
                }
            }
            else
            {
                var needToChange = ActivePokemons.Count(p => !IsPokemonUsable(p));
                if (needToChange == 0)
                {
                    needToChange = ActivePokemons.Length;
                }
                var switched = 0;
                for (int j = 0; j < ActivePokemons.Length; ++j)
                {
                    var active = ActivePokemons[j];
                    if (!IsPokemonUsable(active))
                    {
                        changeWith = j + 1;
                    }

                    for (int i = 0; i < Side.pokemon.Length; ++i)
                    {
                        var pokemon = Battle.GetSwitchedPokemon(Side.pokemon[i]);
                        if (!pokemon.Sent && pokemon.Health > 0)
                        {
                            switched++;
                            if (switched <= needToChange)
                            {
                                var result = _client.ChangePokemon(i + 1, changeWith);
                                results.Add(result ? SwitchingResult.Success : SwitchingResult.NotPossible);
                                continue;
                            }
                            _client.UseAttack(0, j + 1, 0);
                            results.Add(SwitchingResult.Success);
                        }
                        else if (i == Side.pokemon.Length - 1)
                            results.Add(SwitchingResult.NotPossible);
                    }
                }
            }

            return results.All(r => r == SwitchingResult.Success);
        }

        public bool Run()
        {
            if (ActivePokemons is null || ActivePokemons?.All(p => p.Health <= 0) == true) return false;
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
                var activePoke = ActivePokemons.FirstOrDefault(x => x.Personality == ActivePokemon.PokemonData.Pokemon.Payload.Personality);
                var move = activePoke.Moves[i];
                if (move.pp > 0 && !move.disabled)
                {
                    var moveData = MovesManager.Instance.GetMoveData(move.id);
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
            if (ActivePokemons is null) return false;
            var result = false;
            for (int i = 0; i < ActivePokemons.Length; ++i)
            {
                var opponentIndex = _client.Rand.Next(0, ActiveOpponentPokemons.Length - 1);
                for (int j = 0; j < ActivePokemons[i].Moves.Length; ++j)
                {
                    var move = ActivePokemons[i].Moves[j];
                    if (move.pp > 0 && !move.disabled)
                    {
                        _client.UseAttack(j + 1, i + 1, ActiveOpponentPokemons.Length == 1 ? opponentIndex : opponentIndex + 1);
                        result = true;
                    }
                }
                // Struggle
                if (!result)
                {
                    _client.UseAttack(1, i + 1, ActiveOpponentPokemons.Length == 1 ? opponentIndex : opponentIndex + 1);
                    result = true;
                }
            }

            return result;
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
            if (ActiveOpponentPokemons is null || ActiveOpponentPokemons.Length <= 0) return false;
            if (ActivePokemons is null || ActivePokemon.BattleCurrentHealth <= 0 || !_client.ActiveBattle.RepeatAttack) return false;

            for (var i = 0; i < ActivePokemons.Length; ++i)
            {
                if (!ActivePokemons[i].RepeatAttack || ActivePokemons[i].Health <= 0) continue;
                _client.UseAttack(1, i + 1);
                _client.ActiveBattle.RepeatAttack = false;
            }
            _client.ActiveBattle.RepeatAttack = false;
            return true;
        }

        private ResultUsingMove UseAttack(bool useBestAttack, int activePoke, int oppenentPoke)
        {
            var activePokemon = ActivePokemons[activePoke];
            if (!IsPokemonUsable(activePokemon))
            {
                if (Pokemons.FindAll(x => !x.Sent && x.Health > 0).Count > 0) return ResultUsingMove.Fainted;
                if (activePokemon.Health > 0) return ResultUsingMove.NoLongerUsable;
                _client.UseAttack(0, activePoke + 1);
                return ResultUsingMove.Success;
            }

            PSXAPI.Response.Payload.BattleMove bestMove = null;
            var bestIndex = 0;
            double bestPower = 0;

            PSXAPI.Response.Payload.BattleMove worstMove = null;
            var worstIndex = 0;
            double worstPower = 0;

            if (activePoke < 0)
                return ResultUsingMove.NoLongerUsable;

            for (int i = 0; i < activePokemon.Moves.Length; i++)
            {
                var move = activePokemon.Moves[i];
                if (move is null)
                    continue;
                if (move.pp == 0 || move.disabled)
                    continue;

                var moveData = MovesManager.Instance.GetMoveData(move.id);

                if (moveData is null)
                    continue;

                if (moveData.ID == DreamEater && ActiveOpponentPokemons[oppenentPoke].Status != "slp")
                {
                    continue;
                }

                if (moveData.ID == Explosion || moveData.ID == Selfdestruct ||
                    (moveData.ID == DoubleEdge && activePokemon.Health < ActiveOpponentPokemons[oppenentPoke].Health / 3))
                {
                    continue;
                }

                if (!IsMoveOffensive(moveData))
                    continue;

                var attackType = PokemonTypeExtensions.FromName(moveData.Type);

                var playerType1 = TypesManager.Instance.Type1[activePokemon.ID];
                var playerType2 = TypesManager.Instance.Type2[activePokemon.ID];

                var opponentType1 = TypesManager.Instance.Type1[ActiveOpponentPokemons[oppenentPoke].ID];
                var opponentType2 = TypesManager.Instance.Type2[ActiveOpponentPokemons[oppenentPoke].ID];

                var accuracy = (moveData.Accuracy < 0 ? 101.0 : moveData.Accuracy);

                var power = moveData.RealPower * accuracy;

                if (attackType == playerType1 || attackType == playerType2)
                {
                    power *= 1.5;
                }

                power *= TypesManager.Instance.GetMultiplier(attackType, opponentType1);
                power *= TypesManager.Instance.GetMultiplier(attackType, opponentType2);

                if (attackType == PokemonType.Ground && PokemonManager.Instance.IsLavitatingPokemon(ActiveOpponentPokemons[oppenentPoke].ID))
                {
                    power = 0;
                }

                power = ApplySpecialEffects(moveData, ActiveOpponentPokemons[oppenentPoke], activePokemon, power);

                if (moveData.ID == Synchronoise)
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

            if (ActivePokemons.Length <= 1)
                IsBusy = true;

            if (useBestAttack && bestMove != null)
            {
                _client.UseAttack(bestIndex + 1, activePoke + 1, ActiveOpponentPokemons.Length == 1 ? oppenentPoke : oppenentPoke + 1);
                return ResultUsingMove.Success;
            }
            if (!useBestAttack && worstMove != null)
            {
                _client.UseAttack(worstIndex + 1, activePoke + 1, ActiveOpponentPokemons.Length == 1 ? oppenentPoke : oppenentPoke + 1);
                return ResultUsingMove.Success;
            }
            return ResultUsingMove.NoLongerUsable;
        }

        //private bool UseAttack(bool useBestAttack)
        //{
        //    PokemonMove bestMove = null;
        //    int bestIndex = 0;
        //    double bestPower = 0;

        //    PokemonMove worstMove = null;
        //    int worstIndex = 0;
        //    double worstPower = 0;

        //    for (int i = 0; i < ActivePokemon.Moves.Length; ++i)
        //    {
        //        PokemonMove move = ActivePokemon.Moves[i];
        //        if (move.CurrentPoints == 0)
        //            continue;

        //        MovesManager.MoveData moveData = MovesManager.Instance.GetMoveData(move.Id);

        //        if (move.Id + 1 == DreamEater && _client.ActiveBattle.OpponentStatus != "slp")
        //        {
        //            continue;
        //        }

        //        if (move.Id + 1 == Explosion || move.Id + 1 == Selfdestruct ||
        //            (move.Id + 1 == DoubleEdge && ActivePokemon.BattleCurrentHealth < _client.ActiveBattle.CurrentHealth / 3))
        //        {
        //            continue;
        //        }

        //        if (!IsMoveOffensive(move, moveData))
        //            continue;

        //        PokemonType attackType = PokemonTypeExtensions.FromName(moveData.Type);

        //        PokemonType playerType1 = TypesManager.Instance.Type1[ActivePokemon.Id];
        //        PokemonType playerType2 = TypesManager.Instance.Type2[ActivePokemon.Id];

        //        PokemonType opponentType1 = TypesManager.Instance.Type1[_client.ActiveBattle.OpponentId];
        //        PokemonType opponentType2 = TypesManager.Instance.Type2[_client.ActiveBattle.OpponentId];

        //        double accuracy = (moveData.Accuracy < 0 ? 101.0 : moveData.Accuracy);

        //        double power = moveData.RealPower * accuracy;

        //        if (attackType == playerType1 || attackType == playerType2)
        //        {
        //            power *= 1.5;
        //        }

        //        power *= TypesManager.Instance.GetMultiplier(attackType, opponentType1);
        //        power *= TypesManager.Instance.GetMultiplier(attackType, opponentType2);

        //        if (attackType == PokemonType.Ground && PokemonManager.Instance.IsLavitatingPokemon(_client.ActiveBattle.OpponentId))
        //        {
        //            power = 0;
        //        }

        //        power = ApplySpecialEffects(move, power);

        //        if (move.Id + 1 == Synchronoise)
        //        {
        //            if (playerType1 != opponentType1 && playerType1 != opponentType2 &&
        //                (playerType2 == PokemonType.None || playerType2 != opponentType1) &&
        //                (playerType2 == PokemonType.None || playerType2 != opponentType2))
        //            {
        //                power = 0;
        //            }
        //        }

        //        if (power < 0.01)
        //            continue;

        //        if (bestMove == null || power > bestPower)
        //        {
        //            bestMove = move;
        //            bestPower = power;
        //            bestIndex = i;
        //        }

        //        if (worstMove == null || power < worstPower)
        //        {
        //            worstMove = move;
        //            worstPower = power;
        //            worstIndex = i;
        //        }
        //    }

        //    if (useBestAttack && bestMove != null)
        //    {
        //        _client.UseAttack(bestIndex + 1);
        //        return true;
        //    }
        //    if (!useBestAttack && worstMove != null)
        //    {
        //        _client.UseAttack(worstIndex + 1);
        //        return true;
        //    }
        //    return false;
        //}

        public bool IsPokemonUsable(Pokemon pokemon)
        {
            if (_client.IsInBattle)
            {
                if (Side != null)
                {
                    var sidePoke = Side.pokemon.FirstOrDefault(p => p.personality == pokemon.Personality);
                    if (sidePoke != null)
                    {
                        var pok = Battle.GetSwitchedPokemon(sidePoke);
                        return IsPokemonUsable(pok);
                    }
                }
                return false;
            }
            if (pokemon.CurrentHealth > 0)
            {
                foreach (PokemonMove move in pokemon.Moves)
                {
                    var moveData = MovesManager.Instance.GetMoveData(move.Id) ?? MovesManager.Instance.GetMoveData(move.Name);
                    if (move.CurrentPoints > 0 && IsMoveOffensive(move, moveData)
                        && move.Id + 1 != DreamEater && move.Id + 1 != Synchronoise && move.Id + 1 != DoubleEdge)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsPokemonUsable(SwitchedPokemon pokemon)
        {
            if (pokemon.Health > 0)
            {
                foreach (var move in pokemon.Moves)
                {
                    var moveData = MovesManager.Instance.GetMoveData(move.id);
                    if (move.pp > 0 && IsMoveOffensive(moveData) && !move.disabled &&
                        moveData.ID != DreamEater && moveData.ID != Synchronoise && moveData.ID != DoubleEdge)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsPokemonUsable(PSXAPI.Response.Payload.BattlePokemon sidePoke)
        {
            var pokemon = Battle.GetSwitchedPokemon(sidePoke);
            if (pokemon.Health > 0)
            {
                foreach (var move in pokemon.Moves)
                {
                    var moveData = MovesManager.Instance.GetMoveData(move.id);
                    if (move.pp > 0 && IsMoveOffensive(moveData) && !move.disabled &&
                        moveData.ID != DreamEater && moveData.ID != Synchronoise && moveData.ID != DoubleEdge)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool IsMoveOffensive(PokemonMove move, MovesManager.MoveData moveData)
        {
            if (move.Data is null is false)
                return moveData.RealPower > 0 || move.Data.ID == DragonRage || move.Data.ID == SeismicToss || move.Data.ID == NightShade || move.Data.ID == Psywave;
            return moveData.RealPower > 0 || move.Id + 1 == DragonRage || move.Id + 1 == SeismicToss || move.Id + 1 == NightShade || move.Id + 1 == Psywave;
        }

        private static bool IsMoveOffensive(MovesManager.MoveData moveData)
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

        private double ApplySpecialEffects(MovesManager.MoveData move, SwitchedPokemon poke, SwitchedPokemon active, double power)
        {
            if (move.ID == DragonRage)
            {
                return poke.Health <= 40 ? 10000.0 : 1.0;
            }

            if (move.ID == SeismicToss || move.ID == NightShade)
            {
                return poke.Health <= active.Level ? 10000.0 : 1.0;
            }

            if (move.ID == Psywave)
            {
                return poke.Health <= (active.Level / 2) ? 10000.0 : 1.0;
            }

            if (move.ID == FalseSwipe)
            {
                return 0.1;
            }

            return power;
        }
    }
}
