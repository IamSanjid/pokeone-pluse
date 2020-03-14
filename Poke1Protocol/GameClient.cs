﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using PSXAPI;
using PSXAPI.Request;
using PSXAPI.Response;
using PSXAPI.Response.Payload;
using Area = PSXAPI.Response.Area;
using Badges = PSXAPI.Response.Badges;
using BattleBroadcast = PSXAPI.Request.BattleBroadcast;
using BattleItem = PSXAPI.Request.BattleItem;
using BattleMove = PSXAPI.Request.BattleMove;
using ChatJoin = PSXAPI.Response.ChatJoin;
using ChatMessage = PSXAPI.Response.ChatMessage;
using Effect = PSXAPI.Response.Effect;
using Evolve = PSXAPI.Response.Evolve;
using Evs = PSXAPI.Response.Evs;
using Guild = PSXAPI.Response.Guild;
using GuildEmblem = PSXAPI.Request.GuildEmblem;
using HoldItem = PSXAPI.Request.HoldItem;
using Inventory = PSXAPI.Response.Inventory;
using Learn = PSXAPI.Response.Learn;
using Login = PSXAPI.Response.Login;
using Logout = PSXAPI.Response.Logout;
using Lootbox = PSXAPI.Response.Lootbox;
using LootboxAction = PSXAPI.Request.LootboxAction;
using LootboxType = PSXAPI.Response.LootboxType;
using Message = PSXAPI.Response.Message;
using MessageEvent = PSXAPI.Request.MessageEvent;
using Mount = PSXAPI.Response.Mount;
using Move = PSXAPI.Request.Move;
using MoveAction = PSXAPI.Request.MoveAction;
using Path = PSXAPI.Response.Path;
using Ping = PSXAPI.Request.Ping;
using Pokedex = PSXAPI.Response.Pokedex;
using Quest = PSXAPI.Response.Quest;
using Release = PSXAPI.Request.Release;
using Reorder = PSXAPI.Response.Reorder;
using Request = PSXAPI.Response.Request;
using RequestType = PSXAPI.Request.RequestType;
using Script = PSXAPI.Response.Script;
using Stats = PSXAPI.Response.Stats;
using Sync = PSXAPI.Response.Sync;
using Time = PSXAPI.Response.Time;
using Transfer = PSXAPI.Response.Transfer;
using UseItem = PSXAPI.Response.UseItem;

namespace Poke1Protocol
{
    public class GameClient : IDisposable
    {
        //&& !_moveRelearnerTimeout.IsActive

        public const string Version = "0.85";
        private readonly ProtocolTimeout _battleTimeout = new ProtocolTimeout();
        private readonly List<InventoryPokemon> _cachedPokemon = new List<InventoryPokemon>();

        private readonly GameConnection _connection;
        private Npc _cutOrRockSmashNpc;
        private readonly Queue<object> _dialogResponses = new Queue<object>();
        private readonly ProtocolTimeout _dialogTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _fishingTimeout = new ProtocolTimeout();
        private string _guildChannel = "";
        private byte _guildEmbedId;

        private readonly Dictionary<string, int> _guildLogos;
        private string _guildName = "";
        private bool _isLoggedIn;
        private readonly ProtocolTimeout _itemUseTimeout = new ProtocolTimeout();
        private double _lastCheckTime;
        private DateTime _lastGameTime;

        private double _lastRTime;
        private double _lastSentMovement;
        private int _lastTime;
        private readonly ProtocolTimeout _loadingTimeout = new ProtocolTimeout();
        private Guid _loginId;
        private readonly ProtocolTimeout _lootBoxTimeout = new ProtocolTimeout();

        private readonly MapClient _mapClient;
        private readonly ProtocolTimeout _mountingTimeout = new ProtocolTimeout();
        private readonly List<Move> _movementPackets;

        private readonly List<Direction> _movements;

        private readonly ProtocolTimeout _movementTimeout = new ProtocolTimeout();
        private bool _needToSendAck;
        private bool _needToSendSync;

        private Npc _npcBattler;
        private readonly ProtocolTimeout _npcBattleTimeout = new ProtocolTimeout();
        private string _partyChannel = "";
        private readonly ProtocolTimeout _refreshingPCBox = new ProtocolTimeout();
        private Direction? _slidingDirection;
        private bool _surfAfterMovement;
        private readonly ProtocolTimeout _swapTimeout = new ProtocolTimeout();
        private readonly ProtocolTimeout _teleportationTimeout = new ProtocolTimeout();
        private DateTime _updatePlayers;
        private bool _wasLoggedIn;
        public Direction LastDirection;

        private string mapChatChannel = "";

        private RC4Stream encryptStream;
        private RC4Stream decryptStream;

        public GameClient(GameConnection connection, MapConnection mapConnection)
        {
            _mapClient = new MapClient(mapConnection, this);
            _mapClient.ConnectionOpened += MapClient_ConnectionOpened;
            _mapClient.ConnectionClosed += MapClient_ConnectionClosed;
            _mapClient.ConnectionFailed += MapClient_ConnectionFailed;
            _mapClient.MapLoaded += MapClient_MapLoaded;

            _connection = connection;
            _connection.PacketReceived += OnPacketReceived;
            _connection.Connected += OnConnectionOpened;
            _connection.Disconnected += OnConnectionClosed;

            RecievedLootBoxes = new LootboxHandler();
            RecievedLootBoxes.LootBoxMessage += RecievedLootBoxMsg;
            RecievedLootBoxes.RecievedBox += RecievedLootBoxes_RecievedBox;
            RecievedLootBoxes.BoxOpened += RecievedLootBoxes_BoxOpened;

            Rand = new Random();
            lastPingResponseUtc = DateTime.UtcNow;
            timer = new Timer(Timer, null, PingUpdateTime, PingUpdateTime);
            disposedValue = false;
            _movements = new List<Direction>();
            _movementPackets = new List<Move>();
            Team = new List<Pokemon>();
            Items = new List<InventoryItem>();
            Channels = new Dictionary<string, ChatChannel>();
            Conversations = new List<string>();
            Players = new Dictionary<string, PlayerInfos>();
            _removedPlayers = new Dictionary<string, PlayerInfos>();
            PokedexPokemons = new List<PokedexPokemon>();
            _cachedScripts = new List<Script>();
            Scripts = new List<Script>();
            Effects = new List<PlayerEffect>();
            Quests = new List<PlayerQuest>();
            _guildLogos = new Dictionary<string, int>();
            Badges = new Dictionary<int, string>();
        }

        public string PlayerName { get; private set; }

        public int TotalSteps { get; private set; }

        public string MapName { get; private set; } = "";
        public string AreaName { get; private set; } = "";

        public Level Level { get; private set; }

        public Battle ActiveBattle { get; private set; }

        public double RunningForSeconds => GetRunningTimeInSeconds();

        public bool IsConnected { get; private set; }

        public bool IsMapLoaded =>
            Map != null;

        public bool IsTeleporting =>
            _teleportationTimeout.IsActive;

        public bool IsInactive =>
            _movements.Count == 0
            && !_movementTimeout.IsActive
            && !_battleTimeout.IsActive
            && !_loadingTimeout.IsActive
            && !_mountingTimeout.IsActive
            && !_teleportationTimeout.IsActive
            && !_swapTimeout.IsActive
            && !_dialogTimeout.IsActive
            && !_lootBoxTimeout.IsActive
            && !_itemUseTimeout.IsActive
            && !_fishingTimeout.IsActive
            && !_npcBattleTimeout.IsActive
            && !_refreshingPCBox.IsActive;

        public LootboxHandler RecievedLootBoxes { get; }

        public Shop OpenedShop { get; private set; }
        public PlayerStats PlayerStats { get; private set; }


        public string[] DialogContent { get; private set; }
        public Time LastTimePacket { get; private set; }
        public string PokeTime { get; private set; }
        public string GameTime { get; private set; }
        public string Weather { get; private set; }
        public int PlayerX { get; private set; }
        public int PlayerY { get; private set; }
        public List<Pokemon> Team { get; private set; }
        public List<Pokemon> CurrentPCBox { get; private set; }
        public List<PlayerQuest> Quests { get; private set; }
        public List<PlayerEffect> Effects { get; }
        public List<InventoryItem> Items { get; }
        public Dictionary<string, ChatChannel> Channels { get; }
        public List<string> Conversations { get; }
        public Dictionary<string, PlayerInfos> Players { get; }
        private Dictionary<string, PlayerInfos> _removedPlayers { get; }
        public Random Rand { get; }

        public Map Map { get; private set; }
        public int Money { get; private set; }
        public int Gold { get; private set; }
        public int CurrentPCBoxId { get; private set; }
        public bool IsPCBoxRefreshing { get; private set; }
        public int UsedPCBoxes { get; private set; }
        public int PCTotalPokemon { get; private set; }
        public PokeboxSummary BoxSummary { get; private set; }
        public Dictionary<int, string> Badges { get; }

        public bool IsInBattle { get; private set; }
        public bool IsOnGround { get; private set; }
        public bool IsPCOpen { get; private set; }
        public bool IsSurfing { get; private set; }
        public bool IsBiking { get; private set; }
        public bool IsLoggedIn => _isLoggedIn && IsConnected && _connection.IsConnected;
        public bool CanUseCut { get; private set; }
        public bool CanUseSmashRock { get; private set; }
        public int PokedexOwned { get; private set; }
        public int PokedexSeen { get; private set; }
        public bool AreNpcReceived { get; private set; }
        public bool IsAuthenticated { get; private set; }

        private ScriptRequestType _currentScriptType { get; set; }
        private Script _currentScript { get; set; }
        public List<PokedexPokemon> PokedexPokemons { get; }
        private List<Script> Scripts { get; }
        private List<Script> _cachedScripts { get; }
        private MapUsers _cachedNerbyUsers { get; set; }

        public TimeSpan PingUpdateTime
        {
            get => pingUpdateTime;
            set
            {
                if (PingUpdateTime == value) return;
                pingUpdateTime = value;
                timer.Change(TimeSpan.FromSeconds(0.0), value);
            }
        }

        public int Ping
        {
            get
            {
                if (!IsConnected) return -1;
                var t = DateTime.UtcNow - lastPingResponseUtc;
                if (t > PingUpdateTime + TimeSpan.FromSeconds(2.0)) return (int) t.TotalMilliseconds;
                return ping;
            }
        }

        public void Dispose()
        {
            if (!disposedValue)
            {
                timer.Dispose();
                disposedValue = true;
            }

            GC.SuppressFinalize(this);
        }

        public event Action ConnectionOpened;
        public event Action LinksUpdated;
        public event Action<Exception> ConnectionFailed;
        public event Action<Exception> ConnectionClosed;
        public event Action<LoginError> AuthenticationFailed;
        public event Action LoggedIn;
        public event Action<string, string> GameTimeUpdated;
        public event Action<string, int, int> PositionUpdated;
        public event Action<string, int, int> TeleportationOccuring;
        public event Action InventoryUpdated;
        public event Action PokemonsUpdated;
        public event Action<string> MapLoaded;
        public event Action<string> SystemMessage;
        public event Action<string> LootBoxMessage;
        public event Action<Lootbox> RecievedLootBox;
        public event Action<string> LogMessage;
        public event Action BattleStarted;
        public event Action<string> BattleMessage;
        public event Action BattleEnded;
        public event Action<List<PokedexPokemon>> PokedexUpdated;
        public event Action<PlayerInfos> PlayerUpdated;
        public event Action<PlayerInfos> PlayerAdded;
        public event Action<PlayerInfos> PlayerRemoved;
        public event Action<Level, Level> LevelChanged;
        public event Action RefreshChannelList;
        public event Action<string, string, string> ChannelMessage;
        public event Action<string, string, string> PrivateMessage;
        public event Action<string, string, string> LeavePrivateMessage;
        public event Action<string> DialogOpened;
        public event Action<LootboxRoll[], LootboxType> LootBoxOpened;
        public event Action<Guid> Evolving;
        public event Action<PokemonMoveID, int, Guid> LearningMove;
        public event Action<List<PlayerQuest>> QuestsUpdated;
        public event Action<Path> ReceivedPath;
        public event Action<List<Npc>> NpcReceieved;
        public event Action<Shop> ShopOpened;
        public event Action<string> ServerCommandException;
        public event Action BattleUpdated;
        public event Action<Npc> MoveToBattleWithNpc;
        public event Action<List<Pokemon>> PCBoxUpdated;
        public event Action MountUpdated;
        public event Action<string, string, byte[]> InvalidPacket;

        private static double GetRunningTimeInSeconds()
        {
            var totalTime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return totalTime.TotalSeconds;
        }

        private void AddDefaultChannels()
        {
            Channels.Add("General", new ChatChannel("default", "General", ""));
            Channels.Add("System", new ChatChannel("default", "General", ""));
            Channels.Add("Map", new ChatChannel("default", "Map", ""));
            Channels.Add("Party", new ChatChannel("default", "Party", ""));
            Channels.Add("Battle", new ChatChannel("default", "Battle", ""));
            Channels.Add("Guild", new ChatChannel("default", "Guild", ""));
        }


        public void Move(Direction direction)
        {
            _movements.Add(direction);
        }

        public void PushDialogAnswer(int index)
        {
            _dialogResponses.Enqueue(index);
        }

        public void PushDialogAnswer(string text)
        {
            _dialogResponses.Enqueue(text);
        }

        public void Open()
        {
            _connection.Connect();
        }

        public void Close(Exception error = null)
        {
            _connection.Close(error);
        }

        public void ClearPath()
        {
            _movements.Clear();
            _movementPackets.Clear();
            _lastSentMovement = RunningForSeconds;
        }

        public void Update()
        {
            _mapClient.Update();
            _connection.Update();

            if (!IsAuthenticated)
                return;

            _swapTimeout.Update();
            _movementTimeout.Update();
            _teleportationTimeout.Update();
            _battleTimeout.Update();
            _dialogTimeout.Update();
            _loadingTimeout.Update();
            _lootBoxTimeout.Update();
            _itemUseTimeout.Update();
            _fishingTimeout.Update();
            _mountingTimeout.Update();
            _refreshingPCBox.Update();

            UpdateScript();
            UpdatePlayers();
            UpdateNpcBattle();
            UpdateRegularPacket();
            UpdateMovement();
            UpdateTime();
            UpdatePC();
            RecievedLootBoxes.UpdateFreeLootBox();
        }

        private void UpdatePC()
        {
            if (!IsPCBoxRefreshing && CurrentPCBox != null)
                IsPCOpen = true;
            else
                IsPCOpen = false;
        }

        private void UpdateNpcBattle()
        {
            if (_npcBattler == null) return;

            _npcBattleTimeout.Update();

            if (_npcBattleTimeout.IsActive) return;

            if (_npcBattler.SightAction == SightAction.PlayerToNPC &&
                DistanceBetween(_npcBattler.PositionX, _npcBattler.PositionY, PlayerX, PlayerY) != 1) return;

            TalkToNpc(_npcBattler);
            _npcBattler = null;
        }

        private void UpdateTime()
        {
            if (LastTimePacket != null)
            {
                if (_lastCheckTime + 3 < RunningForSeconds)
                {
                    _lastCheckTime = RunningForSeconds;
                    GameTime = LastTimePacket.GameDayTime + " " + GetGameTime(LastTimePacket.GameTime,
                                   LastTimePacket.TimeFactor, _lastGameTime);
                    PokeTime = GetGameTime(LastTimePacket.GameTime, LastTimePacket.TimeFactor, _lastGameTime)
                        .Replace(" PM", "").Replace(" AM", "");
                    Weather = LastTimePacket.Weather.ToString();
                    GameTimeUpdated?.Invoke(GameTime, Weather);
                }
            }
        }

        // IDK POKEONE CHECK SOMETHING LIKE BELOW.
        private void UpdateRegularPacket()
        {
            if (_needToSendSync && IsInBattle && !_battleTimeout.IsActive)
            {
                Resync(false);
                _needToSendSync = false;
                _battleTimeout.Set(1500);
            }

            if (RunningForSeconds > _lastCheckPing + 2)
            {
                _lastCheckPing = RunningForSeconds;
                if (IsLoggedIn && !string.IsNullOrEmpty(PlayerName))
                {
                    if (Ping >= 5000)
                    {
                        if (PingUpdateTime.TotalSeconds != 2.0) PingUpdateTime = TimeSpan.FromSeconds(5.0);
                    }
                    else
                    {
                        if (PingUpdateTime.TotalSeconds != 2.0) PingUpdateTime = TimeSpan.FromSeconds(5.0);
                    }
                }
            }

            //if (RunningForSeconds > _lastSentMovement + 0.6f && _movementPackets.Count > 0)
            //    SendMovemnetPackets();
        }

        // Don't ask me it is PokeOne's way lol...
        //private void SendMovemnetPackets()
        //{
        //    _lastSentMovement = RunningForSeconds;
        //    if (_movementPackets.Count > 0)
        //    {
        //        var list = new List<PSXAPI.Request.MoveAction>();
        //        int i = 0;

        //        for (int j = 0; i < _movementPackets.Count; ++j)
        //        {

        //        }

        //        while (i < _movementPackets.Count)
        //        {
        //            if (i + 1 >= _movementPackets.Count)
        //            {
        //                goto IL_FF;
        //            }
        //            if (_movementPackets[i].Actions[0] != PSXAPI.Request.MoveAction.TurnDown || _movementPackets[i + 1].Actions[0] != PSXAPI.Request.MoveAction.Down)
        //            {
        //                if (_movementPackets[i].Actions[0] != PSXAPI.Request.MoveAction.TurnLeft || _movementPackets[i + 1].Actions[0] != PSXAPI.Request.MoveAction.Left)
        //                {
        //                    if (_movementPackets[i].Actions[0] != PSXAPI.Request.MoveAction.TurnRight || _movementPackets[i + 1].Actions[0] != PSXAPI.Request.MoveAction.Right)
        //                    {
        //                        if (_movementPackets[i].Actions[0] != PSXAPI.Request.MoveAction.TurnUp || _movementPackets[i + 1].Actions[0] != PSXAPI.Request.MoveAction.Up)
        //                        {
        //                            goto IL_FF;
        //                        }
        //                    }
        //                }
        //            }
        //            IL_118:
        //            i++;
        //            continue;
        //            IL_FF:
        //            list.Add(_movementPackets[i].Actions[0]);
        //            goto IL_118;
        //        }
        //        if (list.Count > 2)
        //            Console.WriteLine("Yes I am working like PokeOne :D");
        //        SendProto(new PSXAPI.Request.Move
        //        {
        //            Actions = list.ToArray(),
        //            Map = _movementPackets[0].Map,
        //            X = _movementPackets[0].X,
        //            Y = _movementPackets[0].Y
        //        });
        //        _movementPackets.Clear();
        //    }
        //}

        private void CheckEvolving()
        {
            var evolvingPoke = Team.FirstOrDefault(pok => pok.CanEvolveTo > PokemonID.missingno);

            if (evolvingPoke != null) OnEvolving(evolvingPoke);
        }

        private void CheckLearningMove()
        {
            var learningPoke = Team.FirstOrDefault(pok => pok.LearnableMoves != null && pok.LearnableMoves.Length > 0);

            if (learningPoke != null) OnLearningMove(learningPoke);
        }

        private void UpdateMovement()
        {
            if (!IsMapLoaded) return;

            if (!_movementTimeout.IsActive && _movements.Count > 0)
            {
                //SendMovemnetPackets();
                var direction = _movements[0];
                _movements.RemoveAt(0);
                var fromX = PlayerX;
                var fromY = PlayerY;
                if (ApplyMovement(direction))
                {
                    var actions = direction.ToMoveActions().ToList();
                    LastDirection = direction;
                    _movementTimeout.Set(IsBiking ? 150 : 300);
                    if (Map.HasLink(PlayerX, PlayerY))
                        _teleportationTimeout.Set();
                    else
                        CheckForNpcInteraction(ref actions);
                    SendMovement(actions.ToArray(), fromX,
                        fromY); // PokeOne sends the (x,y) without applying the movement(but it checks the collisions) to the server.
                }

                if (_movements.Count == 0 && _surfAfterMovement)
                {
                    var waterDir = Map.GetWaterDirectionFrom(PlayerX, PlayerY);
                    if (waterDir != Direction.None && waterDir != LastDirection)
                    {
                        // Facing to the water....
                        SendMovement(new[] {waterDir.ToOneStepMoveActions()}, PlayerX, PlayerY);
                        LastDirection = waterDir;
                    }

                    _movementTimeout.Set(Rand.Next(750, 900));
                }
            }

            if (!_movementTimeout.IsActive && _movements.Count == 0 && _surfAfterMovement)
            {
                _surfAfterMovement = false;
                UseSurf();
            }

            if (!_movementTimeout.IsActive && _movements.Count == 0 && _cutOrRockSmashNpc != null
                && DistanceBetween(_cutOrRockSmashNpc.PositionX, _cutOrRockSmashNpc.PositionY, PlayerX, PlayerY) == 1)
            {
                var npcDir = _cutOrRockSmashNpc.GetDriectionFrom(PlayerX, PlayerY);
                if (npcDir != Direction.None && npcDir != LastDirection)
                {
                    // Facing to the cut or rock smash npc....
                    SendMovement(new[] {npcDir.ToOneStepMoveActions()}, PlayerX, PlayerY);
                    LastDirection = npcDir;
                }

                TalkToNpc(_cutOrRockSmashNpc);
                _cutOrRockSmashNpc = null;
            }
        }

        private void CheckForNpcInteraction(ref List<MoveAction> actions)
        {
            var battler = Map.Npcs.FirstOrDefault(npc => npc.CanBattle && npc.IsInLineOfSight(PlayerX, PlayerY));
            if (battler != null && _npcBattler != battler && !IsInBattle)
            {
                ClearPath();
                var fromNpcDir = battler.Direction.GetOpposite();
                if (LastDirection != fromNpcDir)
                {
                    actions.Add(fromNpcDir.ToOneStepMoveActions());
                    LastDirection = fromNpcDir;
                }

                battler.CanBattle = false;
                LogMessage?.Invoke("The NPC " + (battler.NpcName ?? battler.Id.ToString()) + " saw us, interacting...");
                var distanceFromBattler = DistanceBetween(PlayerX, PlayerY, battler.PositionX, battler.PositionY);
                if (battler.SightAction == SightAction.PlayerToNPC)
                {
                    //npcs which will ask the player to come to them lol....
                    MoveToBattleWithNpc?.Invoke(battler);
                    _npcBattleTimeout.Set(Rand.Next(1000, 2000) + distanceFromBattler);
                    _npcBattler = battler;
                }
                else
                {
                    //npcs which going to come to the player...
                    _npcBattleTimeout.Set(Rand.Next(1000, 2000) + distanceFromBattler * (IsBiking ? 150 : 300));
                    _npcBattler = battler;
                }
            }
        }

        private void UpdatePlayers()
        {
            if (_updatePlayers < DateTime.UtcNow)
            {
                foreach (var playerName in Players.Keys.ToArray())
                    if (Players[playerName].IsExpired())
                    {
                        var player = Players[playerName];

                        PlayerRemoved?.Invoke(player);

                        if (_removedPlayers.ContainsKey(player.Name))
                            _removedPlayers[player.Name] = player;
                        else
                            _removedPlayers.Add(player.Name, player);

                        Players.Remove(playerName);
                    }

                _updatePlayers = DateTime.UtcNow.AddSeconds(5);
            }
        }

        private void UpdateScript()
        {
            if (_cachedScripts.Count > 0 && IsMapLoaded)
            {
                var cacheScript = _cachedScripts[0];
                _cachedScripts.RemoveAt(0);

                var processTexts = new List<string>();

                if (cacheScript.Text != null)
                    foreach (var scriptText in cacheScript.Text)
                        if (!scriptText.Text.EndsWith(")", StringComparison.CurrentCulture) &&
                            scriptText.Text.IndexOf("(", StringComparison.CurrentCulture) == -1)
                            DialogOpened?.Invoke(Regex.Replace(scriptText.Text, @"\[(\/|.\w+)\]", ""));
                        else
                            processTexts.Add(scriptText.Text);
                if (processTexts.Count > 0) ProcessScriptMessage(processTexts.ToArray());
                //_dialogTimeout.Set();
            }

            if (IsMapLoaded && !_dialogTimeout.IsActive && Scripts.Count > 0)
            {
                var script = Scripts[0];
                Scripts.RemoveAt(0);

                DialogContent = script.Data;

                switch (script.Type)
                {
                    case ScriptRequestType.Choice:
                        if (_dialogResponses.Count <= 0)
                            SendScriptResponse(script.ScriptID, "0");
                        else
                            SendScriptResponse(script.ScriptID, GetNextDialogResponse().ToString());
                        _dialogTimeout.Set();
                        break;
                    case ScriptRequestType.WalkNpc:
                        if (DialogContent != null && DialogContent.Length >= 2)
                        {
                            var walkingNpc = Map?.Npcs?.Find(npc => npc.Id == Guid.Parse(DialogContent[0]));
                            if (walkingNpc != null) walkingNpc.ProcessActions(DialogContent[1]);
                        }

                        if (IsMapLoaded)
                        {
                            AreNpcReceived = true;
                            NpcReceieved?.Invoke(Map.Npcs);
                        }

                        SendScriptResponse(script.ScriptID, "");
                        _dialogTimeout.Set();
                        break;
                    case ScriptRequestType.WalkUser:
                        SendScriptResponse(script.ScriptID, "");
                        //_dialogTimeout.Set();
                        if (script.Data != null && script.Data.Length > 0)
                            foreach (var d in script.Data)
                            {
                                var dir = LastDirection;
                                var x = PlayerX;
                                var y = PlayerY;

                                DirectionExtensions.ApplyToDirectionFromChar(ref dir, d, ref x, ref y);

                                if (x != PlayerX || y != PlayerY)
                                    foreach (var c in d)
                                        if (c == 'd' || c == 'l' || c == 'r' || c == 'u')
                                            Move(DirectionExtensions.FromChar(c));
                                LastDirection = dir;
                            }

                        break;
                    case ScriptRequestType.WaitForInput:
                        SendScriptResponse(script.ScriptID, "");
                        _dialogTimeout.Set();
                        break;
                    case ScriptRequestType.Unfreeze:
                        _dialogResponses.Clear();
                        break;
                    case ScriptRequestType.Shop:
                        OpenedShop = new Shop(script.Data, script.ScriptID);
                        ShopOpened?.Invoke(OpenedShop);
                        break;
                    case ScriptRequestType.SelectItem:
                        if (_dialogResponses.Count <= 0)
                            SendScriptResponse(script.ScriptID, "-1");
                        else
                            SendScriptResponse(script.ScriptID, GetNextSelectionResponse() ?? "-1");
                        _dialogTimeout.Set();
                        break;
                    default:
#if DEBUG
                        Console.WriteLine($"UNKNOWN SCRIPT TYPE: {script.Type}");
#endif
                        break;
                }
            }
        }

        private string GetNextSelectionResponse()
        {
            if (_dialogResponses.Count > 0)
            {
                var response = _dialogResponses.Dequeue();
                return response.ToString() ?? null;
            }

            return null;
        }

        private int GetNextDialogResponse()
        {
            if (_dialogResponses.Count > 0)
            {
                var response = _dialogResponses.Dequeue();
                if (response is int) return (int) response;

                if (response is string)
                {
                    var text = ((string) response).ToUpperInvariant();
                    for (var i = 0; i < DialogContent.Length; ++i)
                    {
                        var option = Regex.Replace(DialogContent[i].ToUpperInvariant(), @"\[(\/|.\w+)\]", "");
                        if (option.ToUpperInvariant().Equals(text)) return i;
                    }
                }
            }

            return 0;
        }

        private bool ApplyMovement(Direction direction)
        {
            var destinationX = PlayerX;
            var destinationY = PlayerY;
            var isOnGround = IsOnGround;
            var isSurfing = IsSurfing;

            direction.ApplyToCoordinates(ref destinationX, ref destinationY);
            var result = Map.CanMove(direction, destinationX, destinationY, isOnGround, isSurfing, CanUseCut,
                CanUseSmashRock);
            if (Map.ApplyMovement(direction, result, ref destinationX, ref destinationY, ref isOnGround, ref isSurfing))
            {
                PlayerX = destinationX;
                PlayerY = destinationY;
                IsOnGround = isOnGround;
                IsSurfing = isSurfing;
                CheckArea();
                if (result == Map.MoveResult.Icing) _movements.Insert(0, direction);

                if (result == Map.MoveResult.Sliding)
                {
                    var slider = Map.GetSlider(destinationX, destinationY);
                    if (slider != -1) _slidingDirection = Map.SliderToDirection(slider);
                }

                if (_slidingDirection != null) _movements.Insert(0, direction);

                return true;
            }

            return false;
        }

        private void MapClient_ConnectionOpened()
        {
#if DEBUG
            Console.WriteLine("[+++] Connected to the map server");
#endif
            if (MapName != null && Map is null)
                _mapClient.DownloadMap(MapName);
        }

        private void MapClient_ConnectionFailed(Exception ex)
        {
            ConnectionFailed?.Invoke(ex);
            _connection.Close();
        }

        private void MapClient_ConnectionClosed(Exception ex)
        {
            Close(ex);
        }

        private void OnPacketReceived(string packet)
        {
#if DEBUG
            Console.WriteLine("Receiving Packet [<]: " + packet);
#endif
            ProcessPacket(packet);
        }

        private void OnConnectionOpened()
        {
            IsConnected = true;
#if DEBUG
            Console.WriteLine("[+++] Connection opened");
#endif
            _mapClient.Open();
            ConnectionOpened?.Invoke();

            lastPingResponseUtc = DateTime.UtcNow;
            receivedPing = true;
        }

        private void OnConnectionClosed(Exception ex)
        {
            _isLoggedIn = false;
            if (_mapClient.IsConnected)
                _mapClient.Close();

            if (!IsConnected)
            {
#if DEBUG
                Console.WriteLine("[---] Connection failed");
#endif
                ConnectionFailed?.Invoke(ex);
            }
            else
            {
                IsConnected = false;
#if DEBUG
                Console.WriteLine("[---] Connection closed");
#endif
                ConnectionClosed?.Invoke(ex);
            }
        }

        private void Timer(object obj)
        {
            if (IsConnected && receivedPing && !disposedValue)
            {
                receivedPing = false;
                SendProto(new Ping
                {
                    DateTimeUtc = DateTime.UtcNow
                });
            }
        }

        private void SendAck()
        {
            _needToSendAck = false;
            var s = new Ack
            {
                Data = StringCipher.EncryptOrDecryptToBase64Byte(PlayerName.ReverseString(), _loginId.ToString().ReverseString())
            };
            SendProto(s);
        }

        private void SendSwapPokemons(int poke1, int poke2)
        {
            var packet = new PSXAPI.Request.Reorder
            {
                Pokemon = Team[poke1 - 1].PokemonData.Pokemon.UniqueID,
                Position = poke2
            };
            SendProto(packet);
        }

        private void SendScriptResponse(Guid id, string response)
        {
            SendProto(new PSXAPI.Request.Script
            {
                Response = response,
                ScriptID = id
            });
        }

        private void SendTalkToNpc(Guid npcId)
        {
            SendProto(new Talk
            {
                NpcID = npcId
            });
        }

        private void SendRefreshPCBox(int box)
        {
            SendProto(new PSXAPI.Request.Pokemon
            {
                Box = box
            });
        }

        private void SendReleasePokemon(Guid pokemonGuid)
        {
            SendProto(new Release
            {
                Pokemon = pokemonGuid
            });
        }

        private void SendPCSwapPokemon(Guid boxPokemonGuid, Guid teamPokemonGuid)
        {
            SendProto(new Swap
            {
                Pokemon1 = boxPokemonGuid,
                Pokemon2 = teamPokemonGuid
            });
        }

        private void SendMovePokemonFromPC(Guid pokemonGuid)
        {
            SendProto(new PSXAPI.Request.Transfer
            {
                Box = 0,
                Pokemon = pokemonGuid
            });
        }

        private void SendMovePokemonToPC(Guid pokemonUid)
        {
            SendProto(new PSXAPI.Request.Transfer
            {
                Box = CurrentPCBoxId,
                Pokemon = pokemonUid
            });
        }

        private void SendSetCollectedEvs(Guid pokemonGuid, string statType, PokemonStats evs, int amount)
        {
            var packet = EffortValuesManager.GetEvsSetPacket(statType, pokemonGuid, evs, amount);
            SendProto(packet);
        }

        public void SendProto(IProto proto)
        {
            var array = Proto.Serialize(proto);           
            if (array == null) return;
            if (encryptStream != null)
                array = encryptStream.Crypt(array);
            var packet = Convert.ToBase64String(array);
            packet = proto._Name + " " + packet;
            SendPacket(packet);
        }

        public void SendPacket(string packet)
        {
#if DEBUG
            Console.WriteLine("Sending Packet [>]: " + packet);
#endif
            _connection.Send(packet);
        }

        private void SendRequestGuildLogo(string name, int version = 0)
        {
            if (_guildLogos.ContainsKey(name.ToUpperInvariant()))
            {
                if (_guildLogos[name.ToUpperInvariant()] != version)
                {
                    _guildLogos[name.ToUpperInvariant()] = version;
                    SendProto(new GuildEmblem
                    {
                        Name = name
                    });
                }
            }
            else
            {
                _guildLogos.Add(name.ToUpperInvariant(), version);
                SendProto(new GuildEmblem
                {
                    Name = name
                });
            }
        }

        public void SendMessage(string channel, string message)
        {
            if (channel == "Map")
                channel = mapChatChannel;
            if (channel == "Party")
                channel = _partyChannel;
            if (channel == "Guild")
                channel = _guildChannel;
            var pokeList = new List<Guid>();
            SendProto(new PSXAPI.Request.ChatMessage
            {
                Channel = channel,
                Message = message,
                Pokemon = pokeList.ToArray()
            });
        }

        public void CloseChannel(string channel)
        {
            if (Channels.Any(c => c.Key == channel) || channel.StartsWith("map:", StringComparison.CurrentCulture))
                SendProto(new PSXAPI.Request.ChatJoin
                {
                    Channel = channel,
                    Action = ChatJoinAction.Leave
                });
        }

        public void CloseConversation(string pmName)
        {
            if (Conversations.Contains(pmName))
                SendProto(new PSXAPI.Request.Message
                {
                    Event = MessageEvent.Closed,
                    Name = pmName,
                    Text = ""
                });
        }

        public void SendMovement(MoveAction[] actions, int fromX, int fromY)
        {
            OpenedShop = null;
            CurrentPCBox = null;
            TotalSteps += actions.Count(m =>
                             m != MoveAction.TurnDown && m != MoveAction.TurnLeft
                                                      && m != MoveAction.TurnRight && m != MoveAction.TurnUp);

            var movePacket = new Move
            {
                Actions = actions,
                Map = MapName,
                X = fromX,
                Y = fromY
            };
            //_movementPackets.Add(movePacket);
            SendProto(movePacket);
        }

        public bool OpenLootBox(PSXAPI.Request.LootboxType type)
        {
            if (RecievedLootBoxes != null && RecievedLootBoxes.TotalLootBoxes > 0)
            {
                SendOpenLootBox(type);
                _lootBoxTimeout.Set();
                return true;
            }

            return false;
        }


        public void TalkToNpc(Npc npc)
        {
            SendTalkToNpc(npc.Id);
            npc.CanBattle = false;
            _dialogTimeout.Set();
        }

        private void SendCharacterCustomization(int gender, int skin, int hair, int haircolour, int eyes)
        {
            var packet = new PSXAPI.Request.Script
            {
                Response = string.Concat(gender.ToString(), ",", skin.ToString(), ",", eyes.ToString(), ",",
                    hair.ToString(), ",", haircolour.ToString()),
                ScriptID = _currentScript.ScriptID
            };
            SendProto(packet);
            _dialogTimeout.Set();
        }

        private void SendOpenLootBox(PSXAPI.Request.LootboxType type)
        {
            SendProto(new PSXAPI.Request.Lootbox
            {
                Action = LootboxAction.Open,
                Type = type
            });
        }

        private void SendJoinChannel(string channel)
        {
            SendProto(new PSXAPI.Request.ChatJoin
            {
                Channel = channel,
                Action = ChatJoinAction.Join
            });
        }

        public void SendPrivateMessage(string nickname, string text)
        {
            if (!Conversations.Contains(nickname))
                Conversations.Add(nickname);
            SendProto(new Message
            {
                Event = PSXAPI.Response.MessageEvent.Message,
                Name = nickname,
                Text = text
            });
        }

        private void SendAttack(int id, int selected, int opponent, bool megaEvo)
        {
            if (id > 0)
                SendProto(new BattleBroadcast
                {
                    RequestID = ActiveBattle.ResponseID,
                    Message = string.Concat("1|", PlayerName, "|", opponent.ToString(), "|",
                        ActiveBattle.Turn.ToString(), "|", (selected - 1).ToString(), "|",
                        ActiveBattle.AttackTargetType(id, selected))
                });
            SendProto(new BattleMove
            {
                MoveID = id,
                Target = opponent,
                Position = selected,
                RequestID = ActiveBattle.ResponseID,
                MegaEvo = megaEvo,
                ZMove = false
            });
        }

        private void SendRunFromBattle(int selected)
        {
            SendProto(new BattleRun
            {
                RequestID = ActiveBattle.ResponseID
            });

            SendProto(new BattleBroadcast
            {
                RequestID = ActiveBattle.ResponseID,
                Message = string.Concat("5|", PlayerName, "|0|", ActiveBattle.Turn.ToString(), "|",
                    (selected - 1).ToString())
            });
        }

        private void SendChangePokemon(int currentPos, int newPos)
        {
            SendProto(new BattleSwitch
            {
                RequestID = ActiveBattle.ResponseID,
                Position = currentPos,
                NewPosition = newPos
            });
        }

        private void SendUseItemInBattle(int id, int targetId, int selected, int moveTarget = 0)
        {
            SendProto(new BattleBroadcast
            {
                RequestID = ActiveBattle.ResponseID,
                Message = string.Concat("2|", PlayerName, "|", targetId.ToString(), "|", ActiveBattle.Turn.ToString(),
                    "|", (selected - 1).ToString())
            });

            SendProto(new BattleItem
            {
                Item = id,
                RequestID = ActiveBattle.ResponseID,
                Target = targetId,
                TargetMove = moveTarget,
                Position = selected
            });
        }

        public void SendAcceptEvolution(Guid evolvingPokemonUid)
        {
            SendProto(new PSXAPI.Request.Evolve
            {
                Accept = true,
                Pokemon = evolvingPokemonUid
            });
        }

        public void SendCancelEvolution(Guid evolvingPokemonUid)
        {
            SendProto(new PSXAPI.Request.Evolve
            {
                Accept = false,
                Pokemon = evolvingPokemonUid
            });
        }

        private void SendUseItem(int id, int pokemonUid = 0, int moveId = 0)
        {
            Pokemon foundPoke = null;
            if (pokemonUid > 0)
                foundPoke = Team[pokemonUid - 1];
            SendProto(new PSXAPI.Request.UseItem
            {
                Item = id,
                Move = moveId,
                Pokemon = foundPoke != null ? foundPoke.UniqueID : default(Guid)
            });
        }

        private void SendGiveItem(Guid id, int itemId)
        {
            SendProto(new HoldItem
            {
                Item = itemId,
                Pokemon = id,
                Remove = false
            });
        }

        // The held item will be lost forever
        private void SendRemoveHeldItem(Guid id)
        {
            SendProto(new HoldItem
            {
                Item = 0,
                Pokemon = id,
                Remove = true
            });
        }

        private void SendUseMount()
        {
            SendProto(new PSXAPI.Request.Mount());
        }

        public bool UseMount()
        {
            if (IsMapLoaded && Map.IsOutside)
            {
                SendUseMount();
                return true;
            }

            return false;
        }

        private void SendShopPokemart(Guid scriptId, int itemId, int quantity)
        {
            var response = itemId + "," + quantity + "," + "0";
            SendScriptResponse(scriptId, response);
        }

        public bool BuyItem(int itemId, int quantity)
        {
            if (OpenedShop != null && OpenedShop.Items.Any(item => item.Id == itemId))
            {
                _itemUseTimeout.Set();
                SendShopPokemart(OpenedShop.ScriptId, itemId, quantity);
                CloseShop();
                return true;
            }

            return false;
        }

        public bool CloseShop()
        {
            if (OpenedShop != null)
            {
                SendScriptResponse(OpenedShop.ScriptId, "");
                OpenedShop = null;
                return true;
            }

            return false;
        }

        private void SendPlayerStatsRequest(string username = null)
        {
            SendProto(new PSXAPI.Request.Stats
            {
                Username = username
            });
        }

        // Asking trainer card info
        public bool AskForPlayerStats()
        {
            if (PlayerStats is null)
            {
                SendPlayerStatsRequest();
                if (!_itemUseTimeout.IsActive)
                {
                    _itemUseTimeout.Set(Rand.Next(1500, 2000));
                }
                else
                {
                    _lootBoxTimeout.Cancel();
                    _lootBoxTimeout.Set(Rand.Next(1500, 2000));
                }

                return true;
            }

            return false;
        }

        public bool AutoCompleteQuest(PlayerQuest quest)
        {
            if (string.IsNullOrEmpty(quest.Id) || !quest.AutoComplete) return false;
            SendProto(new PSXAPI.Request.Quest
            {
                Action = QuestAction.Complete,
                ID = quest.Id
            });
            return true;
        }

        public bool RequestPathForInCompleteQuest(PlayerQuest quest)
        {
            if (quest.Target == Guid.Empty || quest.Completed || quest.IsRequestedForPath) return false;
            quest.UpdateRequests(true);
            SendProto(new PSXAPI.Request.Path
            {
                Request = quest.Target
            });
            return true;
        }

        public bool RequestPathForCompletedQuest(PlayerQuest quest)
        {
            if (quest.QuestData.TargetCompleted == Guid.Empty || !quest.Completed || quest.IsRequestedForPath)
                return false;
            quest.UpdateRequests(true);
            SendProto(new PSXAPI.Request.Path
            {
                Request = quest.QuestData.TargetCompleted
            });
            return true;
        }

        private void ProcessPacket(string packet)
        {
            var data = packet.Split(" ".ToCharArray());

            var array = Convert.FromBase64String(data[1]);
            if (decryptStream != null)
                array = decryptStream.Crypt(array);

            var type = Type.GetType($"PSXAPI.Response.{data[0]}, PSXAPI");

            if (type is null)
            {
                InvalidPacket?.Invoke(data[0], "Received Unknown Response", array);
            }
            else
            {
                IProto proto = typeof(Proto).GetMethod("Deserialize").MakeGenericMethod(type).Invoke(null, new object[]
                {
                    array
                }) as IProto;

                if (proto is null)
                {
                    InvalidPacket?.Invoke(type.Name, "Failed to Deserialize", array);
                    //Console.WriteLine("Error: " + type.Name + " " + data[1]);
                    return;
                }

                if (proto is PSXAPI.Response.Ping)
                {
                    ping = (int) (DateTime.UtcNow - ((PSXAPI.Response.Ping) proto).DateTimeUtc).TotalMilliseconds;
                    lastPingResponseUtc = DateTime.UtcNow;
                    receivedPing = true;
                    if (_needToSendAck) SendAck();
                }
                else
                {
                    switch (proto)
                    {
                        case Greeting gr:
#if DEBUG
                            Console.WriteLine($"Server Version: {gr.ServerVersion}\nUsers Online: {gr.UsersOnline}");
#endif
                            break;
                        case Broadcast cast:
                            if (cast.Type == BroadcastMessageType.System)
                                SystemMessage?.Invoke(cast.Message);
                            else
                                LogMessage?.Invoke($"Got a Broadcast message: {cast.Message}");
                            break;
                        case Request req:
                            OnRequests(req);
                            break;
                        case PSXAPI.Response.GuildEmblem em:

                            break;
                        case Fishing fish:
                            _itemUseTimeout.Cancel();
                            _fishingTimeout.Set(2500 + Rand.Next(500, 1500));
                            SystemMessage?.Invoke("You've started fishing!");
                            break;
                        case Badges bd:
                            foreach (var badge in bd.All)
                                if (!Badges.ContainsKey(badge))
                                {
                                    Badges.Add(badge, BadgeFromID(badge));
                                    SystemMessage?.Invoke("You've obtained " + BadgeFromID(badge) + " !");
                                }

                            Console.WriteLine("BADGES: " + bd.All.Length);
                            break;
                        case Stats stats:
                            OnPlayerStats(stats);
                            break;
                        case Badge badge:
                            if (!Badges.ContainsKey(badge.Id))
                            {
                                Badges.Add(badge.Id, BadgeFromID(badge.Id));
                                SystemMessage?.Invoke("You've obtained " + BadgeFromID(badge.Id) + " !");
                            }

                            break;
                        case Level lvl:
                            OnLevel(lvl);
                            break;
                        case Learn learn:
                            OnLearn(learn);
                            break;
                        case LoginQueue queue:
                            SystemMessage?.Invoke("Login Queue: Average Wait-Time: " +
                                                  queue.EstimatedTime.FormatTimeString());
                            break;
                        case Money money:
                            Money = (int) money.Game;
                            Gold = (int) money.Gold;
                            InventoryUpdated?.Invoke();
                            break;
                        case PSXAPI.Response.Move move:
                            OnPlayerPosition(move, true);
                            break;
                        case Mount mtP:
                            OnMountUpdate(mtP);
                            break;
                        case Area area:
                            OnAreaPokemon(area);
                            break;
                        case ChatJoin join:
                            OnChannels(join);
                            break;
                        case ChatMessage msg:
                            OnChatMessage(msg);
                            break;
                        case Message pm:
                            OnPrivateMessage(pm);
                            break;
                        case Time time:
                            OnUpdateTime(time);
                            break;
                        case Login login:
                            CheckLogin(login);
                            break;
                        case Sync sync:
                            OnPlayerSync(sync);
                            break;
                        case DebugMessage dMsg:
                            if (dMsg.Message.Contains("Command Exception"))
                            {
                                ServerCommandException?.Invoke(dMsg.Message);
                                break;
                            }

                            SystemMessage?.Invoke(dMsg.Message);
                            break;
                        case InventoryPokemon iPoke:
                            OnPokemonUpdated(new[] {iPoke});
                            break;
                        case DailyLootbox dl:
                            OnLootBoxRecieved(dl);
                            break;
                        case Lootbox bx:
                            OnLootBoxRecieved(bx);
                            break;
                        case MapUsers mpusers:
                            OnUpdatePlayer(mpusers);
                            break;
                        case Inventory Inv:
                            OnInventoryUpdate(Inv);
                            break;
                        case PSXAPI.Response.InventoryItem invItm:
                            UpdateItems(new[] {invItm});
                            break;
                        case PSXAPI.Response.Pokemon pokes:
                            OnPcPokemon(pokes);
                            break;
                        case Transfer tr:
                            OnTransfered(tr);
                            break;
                        case Script sc:
                            OnScript(sc);
                            break;
                        case PSXAPI.Response.Battle battle:
                            OnBattle(battle);
                            break;
                        case PokedexUpdate dexUpdate:
                            OnPokedexUpdate(dexUpdate);
                            break;
                        case Reorder reorder:
                            OnReorderPokemon(reorder);
                            break;
                        case Evolve evolve:
                            OnEvolved(evolve);
                            break;
                        case Evs evs:
                            OnEvs(evs);
                            break;
                        case Effect effect:
                            OnEffects(effect);
                            break;
                        case UseItem itm:
                            OnUsedItem(itm);
                            break;
                        case Quest quest:
                            OnQuest(new[] {quest});
                            break;
                        case Path path:
                            OnPathReceived(path);
                            break;
                        case Party party:
                            if (party.ChatID.ToString() != _partyChannel)
                            {
                                _partyChannel = party.ChatID.ToString();
                                SendJoinChannel(_partyChannel);
                            }

                            break;
                        case Guild guild:
                            OnGuild(guild);
                            break;
                        case Logout lg:
#if DEBUG
                            Console.WriteLine("[SYSTEM COMMAND]Got command to logout..");
#endif
                            Close();
                            break;
                        default:
#if DEBUG
                            Console.WriteLine("[-]Unknown packet type[-]: " + type.Name);
#endif
                            break;
                    }
#if DEBUG

                    Console.WriteLine(proto?._Name);
#endif
                }
            }
        }

        private void OnEvs(Evs evs)
        {
            if (evs.Result != EvsResult.Failed)
            {
                var poke = Team.Find(p => p.UniqueID == evs.PokemonUID);
                if (poke != null)
                    poke.UpdatePokemonData(evs.Pokemon);
            }

            SortPokemon(Team);
            PokemonsUpdated?.Invoke();
        }

        private void OnTransfered(Transfer tr)
        {
            _refreshingPCBox.Cancel();
            if (tr.Result == TransferResult.Success)
            {
                var cachePoke = _cachedPokemon.Find(p => p.Pokemon.UniqueID == tr.Pokemon);
                if (cachePoke != null)
                {
                    cachePoke.Box = tr.Box;
                    if (cachePoke.Box == 0)
                    {
                        cachePoke.Position = Team.Count + 1;
                        OnPokemonUpdated(new[] {cachePoke});
                    }
                    else
                    {
                        var inTeam = Team.Find(t => t.UniqueID == cachePoke.Pokemon.UniqueID);
                        if (inTeam != null)
                            Team.Remove(inTeam);
                    }
                }

                SortPokemon(Team);
                PokemonsUpdated?.Invoke();
            }
        }

        private void OnPcPokemon(PSXAPI.Response.Pokemon pokes)
        {
            _refreshingPCBox.Cancel();
            CurrentPCBox = new List<Pokemon>();
            if (pokes.Box == CurrentPCBoxId && pokes.All != null)
                foreach (var poke in pokes.All)
                    CurrentPCBox.Add(new Pokemon(poke));

            if (pokes.BoxSummary != null)
            {
                var summ = pokes.BoxSummary;
                var totalPok = 0;
                if (summ.UsedBoxes != null)
                {
                    foreach (var used in summ.UsedBoxes)
                    {
                        var boxId = used.Key;
                        var total = used.Value;

                        totalPok = totalPok + total;
                    }

                    UsedPCBoxes = summ.UsedBoxes.Count;
                    PCTotalPokemon = totalPok;
                }

                BoxSummary = summ;
            }

            IsPCBoxRefreshing = false;
            PCBoxUpdated?.Invoke(CurrentPCBox);
        }

        private void OnLearn(Learn learn)
        {
            if (learn.Result == LearnResult.Success)
            {
                var getMove = MovesManager.Instance.GetMoveNameFromEnum(learn.Move);
                var poke = PokemonManager.Instance.GetNameFromEnum(learn.Pokemon.Pokemon.Payload.PokemonID);
                SystemMessage?.Invoke($"{poke} learned the move {getMove}!");
            }
        }

        private void OnPlayerStats(Stats stats)
        {
            var playerStats = new PlayerStats(stats);
            if (playerStats.PlayerName == PlayerName || stats.Result == StatsResult.Self)
            {
                PlayerStats = playerStats;
                Badges.Clear();
                TotalSteps = (int) stats.Data.StepsTaken;
                foreach (var id in PlayerStats.Badges)
                    Badges.Add(id, BadgeFromID(id));
                CanUseCut = HasCutAbility();
                CanUseSmashRock = HasRockSmashAbility();
            }
        }

        private void OnRequests(Request req)
        {
            if (req.Type.ToString().Contains("Decline")) return;
            var type = RequestType.None;
            switch (req.Type)
            {
                case PSXAPI.Response.RequestType.Battle:
                    type = RequestType.Battle;
                    break;
                case PSXAPI.Response.RequestType.Friend:
                    type = RequestType.Friend;
                    break;
                case PSXAPI.Response.RequestType.Trade:
                    type = RequestType.Trade;
                    break;
                case PSXAPI.Response.RequestType.Guild:
                    type = RequestType.Guild;
                    break;
                case PSXAPI.Response.RequestType.Party:
                    type = RequestType.Party;
                    break;
                default:
                    return;
            }

            SystemMessage?.Invoke($"{req.Sender} sent {req.Type.ToString().ToLowerInvariant()} request.");
            LogMessage?.Invoke("Saying no to the request....");

            SendProto(new PSXAPI.Request.Request
            {
                Accepted = false,
                Sender = req.Sender,
                Type = type
            });
        }

        private void OnGuild(Guild guild)
        {
            if (guild.Chat.ToString() != _guildChannel)
            {
                _guildChannel = guild.Chat.ToString();
                SendJoinChannel(_guildChannel);
            }

            if (_guildName != guild.Name)
            {
                _guildName = guild.Name;
                _guildEmbedId = guild.EmblemId;
                SendRequestGuildLogo(guild.Name, _guildEmbedId);
            }

            if (_guildEmbedId != guild.EmblemId)
            {
                _guildEmbedId = guild.EmblemId;
                SendRequestGuildLogo(guild.Name, _guildEmbedId);
            }
        }

        private void OnAreaPokemon(Area area)
        {
            if (area.Pokemon is null || area.Pokemon.Length <= 0) return;
            foreach (var poke in area.Pokemon)
            {
                var findDexPok = PokedexPokemons.Find(o => o.Id == poke.PokemonID);
                if (findDexPok != null) findDexPok.UpdateStatus(poke.Pokedex);
            }

            PokedexUpdated?.Invoke(PokedexPokemons);
        }

        private void OnUsedItem(UseItem useItem)
        {
            switch (useItem.Result)
            {
                case UseItemResult.Success:

                    break;
                case UseItemResult.Failed:
                    SystemMessage?.Invoke(
                        $"Failed to use {ItemsManager.Instance.ItemClass.items.FirstOrDefault(i => i.ID == useItem.Item).Name}");
                    break;
                case UseItemResult.InvalidItem:
                case UseItemResult.InvalidPokemon:

                    break;
                default:
                    throw new Exception("Unexpected Case");
            }
        }

        private void OnPathReceived(Path path)
        {
            if (path.Links != null)
                ReceivedPath?.Invoke(path);
        }

        private void OnEffects(Effect effect)
        {
            if (effect.Effects is null) return;

            if (effect.Type == EffectUpdateType.All)
            {
                Effects.Clear();
                foreach (var ef in effect.Effects) Effects.Add(new PlayerEffect(ef, DateTime.UtcNow));
            }
            else if (effect.Type == EffectUpdateType.AddOrUpdate)
            {
                foreach (var ef in effect.Effects)
                {
                    var foundEf = Effects.Find(e => e.UID == ef.UID);
                    if (foundEf != null)
                        foundEf = new PlayerEffect(ef, DateTime.UtcNow);
                    else
                        Effects.Add(new PlayerEffect(ef, DateTime.UtcNow));
                }
            }
            else if (effect.Type == EffectUpdateType.Remove)
            {
                foreach (var ef in effect.Effects)
                {
                    var foundEf = Effects.Find(e => e.UID == ef.UID);
                    if (foundEf != null) Effects.Remove(foundEf);
                }
            }
        }

        private void OnEvolved(Evolve evolve)
        {
            switch (evolve.Result)
            {
                case EvolutionResult.Success:
                    SystemMessage?.Invoke($"{PokemonManager.Instance.GetNameFromEnum(evolve.Previous)} evolved into " +
                                          $"{PokemonManager.Instance.GetNameFromEnum(evolve.Pokemon.Pokemon.Payload.PokemonID)}");
                    break;
                case EvolutionResult.Failed:
                    SystemMessage?.Invoke("Failed to evolve!");
                    break;
                case EvolutionResult.Canceled:
                    SystemMessage?.Invoke(
                        $"{PokemonManager.Instance.GetNameFromEnum(evolve.Previous)} did not evolve!");
                    break;
                default:
                    Console.WriteLine("Unexpected evolve result: " + evolve.Result);
                    break;
            }

            if (evolve.Pokemon != null)
                OnPokemonUpdated(new[] {evolve.Pokemon});
        }

        private void OnPrivateMessage(Message pm)
        {
            if (pm != null)
            {
                if (pm.Event == PSXAPI.Response.MessageEvent.Message)
                {
                    if (!Conversations.Contains(pm.Name))
                        Conversations.Add(pm.Name);
                    if (!string.IsNullOrEmpty(pm.Text))
                        PrivateMessage?.Invoke(pm.Name, pm.Name, pm.Text);
                }
                else
                {
                    Conversations.Remove(pm.Name);
                    var removeMsg = pm.Event == PSXAPI.Response.MessageEvent.Closed
                        ? $"{pm.Name} closed the Chat Window."
                        : $"{pm.Name} is offline.";
                    LeavePrivateMessage?.Invoke(pm.Name, "System", removeMsg);
                }
            }
        }

        private void OnChatMessage(ChatMessage msg)
        {
            if (string.IsNullOrEmpty(msg.Channel))
            {
                foreach (var m in msg.Messages)
                    SystemMessage?.Invoke(m.Message);

                return;
            }

            var rchannelName = msg.Channel;

            if ((string.Equals(msg.Channel, mapChatChannel, StringComparison.CurrentCultureIgnoreCase) ||
                 msg.Channel.StartsWith("map:", StringComparison.CurrentCulture)) &&
                !string.IsNullOrEmpty(msg.Channel)) msg.Channel = "Map";
            if (string.Equals(msg.Channel, _partyChannel, StringComparison.CurrentCultureIgnoreCase) &&
                !string.IsNullOrEmpty(msg.Channel)) msg.Channel = "Party";
            if (string.Equals(msg.Channel, _guildChannel, StringComparison.CurrentCultureIgnoreCase) &&
                !string.IsNullOrEmpty(msg.Channel)) msg.Channel = "Guild";

            if (Channels.ContainsKey(msg.Channel) && !string.IsNullOrEmpty(msg.Channel))
            {
                var channelName = msg.Channel;
                foreach (var message in msg.Messages)
                    ChannelMessage?.Invoke(channelName, message.Username, message.Message);
            }
            else
            {
                Channels.Add(msg.Channel, new ChatChannel("", msg.Channel, rchannelName));
                RefreshChannelList?.Invoke();
            }
        }

        private void OnUpdatePlayer(MapUsers mpusers)
        {
            var data = mpusers.Users;
            var expiration = DateTime.UtcNow.AddSeconds(20);
            if (data is null) return;
            var isNewPlayer = false;
            foreach (var user in data)
            {
                PlayerInfos player;
                if (Players.ContainsKey(user.Username))
                {
                    player = Players[user.Username];
                    player.Update(user, expiration);
                }
                else if (_removedPlayers.ContainsKey(user.Username))
                {
                    player = _removedPlayers[user.Username];
                    player.Update(user, expiration);
                }
                else
                {
                    isNewPlayer = true;
                    player = new PlayerInfos(user, expiration);
                }

                player.Updated = DateTime.UtcNow;

                Players[player.Name] = player;

                if (isNewPlayer || player.Actions.Any(ac => ac.Action == MapUserAction.Enter))
                {
                    if (!string.IsNullOrEmpty(player?.GuildName))
                        SendRequestGuildLogo(player.GuildName);
                    PlayerAdded?.Invoke(player);
                }
                else if (player.Actions.Any(ac => ac.Action == MapUserAction.Leave))
                {
                    PlayerRemoved?.Invoke(player);
                    if (_removedPlayers.ContainsKey(player.Name))
                        _removedPlayers[player.Name] = player;
                    else
                        _removedPlayers.Add(player.Name, player);
                    Players.Remove(player.Name);
                }
                else if (player.Actions.Any(ac => ac.Action != MapUserAction.Leave))
                {
                    PlayerUpdated?.Invoke(player);
                }
            }
        }

        private void OnReorderPokemon(Reorder reorder)
        {
            if (reorder.Box == 0)
            {
                if (reorder.Pokemon != null)
                    if (reorder.Pokemon.Length > 0)
                    {
                        var tempTeam = new List<Pokemon>();
                        var i = 0;
                        foreach (var id in reorder.Pokemon)
                        {
                            var poke = Team.Find(x => x.PokemonData.Pokemon.UniqueID == id);
                            poke.UpdatePosition(i + 1);
                            tempTeam.Add(poke);
                            i++;
                        }

                        Team = tempTeam;
                    }

                SortPokemon(Team);
                PokemonsUpdated?.Invoke();
            }
            else
            {
                // pc
                if (IsPCOpen && reorder.Box == CurrentPCBoxId && CurrentPCBox != null)
                {
                    var i = 0;
                    foreach (var id in reorder.Pokemon)
                    {
                        var pcPoke = CurrentPCBox.Find(p => p.UniqueID == id);
                        pcPoke.PokemonData.Box = reorder.Box;

                        pcPoke?.UpdatePosition(i + 1);
                        i++;
                    }
                }

                SortPokemon(CurrentPCBox);
                PCBoxUpdated?.Invoke(CurrentPCBox);
            }
        }

        private void SortPokemon(List<Pokemon> pokemons)
        {
            pokemons = pokemons.OrderBy(poke => poke.Uid).ToList();
            CheckEvolving();
            CheckLearningMove();
        }

        private void OnBattle(PSXAPI.Response.Battle battle)
        {
            ClearPath();
            _slidingDirection = null;

            IsInBattle = !battle.Ended;

            if (ActiveBattle != null && !ActiveBattle.OnlyInfo)
                ActiveBattle.UpdateBattle(PlayerName, battle, Team);
            else
                ActiveBattle = new Battle(PlayerName, battle, Team);

            if (!ActiveBattle.OnlyInfo && IsInBattle && IsLoggedIn && ActiveBattle.Turn <= 1 && !ActiveBattle.IsUpdated)
            {
                // encounter message coz the server doesn't send it.
                _fishingTimeout.Cancel();
                _needToSendSync = true;

                #region INFORMATION FOR PLAYER

                var firstEncounterMessage = "";
                if (ActiveBattle.OpponentActivePokemon != null && ActiveBattle.OpponentActivePokemon.Count > 1)
                {
                    if (ActiveBattle.OpponentActivePokemon.Count == 2)
                    {
                        var pok = ActiveBattle.OpponentActivePokemon[0];
                        var secondPok = ActiveBattle.OpponentActivePokemon[1];
                        firstEncounterMessage = ActiveBattle.IsWild ? pok.Shiny
                                ?
                                $"Wild shiny {PokemonManager.Instance.Names[pok.ID]} and "
                                : $"Wild {PokemonManager.Instance.Names[pok.ID]} and "
                                  + (secondPok.Shiny
                                      ? $"shiny {PokemonManager.Instance.Names[secondPok.ID]} has appeared!"
                                      : $"{PokemonManager.Instance.Names[secondPok.ID]} has appeared!")
                            : pok.Shiny ? $"Opponents sent out shiny {PokemonManager.Instance.Names[pok.ID]} and "
                            : $"Opponents sent out {PokemonManager.Instance.Names[pok.ID]} and "
                              + (secondPok.Shiny
                                  ? $"shiny {PokemonManager.Instance.Names[secondPok.ID]}!"
                                  : $"{PokemonManager.Instance.Names[secondPok.ID]}!");
                    }
                    else if (ActiveBattle.OpponentActivePokemon.Count == 3)
                    {
                        var pok = ActiveBattle.OpponentActivePokemon[0];
                        var secondPok = ActiveBattle.OpponentActivePokemon[1];
                        var thridPoke = ActiveBattle.OpponentActivePokemon[2];
                        firstEncounterMessage = ActiveBattle.IsWild ? pok.Shiny
                                ?
                                $"Wild shiny {PokemonManager.Instance.Names[pok.ID]}, "
                                : $"Wild {PokemonManager.Instance.Names[pok.ID]}, "
                                  + (secondPok.Shiny
                                      ? $"shiny {PokemonManager.Instance.Names[secondPok.ID]} and "
                                      : $"{PokemonManager.Instance.Names[secondPok.ID]} and ") +
                                  (thridPoke.Shiny
                                      ? $"shiny {PokemonManager.Instance.Names[thridPoke.ID]} has appeared!"
                                      : $"{PokemonManager.Instance.Names[thridPoke.ID]} has appeared!")
                            : pok.Shiny ? $"Opponents sent out shiny {PokemonManager.Instance.Names[pok.ID]}, "
                            : $"Opponents sent out {PokemonManager.Instance.Names[pok.ID]}, "
                              + (secondPok.Shiny
                                  ? $"shiny {PokemonManager.Instance.Names[secondPok.ID]} and "
                                  : $"{PokemonManager.Instance.Names[secondPok.ID]} and ") +
                              (thridPoke.Shiny
                                  ? $"shiny {PokemonManager.Instance.Names[thridPoke.ID]}!"
                                  : $"{PokemonManager.Instance.Names[thridPoke.ID]}!");
                    }
                }
                else
                {
                    firstEncounterMessage = ActiveBattle.IsWild
                        ? ActiveBattle.IsShiny
                            ? $"A wild shiny {PokemonManager.Instance.Names[ActiveBattle.OpponentId]} has appeared!"
                            : $"A wild {PokemonManager.Instance.Names[ActiveBattle.OpponentId]} has appeared!"
                        : ActiveBattle.IsShiny
                            ? $"Opponent sent out shiny {PokemonManager.Instance.Names[ActiveBattle.OpponentId]}!"
                            : $"Opponent sent out {PokemonManager.Instance.Names[ActiveBattle.OpponentId]}!";
                }

                #endregion

                BattleMessage?.Invoke(firstEncounterMessage);
                _battleTimeout.Set(Rand.Next(4000, 6000));
                BattleStarted?.Invoke();
            }

            if (ActiveBattle != null)
                ActiveBattle.AlreadyCaught = IsCaughtById(ActiveBattle.OpponentId);

            OnBattleMessage(battle.Log);
            BattleUpdated?.Invoke();
        }

        private void ActiveBattleMessage(string txt)
        {
            BattleMessage?.Invoke(txt);
        }

        private void OnBattleMessage(string[] logs)
        {
            ActiveBattle.ProcessLog(logs, Team, ActiveBattleMessage);

            PokemonsUpdated?.Invoke();

            if (ActiveBattle.IsFinished)
                _battleTimeout.Set(ActiveBattle.PokemonCount > 1 ? Rand.Next(3500, 6000) : Rand.Next(2000, 5000));
            else
                _battleTimeout.Set(ActiveBattle.PokemonCount > 1 ? Rand.Next(3500, 5000) : Rand.Next(2000, 4000));
            if (ActiveBattle.IsFinished)
            {
                IsInBattle = false;
                ActiveBattle = null;
                BattleEnded?.Invoke();
            }
        }

        private void OnMountUpdate(Mount mtP)
        {
            if (mtP.MountType == MountType.Bike || mtP.MountType == MountType.Pokemon)
            {
                IsBiking = true;
                IsSurfing = false;
            }
            else if (mtP.MountType == MountType.None)
            {
                IsBiking = false;
                IsSurfing = false;
            }
            else if (mtP.MountType == MountType.Surfing)
            {
                IsBiking = false;
                IsSurfing = true;
            }

            _mountingTimeout.Set(Rand.Next(500, 1000));
            MountUpdated?.Invoke();
        }

        private void OnScript(Script data)
        {
            if (data is null) return;

            var id = data.ScriptID;
            var type = data.Type;

#if DEBUG
            if (data.Text != null)
                foreach (var s in data.Text)
                    Console.WriteLine(s.Text);
            if (data.Data != null)
                foreach (var d in data.Data)
                    Console.WriteLine(d);
#endif
            if (!IsLoggedIn || !IsMapLoaded)
                _cachedScripts.Add(data);

            var processTexts = new List<string>();

            if (data.Text != null)
                foreach (var scriptText in data.Text)
                    if (!scriptText.Text.EndsWith(")", StringComparison.CurrentCulture) &&
                        scriptText.Text.IndexOf("(", StringComparison.CurrentCulture) == -1)
                        DialogOpened?.Invoke(Regex.Replace(scriptText.Text, @"\[(\/|.\w+)\]", ""));
                    else if (IsMapLoaded && IsLoggedIn)
                        processTexts.Add(scriptText.Text);

            if (processTexts.Count > 0)
                ProcessScriptMessage(processTexts.ToArray());

            _currentScriptType = type;
            _currentScript = data;

            if (_cachedScripts.Count > 0) _dialogTimeout.Set(Rand.Next(1500, 4000));
            Scripts.Add(data);
        }

        private void ProcessScriptMessage(params string[] texts)
        {
            foreach (var text in texts)
            {
                var st = text;
                var index = st.IndexOf("(", StringComparison.CurrentCulture);
                var scriptType = st.Substring(0, index);

                var tempNpcs = Map.Npcs;

                switch (scriptType.ToLowerInvariant())
                {
                    case "setlos":
                        var command = st.Replace(scriptType, "").Replace("(", "").Replace(")", "");
                        var npcId = Guid.Parse(command.Split(',')[0]);
                        var los = Convert.ToInt32(command.Split(',')[1]);
                        var npc = tempNpcs.Find(x => x.Id == npcId);
                        if (npc != null)
                        {
                            npc.UpdateLos(los);
                        }
                        else if (Map.OriginalNpcs.Find(n => n.Id == npcId) != null)
                        {
                            var clone = Map.OriginalNpcs.Find(n => n.Id == npcId).Clone();
                            clone.UpdateLos(los);
                            tempNpcs.Add(clone);
                        }

                        break;
                    case "enablenpc":
                        command = st.Replace(scriptType, "").Replace("(", "").Replace(")", "");
                        npcId = Guid.Parse(command.Split(',')[0]);
                        var hide = command.Split(',')[1] == "0";
                        npc = tempNpcs.Find(x => x.Id == npcId);
                        if (npc != null)
                        {
                            npc.SetVisibility(hide);
                            if (hide)
                                tempNpcs.Remove(npc);
                        }
                        else if (!hide && Map.OriginalNpcs.Find(n => n.Id == npcId) != null)
                        {
                            var clone = Map.OriginalNpcs.Find(n => n.Id == npcId).Clone();
                            clone.SetVisibility(hide);
                            tempNpcs.Add(clone);
                        }

                        //OnNpcs(tempNpcs);
                        break;
                    case "enablelink":
                        command = st.Replace(scriptType, "").Replace("(", "").Replace(")", "");
                        var linkId = Guid.Parse(command.Split(',')[0]);
                        hide = command.Split(',')[1] == "0";
                        if (Map.Links.Find(link => link.Id == linkId) != null)
                            Map.Links.Find(link => link.Id == linkId).SetVisibility(hide);
                        LinksUpdated?.Invoke();
                        break;
                    case "openpc":
                        // Asking default or first Box's pokemon...
                        if (!IsPCOpen || CurrentPCBox is null)
                        {
                            _refreshingPCBox.Set(Rand.Next(1500,
                                2000)); // this is the amount of time we wait for an answer
                            SendRefreshPCBox(1);
                            CurrentPCBoxId = 1;
                            IsPCBoxRefreshing = true;
                        }

                        break;
                    default:
                        Console.WriteLine("[-]Unknown action[-]: " + scriptType);
                        break;
                }
            }

            if (_wasLoggedIn)
            {
                var moveActions = new List<MoveAction>();
                CheckForNpcInteraction(ref moveActions);
                if (moveActions.Count > 0) SendMovement(moveActions.ToArray(), PlayerX, PlayerY);
            }

            //OnNpcs(tempNpcs);
            //NpcReceieved?.Invoke(Map.Npcs);
        }

        private void OnLootBoxRecieved(IProto dl, TimeSpan? timeSpan = null)
        {
            if (dl is DailyLootbox db)
                RecievedLootBoxes.HandleDaily(db, timeSpan != null ? db.Timer : timeSpan);
            else if (dl is Lootbox lb)
                RecievedLootBoxes.HandleLootbox(lb);
        }

        private void RecievedLootBoxes_BoxOpened(LootboxRoll[] rewards, LootboxType type)
        {
            _lootBoxTimeout.Set(Rand.Next(3000, 4000));
            LootBoxOpened?.Invoke(rewards, type);
        }

        private void RecievedLootBoxes_RecievedBox(Lootbox obj)
        {
            RecievedLootBox?.Invoke(obj);
            _lootBoxTimeout.Set(Rand.Next(1500, 2000));
        }

        private void RecievedLootBoxMsg(string ob)
        {
            LootBoxMessage?.Invoke(ob);
        }

        private void OnPlayerSync(Sync sync)
        {
            OnPlayerPosition(sync, false);
        }

        private void CheckLogin(Login login)
        {
            if (login.Result == LoginResult.Success)
            {
                IsAuthenticated = true;
                OnLoggedIn(login);
            }
            else
            {
                AuthenticationFailed?.Invoke(login.Error);
                Close();
            }
        }

        private void OnPlayerPosition(IProto proto, bool sync = true)
        {
            if (proto is PSXAPI.Response.Move movement)
            {
                if (MapName != movement.Map || PlayerX != movement.X || PlayerY != movement.Y)
                {
                    TeleportationOccuring?.Invoke(movement.Map, movement.X, movement.Y);


                    PlayerX = movement.X;
                    PlayerY = movement.Y;

                    _teleportationTimeout.Cancel();

                    var prev = mapChatChannel;
                    mapChatChannel = "map:" + movement.Map;
                    if (mapChatChannel != prev && MapName != movement.Map)
                    {
                        if (Channels.ContainsKey("Map") && !string.IsNullOrEmpty(Channels["Map"].ChannelName))
                            CloseChannel(Channels["Map"].ChannelName);

                        SendJoinChannel(mapChatChannel);
                    }

                    LoadMap(movement.Map);

                    if (movement.Height == 1)
                        IsOnGround = false;
                    else
                        IsOnGround = true;
                    LastDirection = DirectionExtensions.FromPlayerDirectionResponse(movement.Direction);
                }

                if (sync)
                    Resync(MapName == movement.Map);
            }
            else if (proto is Sync syncP)
            {
                if (MapName != syncP.Map || PlayerX != syncP.PosX || PlayerY != syncP.PosY)
                {
                    TeleportationOccuring?.Invoke(syncP.Map, syncP.PosX, syncP.PosY);

                    PlayerX = syncP.PosX;
                    PlayerY = syncP.PosY;

                    _teleportationTimeout.Cancel();

                    var prev = mapChatChannel;
                    mapChatChannel = "map:" + syncP.Map;
                    if (mapChatChannel != prev && MapName != syncP.Map)
                    {
                        if (Channels.ContainsKey("Map") && !string.IsNullOrEmpty(Channels["Map"].ChannelName))
                            CloseChannel(Channels["Map"].ChannelName);
                        SendJoinChannel(mapChatChannel);
                    }

                    LoadMap(syncP.Map);
                    if (syncP.Height == 1)
                        IsOnGround = false;
                    else
                        IsOnGround = true;
                }

                if (sync)
                    Resync(MapName == syncP.Map);
            }

            CheckArea();
        }

        private void OnPokedexData(Pokedex data)
        {
            if (data.Entries != null)
            {
                foreach (var en in data.Entries) PokedexPokemons.Add(new PokedexPokemon(en));
                PokedexOwned = data.Caught;
                PokedexSeen = data.Seen;
            }

            PokedexUpdated?.Invoke(PokedexPokemons);
        }

        private void OnPokedexUpdate(PokedexUpdate data)
        {
            if (data.Entry != null)
            {
                var dexPoke = new PokedexPokemon(data.Entry);
                if (PokedexPokemons.Count > 0)
                {
                    var findDex = PokedexPokemons.Find(x => x.Id == dexPoke.Id);
                    if (findDex != null)
                    {
                        PokedexPokemons.Insert(PokedexPokemons.IndexOf(findDex), dexPoke);
                        PokedexPokemons.Remove(findDex);
                    }
                    else
                    {
                        PokedexPokemons.Add(dexPoke);
                    }
                }
                else
                {
                    PokedexPokemons.Add(dexPoke);
                }
            }

            RequestArea(MapName, AreaName);
            PokedexUpdated?.Invoke(PokedexPokemons);
        }

        public void RequestArea(string Map, string Areaname)
        {
            SendProto(new PSXAPI.Request.Area
            {
                Map = Map.ToLowerInvariant(),
                AreaName = Areaname
            });
        }

        public bool IsCaughtById(int id)
        {
            return PokedexPokemons.Count > 0 && PokedexPokemons.Any(x => x.Caught && x.Id == id);
        }

        public bool IsCaughtByName(string name)
        {
            return PokedexPokemons.Count > 0 && PokedexPokemons.Any(x =>
                       x.Caught && string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }

        private void OnLoggedIn(Login login)
        {
            _isLoggedIn = true;
            PlayerName = login.Username;
            Console.WriteLine($"[Login] [ID={login.LoginID}] Authenticated successfully");
            _loginId = login.LoginID;

            encryptStream = new RC4Stream(_loginId.ToByteArray());
            decryptStream = new RC4Stream(_loginId.ToByteArray());

            LoggedIn?.Invoke();
            AddDefaultChannels();

            if (_currentScript != null)
                switch (_currentScriptType)
                {
                    case ScriptRequestType.Customize:
                        //Character!

                        SendCharacterCustomization(0, Rand.Next(0, 3), Rand.Next(0, 13), Rand.Next(0, 27),
                            Rand.Next(0, 4));

                        break;
                    default:
                        Console.WriteLine("Unexpected Script Type: " + _currentScriptType);
                        break;
                }
            if (login.Level != null)
                OnLevel(login.Level);
            OnPokemonUpdated(login.Inventory.ActivePokemon);
            OnInventoryUpdate(login.Inventory);
            OnMountUpdate(login.Mount);
            OnPlayerPosition(login.Position, false);
            TotalSteps = (int) login.TotalSteps;

            if (login.Pokedex != null) OnPokedexData(login.Pokedex);

            if (login.Battle != null)
                OnBattle(login.Battle);

            if (login.Quests != null && login.Quests.Length > 0)
                OnQuest(login.Quests);

            if (login.Effects != null)
                OnEffects(login.Effects);

            SendProto(new PSXAPI.Request.Time());
            //SendPacket("Ack " + _loginId.ToByteArray().Hexdigest());

            if (login.DailyLootbox != null)
                OnLootBoxRecieved(login.DailyLootbox, login.DailyReset);
            if (login.Lootboxes != null)
                foreach (var lootBox in login.Lootboxes)
                    OnLootBoxRecieved(lootBox);
            if (login.NearbyUsers != null)
                _cachedNerbyUsers = login.NearbyUsers;

            if (login.Party != null)
                if (login.Party.ChatID.ToString() != _partyChannel)
                {
                    _partyChannel = login.Party.ChatID.ToString();
                    SendJoinChannel(_partyChannel);
                }

            if (login.Guild != null) OnGuild(login.Guild);
        }

        private void OnQuest(Quest[] quests)
        {
            foreach (var quest in quests)
            {
                var foundQuest = Quests.Find(q => q.Id == quest.ID);
                if (foundQuest != null) Quests.Remove(foundQuest);
                if (!quest.Completed && !string.IsNullOrEmpty(quest.Name))
                    Quests.Add(new PlayerQuest(quest));
            }

            if (Quests.Count > 1)
                Quests = (from x in Quests
                    orderby x.Type
                    select x).ToList();
            QuestsUpdated?.Invoke(Quests);
        }

        private void OnLevel(Level data)
        {
            var preLevel = Level;
            Level = data;
            LevelChanged?.Invoke(preLevel, Level);
        }

        private void OnChannels(ChatJoin join)
        {
#if DEBUG
            Console.WriteLine("Received Channel: " + join.Channel);
#endif
            if (join.Result == ChatJoinResult.Joined)
            {
                var rchannelName = join.Channel;
                if (string.Equals(join.Channel, mapChatChannel, StringComparison.CurrentCultureIgnoreCase) ||
                    join.Channel.StartsWith("map:", StringComparison.CurrentCulture))
                {
                    join.Channel = "Map";
                    if (Channels.ContainsKey(join.Channel))
                        if (Channels[join.Channel].ChannelName != rchannelName)
                            Channels[join.Channel] = new ChatChannel("default", join.Channel, rchannelName);
                }

                if (string.Equals(join.Channel, _partyChannel, StringComparison.CurrentCultureIgnoreCase))
                {
                    join.Channel = "Party";
                    if (Channels.ContainsKey(join.Channel))
                        if (Channels[join.Channel].ChannelName != rchannelName)
                            Channels[join.Channel] = new ChatChannel("default", join.Channel, rchannelName);
                }

                if (string.Equals(join.Channel, _guildChannel, StringComparison.CurrentCultureIgnoreCase))
                {
                    join.Channel = "Guild";
                    if (Channels.ContainsKey(join.Channel))
                        if (Channels[join.Channel].ChannelName != rchannelName)
                            Channels[join.Channel] = new ChatChannel("default", join.Channel, rchannelName);
                }

                if (!Channels.ContainsKey(join.Channel))
                    Channels.Add(join.Channel, new ChatChannel("", join.Channel, rchannelName));
            }
            else if (join.Result == ChatJoinResult.Left)
            {
                if (Channels.ContainsKey(join.Channel))
                    Channels.Remove(join.Channel);
            }

            RefreshChannelList?.Invoke();
        }

        private void OnPokemonUpdated(InventoryPokemon[] pokemons)
        {
            if (pokemons is null) return;
            if (pokemons.Length <= 1)
            {
#if DEBUG
                Console.WriteLine("Received One Pokemon Data!");
#endif
                if (Team.Count > 0)
                {
                    var poke = new Pokemon(pokemons[0]);

                    if (poke?.PokemonData?.Box == CurrentPCBoxId)
                    {
                        // This pokemon data is received when the player transfer a pokemon to the box

                        (CurrentPCBox ?? (CurrentPCBox = new List<Pokemon>())).Add(poke);
                        IsPCOpen = true;
                        IsPCBoxRefreshing = false;
                    }
                    else
                    {
                        _cachedPokemon.Add(pokemons[0]);
                    }

                    var foundPoke = Team.Find(x => x?.PokemonData?.Pokemon?.UniqueID == pokemons[0]?.Pokemon?.UniqueID);
                    if (foundPoke != null)
                        foundPoke.UpdatePokemonData(pokemons[0]);
                    else if (poke?.PokemonData?.Box == 0) Team.Add(poke);
                }
                else
                {
                    Team.Clear();
                    foreach (var poke in pokemons)
                    {
                        var p = new Pokemon(poke);
                        if (poke.Box == 0)
                        {
                            Team.Add(p);
                        }
                        else if (p.PokemonData.Box > 0 && p.PokemonData.Box == CurrentPCBoxId)
                        {
                            // This pokemon data is received when the player transfer a pokemon to the box

                            if (CurrentPCBox is null)
                                CurrentPCBox = new List<Pokemon>();
                            CurrentPCBox.Add(p);
                            IsPCOpen = true;
                            IsPCBoxRefreshing = false;
                        }
                        else
                        {
                            _cachedPokemon.Add(poke);
                        }
                    }
                }
            }
            else
            {
                Team.Clear();
                foreach (var poke in pokemons)
                {
                    var p = new Pokemon(poke);
                    if (poke.Box == 0)
                    {
                        Team.Add(p);
                    }
                    else if (p.PokemonData.Box > 0 && p.PokemonData.Box == CurrentPCBoxId)
                    {
                        // This pokemon data is received when the player transfer a pokemon to the box

                        if (CurrentPCBox is null)
                            CurrentPCBox = new List<Pokemon>();
                        CurrentPCBox.Add(p);
                        IsPCOpen = true;
                        IsPCBoxRefreshing = false;
                    }
                    else
                    {
                        _cachedPokemon.Add(poke);
                    }
                }
            }

            CanUseCut = HasCutAbility();
            CanUseSmashRock = HasRockSmashAbility();

            if (_swapTimeout.IsActive) _swapTimeout.Set(Rand.Next(500, 1000));

            SortPokemon(Team);
            PokemonsUpdated?.Invoke();
        }

        private void OnLearningMove(Pokemon learningPoke)
        {
            LearningMove?.Invoke(learningPoke.LearnableMoves[0],
                learningPoke.Uid, learningPoke.UniqueID);
            _itemUseTimeout.Cancel();
        }

        private void OnEvolving(Pokemon poke)
        {
            Evolving?.Invoke(poke.UniqueID);
        }

        public bool SwapPokemon(int pokemon1, int pokemon2)
        {
            if (IsInBattle || pokemon1 < 1 || pokemon2 < 1 || Team.Count < pokemon1 || Team.Count < pokemon2 ||
                pokemon1 == pokemon2) return false;
            if (!_swapTimeout.IsActive)
            {
                SendSwapPokemons(pokemon1, pokemon2);
                _swapTimeout.Set();
                return true;
            }

            return false;
        }

        private void OnInventoryUpdate(Inventory data)
        {
            Money = (int) data.Money;
            Gold = (int) data.Gold;

            data?.Badges?.ToList().ForEach(id => Badges.Add(id, BadgeFromID(id)));

            UpdateItems(data.Items);
        }

        private void UpdateItems(PSXAPI.Response.InventoryItem[] items)
        {
            if (items != null && items.Length == 1)
            {
                var item = new InventoryItem(items[0]);
                if (Items.Count > 0)
                {
                    var foundItem = Items.Find(x => x.Id == item.Id);
                    if (foundItem != null)
                    {
                        if (item.Quantity > 0)
                            Items[Items.IndexOf(foundItem)] = item;
                        else
                            Items.Remove(foundItem);
                    }
                    else
                    {
                        Items.Add(item);
                    }
                }
                else
                {
                    Items.Clear();
                    Items.Add(item);
                }
            }
            else if (items != null)
            {
                Items.Clear();
                foreach (var item in items) Items.Add(new InventoryItem(item));
            }

            InventoryUpdated?.Invoke();
        }

        private void OnUpdateTime(Time time)
        {
            LastTimePacket = time;
            _lastGameTime = DateTime.UtcNow;
            GameTime = time.GameDayTime + " " + GetGameTime(time.GameTime, time.TimeFactor, _lastGameTime);
            PokeTime = GetGameTime(LastTimePacket.GameTime, LastTimePacket.TimeFactor, _lastGameTime).Replace(" PM", "")
                .Replace(" AM", "");
            Weather = time.Weather.ToString();
            GameTimeUpdated?.Invoke(GameTime, Weather);
        }

        public void SendAuthentication(string username, string password)
        {
            SendProto(new PSXAPI.Request.Login
            {
                Name = username,
                Password = password,
                Platform = ClientPlatform.PC,
                Version = Version,
                Handle = "*@b4c80b5fa0dbe8f89ad87f9f71f59263fdbe25bf00AC7C081FD640167E266DC8"
            });
        }

        public string GetGameTime(TimeSpan time, double sc, DateTime dt)
        {
            var timeSpan = time;
            timeSpan = timeSpan.Add(TimeSpan.FromSeconds((DateTime.UtcNow - dt).TotalSeconds * sc));
            if (RunningForSeconds >= _lastRTime + 300f)
            {
                SendProto(new PSXAPI.Request.Time());
                _lastRTime = RunningForSeconds;
            }

            if (_lastTime != time.Hours)
                SendProto(new PSXAPI.Request.Time());
            _lastTime = timeSpan.Hours;

            string result;
            if (timeSpan.Hours >= 12)
            {
                if (timeSpan.Hours == 12)
                    result = "12:" + timeSpan.Minutes.ToString().PadLeft(2, '0') + " PM";
                else
                    result = timeSpan.Hours - 12 + ":" + timeSpan.Minutes.ToString().PadLeft(2, '0') + " PM";
            }
            else if (timeSpan.Hours == 0)
            {
                result = "12:" + timeSpan.Minutes.ToString().PadLeft(2, '0') + " AM";
            }
            else
            {
                result = timeSpan.Hours + ":" + timeSpan.Minutes.ToString().PadLeft(2, '0') + " AM";
            }

            return result;
        }

        public void RunFromBattle(int selected = 0)
        {
            if (selected == 0)
                selected = ActiveBattle.CurrentBattlingPokemonIndex;
            SendRunFromBattle(selected);
            _battleTimeout.Set(ActiveBattle.PokemonCount > 1 ? Rand.Next(2500, 4000) : Rand.Next(1500, 3000));
        }

        public void UseAttack(int number, int selectedPoke = 0, int opponent = 0, bool megaEvolve = false)
        {
            if (selectedPoke == 0)
                selectedPoke = ActiveBattle.CurrentBattlingPokemonIndex;
            SendAttack(number, selectedPoke, opponent, megaEvolve);
            _battleTimeout.Set(ActiveBattle.PokemonCount > 1 ? Rand.Next(3500, 4000) : Rand.Next(1500, 3000));
        }

        public void UseItem(int id, int pokemonUid = 0, int moveId = 0)
        {
            if (!(pokemonUid >= 0 && pokemonUid <= 6) || !HasItemId(id)) return;
            var item = GetItemFromId(id);
            if (item == null || item.Quantity == 0) return;
            if (pokemonUid == 0) // simple use
            {
                if (!_itemUseTimeout.IsActive && !IsInBattle && item.CanBeUsedOutsideOfBattle)
                {
                    SendUseItem(item.Id);
                    _itemUseTimeout.Set();
                }
                else if (!_battleTimeout.IsActive && IsInBattle && item.CanBeUsedInBattle &&
                         !item.CanBeUsedOnPokemonInBattle)
                {
                    pokemonUid = ActiveBattle.CurrentBattlingPokemonIndex;
                    SendUseItemInBattle(item.Id, pokemonUid, pokemonUid, moveId);
                    _battleTimeout.Set(ActiveBattle.PokemonCount > 1 ? Rand.Next(3500, 4000) : Rand.Next(1500, 3000));
                }
            }
            else // use item on pokemon
            {
                if (!_itemUseTimeout.IsActive && !IsInBattle && item.CanBeUsedOnPokemonOutsideOfBattle)
                {
                    SendUseItem(item.Id, pokemonUid, moveId);
                    _itemUseTimeout.Set();
                }
                else if (!_battleTimeout.IsActive && IsInBattle && item.CanBeUsedOnPokemonInBattle)
                {
                    SendUseItemInBattle(item.Id, pokemonUid, pokemonUid, moveId);
                    _battleTimeout.Set(ActiveBattle.PokemonCount > 1 ? Rand.Next(3500, 4000) : Rand.Next(1500, 3000));
                }
            }
        }

        public bool GiveItemToPokemon(int pokemonUid, int itemId)
        {
            if (!(pokemonUid >= 1 && pokemonUid <= Team.Count)) return false;
            var item = GetItemFromId(itemId);
            if (item == null || item.Quantity == 0) return false;
            var pokemonGuid = Team[pokemonUid - 1].UniqueID;
            if (!_itemUseTimeout.IsActive && !IsInBattle
                                          && item.CanBeHeld)
            {
                SendGiveItem(pokemonGuid, itemId);
                _itemUseTimeout.Set();
                return true;
            }

            return false;
        }

        public bool RemoveHeldItemFromPokemon(int pokemonUid)
        {
            if (!(pokemonUid >= 1 && pokemonUid <= Team.Count)) return false;
            var pokemonGuid = Team[pokemonUid - 1].UniqueID;
            if (!_itemUseTimeout.IsActive && Team[pokemonUid - 1].ItemHeld != "")
            {
                SendRemoveHeldItem(pokemonGuid);
                _itemUseTimeout.Set();
                return true;
            }

            return false;
        }

        public void LearnMove(Guid pokemonUniqueId, PokemonMoveID learningMoveId, int moveToForget)
        {
            var accept = true;
            if (moveToForget >= 5)
            {
                accept = false;
                moveToForget = 0;
            }

            SendProto(new PSXAPI.Request.Learn
            {
                Accept = accept,
                Move = learningMoveId,
                Pokemon = pokemonUniqueId,
                Position = moveToForget
            });
            _swapTimeout.Set();
        }

        public bool ChangePokemon(int to, int number = 0)
        {
            if (number == 0)
                number = ActiveBattle.CurrentBattlingPokemonIndex;
            if (to > 0)
            {
                SendChangePokemon(number, to);

                ActiveBattle?.UpdateSelectedPokemon();

                _battleTimeout.Set();
                return true;
            }

            return false;
        }

        public void UseSurfAfterMovement()
        {
            _surfAfterMovement = true;
        }

        public void UseRockSmashOrCut(int x, int y)
        {
            _cutOrRockSmashNpc = Map.Npcs.Find(npc => npc.PositionX == x && npc.PositionY == y);
        }

        public void UseSurf()
        {
            SendProto(new PSXAPI.Request.Effect
            {
                Action = EffectAction.Use,
                UID = GetEffectFromName("Surf").UID
            });
            _mountingTimeout.Set();
        }

        public bool RefreshPCBox(int boxId)
        {
            if (!IsPCOpen || boxId < 1 || boxId > 67 || _refreshingPCBox.IsActive) return false;
            CurrentPCBox = null;
            _refreshingPCBox.Set(Rand.Next(1500, 2000)); // this is the amount of time we wait for an answer
            CurrentPCBoxId = boxId;
            SendRefreshPCBox(boxId);
            return true;
        }

        public bool RefreshCurrentPCBox()
        {
            return RefreshPCBox(CurrentPCBoxId);
        }

        public bool DepositPokemonToPC(int pokemonUid)
        {
            if (!IsPCOpen || pokemonUid < 1 || pokemonUid > 6 || Team.Count < pokemonUid) return false;
            var pokeGuid = Team[pokemonUid - 1].UniqueID;
            SendMovePokemonToPC(pokeGuid);
            return true;
        }

        public bool WithdrawPokemonFromPC(int boxId, int boxPokemonId)
        {
            if (!IsPCOpen || IsPCBoxRefreshing || Team.Count >= 6 || boxId != CurrentPCBoxId
                || boxPokemonId < 1 || boxPokemonId > CurrentPCBox.Count)
                return false;
            var pokemonGuid = CurrentPCBox[boxPokemonId - 1].UniqueID;
            SendMovePokemonFromPC(pokemonGuid);
            return true;
        }

        public bool SwapPokemonFromPC(int boxPokemonId, int teamPokemonUid)
        {
            if (!IsPCOpen || IsPCBoxRefreshing || boxPokemonId < 1 || boxPokemonId > CurrentPCBox.Count ||
                teamPokemonUid < 1 || teamPokemonUid > 6 || Team.Count < teamPokemonUid)
                return false;
            var boxPokemonGuid = CurrentPCBox[boxPokemonId - 1].UniqueID;
            var teamPokemonGuid = Team[teamPokemonUid - 1].UniqueID;
            SendPCSwapPokemon(boxPokemonGuid, teamPokemonGuid);
            return true;
        }

        public bool ReleasePokemonFromTeam(int pokemonUid)
        {
            if (!IsPCOpen || IsPCBoxRefreshing
                          || pokemonUid < 1 || pokemonUid > 6 || pokemonUid > Team.Count)
                return false;
            _refreshingPCBox.Set(Rand.Next(1500, 2000));
            var pokemonGuid = Team[pokemonUid - 1].UniqueID;
            SendReleasePokemon(pokemonGuid);
            return true;
        }

        public bool ReleasePokemonFromPC(int boxUid)
        {
            if (!IsPCOpen || IsPCBoxRefreshing
                          || boxUid < 1 || boxUid > CurrentPCBox.Count)
                return false;
            _refreshingPCBox.Set(Rand.Next(1500, 2000));
            var pokemonGuid = CurrentPCBox[boxUid - 1].UniqueID;
            SendReleasePokemon(pokemonGuid);
            return true;
        }

        public bool SetCollectedEvs(int pokemonUid, string statType, int amount)
        {
            if (pokemonUid < 1 || pokemonUid > Team.Count)
                return false;
            var poke = Team[pokemonUid - 1];
            var totalCollected = poke.EVsCollected.GetStat(EffortValuesManager.Stats[statType.ToUpperInvariant()]);
            if (amount > totalCollected)
                return false;
            SendSetCollectedEvs(poke.UniqueID, statType, poke.EV, amount);
            _itemUseTimeout.Set(Rand.Next(1000, 1500));
            return true;
        }

        public bool TurnCharacter(string dir)
        {
            var toDir = DirectionExtensions.FromChar(dir.ToLowerInvariant()[0]);
            LastDirection = toDir;
            SendMovement(new[] {toDir.ToOneStepMoveActions()}, PlayerX, PlayerY);
            return true;
        }

        public int DistanceTo(int cellX, int cellY)
        {
            return Math.Abs(PlayerX - cellX) + Math.Abs(PlayerY - cellY);
        }

        public int DistanceFrom(int fromX, int fromY)
        {
            return DistanceBetween(fromX, fromY, PlayerX, PlayerY);
        }

        public static int DistanceBetween(int fromX, int fromY, int toX, int toY)
        {
            return Math.Abs(fromX - toX) + Math.Abs(fromY - toY);
        }

        private void LoadMap(string mapName)
        {
            mapName = MapClient.RemoveExtension(mapName);

            _loadingTimeout.Set(Rand.Next(1500, 4000));

            ClearPath();
            OpenedShop = null;
            PlayerStats = null;
            _surfAfterMovement = false;
            CurrentPCBox = null;
            _slidingDirection = null;
            _npcBattler = null;
            _dialogResponses.Clear();
            _movementTimeout.Cancel();
            _mountingTimeout.Cancel();
            _itemUseTimeout.Cancel();

            if (Map is null || mapName != MapName) DownloadMap(mapName);
        }

        private void DownloadMap(string mapName)
        {
            Console.WriteLine("[Map] Requesting: " + MapName);

            AreNpcReceived = false;

            Map = null;
            MapName = mapName;
            _mapClient.DownloadMap(mapName);
            Players.Clear();
            _removedPlayers.Clear();
        }

        private void MapClient_MapLoaded(string mapName, Map map)
        {
            Players.Clear();
            Map = map;
            OnNpcs(Map.OriginalNpcs);

            if (Map.IsSessioned) Resync();
            if (string.Equals(mapName, MapName, StringComparison.InvariantCultureIgnoreCase)
            ) // well the received map name is sometimes upper case..
            {
                Players.Clear();
                _removedPlayers.Clear();
                CheckArea();
                MapLoaded?.Invoke(AreaName);
            }

            if (_cachedNerbyUsers?.Users != null) OnUpdatePlayer(_cachedNerbyUsers);

            CanUseCut = HasCutAbility();
            CanUseSmashRock = HasRockSmashAbility();
            if (!_wasLoggedIn)
            {
                _wasLoggedIn = true;
                _needToSendAck = true;
            }
#if DEBUG

            if (Map.MapDump.Areas?.Count > 0)
                foreach (var area in Map.MapDump.Areas)
                    Console.WriteLine($"[{Map.MapDump.Areas.IndexOf(area)}]: {area.AreaName}");
#endif
        }

        private void OnNpcs(List<Npc> originalNpcs)
        {
            if (!IsMapLoaded) return;

            Map.Npcs.Clear();

            foreach (var npc in originalNpcs)
            {
                var clone = npc.Clone();
                if (clone.IsVisible)
                    Map.Npcs.Add(clone);
            }

            AreNpcReceived = true;
            NpcReceieved?.Invoke(Map.Npcs);
        }

        public void CheckArea()
        {
            if (string.Equals(MapName, "default", StringComparison.InvariantCultureIgnoreCase))
                return;

            if (Map != null)
            {
                Map?.UpdateArea();
                if (Map.MapDump.Areas != null && Map.MapDump.Areas.Count > 0)
                {
                    foreach (var area in Map.MapDump.Areas)
                        if (PlayerX >= area.StartX && PlayerX <= area.EndX && PlayerY >= area.StartY &&
                            PlayerY <= area.EndY)
                        {
                            if (!area.AreaName.Equals(AreaName, StringComparison.InvariantCultureIgnoreCase))
                                RequestArea(MapName, area.AreaName);

                            AreaName = area.AreaName;
                            PositionUpdated?.Invoke(AreaName, PlayerX, PlayerY);
                            return;
                        }

                    if (AreaName != Map.MapDump.Settings.MapName)
                    {
                        AreaName = Map.MapDump.Settings.MapName;
                        RequestArea(MapName, AreaName);
                    }
                }
                else if (AreaName != Map.MapDump.Settings.MapName)
                {
                    AreaName = Map.MapDump.Settings.MapName;
                    RequestArea(MapName, AreaName);
                }

                PositionUpdated?.Invoke(AreaName, PlayerX, PlayerY);
            }
        }

        public void Resync(bool mapLoad = true)
        {
            var syncId = Guid.NewGuid();
            SendProto(new PSXAPI.Request.Sync
            {
                ID = syncId,
                MapLoad = mapLoad
            });
        }

        public bool HasSurfAbility()
        {
            return HasMove("Surf") && HasEffectName("Surf")
                                   && Badges.Count > 0 && Badges.ContainsKey(5);
        }

        public bool HasCutAbility()
        {
            return HasMove("Cut") && Badges.Count > 0 && Badges.ContainsKey(2);
        }

        public bool HasRockSmashAbility()
        {
            return HasMove("Rock Smash");
        }

        public bool PokemonUidHasMove(int pokemonUid, string moveName)
        {
            return Team.FirstOrDefault(p => p.Uid == pokemonUid)?.Moves?.Any(m =>
                       m?.Name?.Equals(moveName, StringComparison.InvariantCultureIgnoreCase) ?? false) ?? false;
        }

        public bool HasMove(string moveName)
        {
            return Team.Any(p =>
                p?.Moves != null && p?.Moves?.Any(m =>
                    m?.Name?.Equals(moveName, StringComparison.InvariantCultureIgnoreCase) ?? false) == true);
        }

        public int GetMovePosition(int pokemonUid, string moveName)
        {
            return Team[pokemonUid]?.Moves
                       ?.FirstOrDefault(m =>
                           m?.Name?.Equals(moveName, StringComparison.InvariantCultureIgnoreCase) ?? false)?.Position ??
                   -1;
        }

        public InventoryItem GetItemFromId(int id)
        {
            return Items?.FirstOrDefault(i => i?.Id == id && i?.Quantity > 0);
        }

        public bool HasItemId(int id)
        {
            return GetItemFromId(id) != null;
        }

        public bool HasPokemonInTeam(string pokemonName)
        {
            return FindFirstPokemonInTeam(pokemonName) != null;
        }

        public Pokemon FindFirstPokemonInTeam(string pokemonName)
        {
            return Team?.FirstOrDefault(p =>
                p != null && p.Name.Equals(pokemonName, StringComparison.InvariantCultureIgnoreCase));
        }

        public InventoryItem GetItemFromName(string itemName)
        {
            return Items.FirstOrDefault(i => (i.Name.Equals(itemName, StringComparison.InvariantCultureIgnoreCase)
                                              || ItemsManager.Instance.ItemClass.items.Any(itm =>
                                                  itm.BattleID.Equals(
                                                      itemName.RemoveAllUnknownSymbols().Replace(" ", ""),
                                                      StringComparison.InvariantCultureIgnoreCase)
                                                  && itm.ID == i.Id))
                                             && i.Quantity > 0);
        }

        public bool HasItemName(string itemName)
        {
            return GetItemFromName(itemName) != null;
        }

        public PlayerEffect GetEffectFromName(string effectName)
        {
            return Effects.FirstOrDefault(e =>
                e.Name.Equals(effectName, StringComparison.InvariantCultureIgnoreCase) && e.UID != Guid.Empty);
        }

        public bool HasEffectName(string effectName)
        {
            return GetEffectFromName(effectName) != null;
        }

        public static string CreateTestProto(IProto proto)
        {
            var array = Proto.Serialize(proto);
            if (array == null) return null;
            var packet = Convert.ToBase64String(array);
            packet = proto._Name + " " + packet;
            return packet;
        }

        public static string BadgeFromID(int id)
        {
            string result;
            switch (id)
            {
                case 1:
                    result = "Boulder Badge";
                    break;
                case 2:
                    result = "Cascade Badge";
                    break;
                case 3:
                    result = "Thunder Badge";
                    break;
                case 4:
                    result = "Rainbow Badge";
                    break;
                case 5:
                    result = "Soul Badge";
                    break;
                case 6:
                    result = "Marsh Badge";
                    break;
                case 7:
                    result = "Volcano Badge";
                    break;
                case 8:
                    result = "Earth Badge";
                    break;
                case 9:
                    result = "Zephyr Badge";
                    break;
                case 10:
                    result = "Hive Badge";
                    break;
                case 11:
                    result = "Plain Badge";
                    break;
                case 12:
                    result = "Fog Badge";
                    break;
                case 13:
                    result = "Storm Badge";
                    break;
                case 14:
                    result = "Mineral Badge";
                    break;
                case 15:
                    result = "Glacier Badge";
                    break;
                case 16:
                    result = "Rising Badge";
                    break;
                case 17:
                    result = "Stone Badge";
                    break;
                case 18:
                    result = "Knuckle Badge";
                    break;
                case 19:
                    result = "Dynamo Badge";
                    break;
                case 20:
                    result = "Heat Badge";
                    break;
                case 21:
                    result = "Balance Badge";
                    break;
                case 22:
                    result = "Feather Badge";
                    break;
                case 23:
                    result = "Mind Badge";
                    break;
                case 24:
                    result = "Rain Badge";
                    break;
                case 25:
                    result = "Coal Badge";
                    break;
                case 26:
                    result = "Forest Badge";
                    break;
                case 27:
                    result = "Cobble Badge";
                    break;
                case 28:
                    result = "Fen Badge";
                    break;
                case 29:
                    result = "Relic Badge";
                    break;
                case 30:
                    result = "Mine Badge";
                    break;
                case 31:
                    result = "Icicle Badge";
                    break;
                case 32:
                    result = "Beacon Badge";
                    break;
                default:
                    result = "";
                    break;
            }

            return result;
        }

        public static string GetStatus(string status)
        {
            switch (status)
            {
                case "slp":
                    status = "Sleep";
                    break;
                case "brn":
                    status = "Brun";
                    break;
                case "psn":
                    status = "Posion";
                    break;
                case "par":
                    status = "Paralysis";
                    break;
                case "tox":
                    status = "Posion";
                    break;
                default:
                    status = "None";
                    break;
            }

            return status;
        }

        #region From PSXAPI.DLL

        private TimeSpan pingUpdateTime = TimeSpan.FromSeconds(5.0);
        private Timer timer { get; }
        private DateTime lastPingResponseUtc;
        private bool receivedPing;
        private volatile int ping;
        private bool disposedValue;
        private double _lastCheckPing;

        #endregion
    }

    public static class StringExtention
    {
        public static string RemoveAllUnknownSymbols(this string s)
        {
            var result = new StringBuilder();
            foreach (var c in s)
            {
                var b = (byte) c;
                if (b > 32) //In general, all characters below 32 are non-printable.
                    result.Append(c);
            }

            return result.ToString();
        }
    }
}