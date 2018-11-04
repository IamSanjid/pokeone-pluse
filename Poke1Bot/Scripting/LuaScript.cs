using MoonSharp.Interpreter;
using Poke1Bot.Utils;
using Poke1Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poke1Bot.Scripting
{
    public class LuaScript : BaseScript
    {
        public BotClient Bot { get; private set; }

#if DEBUG
        public int TimeoutDelay = 60000;
#else
		public int TimeoutDelay = 3000;
#endif
        private Script _lua;
        private string _path;
        private string _content;
        private IList<string> _libsContent;
        private IDictionary<string, IList<DynValue>> _hookedFunctions;

        private bool _actionExecuted;

        public LuaScript(BotClient bot, string path, string content, IList<string> libsContent)
        {
            Bot = bot;
            _path = Path.GetDirectoryName(path);
            _content = content;
            _libsContent = libsContent;
        }

        public override async Task Initialize()
        {
            await CreateLuaInstance();
            Name = _lua.Globals.Get("name").CastToString();
            Author = _lua.Globals.Get("author").CastToString();
            Description = _lua.Globals.Get("description").CastToString();
        }

        public override void Start()
        {
            CallFunctionSafe("onStart");
        }

        public override void Stop()
        {
            CallFunctionSafe("onStop");
        }

        public override void Update()
        {
            if (_lua is null) return;
            CallFunctionSafe("onUpdate");
        }

        public override void Pause()
        {
            CallFunctionSafe("onPause");
        }

        public override void Resume()
        {
            CallFunctionSafe("onResume");
        }

        public override void OnBattleMessage(string message)
        {
            CallFunctionSafe("onBattleMessage", message);
        }

        public override void OnSystemMessage(string message)
        {
            CallFunctionSafe("onSystemMessage", message);
        }

        public override void OnDialogMessage(string message)
        {
            CallFunctionSafe("onDialogMessage", message);
        }

        public override void OnLearningMove(string moveName, int pokemonIndex)
        {
            CallFunctionSafe("onLearningMove", moveName, pokemonIndex);
        }

        public override void OnQuestUpdated(string name, string type, string questDescription)
        {
            CallFunctionSafe("onQuestUpdated", name, type, questDescription);
        }

        public override void OnWarningMessage(bool isDifferentMap, int distance = -1)
        {
            CallFunctionSafe("onWarningMessage", isDifferentMap, distance);
        }

        public override void OnMovementLag(int distance)
        {
            CallFunctionSafe("onMovementLag", distance);
        }

        public override bool ExecuteNextAction()
        {
            string functionName = Bot.Game.IsInBattle ? "onBattleAction" : "onPathAction";

            _actionExecuted = false;
            try
            {
                CallFunction(functionName, true);
            }
            catch (ScriptRuntimeException ex)
            {
                throw new Exception(ex.DecoratedMessage, ex);
            }
            return _actionExecuted;
        }

        private async Task CreateLuaInstance()
        {
            _hookedFunctions = new Dictionary<string, IList<DynValue>>();

            _lua = new Script(CoreModules.Preset_SoftSandbox | CoreModules.LoadMethods)
            {
                Options =
                {
                    ScriptLoader = new CustomScriptLoader(_path) {ModulePaths = new[] {"?.lua"}},
                    CheckThreadAccess = false
                }
            };

            await Task.Run(() =>
            {
                _lua.Globals["log"] = new Action<string>(Log);
                _lua.Globals["fatal"] = new Action<string>(Fatal);
                _lua.Globals["logout"] = new Action<string>(Logout);
                _lua.Globals["stringContains"] = new Func<string, string, bool>(StringContains);
                _lua.Globals["playSound"] = new Action<string>(PlaySound);
                _lua.Globals["registerHook"] = new Action<string, DynValue>(RegisterHook);

                // general condition
                _lua.Globals["getPlayerX"] = new Func<int>(GetPlayerX);
                _lua.Globals["getPlayerY"] = new Func<int>(GetPlayerY);
                _lua.Globals["getPlayerLevel"] = new Func<int>(GetPlayerLevel);
                _lua.Globals["getAccountName"] = new Func<string>(GetAccountName);
                _lua.Globals["getAreaName"] = new Func<string>(GetAreaName);
                _lua.Globals["getMapName"] = new Func<string>(GetMapName);
                _lua.Globals["getTeamSize"] = new Func<int>(GetTeamSize);
                _lua.Globals["getPokedexOwned"] = new Func<int>(GetPokedexOwned);
                _lua.Globals["getPokedexSeen"] = new Func<int>(GetPokedexSeen);
                _lua.Globals["getTotalSteps"] = new Func<int>(GetTotalSteps);

                _lua.Globals["getPokemonId"] = new Func<int, int>(GetPokemonId);
                _lua.Globals["getPokemonName"] = new Func<int, string>(GetPokemonName);
                _lua.Globals["getPokemonHealth"] = new Func<int, int>(GetPokemonHealth);
                _lua.Globals["getPokemonHealthPercent"] = new Func<int, int>(GetPokemonHealthPercent);
                _lua.Globals["getPokemonMaxHealth"] = new Func<int, int>(GetPokemonMaxHealth);
                _lua.Globals["getPokemonLevel"] = new Func<int, int>(GetPokemonLevel);
                _lua.Globals["getPokemonStatus"] = new Func<int, string>(GetPokemonStatus);
                _lua.Globals["getPokemonForme"] = new Func<int, string>(GetPokemonForme);
                _lua.Globals["getPokemonHeldItem"] = new Func<int, string>(GetPokemonHeldItem);
                _lua.Globals["getRemainingPowerPoints"] = new Func<int, string, int>(GetRemainingPowerPoints);
                _lua.Globals["getPokemonMaxPowerPoints"] = new Func<int, int, int>(GetPokemonMaxPowerPoints);
                _lua.Globals["isPokemonShiny"] = new Func<int, bool>(IsPokemonShiny);
                _lua.Globals["getPokemonMoveName"] = new Func<int, int, string>(GetPokemonMoveName);
                _lua.Globals["getPokemonMoveAccuracy"] = new Func<int, int, int>(GetPokemonMoveAccuracy);
                _lua.Globals["getPokemonMovePower"] = new Func<int, int, int>(GetPokemonMovePower);
                _lua.Globals["getPokemonMoveType"] = new Func<int, int, string>(GetPokemonMoveType);
                _lua.Globals["getPokemonMoveDamageType"] = new Func<int, int, string>(GetPokemonMoveDamageType);
                _lua.Globals["getPokemonMoveStatus"] = new Func<int, int, bool>(GetPokemonMoveStatus);
                _lua.Globals["getPokemonNature"] = new Func<int, string>(GetPokemonNature);
                _lua.Globals["getPokemonAbility"] = new Func<int, string>(GetPokemonAbility);
                _lua.Globals["getPokemonStat"] = new Func<int, string, int>(GetPokemonStat);
                _lua.Globals["getPokemonEffortValue"] = new Func<int, string, int>(GetPokemonEffortValue);
                _lua.Globals["getPokemonIndividualValue"] = new Func<int, string, int>(GetPokemonIndividualValue);
                _lua.Globals["getPokemonHappiness"] = new Func<int, int>(GetPokemonHappiness);
                _lua.Globals["getPokemonOriginalTrainer"] = new Func<int, string>(GetPokemonOriginalTrainer);
                _lua.Globals["getPokemonGender"] = new Func<int, string>(GetPokemonGender);
                _lua.Globals["getPokemonType"] = new Func<int, string[]>(GetPokemonType);
                _lua.Globals["getDamageMultiplier"] = new Func<string, DynValue[], double>(GetDamageMultiplier);
                _lua.Globals["isPokemonUsable"] = new Func<int, bool>(IsPokemonUsable);
                _lua.Globals["getUsablePokemonCount"] = new Func<int>(GetUsablePokemonCount);
                _lua.Globals["hasMove"] = new Func<int, string, bool>(HasMove);

                _lua.Globals["hasItem"] = new Func<string, bool>(HasItem);
                _lua.Globals["getItemQuantity"] = new Func<string, int>(GetItemQuantity);
                _lua.Globals["hasItemId"] = new Func<int, bool>(HasItemId);
                _lua.Globals["getItemQuantityId"] = new Func<int, int>(GetItemQuantityID);

                _lua.Globals["hasPokemonInTeam"] = new Func<string, bool>(HasPokemonInTeam);
                _lua.Globals["isTeamSortedByLevelAscending"] = new Func<bool>(IsTeamSortedByLevelAscending);
                _lua.Globals["isTeamSortedByLevelDescending"] = new Func<bool>(IsTeamSortedByLevelDescending);
                _lua.Globals["isTeamRangeSortedByLevelAscending"] = new Func<int, int, bool>(IsTeamRangeSortedByLevelAscending);
                _lua.Globals["isTeamRangeSortedByLevelDescending"] = new Func<int, int, bool>(IsTeamRangeSortedByLevelDescending);
                _lua.Globals["isNpcVisible"] = new Func<string, bool>(IsNpcVisible);
                _lua.Globals["isNpcOnCell"] = new Func<int, int, bool>(IsNpcOnCell);
                _lua.Globals["isShopOpen"] = new Func<bool>(IsShopOpen);
                _lua.Globals["getMoney"] = new Func<int>(GetMoney);
                _lua.Globals["isMounted"] = new Func<bool>(IsMounted);
                _lua.Globals["isSurfing"] = new Func<bool>(IsSurfing);
                _lua.Globals["getTime"] = new GetTimeDelegate(GetTime);
                _lua.Globals["isMorning"] = new Func<bool>(IsMorning);
                _lua.Globals["isNoon"] = new Func<bool>(IsNoon);
                _lua.Globals["isEvening"] = new Func<bool>(IsEvening);
                _lua.Globals["isNight"] = new Func<bool>(IsNight);
                _lua.Globals["isOutside"] = new Func<bool>(IsOutside);
                _lua.Globals["getDestinationId"] = new Func<int, int, string>(GetDestinationId);
                _lua.Globals["isTrainerInfoReceived"] = new Func<bool>(IsTrainerInfoReceived);
                _lua.Globals["askForTrainerInfo"] = new Func<bool>(AskForTrainerInfo);
                _lua.Globals["getTrainerKantoLevel"] = new Func<int>(GetTrainerKantoLevel);
                _lua.Globals["getTrainerJohtoLevel"] = new Func<int>(GetTrainerJohtoLevel);
                _lua.Globals["hasBadge"] = new Func<string, bool>(HasBadge);
                _lua.Globals["hasBadgeId"] = new Func<int, bool>(HasBadgeId);
                _lua.Globals["countBadges"] = new Func<int>(CountBadges);

                _lua.Globals["getNearestMoveAbleCell"] = new Func<DynValue[], DynValue[]>(GetNearestMoveAbleCell);

                // Quest conditions
                _lua.Globals["isMainQuestId"] = new Func<string, bool>(IsMainQuestId);
                _lua.Globals["isMainQuest"] = new Func<string, bool>(IsMainQuest);
                _lua.Globals["isQuestIdCompleted"] = new Func<string, bool>(IsQuestIdCompleted);
                _lua.Globals["isQuestCompleted"] = new Func<string, bool>(IsQuestCompleted);
                _lua.Globals["isQuestPathRequested"] = new Func<string, bool>(IsQuestPathRequested);
                _lua.Globals["isQuestIdPathRequested"] = new Func<string, bool>(IsQuestIdPathRequested);
                _lua.Globals["getQuestId"] = new Func<string, string>(GetQuestId);
                _lua.Globals["getQuestIdType"] = new Func<string, string>(GetQuestIdType);
                _lua.Globals["getQuestType"] = new Func<string, string>(GetQuestType);
                _lua.Globals["getQuestTargetArea"] = new Func<string, string>(GetQuestTargetArea);
                _lua.Globals["getQuestIdTargetArea"] = new Func<string, string>(GetQuestIdTargetArea);
                _lua.Globals["getQuestTargetCompletedArea"] = new Func<string, string>(GetQuestTargetCompletedArea);
                _lua.Globals["getQuestIdTargetCompletedArea"] = new Func<string, string>(GetQuestIdTargetCompletedArea);
                _lua.Globals["getQuestSourceNpc"] = new Func<string, string>(GetQuestSourceNpc);
                _lua.Globals["getQuestIdSourceNpc"] = new Func<string, string>(GetQuestIdSourceNpc);
                _lua.Globals["getQuestSourceArea"] = new Func<string, string>(GetQuestSourceArea);
                _lua.Globals["getQuestIdSourceArea"] = new Func<string, string>(GetQuestIdSourceArea);
                _lua.Globals["getMainQuestName"] = new Func<string>(GetMainQuestName);
                _lua.Globals["getMainQuestId"] = new Func<string>(GetMainQuestId);

                // Quest actions
                _lua.Globals["requestPathForQuestId"] = new Func<string, bool>(RequestPathForQuestId);
                _lua.Globals["requestPathForQuest"] = new Func<string, bool>(RequestPathForQuest);

                // Battle conditions
                _lua.Globals["isDoubleBattle"] = new Func<bool>(IsDoubleBattle);
                _lua.Globals["isOpponentShiny"] = new Func<bool>(IsOpponentShiny);
                _lua.Globals["isAlreadyCaught"] = new Func<bool>(IsAlreadyCaught);
                _lua.Globals["isWildBattle"] = new Func<bool>(IsWildBattle);
                _lua.Globals["getActivePokemonNumber"] = new Func<int>(GetActivePokemonNumber);
                _lua.Globals["getTeamPositionFromPersonality"] = new Func<int, int>(GetTeamPositionFromPersonality);
                _lua.Globals["getActivePokemons"] = new Func<List<Dictionary<string, DynValue>>>(GetActivePokemons);
                _lua.Globals["getOpponentId"] = new Func<int>(GetOpponentId);
                _lua.Globals["getOpponentName"] = new Func<string>(GetOpponentName);
                _lua.Globals["getOpponentHealth"] = new Func<int>(GetOpponentHealth);
                _lua.Globals["getOpponentMaxHealth"] = new Func<int>(GetOpponentMaxHealth);
                _lua.Globals["getOpponentHealthPercent"] = new Func<int>(GetOpponentHealthPercent);
                _lua.Globals["getOpponentLevel"] = new Func<int>(GetOpponentLevel);
                _lua.Globals["getOpponentStatus"] = new Func<string>(GetOpponentStatus);
                _lua.Globals["getOpponentForme"] = new Func<string>(GetOpponentForme);
                _lua.Globals["isOpponentEffortValue"] = new Func<string, bool>(IsOpponentEffortValue);
                _lua.Globals["getOpponentEffortValue"] = new Func<string, int>(GetOpponentEffortValue);
                _lua.Globals["getOpponentType"] = new Func<string[]>(GetOpponentType);

                // Path actions
                _lua.Globals["moveToCell"] = new Func<int, int, bool>(MoveToCell);
                _lua.Globals["moveToArea"] = new Func<string, bool>(MoveToArea);
                _lua.Globals["moveToLink"] = new Func<string, bool>(MoveToLink);
                _lua.Globals["moveToNearestLink"] = new Func<bool>(MoveToNearestLink);
                _lua.Globals["moveToRectangle"] = new Func<DynValue[], bool>(MoveToRectangle);
                _lua.Globals["moveLinearX"] = new Func<DynValue[], bool>(MoveLinearX);
                _lua.Globals["moveLinearY"] = new Func<DynValue[], bool>(MoveLinearY);
                _lua.Globals["moveToGrass"] = new Func<bool>(MoveToGrass);
                _lua.Globals["moveToWater"] = new Func<bool>(MoveToWater);
                _lua.Globals["talkToNpc"] = new Func<string, bool>(TalkToNpc);
                _lua.Globals["talkToNpcOnCell"] = new Func<int, int, bool>(TalkToNpcOnCell);
                _lua.Globals["usePokecenter"] = new Func<bool>(UsePokecenter);
                _lua.Globals["swapPokemon"] = new Func<int, int, bool>(SwapPokemon);
                _lua.Globals["sortTeamByLevelAscending"] = new Func<bool>(SortTeamByLevelAscending);
                _lua.Globals["sortTeamByLevelDescending"] = new Func<bool>(SortTeamByLevelDescending);
                _lua.Globals["sortTeamRangeByLevelAscending"] = new Func<int, int, bool>(SortTeamRangeByLevelAscending);
                _lua.Globals["sortTeamRangeByLevelDescending"] = new Func<int, int, bool>(SortTeamRangeByLevelDescending);
                _lua.Globals["buyItem"] = new Func<string, int, bool>(BuyItem);
                _lua.Globals["closeShop"] = new Func<bool>(CloseShop);

                // Battle actions
                _lua.Globals["attack"] = new Func<bool>(Attack);
                _lua.Globals["weakAttack"] = new Func<bool>(WeakAttack);
                _lua.Globals["run"] = new Func<bool>(Run);
                _lua.Globals["sendUsablePokemon"] = new Func<bool>(SendUsablePokemon);
                _lua.Globals["sendAnyPokemon"] = new Func<bool>(SendAnyPokemon);
                _lua.Globals["sendPokemon"] = new Func<int, bool>(SendPokemon);
                _lua.Globals["sendPokemonDoubleBattle"] = new Func<int, int, bool>(SendPokemonDoubleBattle);
                _lua.Globals["useMove"] = new Func<string, bool>(UseMove);
                _lua.Globals["useAnyMove"] = new Func<bool>(UseAnyMove);

                // Move learning actions
                _lua.Globals["forgetMove"] = new Func<string, bool>(ForgetMove);
                _lua.Globals["forgetAnyMoveExcept"] = new Func<DynValue[], bool>(ForgetAnyMoveExcept);

                // Path functions
                _lua.Globals["pushDialogAnswer"] = new Action<DynValue>(PushDialogAnswer);

                // General actions
                _lua.Globals["useItem"] = new Func<string, bool>(UseItem);
                _lua.Globals["useItemOnPokemon"] = new Func<string, int, bool>(UseItemOnPokemon);
                _lua.Globals["useItemOnMove"] = new Func<string, string, int, bool>(UseItemOnMove);
                _lua.Globals["useEquippedMount"] = new Func<bool>(UseEquippedMount);

                // File editing actions
                _lua.Globals["logToFile"] = new Action<string, DynValue, bool>(LogToFile);
                _lua.Globals["readLinesFromFile"] = new Func<string, string[]>(ReadLinesFromFile);
            });

            foreach (string content in _libsContent)
            {
                CallContent(content);
            }
            CallContent(_content);
            IsLoaded = true;
        }        

        private void CallFunctionSafe(string functionName, params object[] args)
        {
            try
            {
                try
                {
                    CallFunction(functionName, false, args);
                }
                catch (ScriptRuntimeException ex)
                {
                    throw new Exception(ex.DecoratedMessage, ex);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                Fatal("Error during the execution of '" + functionName + "': " + ex);
#else
				Fatal("Error during the execution of '" + functionName + "': " + ex.Message);
#endif
            }
        }

        private void CallContent(string content)
        {
            try
            {
                TaskUtils.CallActionWithTimeout(() => _lua.DoString(content), delegate
                {
                    throw new Exception("The execution of the script timed out.");
                }, TimeoutDelay);
            }
            catch (SyntaxErrorException ex)
            {
                throw new Exception(ex.DecoratedMessage, ex);
            }
        }

        private void CallFunction(string functionName, bool isPathAction, params object[] args)
        {
            if (_hookedFunctions.ContainsKey(functionName))
            {
                foreach (DynValue function in _hookedFunctions[functionName])
                {
                    CallDynValueFunction(function, "hook:" + functionName, args);
                    if (isPathAction && _actionExecuted) return;
                }
            }
            CallDynValueFunction(_lua.Globals.Get(functionName), functionName, args);
        }

        private void CallDynValueFunction(DynValue function, string functionName, params object[] args)
        {
            if (function.Type != DataType.Function) return;
            TaskUtils.CallActionWithTimeout(() => _lua.Call(function, args), delegate
            {
                Fatal("The execution of the script timed out (" + functionName + ").");
            }, TimeoutDelay);
        }

        private bool ValidateAction(string source, bool inBattle)
        {
            if (_actionExecuted)
            {
                Fatal("error: " + source + ": the script can only execute one action per frame.");
                return false;
            }
            if (Bot.Game.IsInBattle != inBattle)
            {
                if (inBattle)
                {
                    Fatal("error: " + source + " you cannot execute a battle action while not in a battle.");
                }
                else
                {
                    Fatal("error: " + source + " you cannot execute a path action while in a battle.");
                }
                return false;
            }
            return true;
        }

        private bool ExecuteAction(bool result)
        {
            if (result)
            {
                _actionExecuted = true;
            }
            return result;
        }

        // API: Displays the specified message to the message log.
        private void Log(string message)
        {
            LogMessage(message);
        }

        // API: Displays the specified message to the message log and stop the bot.
        private void Fatal(string message)
        {
            LogMessage(message);
            Bot.Stop();
        }

        // API: Displays the specified message to the message log and logs out.
        private void Logout(string message)
        {
            LogMessage(message);
            Bot.Stop();
            Bot.Logout(false);
        }

        // API: Returns true if the string contains the specified part, ignoring the case.
        private bool StringContains(string haystack, string needle)
        {
            return haystack.ToUpperInvariant().Contains(needle.ToUpperInvariant());
        }

        // API: Returns playing a custom sound.
        private void PlaySound(string file)
        {
            if (File.Exists(file))
            {
                using (SoundPlayer player = new SoundPlayer(file))
                {
                    player.Play();
                }
            };
        }

        // API: Calls the specified function when the specified event occurs.
        private void RegisterHook(string eventName, DynValue callback)
        {
            if (callback.Type != DataType.Function)
            {
                Fatal("error: registerHook: the callback must be a function.");
                return;
            }
            if (!_hookedFunctions.ContainsKey(eventName))
            {
                _hookedFunctions.Add(eventName, new List<DynValue>());
            }
            _hookedFunctions[eventName].Add(callback);
        }

        // API: Returns the X-coordinate of the current cell.
        private int GetPlayerX()
        {
            return Bot.Game.PlayerX;
        }

        // API: Returns the Y-coordinate of the current cell.
        private int GetPlayerY()
        {
            return Bot.Game.PlayerY;
        }

        // API:Returns player's current level of current region.
        private int GetPlayerLevel()
        {
            return Bot.Game.Level != null ? (int)Bot.Game.Level.UserLevel : 5; // 5 is default level
        }

        // API: Returns current account name.
        private string GetAccountName()
        {
            return Bot.Account.Name;
        }

        // API: Returns the name of the current map.
        private string GetMapName() => Bot.Game.MapName;

        // API: Returns the name of the current area.
        private string GetAreaName()
        {
            return Bot.Game.AreaName;
        }

        // API: Returns the amount of pokémon in the team.
        private int GetTeamSize()
        {
            return Bot.Game.Team.Count;
        }

        // API: Returns Owned Entry of the pokedex
        private int GetPokedexOwned()
        {
            return Bot.Game.PokedexOwned;
        }

        // API: Returns Seen Entry of the pokedex
        private int GetPokedexSeen()
        {
            return Bot.Game.PokedexSeen;
        }

        // API: Returns total steps taken by the account.
        private int GetTotalSteps()
        {
            return Bot.Game.ToatlSteps;
        }

        // API: Returns the ID of the specified pokémon in the team.
        private int GetPokemonId(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonId: tried to retrieve the non-existing pokemon " + index + ".");
                return 0;
            }
            return Bot.Game.Team[index - 1].Id;
        }

        // API: Returns the name of the specified pokémon in the team.
        private string GetPokemonName(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonName: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            return Bot.Game.Team[index - 1].Name;
        }

        // API: Returns the current health of the specified pokémon in the team.
        private int GetPokemonHealth(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonHealth: tried to retrieve the non-existing pokemon " + index + ".");
                return 0;
            }
            return Bot.Game.Team[index - 1].CurrentHealth;
        }

        // API: Returns the percentage of remaining health of the specified pokémon in the team.
        private int GetPokemonHealthPercent(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonHealthPercent: tried to retrieve the non-existing pokemon " + index + ".");
                return 0;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.CurrentHealth * 100 / pokemon.MaxHealth;
        }

        // API: Returns the maximum health of the specified pokémon in the team.
        private int GetPokemonMaxHealth(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonMaxHealth: tried to retrieve the non-existing pokemon " + index + ".");
                return 0;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.MaxHealth;
        }

        // API: Returns the shyniness of the specified pokémon in the team.
        private bool IsPokemonShiny(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: isPokemonShiny: tried to retrieve the non-existing pokemon " + index + ".");
                return false;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.IsShiny;
        }

        // API: Returns the move of the specified pokémon in the team at the specified index.
        private string GetPokemonMoveName(int index, int moveId)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonMove: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            if (moveId < 1 || moveId > 4)
            {
                Fatal("error: getPokemonMove: tried to access an impossible move #" + moveId + ".");
                return null;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.Moves[moveId - 1].Name;
        }

        // API: Returns the move accuracy of the specified pokémon in the team at the specified index.
        private int GetPokemonMoveAccuracy(int index, int moveId)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonMoveAccuracy: tried to retrieve the non-existing pokemon " + index + ".");
                return -1;
            }
            if (moveId < 1 || moveId > 4)
            {
                Fatal("error: getPokemonMoveAccuracy: tried to access an impossible move #" + moveId + ".");
                return -1;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.Moves[moveId - 1].Data.Accuracy;
        }

        // API: Returns the move power of the specified pokémon in the team at the specified index.
        private int GetPokemonMovePower(int index, int moveId)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonMovePower: tried to retrieve the non-existing pokemon " + index + ".");
                return -1;
            }
            if (moveId < 1 || moveId > 4)
            {
                Fatal("error: getPokemonMovePower: tried to access an impossible move #" + moveId + ".");
                return -1;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.Moves[moveId - 1].Data.Power.GetValueOrDefault();
        }

        // API: Returns the move type of the specified pokémon in the team at the specified index.
        private string GetPokemonMoveType(int index, int moveId)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonMoveType: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            if (moveId < 1 || moveId > 4)
            {
                Fatal("error: getPokemonMoveType: tried to access an impossible move #" + moveId + ".");
                return null;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.Moves[moveId - 1].Data.Type.ToString();
        }

        // API: Returns the move damage type of the specified pokémon in the team at the specified index.
        private string GetPokemonMoveDamageType(int index, int moveId)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonMoveDamageType: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            if (moveId < 1 || moveId > 4)
            {
                Fatal("error: getPokemonMoveDamageType: tried to access an impossible move #" + moveId + ".");
                return null;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.Moves[moveId - 1].Data.DamageType.ToString();
        }

        // API: Returns true if the move of the specified pokémon in the team at the specified index can apply a status .
        private bool GetPokemonMoveStatus(int index, int moveId)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonMoveStatus: tried to retrieve the non-existing pokemon " + index + ".");
                return false;
            }
            if (moveId < 1 || moveId > 4)
            {
                Fatal("error: getPokemonMoveStatus: tried to access an impossible move #" + moveId + ".");
                return false;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.Moves[moveId - 1].Data.Status;
        }

        // API: Max move PP of the pokemon of the current box matching the ID.
        private int GetPokemonMaxPowerPoints(int index, int moveId)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonMove: tried to retrieve the non-existing pokemon " + index + ".");
                return -1;
            }
            if (moveId < 1 || moveId > 4)
            {
                Fatal("error: getPokemonMove: tried to access an impossible move #" + moveId + ".");
                return -1;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.Moves[moveId - 1].MaxPoints;
        }

        // API: Nature of the pokemon of the current box matching the ID.
        private string GetPokemonNature(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonNature: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.Nature;
        }

        // API: Ability of the pokemon of the current box matching the ID.
        private string GetPokemonAbility(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonAbility: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            Pokemon pokemon = Bot.Game.Team[index - 1];
            return pokemon.Ability.Name;
        }

        // API: Returns the level of the specified pokémon in the team.
        private int GetPokemonLevel(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonLevel: tried to retrieve the non-existing pokemon " + index + ".");
                return 0;
            }
            return Bot.Game.Team[index - 1].Level;
        }

        // API: Returns the happiness of the specified pokémon in the team.
        private int GetPokemonHappiness(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonHappiness: tried to retrieve the non-existing pokemon " + index + ".");
                return -1;
            }
            return Bot.Game.Team[index - 1].Happiness;
        }

        // API: Returns the original trainer of the specified pokémon in the team.
        private string GetPokemonOriginalTrainer(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonOriginalTrainer: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            return Bot.Game.Team[index - 1].OriginalTrainer;
        }

        // API: Returns the gender of the specified pokémon in the team.
        private string GetPokemonGender(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonGender: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            return Bot.Game.Team[index - 1].Gender;
        }

        // API: Returns the type of the specified pokémon in the team as an array of length 2.
        private string[] GetPokemonType(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonType: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }

            int id = Bot.Game.Team[index - 1].Id;

            if (id <= 0 || id >= TypesManager.Instance.Type1.Count())
            {
                return new string[] { "Unknown", "Unknown" };
            }

            return new string[] { TypesManager.Instance.Type1[id].ToString(), TypesManager.Instance.Type2[id].ToString() };
        }

        private string[] _types = { "", "NORMAL", "FIGHTING", "FLYING", "POISON", "GROUND", "ROCK", "BUG", "GHOST", "STEEL", "FIRE", "WATER", "GRASS", "ELECTRIC", "PSYCHIC", "ICE", "DRAGON", "DARK", "FAIRY" };

        // API: Returns the multiplier of the damage type between an attacking type and one or two defending types.
        private double GetDamageMultiplier(string attacker, params DynValue[] defender)
        {
            if (defender[0].Type == DataType.Table)
            {
                if (defender[0].Table.Length == 1)
                {
                    defender = new DynValue[] { defender[0].Table.Values.ToArray()[0], DynValue.NewString("") };
                }
                else
                {
                    defender = defender[0].Table.Values.ToArray();
                }
            }
            else if (defender.Length == 1)
            {
                defender = new DynValue[] { defender[0], DynValue.NewString("") };
            }

            if (attacker.ToUpperInvariant() == "NONE")
                attacker = "";

            if (defender[0].CastToString().ToUpperInvariant() == "NONE")
                defender[0] = DynValue.NewString("");

            if (defender[1].CastToString().ToUpperInvariant() == "NONE")
                defender[1] = DynValue.NewString("");

            if (!Array.Exists(_types, e => e == attacker.ToUpperInvariant()))
            {
                Fatal("error: getDamageMultiplier: the damage type '" + attacker + "' does not exist.");
                return -1;
            }

            if (!Array.Exists(_types, e => e == defender[0].CastToString().ToUpperInvariant()))
            {
                Fatal("error: getDamageMultiplier: the damage type '" + defender[0].CastToString() + "' does not exist.");
                return -1;
            }

            if (!Array.Exists(_types, e => e == defender[1].CastToString().ToUpperInvariant()))
            {
                Fatal("error: getDamageMultiplier: the damage type '" + defender[1].CastToString() + "' does not exist.");
                return -1;
            }

            double power = 1;

            power *= TypesManager.Instance.GetMultiplier(PokemonTypeExtensions.FromName(attacker), PokemonTypeExtensions.FromName(defender[0].CastToString()));
            power *= TypesManager.Instance.GetMultiplier(PokemonTypeExtensions.FromName(attacker), PokemonTypeExtensions.FromName(defender[1].CastToString()));

            return power;
        }

        // API: Returns the status of the specified pokémon in the team.
        private string GetPokemonStatus(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonStatus: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            return Bot.Game.Team[index - 1].Status;
        }

        // API: Returns the forme of the specified pokémon in the team.
        private string GetPokemonForme(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonForme: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            return Bot.Game.Team[index - 1].Forme;
        }

        // API: Returns the item held by the specified pokemon in the team, null if empty.
        private string GetPokemonHeldItem(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonHeldItem: tried to retrieve the non-existing pokemon " + index + ".");
                return null;
            }
            string itemHeld = Bot.Game.Team[index - 1].ItemHeld;
            return itemHeld == string.Empty ? null : itemHeld;
        }

        // API: Returns true if the specified pokémon has is alive and has an offensive attack available.
        private bool IsPokemonUsable(int index)
        {
            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: isPokemonUsable: tried to retrieve the non-existing pokemon " + index + ".");
                return false;
            }
            return Bot.AI.IsPokemonUsable(Bot.Game.Team[index - 1]);
        }

        // API: Returns the amount of usable pokémon in the team.
        private int GetUsablePokemonCount()
        {
            return Bot.AI.UsablePokemonsCount;
        }

        // API: Returns true if the specified pokémon has a move with the specified name.
        private bool HasMove(int pokemonIndex, string moveName)
        {
            if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
            {
                Fatal("error: hasMove: tried to retrieve the non-existing pokemon " + pokemonIndex + ".");
                return false;
            }

            return Bot.Game.PokemonUidHasMove(pokemonIndex, moveName.ToUpperInvariant());
        }

        // API: Returns the remaining power points of the specified move of the specified pokémon in the team.
        private int GetRemainingPowerPoints(int pokemonIndex, string moveName)
        {
            if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
            {
                Fatal("error: getRemainingPowerPoints: tried to retrieve the non-existing pokémon " + pokemonIndex + ".");
                return 0;
            }

            moveName = moveName.ToUpperInvariant();
            PokemonMove move = Bot.Game.Team[pokemonIndex - 1].Moves.FirstOrDefault(m => MovesManager.Instance.GetMoveData(m.Id)?.Name.ToUpperInvariant() == moveName);
            if (move == null)
            {
                Fatal("error: getRemainingPowerPoints: the pokémon " + pokemonIndex + " does not have a move called '" + moveName + "'.");
                return 0;
            }

            return move.CurrentPoints;
        }

        // API: Returns the value for the specified stat of the specified pokémon in the team.
        private int GetPokemonStat(int pokemonIndex, string statType)
        {
            if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonStat: tried to retrieve the non-existing pokémon " + pokemonIndex + ".");
                return 0;
            }

            if (!_stats.ContainsKey(statType.ToUpperInvariant()))
            {
                Fatal("error: getPokemonStat: the stat '" + statType + "' does not exist.");
                return 0;
            }

            return Bot.Game.Team[pokemonIndex - 1].Stats.GetStat(_stats[statType.ToUpperInvariant()]);
        }

        // API: Returns the effort value for the specified stat of the specified pokémon in the team.
        private int GetPokemonEffortValue(int pokemonIndex, string statType)
        {
            if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonEffortValue: tried to retrieve the non-existing pokémon " + pokemonIndex + ".");
                return 0;
            }

            if (!_stats.ContainsKey(statType.ToUpperInvariant()))
            {
                Fatal("error: getPokemonEffortValue: the stat '" + statType + "' does not exist.");
                return 0;
            }

            return Bot.Game.Team[pokemonIndex - 1].EV.GetStat(_stats[statType.ToUpperInvariant()]);
        }

        private static Dictionary<string, StatType> _stats = new Dictionary<string, StatType>()
        {
            { "HP", StatType.Health },
            { "HEALTH", StatType.Health },
            { "ATK", StatType.Attack },
            { "ATTACK", StatType.Attack },
            { "DEF", StatType.Defence },
            { "DEFENCE", StatType.Defence },
            { "DEFENSE", StatType.Defence },
            { "SPATK", StatType.SpAttack },
            { "SPATTACK", StatType.SpAttack },
            { "SPDEF", StatType.SpDefence },
            { "SPDEFENCE", StatType.SpDefence },
            { "SPDEFENSE", StatType.SpDefence },
            { "SPD", StatType.Speed },
            { "SPEED", StatType.Speed }
        };

        // API: Returns the individual value for the specified stat of the specified pokémon in the team.
        private int GetPokemonIndividualValue(int pokemonIndex, string statType)
        {
            if (pokemonIndex < 1 || pokemonIndex > Bot.Game.Team.Count)
            {
                Fatal("error: getPokemonIndividualValue: tried to retrieve the non-existing pokémon " + pokemonIndex + ".");
                return 0;
            }

            if (!_stats.ContainsKey(statType.ToUpperInvariant()))
            {
                Fatal("error: getPokemonIndividualValue: the stat '" + statType + "' does not exist.");
                return 0;
            }

            return Bot.Game.Team[pokemonIndex - 1].IV.GetStat(_stats[statType.ToUpperInvariant()]);
        }

        // API: Returns true if the specified item is in the inventory.
        private bool HasItem(string itemName)
        {
            return Bot.Game.HasItemName(itemName.ToUpperInvariant());
        }

        // API: Returns the quantity of the specified item in the inventory.
        private int GetItemQuantity(string itemName)
        {
            return Bot.Game.GetItemFromName(itemName.ToUpperInvariant())?.Quantity ?? 0;
        }

        // API: Returns true if the specified item is in the inventory.
        private bool HasItemId(int itemid)
        {
            return Bot.Game.HasItemId(itemid);
        }
        // API: Returns the quantity of the specified item in the inventory.
        private int GetItemQuantityID(int itemid)
        {
            return Bot.Game.GetItemFromId(itemid)?.Quantity ?? 0;
        }

        // API: Returns true if the specified pokémon is present in the team.
        private bool HasPokemonInTeam(string pokemonName)
        {
            return Bot.Game.HasPokemonInTeam(pokemonName.ToUpperInvariant());
        }

        // API: Returns true if the team is sorted by level in ascending order.
        private bool IsTeamSortedByLevelAscending()
        {
            return IsTeamSortedByLevel(true, 1, 6);
        }

        // API: Returns true if the team is sorted by level in descending order.
        private bool IsTeamSortedByLevelDescending()
        {
            return IsTeamSortedByLevel(false, 1, 6);
        }

        // API: Returns true if the specified part of the team is sorted by level in ascending order.
        private bool IsTeamRangeSortedByLevelAscending(int fromIndex, int toIndex)
        {
            return IsTeamSortedByLevel(true, fromIndex, toIndex);
        }

        // API: Returns true if the specified part of the team the team is sorted by level in descending order.
        private bool IsTeamRangeSortedByLevelDescending(int fromIndex, int toIndex)
        {
            return IsTeamSortedByLevel(false, fromIndex, toIndex);
        }

        private bool IsTeamSortedByLevel(bool ascending, int from, int to)
        {
            from = Math.Max(from, 1);
            to = Math.Min(to, Bot.Game.Team.Count);

            int level = ascending ? 0 : int.MaxValue;
            for (int i = from - 1; i < to; ++i)
            {
                Pokemon pokemon = Bot.Game.Team[i];
                if (ascending && pokemon.Level < level) return false;
                if (!ascending && pokemon.Level > level) return false;
                level = pokemon.Level;
            }
            return true;
        }

        // API: Returns true if there is a visible NPC with the specified name on the map.
        private bool IsNpcVisible(string npcName)
        {
            npcName = npcName.ToUpperInvariant();
            return Bot.Game.Map.OriginalNpcs.Any(npc => npc.NpcName.ToUpperInvariant() == npcName && npc.IsVisible);
        }

        // API: Returns true if there is a visible NPC the specified coordinates.
        private bool IsNpcOnCell(int cellX, int cellY)
        {
            return Bot.Game.Map.OriginalNpcs.Any(npc => npc.PositionX == cellX && npc.PositionY == cellY && npc.IsVisible);
        }

        // API: Returns true if there is a shop opened.
        private bool IsShopOpen()
        {
            return Bot.Game.OpenedShop != null;
        }

        // API: Returns the amount of money in the inventory.
        private int GetMoney()
        {
            return Bot.Game.Money;
        }

        // API: Returns true if the player is riding a mount or the bicycle.
        private bool IsMounted()
        {
            return Bot.Game.IsBiking;
        }

        // API: Returns true if the player is surfing
        private bool IsSurfing()
        {
            return Bot.Game.IsSurfing;
        }

        private delegate int GetTimeDelegate(out int minute);

        // API: Return the current in game hour and minute.
        private int GetTime(out int minute)
        {
            DateTime dt = Convert.ToDateTime(Bot.Game.PokeTime);
            minute = dt.Minute;
            return dt.Hour;
        }

        // API: Return true if morning time.
        private bool IsMorning()
        {
            if (Bot.Game.LastTimePacket != null
                && Bot.Game.LastTimePacket.GameDayTime == PSXAPI.Response.GameDayTime.Morning)
            {
                return true;
            }
            return false;
        }

        // API: Return true if noon time.
        private bool IsNoon()
        {
            if (Bot.Game.LastTimePacket != null
                 && Bot.Game.LastTimePacket.GameDayTime == PSXAPI.Response.GameDayTime.Day)
            {
                return true;
            }
            return false;
        }

        // API: Return true if evening time.
        private bool IsEvening()
        {
            if (Bot.Game.LastTimePacket != null
                && Bot.Game.LastTimePacket.GameDayTime == PSXAPI.Response.GameDayTime.Evening)
            {
                return true;
            }
            return false;
        }

        // API: Return true if night time.
        private bool IsNight()
        {
            if (Bot.Game.LastTimePacket != null 
                && Bot.Game.LastTimePacket.GameDayTime == PSXAPI.Response.GameDayTime.Night)
            {
                return true;
            }
            return false;
        }

        // API: Return true if the character is outside.
        private bool IsOutside()
        {
            return Bot.Game.Map.IsOutside;
        }

        // API: 
        private bool IsDoubleBattle()
        {
            if(!Bot.Game.IsInBattle)
            {
                Fatal("error: isDoubleBattle is only usable in battle.");
                return false;
            }
            return Bot.AI.ActivePokemons != null && Bot.AI.ActivePokemons.Length > 1;
        }

        // API: Returns true if the opponent pokémon is shiny.
        private bool IsOpponentShiny()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: isOpponentShiny is only usable in battle.");
                return false;
            }
            return Bot.Game.ActiveBattle.IsShiny;
        }

        // API: Returns true if the opponent pokémon has already been caught and has a pokédex entry.
        private bool IsAlreadyCaught()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: isAlreadyCaught is only usable in battle.");
                return false;
            }
            return Bot.Game.ActiveBattle.AlreadyCaught;
        }

        // API: Returns true if the current battle is against a wild pokémon.
        private bool IsWildBattle()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: isWildBattle is only usable in battle.");
                return false;
            }
            return Bot.Game.ActiveBattle.IsWild;
        }

        // API: Returns the index of the active team pokémon in the current battle.
        private int GetActivePokemonNumber()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getActivePokemonNumber is only usable in battle.");
                return 0;
            }
            return Bot.Game.ActiveBattle.SelectedPokemonIndex + 1;
        }

        // API: 
        private int GetTeamPositionFromPersonality(int personality)
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getTeamPosition is only usable in battle.");
                return 0;
            }
            return Bot.Game.Team.FindIndex(x => x.PokemonData.Pokemon.Payload.Personality == personality);
        }

        // API: Returns all active pokemons during double/triple battle, format : { { "Name" = name, "Id" = id, "Form" = form }, {...}, ... }
        private List<Dictionary<string, DynValue>> GetActivePokemons()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getActivePokemons is only usable in battle.");
                return null;
            }
            if (Bot.AI.ActivePokemons is null)
                return null;
            var pokes = new List<Dictionary<string, DynValue>>();
            foreach (var poke in Bot.AI.ActivePokemons)
            {
                var pokeData = new Dictionary<string, DynValue>();
                pokeData["Name"] = DynValue.NewString(PokemonManager.Instance.Names[poke.ID]);
                pokeData["Id"] = DynValue.NewNumber(poke.ID);
                pokeData["Personality"] = DynValue.NewNumber(poke.Personality);
                pokeData["Form"] = DynValue.NewString(poke.Forme);
                pokes.Add(pokeData);
            }
            return pokes;
        }

        // API: Returns the id of the opponent pokémon in the current battle.
        private int GetOpponentId()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentId can only be used in battle.");
                return 0;
            }
            return Bot.Game.ActiveBattle.OpponentId;
        }

        // API: Returns the name of the opponent pokémon in the current battle.
        private string GetOpponentName()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentName can only be used in battle.");
                return null;
            }
            return PokemonManager.Instance.Names[Bot.Game.ActiveBattle.OpponentId];
        }

        // API: Returns the current health of the opponent pokémon in the current battle.
        private int GetOpponentHealth()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentHealth can only be used in battle.");
                return 0;
            }
            return Bot.Game.ActiveBattle.CurrentHealth;
        }

        // API: Returns the maximum health of the opponent pokémon in the current battle.
        private int GetOpponentMaxHealth()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentMaxHealth can only be used in battle.");
                return 0;
            }
            return Bot.Game.ActiveBattle.OpponentHealth;
        }

        // API: Returns the percentage of remaining health of the opponent pokémon in the current battle.
        private int GetOpponentHealthPercent()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentHealthPercent can only be used in battle.");
                return 0;
            }
            return Bot.Game.ActiveBattle.CurrentHealth * 100 / Bot.Game.ActiveBattle.OpponentHealth;
        }

        // API: Returns the level of the opponent pokémon in the current battle.
        private int GetOpponentLevel()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentLevel can only be used in battle.");
                return 0;
            }
            return Bot.Game.ActiveBattle.OpponentLevel;
        }

        // API: Returns the status of the opponent pokémon in the current battle.
        private string GetOpponentStatus()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentStatus can only be used in battle.");
                return null;
            }
            return Bot.Game.ActiveBattle.OpponentStatus;
        }

        // API: Returns the forme of the opponent pokémon in the current battle.
        private string GetOpponentForme()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentForme can only be used in battle.");
                return null;
            }
            var forme = Bot.Game.ActiveBattle.OpponentForme;
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
                        forme = null;
                        break;
                }

            }
            return forme;
        }

        // API: Returns true if the opponent is only giving the specified effort value.
        private bool IsOpponentEffortValue(string statType)
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: isOpponentEffortValue can only be used in battle.");
                return false;
            }
            if (!_stats.ContainsKey(statType.ToUpperInvariant()))
            {
                Fatal("error: isOpponentEffortValue: the stat '" + statType + "' does not exist.");
                return false;
            }
            if (!EffortValuesManager.Instance.BattleValues.ContainsKey(Bot.Game.ActiveBattle.OpponentId))
            {
                return false;
            }

            PokemonStats stats = EffortValuesManager.Instance.BattleValues[Bot.Game.ActiveBattle.OpponentId];
            return stats.HasOnly(_stats[statType.ToUpperInvariant()]);
        }

        // API: Returns the amount of a particular EV given by the opponent.
        private int GetOpponentEffortValue(string statType)
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentEffortValue can only be used in battle.");
                return -1;
            }
            if (!_stats.ContainsKey(statType.ToUpperInvariant()))
            {
                Fatal("error: getOpponentEffortValue: the stat '" + statType + "' does not exist.");
                return -1;
            }
            if (!EffortValuesManager.Instance.BattleValues.ContainsKey(Bot.Game.ActiveBattle.OpponentId))
            {
                return -1;
            }

            PokemonStats stats = EffortValuesManager.Instance.BattleValues[Bot.Game.ActiveBattle.OpponentId];
            return stats.GetStat(_stats[statType.ToUpperInvariant()]);
        }

        // API: Returns the type of the opponent pokémon in the current battle as an array of length 2.
        private string[] GetOpponentType()
        {
            if (!Bot.Game.IsInBattle)
            {
                Fatal("error: getOpponentType can only be used in battle.");
                return null;
            }

            int id = Bot.Game.ActiveBattle.OpponentId;

            if (id <= 0 || id >= TypesManager.Instance.Type1.Count())
            {
                return new string[] { "Unknown", "Unknown" };
            }

            return new string[] { TypesManager.Instance.Type1[id].ToString(), TypesManager.Instance.Type2[id].ToString() };
        }

        // API: Moves to the specified coordinates.
        private bool MoveToCell(int x, int y)
        {
            if (!ValidateAction("moveToCell", false)) return false;

            return ExecuteAction(Bot.MoveToCell(x, y));
        }

        // API: Moves to the nearest cell of specified area.
        private bool MoveToArea(string areaName)
        {
            if (!ValidateAction("moveToArea", false)) return false;

            return ExecuteAction(Bot.MoveToAreaLink(areaName.ToUpperInvariant()));
        }

        // API: Moves to spcefied link with Id.
        private bool MoveToLink(string id)
        {
            if (!ValidateAction("moveToLink", false) || string.IsNullOrEmpty(id)) return false;
            var valid = Guid.TryParse(id, out var linkId);
            if (!valid)
            {
                Fatal("error: 'moveToLink' given link id was invalid.");
                return false;
            }
            var findLink = Bot.Game.Map.Links.Find(l => l.DestinationID == linkId);
            if (findLink is null)
            {
                Fatal($"error: 'moveToLink' there is no link id which matches this {id} id.");
                return false;
            }
            return ExecuteAction(Bot.MoveToCell(findLink.x, -findLink.z));
        }

        // API: Moves to nearest accessible link from player.
        private bool MoveToNearestLink()
        {
            if (!ValidateAction("moveToNearestLink", false)) return false;
            return ExecuteAction(Bot.MoveToNearestLink());
        }

        // API: Moves to a random accessible cell of the specified rectangle.
        private bool MoveToRectangle(params DynValue[] values)
        {
            if (values.Length != 1 && values.Length != 4 ||
                (values.Length == 1 && values[0].Type != DataType.Table) ||
                (values.Length == 4
                    && (values[0].Type != DataType.Number || values[1].Type != DataType.Number
                    || values[2].Type != DataType.Number || values[3].Type != DataType.Number)))
            {
                Fatal("error: moveToRectangle: must receive either a table or four numbers.");
                return false;
            }
            if (values.Length == 1)
            {
                values = values[0].Table.Values.ToArray();
            }
            return MoveToRectangle((int)values[0].Number, (int)values[1].Number, (int)values[2].Number, (int)values[3].Number);
        }

        // API: Moves to a random accessible cell of the specified rectangle.
        private bool MoveToRectangle(int minX, int minY, int maxX, int maxY)
        {
            if (!ValidateAction("moveToRectangle", false)) return false;

            if (minX > maxX || minY > maxY)
            {
                Fatal("error: moveToRectangle: the maximum cell cannot be less than the minimum cell.");
                return false;
            }

            int x;
            int y;
            int tries = 0;
            do
            {
                if (++tries > 100) return false;
                x = Bot.Game.Rand.Next(minX, maxX + 1);
                y = Bot.Game.Rand.Next(minY, maxY + 1);
            } while (x == Bot.Game.PlayerX && y == Bot.Game.PlayerY);

            return ExecuteAction(Bot.MoveToCell(x, y));
        }

        private bool MoveLinearX(params DynValue[] values)
        {
            if (values.Length != 1 && values.Length != 3 ||
                (values.Length == 1 && values[0].Type != DataType.Table) ||
                (values.Length == 3
                    && (values[0].Type != DataType.Number || values[1].Type != DataType.Number
                    || values[2].Type != DataType.Number)))
            {
                Fatal("error: moveLinearX: must receive either a table or three numbers.");
                return false;
            }

            if (values.Length == 1)
            {
                values = values[0].Table.Values.ToArray();
            }

            return MoveLinearX((int)values[0].Number, (int)values[1].Number, (int)values[2].Number);
        }

        private bool MoveLinearY(params DynValue[] values)
        {
            if (values.Length != 1 && values.Length != 3 ||
                (values.Length == 1 && values[0].Type != DataType.Table) ||
                (values.Length == 3
                    && (values[0].Type != DataType.Number || values[1].Type != DataType.Number
                    || values[2].Type != DataType.Number)))
            {
                Fatal("error: moveLinearY: must receive either a table or three numbers.");
                return false;
            }

            if (values.Length == 1)
            {
                values = values[0].Table.Values.ToArray();
            }

            return MoveLinearY((int)values[0].Number, (int)values[1].Number, (int)values[2].Number);
        }

        // API: Moves left and right from one spcified cell to another specified cell.
        private bool MoveLinearX(int x1, int x2, int y)
        {
            return ExecuteAction(Bot.MoveLeftRight(x1, y, x2, y));
        }

        // API: Moves up and down from one spcified cell to another specified cell.
        private bool MoveLinearY(int y1, int y2, int x)
        {
            return ExecuteAction(Bot.MoveLeftRight(x, y1, x, y2));
        }

        // API: Moves to the nearest grass patch then move randomly inside it.
        private bool MoveToGrass()
        {
            if (!ValidateAction("moveToGrass", false)) return false;

            return ExecuteAction(MoveToCellType((x, y) => Bot.Game.Map.IsGrass(x, y)));
        }

        // API: Moves to the nearest water area then move randomly inside it.
        private bool MoveToWater()
        {
            if (!ValidateAction("moveToWater", false)) return false;

            return ExecuteAction(MoveToCellType((x, y) => Bot.Game.Map.IsWater(x, y)));
        }

        private bool MoveToCellType(Func<int, int, bool> cellTypePredicate)
        {
            bool alreadyInCell = cellTypePredicate(Bot.Game.PlayerX, Bot.Game.PlayerY);

            List<Tuple<int, int, int>> cells = new List<Tuple<int, int, int>>();

            for (int x = 0; x < Bot.Game.Map.Width; ++x)
            {
                for (int y = 0; y < Bot.Game.Map.Height; ++y)
                {
                    if (cellTypePredicate(x, y) && (x != Bot.Game.PlayerX || y != Bot.Game.PlayerY))
                    {
                        int distance = Bot.Game.DistanceTo(x, y);
                        cells.Add(new Tuple<int, int, int>(x, y, distance));
                    }
                }
            }

            List<Tuple<int, int, int>> trash = new List<Tuple<int, int, int>>();
            if (alreadyInCell)
            {
                foreach (var cell in cells)
                {
                    if (cell.Item3 >= 10)
                    {
                        trash.Add(cell);
                    }
                }
            }
            else
            {
                int minDistance = -1;
                foreach (var cell in cells)
                {
                    if (minDistance == -1 || cell.Item3 < minDistance)
                    {
                        minDistance = cell.Item3;
                    }
                }
                foreach (var cell in cells)
                {
                    if (cell.Item3 > minDistance + 5)
                    {
                        trash.Add(cell);
                    }
                }
            }
            while (trash.Count > 0)
            {
                cells.Remove(trash[0]);
                trash.RemoveAt(0);
            }

            if (cells.Count > 0)
            {
                var randomCell = cells[Bot.Game.Rand.Next(cells.Count)];
                return Bot.MoveToCell(randomCell.Item1, randomCell.Item2);
            }
            return false;
        }

        // API: Moves then talk to NPC specified by its name.
        private bool TalkToNpc(string npcName)
        {
            if (!ValidateAction("talkToNpc", false)) return false;

            npcName = npcName.ToUpperInvariant();
            Npc target = Bot.Game.Map.OriginalNpcs.FirstOrDefault(npc => npc.NpcName.ToUpperInvariant() == npcName);
            if (target == null)
            {
                Fatal("error: talkToNpc: could not find the NPC '" + npcName + "'.");
                return false;
            }

            return ExecuteAction(Bot.TalkToNpc(target));
        }

        // API: Moves then talk to NPC located on the specified cell.
        private bool TalkToNpcOnCell(int cellX, int cellY)
        {
            if (!ValidateAction("talkToNpcOnCell", false)) return false;

            Npc target = Bot.Game.Map.OriginalNpcs.FirstOrDefault(npc => npc.PositionX == cellX && npc.PositionY == cellY);
            if (target == null)
            {
                Fatal("error: talkToNpcOnCell: could not find any NPC on the cell [" + cellX + "," + cellY + "].");
                return false;
            }

            return ExecuteAction(Bot.TalkToNpc(target));
        }

        // API: Moves to the Nurse Joy then talk to the cell below her.
        private bool UsePokecenter()
        {
            if (!ValidateAction("usePokecenter", false)) return false;

            Npc nurse = Bot.Game.Map.OriginalNpcs.FirstOrDefault(npc => npc.NpcName.StartsWith("Nurse"));
            if (nurse == null)
            {
                Fatal("error: usePokecenter: could not find the Nurse Joy.");
                return false;
            }
            Npc target = Bot.Game.Map.OriginalNpcs.FirstOrDefault(npc => npc.PositionX == nurse.PositionX && npc.PositionY == nurse.PositionY + 1);
            if (target == null)
            {
                Fatal("error: usePokecenter: could not find the entity below the Nurse Joy.");
                return false;
            }

            return ExecuteAction(Bot.TalkToNpc(target));
        }

        // API: Swaps the two pokémon specified by their position in the team.
        private bool SwapPokemon(int index1, int index2)
        {
            if (!ValidateAction("swapPokemon", false)) return false;

            return ExecuteAction(Bot.Game.SwapPokemon(index1, index2));
        }

        // API: Swaps the first pokémon with the specified name with the leader of the team.
        private bool SwapPokemonWithLeader(string pokemonName)
        {
            if (!ValidateAction("swapPokemonWithLeader", false)) return false;

            Pokemon pokemon = Bot.Game.FindFirstPokemonInTeam(pokemonName.ToUpperInvariant());
            if (pokemon == null)
            {
                Fatal("error: swapPokemonWithLeader: there is no pokémon '" + pokemonName + "' in the team.");
                return false;
            }
            if (pokemon.Uid == 1)
            {
                Fatal("error: swapPokemonWithLeader: '" + pokemonName + "' is already the leader of the team.");
                return false;
            }

            return ExecuteAction(Bot.Game.SwapPokemon(1, pokemon.Uid));
        }

        // API: Sorts the pokémon in the team by level in ascending order, one pokémon at a time.
        private bool SortTeamByLevelAscending()
        {
            if (!ValidateAction("sortTeamByLevelAscending", false)) return false;

            return ExecuteAction(SortTeamByLevel(true, 1, 6));
        }

        // API: Sorts the pokémon in the team by level in descending order, one pokémon at a time.
        private bool SortTeamByLevelDescending()
        {
            if (!ValidateAction("sortTeamByLevelDescending", false)) return false;

            return ExecuteAction(SortTeamByLevel(false, 1, 6));
        }

        // API: Sorts the specified part of the team by level in ascending order, one pokémon at a time.
        private bool SortTeamRangeByLevelAscending(int fromIndex, int toIndex)
        {
            if (!ValidateAction("sortTeamRangeByLevelAscending", false)) return false;

            return ExecuteAction(SortTeamByLevel(true, fromIndex, toIndex));
        }

        // API: Sorts the specified part of the team by level in descending order, one pokémon at a time.
        private bool SortTeamRangeByLevelDescending(int fromIndex, int toIndex)
        {
            if (!ValidateAction("sortTeamRangeByLevelDescending", false)) return false;

            return ExecuteAction(SortTeamByLevel(false, fromIndex, toIndex));
        }

        private bool SortTeamByLevel(bool ascending, int from, int to)
        {
            from = Math.Max(from, 1);
            to = Math.Min(to, Bot.Game.Team.Count);

            for (int i = from - 1; i < to - 1; ++i)
            {
                int currentIndex = i;
                int currentLevel = Bot.Game.Team[i].Level;
                for (int j = i + 1; j < to; ++j)
                {
                    if ((ascending && Bot.Game.Team[j].Level < currentLevel) ||
                        (!ascending && Bot.Game.Team[j].Level > currentLevel))
                    {
                        currentIndex = j;
                        currentLevel = Bot.Game.Team[j].Level;
                    }
                }

                if (currentIndex != i)
                {
                    Bot.Game.SwapPokemon(i + 1, currentIndex + 1);
                    return true;
                }
            }
            return false;
        }

        // API: Adds the specified answer to the answer queue. It will be used in the next dialog.
        private void PushDialogAnswer(DynValue answerValue)
        {
            if (answerValue.Type == DataType.String)
            {
                Bot.Game.PushDialogAnswer(answerValue.CastToString());
            }
            else if (answerValue.Type == DataType.Number)
            {
                Bot.Game.PushDialogAnswer((int)answerValue.CastToNumber());
            }
            else
            {
                Fatal("error: pushDialogAnswer: the argument must be a number (index) or a string (search text).");
            }
        }

        // API: Uses the specified item.
        private bool UseItem(string itemName)
        {
            InventoryItem item = Bot.Game.GetItemFromName(itemName.ToUpperInvariant());
            if (item != null && item.Quantity > 0)
            {
                if (Bot.Game.IsInBattle && item.CanBeUsedInBattle)
                {
                    if (!ValidateAction("useItem", true)) return false;
                    return ExecuteAction(Bot.AI.UseItem(item.Id));
                }
                else if (!Bot.Game.IsInBattle && item.CanBeUsedOutsideOfBattle)
                {
                    if (!ValidateAction("useItem", false)) return false;
                    Bot.Game.UseItem(item.Id);
                    return ExecuteAction(true);
                }
            }
            return false;
        }

        // API: Uses the specified item on the specified pokémon.
        private bool UseItemOnPokemon(string itemName, int pokemonIndex)
        {
            InventoryItem item = Bot.Game.GetItemFromName(itemName.ToUpperInvariant());

            if (item != null && item.Quantity > 0)
            {
                if (Bot.Game.IsInBattle && item.CanBeUsedOnPokemonInBattle)
                {
                    if (!ValidateAction("useItemOnPokemon", true)) return false;
                    return ExecuteAction(Bot.AI.UseItem(item.Id, pokemonIndex));
                }
                else if (!Bot.Game.IsInBattle && item.CanBeUsedOnPokemonOutsideOfBattle)
                {
                    if (!ValidateAction("useItemOnPokemon", false)) return false;
                    Bot.Game.UseItem(item.Id, pokemonIndex);
                    return ExecuteAction(true);
                }
            }
            return false;
        }

        // API: Uses the specified item on the specified pokémon on specified move.
        private bool UseItemOnMove(string itemName, string moveName, int pokemonIndex)
        {
            itemName = itemName.ToUpperInvariant();
            InventoryItem item = Bot.Game.GetItemFromName(itemName.ToUpperInvariant());

            if (item != null && item.Quantity > 0)
            {
                if (Bot.Game.IsInBattle && item.CanBeUsedOnPokemonInBattle)
                {
                    if (!ValidateAction("useItemOnMove", true)) return false;
                    var findPoke = Bot.Game.Team.Find(pok => pok.Uid == pokemonIndex || pok.Uid * -1 == pokemonIndex);
                    if (findPoke is null) return false;
                    var findMove = findPoke.Moves.FirstOrDefault(m => MovesManager.Instance.GetMoveData(m.Id).Name.Equals(moveName, StringComparison.InvariantCultureIgnoreCase));
                    if (findMove is null) return false;
                    return ExecuteAction(Bot.AI.UseItemOnMove(item.Id, pokemonIndex, findMove.Position));
                }
                else if (!Bot.Game.IsInBattle && item.CanBeUsedOnPokemonOutsideOfBattle)
                {
                    if (!ValidateAction("useItemOnMove", false)) return false;
                    var findPoke = Bot.Game.Team.Find(pok => pok.Uid == pokemonIndex || pok.Uid * -1 == pokemonIndex);
                    if (findPoke is null) return false;
                    var findMove = findPoke.Moves.FirstOrDefault(m => MovesManager.Instance.GetMoveData(m.Id).Name.Equals(moveName, StringComparison.InvariantCultureIgnoreCase));
                    if (findMove is null) return false;
                    Bot.Game.UseItem(item.Id, pokemonIndex, findMove.Position);
                    return ExecuteAction(true);
                }
            }
            return false;
        }

        // API: Buys the specified item from the opened shop.
        private bool BuyItem(string itemName, int quantity)
        {
            if (!ValidateAction("buyItem", false)) return false;

            if (Bot.Game.OpenedShop == null)
            {
                Fatal("error: buyItem can only be used when a shop is open.");
                return false;
            }

            ShopItem item = Bot.Game.OpenedShop.Items.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.InvariantCultureIgnoreCase));

            if (item == null)
            {
                Fatal("error: buyItem: the item '" + itemName + "' does not exist in the opened shop.");
                return false;
            }

            return ExecuteAction(Bot.Game.BuyItem(item.Id, quantity));
        }

        // API: Closes the opened shop.
        private bool CloseShop()
        {
            if (!ValidateAction("closeShop", false)) return false;

            return (ExecuteAction(Bot.Game.CloseShop()));
        }

        // API: Uses the most effective offensive move available.
        private bool Attack()
        {
            if (!ValidateAction("attack", true)) return false;

            return ExecuteAction(Bot.AI.Attack());
        }

        // API: Uses the least effective offensive move available.
        private bool WeakAttack()
        {
            if (!ValidateAction("weakAttack", true)) return false;

            return ExecuteAction(Bot.AI.WeakAttack());
        }

        // API: Tries to escape from the current wild battle.
        private bool Run()
        {
            if (!ValidateAction("run", true)) return false;

            return ExecuteAction(Bot.AI.Run());
        }

        // API: Sends the first usable pokemon different from the active one.
        private bool SendUsablePokemon()
        {
            if (!ValidateAction("sendUsablePokemon", true)) return false;

            return ExecuteAction(Bot.AI.SendUsablePokemon());
        }

        // API: Sends the first available pokemon different from the active one.
        private bool SendAnyPokemon()
        {
            if (!ValidateAction("sendAnyPokemon", true)) return false;

            return ExecuteAction(Bot.AI.SendAnyPokemon());
        }

        // API: Sends the specified pokemon to battle.
        private bool SendPokemon(int index)
        {
            if (!ValidateAction("sendPokemon", true)) return false;

            if (index < 1 || index > Bot.Game.Team.Count)
            {
                Fatal("error: sendPokemon: tried to send the non-existing pokemon " + index + ".");
                return false;
            }

            return ExecuteAction(Bot.AI.SendPokemon(index));
        }

        // API: Changes the specified pokemon with the specified pokemon during double/triple battle.
        private bool SendPokemonDoubleBattle(int index, int changeWith)
        {
            if (!ValidateAction("sendPokemonDoubleBattle", true)) return false;

            if (index < 1 || index > Bot.Game.Team.Count || changeWith > Bot.AI.ActivePokemons.Length || changeWith < 1)
            {
                Fatal("error: sendPokemonDoubleBattle: tried to send the non-existing pokemon " + index + " or tried to change with the non-existing pokemon" + changeWith + " .");
                return false;
            }

            return ExecuteAction(Bot.AI.SendPokemon(index, changeWith));
        }

        // API: Uses the specified move in the current battle if available.
        private bool UseMove(string moveName)
        {
            if (!ValidateAction("useMove", true)) return false;

            return ExecuteAction(Bot.AI.UseMove(moveName));
        }

        // API: Uses the first available move or struggle if out of PP.
        private bool UseAnyMove()
        {
            if (!ValidateAction("useAnyMove", true)) return false;

            return ExecuteAction(Bot.AI.UseAnyMove());
        }

        // API: Forgets the specified move, if existing, in order to learn a new one.
        private bool ForgetMove(string moveName)
        {
            if (!Bot.MoveTeacher.IsLearning)
            {
                Fatal("error: ‘forgetMove’ can only be used when a pokémon is learning a new move.");
                return false;
            }

            moveName = moveName.ToUpperInvariant();
            Pokemon pokemon = Bot.Game.Team.Find(pok => pok.PokemonData.Pokemon.UniqueID == Bot.MoveTeacher.PokemonUniqueId);
            PokemonMove move = pokemon.Moves.FirstOrDefault(m => MovesManager.Instance.GetMoveData(m.Id)?.Name.ToUpperInvariant() == moveName);

            if (move != null)
            {
                Bot.MoveTeacher.MoveToForget = move.Position;
                return true;
            }
            Bot.MoveTeacher.MoveToForget = 5;
            return false;
        }

        // API: Forgets the first move that is not one of the specified moves.
        private bool ForgetAnyMoveExcept(DynValue[] moveNames)
        {
            if (!Bot.MoveTeacher.IsLearning)
            {
                Fatal("error: ‘forgetAnyMoveExcept’ can only be used when a pokémon is learning a new move.");
                return false;
            }

            HashSet<string> movesInvariantNames = new HashSet<string>();
            foreach (DynValue value in moveNames)
            {
                movesInvariantNames.Add(value.CastToString().ToUpperInvariant());
            }

            Pokemon pokemon = Bot.Game.Team.Find(pok => pok.PokemonData.Pokemon.UniqueID == Bot.MoveTeacher.PokemonUniqueId);
            PokemonMove move = pokemon.Moves.FirstOrDefault(m => !movesInvariantNames.Contains(m.Data?.Name.ToUpperInvariant()));

            if (move != null)
            {
                Bot.MoveTeacher.MoveToForget = move.Position;
                return true;
            }
            Bot.MoveTeacher.MoveToForget = 5;
            return false;
        }

        // API: Writes a string, a number, or a table of strings and/or numbers to file
        // overwrite is an optional parameter, and will append the line(s) if absent
        private void LogToFile(string file, DynValue text, bool overwrite = false)
        {
            DirectoryInfo directory = new DirectoryInfo("Logs/");
            FileInfo info = new FileInfo("Logs/" + file);

            // Restricting access to Logs folder
            if (!info.FullName.StartsWith(directory.FullName))
            {
                Fatal("Error: Invalid file write access");
                return;
            }

            // Creating all necessary folders
            Directory.CreateDirectory(Path.GetDirectoryName(info.FullName));

            StringBuilder sb = new StringBuilder();

            if (text.Type == DataType.Table)
            {
                DynValue[] lines = text.Table.Values.ToArray();
                for (int i = 0; i < lines.Length; i++)
                {
                    sb.AppendLine(lines[i].CastToString());
                }
            }
            else
            {
                sb.AppendLine(text.CastToString());
            }

            if (overwrite)
                File.WriteAllText(info.FullName, sb.ToString());
            else
                File.AppendAllText(info.FullName, sb.ToString());
        }

        // API: Returns a table of every line in file
        private string[] ReadLinesFromFile(string file)
        {
            DirectoryInfo directory = new DirectoryInfo("Logs/");
            FileInfo info = new FileInfo("Logs/" + file);

            if (!info.FullName.StartsWith(directory.FullName))
            {
                Fatal("Error: Invalid File read access");
                return new string[] { };
            }

            file = info.FullName;

            if (!File.Exists(file)) return new string[] { };
            return File.ReadAllLines(file);
        }

        // API: Returns true if the spcified id is the Main Quest's Id.
        private bool IsMainQuestId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return Bot.Game.Quests.Count > 0 && Bot.Game.Quests.Find(q => q.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)
                && q.Type == PSXAPI.Response.QuestType.Main) != null;
        }

        // API: Returns true if the spcified name is the Main Quest's Name.
        private bool IsMainQuest(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return Bot.Game.Quests.Count > 0 && Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                && q.Type == PSXAPI.Response.QuestType.Main) != null;
        }

        // API: Returns true if the specified quest id is completed.
        private bool IsQuestIdCompleted(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return Bot.Game.Quests.Count > 0 && Bot.Game.Quests.Find(q => q.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)
                && q.Completed) != null;
        }

        // API: Returns true if the specified quest name is completed.
        private bool IsQuestCompleted(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            return Bot.Game.Quests.Count > 0 && Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                && q.Completed) != null;
        }

        // API: Retruns true if path request was sent of the specified quest name.
        private bool IsQuestPathRequested(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (Bot.QuestManager.SelectedQuest != null && Bot.QuestManager.SelectedQuest.Name == name)
                return Bot.QuestManager.SelectedQuest.IsRequestedForPath;

            return Bot.Game.Quests.Count > 0 && Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)
                && q.IsRequestedForPath) != null;
        }

        // API: Retruns true if path request was sent of the specified quest name.
        private bool IsQuestIdPathRequested(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (Bot.QuestManager.SelectedQuest != null && Bot.QuestManager.SelectedQuest.Id == id)
                return Bot.QuestManager.SelectedQuest.IsRequestedForPath;

            return Bot.Game.Quests.Count > 0 && Bot.Game.Quests.Find(q => q.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase)
                && q.IsRequestedForPath) != null;
        }

        // Returns quest type from id as string.
        private string GetQuestIdType(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.Type.ToString() : null;
        }

        // API: Returns quest type from name as string.
        private string GetQuestType(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.Type.ToString() : null;
        }

        // API: Retruns id of a spcefied quest in string format.
        private string GetQuestId(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.Id : null;
        }

        // API: Returns the main quest's name.
        private string GetMainQuestName()
        {
            var findQuest = Bot.Game.Quests.Find(q => q.Type == PSXAPI.Response.QuestType.Main);
            return findQuest != null ? findQuest.Name : null;
        }

        // API: Returns the main quest's Id.
        private string GetMainQuestId()
        {
            var findQuest = Bot.Game.Quests.Find(q => q.Type == PSXAPI.Response.QuestType.Main);
            return findQuest != null ? findQuest.Id : null;
        }

        // API: Returns target area of a spcefied quest name.
        private string GetQuestTargetArea(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.TargetArea : null;
        }

        // API: Returns target area of a spcefied quest id.
        private string GetQuestIdTargetArea(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.TargetArea : null;
        }

        // API: Returns target completed area of a spcefied quest name.
        private string GetQuestTargetCompletedArea(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.TargetCompletedArea : null;
        }

        // API: Returns target completed area of a spcefied quest id.
        private string GetQuestIdTargetCompletedArea(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.TargetCompletedArea : null;
        }

        // API: Returns source npc name of a spcefied quest name.
        private string GetQuestSourceNpc(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.SourceNPC : null;
        }

        // API: Returns source npc name of a spcefied quest id.
        private string GetQuestIdSourceNpc(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.SourceNPC : null;
        }

        // API: Returns source area of a spcefied quest name.
        private string GetQuestSourceArea(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.SourceArea : null;
        }

        // API: Returns source area of a spcefied quest id.
        private string GetQuestIdSourceArea(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            var findQuest = Bot.Game.Quests.Find(q => q.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
            return findQuest != null ? findQuest.SourceArea : null;
        }



        // API: Requests path for spceified quest.
        private bool RequestPathForQuest(string name)
        {
            if (string.IsNullOrEmpty(name) || !ValidateAction("requestPathForQuest", false)) return false;
            var findQuest = Bot.Game.Quests.Find(q => q.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (findQuest is null) return false;
            Bot.QuestManager.SelectedQuest = findQuest;
            if (findQuest.IsRequestedForPath)
            {
                return ExecuteAction(true);
            }
            return !findQuest.Completed ? ExecuteAction(Bot.Game.RequestPathForInCompleteQuest(findQuest)) : 
                ExecuteAction(Bot.Game.RequestPathForCompletedQuest(findQuest));
        }

        // API: Requests path for spceified quest.
        private bool RequestPathForQuestId(string id)
        {
            if (string.IsNullOrEmpty(id) || !ValidateAction("requestPathForQuestId", false)) return false;
            var findQuest = Bot.Game.Quests.Find(q => q.Id.Equals(id, StringComparison.InvariantCultureIgnoreCase));
            if (findQuest is null) return false;
            Bot.QuestManager.SelectedQuest = findQuest;
            if (findQuest.IsRequestedForPath)
            {
                return ExecuteAction(true);
            }
            return !findQuest.Completed ? ExecuteAction(Bot.Game.RequestPathForInCompleteQuest(findQuest)) : 
                ExecuteAction(Bot.Game.RequestPathForCompletedQuest(findQuest));
        }

        // API: Gets destination id of a specified link.
        private string GetDestinationId(int x, int y)
        {
            if (!Bot.Game.Map.HasLink(x, y))
            {
                Fatal($"error: ‘getDestinationId’ there is no link at {x}, {y}");
                return null;
            }

            var destination = Bot.Game.Map.Links.Find(dest => dest.x == x && dest.z == -y);
            if (destination is null)
            {
                Fatal($"error: ‘getDestinationId’ there is no link at {x}, {y}");
                return null;
            }
            return destination.DestinationID.ToString();
        }

        // API: Gets the nearest moveable cell from specified cells.
        private DynValue[] GetNearestMoveAbleCell(params DynValue[] values)
        {
            if (values.Length != 1 && values.Length != 3 ||
                (values.Length == 1 && values[0].Type != DataType.Table) ||
                (values.Length == 3
                    && (values[0].Type != DataType.Number || values[1].Type != DataType.Number
                    || values[2].Type != DataType.Number)))
            {
                Fatal("error: getNearestMoveAbleCell: must receive either a table or three numbers.");
                return null;
            }
            if (values.Length == 1)
            {
                values = values[0].Table.Values.ToArray();
            }
            return GetNearestMoveAbleCell((int)values[0].Number, (int)values[1].Number, (int)values[2].Number, (int)values[3].Number);
        }

        // API: Gets the nearest moveable cell from specified cells.
        private DynValue[] GetNearestMoveAbleCell(int minX, int minY, int maxX, int maxY)
        {
            if (minX > maxX || minY > maxY)
            {
                Fatal("error: getNearestMoveAbleCell: the maximum cell cannot be less than the minimum cell.");
                return null;
            }
            var cells = new List<Tuple<int, int>>();
            for (int y = minY; y <= maxY; ++y)
            {
                for (int x = minX; x <= maxX; ++x)
                {
                    if (Bot.CanMoveTo(x, y))
                        cells.Add(new Tuple<int, int>(x, y));
                }
            }
            var orderedCells = cells.OrderBy(cell => GameClient.DistanceBetween(GetPlayerX(), GetPlayerY(), cell.Item1, cell.Item2)).ToList();
            if (orderedCells.Count > 0)
            {
                return new DynValue[] { DynValue.NewNumber(orderedCells.FirstOrDefault().Item1), DynValue.NewNumber(orderedCells.FirstOrDefault().Item2) };
            }
            return null;
        }

        // API: Uses an equipped mount.
        private bool UseEquippedMount()
        {
            if (!ValidateAction("useEquippedMount", false)) return false;
            return ExecuteAction(Bot.Game.UseMount());
        }

        // API: Counts received badges.

        private int CountBadges()
        {
            if(Bot.Game.PlayerStats is null && Bot.Game.Badges.Count <= 0)
            {
                Fatal("error: 'countBadges' haven't received player stats yet.");
                return -1;
            }
            return Bot.Game.Badges.Count;
        }

        // API: Retruns true if player contains specified badge id.
        private bool HasBadgeId(int id)
        {
            if (Bot.Game.PlayerStats is null && Bot.Game.Badges.Count <= 0)
            {
                Fatal("error: 'hasBadgeId' haven't received player stats yet.");
                return false;
            }
            return Bot.Game.Badges.ContainsKey(id);
        }

        // API: Retruns true if player contains specified badge.
        private bool HasBadge(string name)
        {
            if (Bot.Game.PlayerStats is null && Bot.Game.Badges.Count <= 0)
            {
                Fatal("error: 'hasBadge' haven't received player stats yet.");
                return false;
            }
            return Bot.Game.Badges.Values.Any(badge => badge.ToUpperInvariant() == name.ToUpperInvariant());
        }

        // API: Retruns player's kanto level.
        private int GetTrainerKantoLevel()
        {
            if (Bot.Game.PlayerStats is null)
            {
                Fatal("error: 'getTrainerKantoLevel' haven't received player stats yet.");
                return -1;
            }
            return Bot.Game.PlayerStats.KantoLevel;
        }

        // API: Returns player's johto level.
        private int GetTrainerJohtoLevel()
        {
            if (Bot.Game.PlayerStats is null)
            {
                Fatal("error: 'getTrainerJohtoLevel' haven't received player stats yet.");
                return -1;
            }
            return Bot.Game.PlayerStats.JohtoLevel;
        }

        // API: Asks for trainer info.
        private bool AskForTrainerInfo()
        {
            if (!ValidateAction("askForTrainerInfo", false)) return false;
            return ExecuteAction(Bot.Game.AskForPlayerStats());
        }

        // API: Returns true if have received trainer info.
        private bool IsTrainerInfoReceived()
        {
            return Bot.Game.PlayerStats != null;
        }
    }
}
