using Poke1Bot.Modules;
using Poke1Bot.Scripting;
using Poke1Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using ProtoBuf.Meta;
using PSXAPI;

namespace Poke1Bot
{
    public class BotClient
    {
        public enum State
        {
            Stopped,
            Started,
            Paused
        };

        public AccountManager AccountManager { get; }
        public GameClient Game { get; private set; }
        public BattleAI AI { get; private set; }
        public BaseScript Script { get; private set; }
        public Random Rand { get; } = new Random();
        public Account Account { get; set; }
        public State Running { get; private set; }
        public MovementResynchronizer MovementResynchronizer { get; }
        public AutoReconnector AutoReconnector { get; }
        public PokemonEvolver PokemonEvolver { get; }
        public MoveTeacher MoveTeacher { get; }
        public AutoLootBoxOpener AutoLootBoxOpener { get; }
        public QuestManager QuestManager { get; }
        public UserSettings Settings { get; }

        private Npc _npcBattler;


        public event Action<State> StateChanged;
        public event Action<string> MessageLogged;
        public event Action<string, Brush> ColorMessageLogged;
        public event Action ClientChanged;
        public event Action ConnectionOpened;
        public event Action ConnectionClosed;


        private ProtocolTimeout _actionTimeout = new ProtocolTimeout();

        private bool _loginRequested;

        public BotClient()
        {
            AccountManager = new AccountManager("Accounts");
            PokemonEvolver = new PokemonEvolver(this);
            MovementResynchronizer = new MovementResynchronizer(this);
            MoveTeacher = new MoveTeacher(this);
            AutoReconnector = new AutoReconnector(this);
            AutoLootBoxOpener = new AutoLootBoxOpener(this);
            QuestManager = new QuestManager(this);
            Settings = new UserSettings();

#if DEBUG
            var dc = StringCipher.Base64Decode("F0IHKAtNWA4u");
            var s = "776463e6-59c7-4a42-b861-fccdf79def1c".ReverseString();
            var enc = Poke1Protocol.StringCipher.EncryptOrDecrypt("Username:)".ReverseString(), "776463e6-59c7-4a42-b861-fccdf79def1c".ReverseString());
            Console.WriteLine(enc);
            Console.WriteLine(dc);


            //var packet = @"InventoryPokemon CtEBChIJ8TZvO060XkYRhk8er86fOWQSjgEIowEQCxjPCyAkKgYIXRAZGBkqBggtECgYKCoGCCEQIxgjKgYIXxAUGBQwAjhoQAtSDAgTEAQYHyAJKAEwDloAYAJqEgm/i6xDrmcMTxGCrF/kjgIhyXISCb+LrEOuZwxPEYKsX+SOAiHJgAEEkAHDruOwAaIBBhACIAEwBqoBCwi+8s/AlfvlNhAFsAEDGgwIJBAMGBAgCygRMBEiBXhjb2RlKgV4Y29kZTIIS2VlbiBFeWU4swpAwA0Q////////////ARj///////////8BIP///////////wE=";
            //var packet = @"Transfer CAESEgnxNm87TrReRhGGTx6vzp85ZA==";
            //var packet = @"Reorder ChIJ8TZvO060XkYRhk8er86fOWQKEgkmzfVu9CvLSRGQRb3nB/7fDAoSCfYOcUXnX0tPEaMc+TIXvZ2yChIJYRN0DkiWxU0RgB2UOdBYu10KEgnc3Xc6Zt6VQRG0ts8aa5F9XQ==";            
            var packet = @"Ack CgkXQgcoC01YDi4=";
            //var packet = @"Login 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";
            var data = packet.Split(" ".ToCharArray());

            byte[] array = Convert.FromBase64String(data[1]);
            var type = Type.GetType($"PSXAPI.Request.{data[0]}, PSXAPI");
            //goto RESP;
            if (type != null)
            {
                //var s = proto as PSXAPI.Request.Ack;

                //string decodedString = Encoding.ASCII.GetString(array);
                //Console.WriteLine(decodedString);
                

                if (!(typeof(PSXAPI.Proto).GetMethod("Deserialize").MakeGenericMethod(type).Invoke(null, new object[]
                {
                    array
                }) is IProto proto))
                    goto RESP;

                if (proto is PSXAPI.Request.Login lg)
                {
                    Console.WriteLine(Encoding.ASCII.GetString(Convert.FromBase64String(lg.Test)));
                }

                Console.WriteLine(ToJsonString(proto));
                return;
                //Console.WriteLine($"MapLoad: {(proto as PSXAPI.Request.BattleBroadcast).RequestID}, ID: {(proto as PSXAPI.Request.BattleBroadcast)._Name.ToString()}");
            }
            RESP:
            type = Type.GetType($"PSXAPI.Response.{data[0]}, PSXAPI");
            if (type != null)
            {
                var proto = typeof(PSXAPI.Proto).GetMethod("Deserialize").MakeGenericMethod(new Type[]
                {
                        type
                }).Invoke(null, new object[]
                {
                        array
                }) as PSXAPI.IProto;
                Console.WriteLine(ToJsonString(proto));
                // Console.WriteLine($"MapLoad: {(proto as PSXAPI.Request.BattleBroadcast).RequestID}, ID: {(proto as PSXAPI.Request.BattleBroadcast)._Name.ToString()}");
            }
#endif
        }
        private static string ToJsonString(PSXAPI.IProto p) => Newtonsoft.Json.JsonConvert.SerializeObject(p, new Newtonsoft.Json.JsonSerializerSettings
        {
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() },
            Formatting = Newtonsoft.Json.Formatting.Indented
        });

        public void LogMessage(string message)
        {
            MessageLogged?.Invoke(message);
        }

        public void LogMessage(string message, Brush color)
        {
            ColorMessageLogged?.Invoke(message, color);
        }

        public void Login(Account account)
        {
            Account = account;
            _loginRequested = true;
        }
        private void LoginUpdate()
        {
            GameClient client;
            if (Account.Socks.Version != SocksVersion.None)
            {
                // TODO: Clean this code.
                client = new GameClient(new GameConnection((int)Account.Socks.Version, Account.Socks.Host, Account.Socks.Port, Account.Socks.Username, Account.Socks.Password),
                    new MapConnection((int)Account.Socks.Version, Account.Socks.Host, Account.Socks.Port, Account.Socks.Username, Account.Socks.Password));
            }
            else
            {
                client = new GameClient(new GameConnection(), new MapConnection());
            }
            SetClient(client);
            client.Open();
        }

        public void Logout(bool allowAutoReconnect)
        {
            if (!allowAutoReconnect)
            {
                AutoReconnector.IsEnabled = false;
            }
            Game.Close();
        }

        public void Update()
        {

            if (_loginRequested)
            {
                LoginUpdate();
                _loginRequested = false;
                return;
            }

            AutoReconnector.Update();
            AutoLootBoxOpener.Update();
            QuestManager.Update();

            if (_npcBattler != null && Game.DistanceFrom(_npcBattler.PositionX, _npcBattler.PositionY) > 1
                && Game != null && Game.IsMapLoaded && Game.IsInactive && !Game.IsInBattle)
            {
                Game.ClearPath();
                MoveToCell(_npcBattler.PositionX, _npcBattler.PositionY, 1);
                _npcBattler = null;
                return;
            }
            if (_npcBattler != null && Game != null && Game?.DistanceFrom(_npcBattler.PositionX, _npcBattler.PositionY) <= 1)
                _npcBattler = null;

            if (Script?.IsLoaded == true)
                Script?.Update();

            if (Running != State.Started)
            {
                return;
            }

            if (PokemonEvolver.Update()) return;
            if (MoveTeacher.Update()) return;
            if (AI != null && AI.IsBusy) return;

            if (Game.IsMapLoaded && Game.AreNpcReceived && Game.IsInactive)
            {
                ExecuteNextAction();
            }
        }

        public void Start()
        {
            if (Game != null && Script != null && Running == State.Stopped)
            {
                _actionTimeout.Set();
                Running = State.Started;
                StateChanged?.Invoke(Running);
                Script.Start();
            }
        }

        public void Pause()
        {
            if (Game != null && Script != null && Running != State.Stopped)
            {
                if (Running == State.Started)
                {
                    Running = State.Paused;
                    StateChanged?.Invoke(Running);
                    Script.Pause();
                }
                else
                {
                    Running = State.Started;
                    StateChanged?.Invoke(Running);
                    Script.Resume();
                }
            }
        }

        public void Stop()
        {
            if (Game != null)
                Game.ClearPath();

            if (Running != State.Stopped)
            {
                Running = State.Stopped;
                StateChanged?.Invoke(Running);
                if (Script != null)
                {
                    Script.Stop();
                }
            }
        }

        public async Task LoadScript(string filename)
        {
            using (var reader = new StreamReader(filename))
            {
                var input = await reader.ReadToEndAsync();

                List<string> libs = new List<string>();
                if (Directory.Exists("Libs"))
                {
                    string[] files = Directory.GetFiles("Libs");
                    foreach (string file in files)
                    {
                        if (file.ToUpperInvariant().EndsWith(".LUA"))
                        {
                            using (var streaReader = new StreamReader(file))
                            {
                                libs.Add(await streaReader.ReadToEndAsync());
                            }
                        }
                    }
                }

                BaseScript script = new LuaScript(this, Path.GetFullPath(filename), input, libs);

                Stop();
                Script = script;
            }

            try
            {
                Script.ScriptMessage += Script_ScriptMessage;
                await Script.Initialize();
            }
            catch (Exception)
            {
                Script = null;
                throw;
            }
        }

        public bool OpenPC()
        {
            var pcNpcs = Game.Map.Npcs.FindAll(npc => Game.Map.IsPc(npc.PositionX, npc.PositionY)).OrderBy(npc => Game.DistanceTo(npc.PositionX, npc.PositionY)).ToList();
            if (pcNpcs is null || pcNpcs.Count <= 0)
                return false;
            var pcNpc = pcNpcs.FirstOrDefault();
            if (pcNpc is null)
                return false;

            return TalkToNpc(pcNpc);
        }

        public bool TalkToNpc(Npc target)
        {
            bool canInteract = Game.Map.CanInteract(Game.PlayerX, Game.PlayerY, target.PositionX, target.PositionY);
            if (canInteract)
            {
                var fromNpcDir = target.GetDriectionFrom(Game.PlayerX, Game.PlayerY);
                if (fromNpcDir != Direction.None && fromNpcDir != Game.LastDirection && !target.IsInLineOfSight(Game.PlayerX, Game.PlayerY))
                {
                    var oneStep = new[] { fromNpcDir.ToOneStepMoveActions() };
                    Game.SendMovement(oneStep, Game.PlayerX, Game.PlayerY);
                    Game.LastDirection = fromNpcDir;
                }
                Game.TalkToNpc(target);
                return true;
            }
            return MoveToCell(target.PositionX, target.PositionY, 1);
        }

        public bool MoveToCell(int x, int y, int requiredDistance = 0)
        {
            MovementResynchronizer.CheckMovement(x, y);

            Pathfinding path = new Pathfinding(Game);
            bool result;

            if (Game.PlayerX == x && Game.PlayerY == y)
            {
                result = path.MoveToSameCell();
            }
            else
            {
                result = path.MoveTo(x, y, requiredDistance);
            }

            if (result)
            {
                MovementResynchronizer.ApplyMovement(x, y);
            }
            return result;
        }

        public bool MoveToNearestLink()
        {
            var links = Game.Map.Links.FindAll(link => link.DestinationId != Guid.Empty).
                OrderBy(link => GameClient.DistanceBetween(Game.PlayerX, Game.PlayerY, link.DestinationX, link.DestinationY));
            foreach (var link in links)
                if (MoveToCell(link.DestinationX, link.DestinationY))
                    return true;
            return false;
        }

        public bool CanMoveTo(int x, int y)
        {
            Pathfinding path = new Pathfinding(Game);
            return path.CanMoveTo(x, y);
        }

        public bool MoveToAreaLink(string destinationMap)
        {
            IEnumerable<Tuple<int, int>> nearest = Game.Map.GetNearestLinks(destinationMap, Game.PlayerX, Game.PlayerY);
            if (nearest != null)
            {
                foreach (Tuple<int, int> link in nearest)
                {
                    if (MoveToCell(link.Item1, link.Item2))
                        return true;
                }
            }
            return false;
        }

        public bool MoveLeftRight(int startX, int startY, int destX, int destY)
        {
            bool result;
            if (startX != destX && startY != destY)
            {
                // ReSharper disable once RedundantAssignment
                return false;
            }
            if (Game.PlayerX == destX && Game.PlayerY == destY)
            {
                result = MoveToCell(startX, startY);
            }
            else if (Game.PlayerX == startX && Game.PlayerY == startY)
            {
                result = MoveToCell(destX, destY);
            }
            else
            {
                result = MoveToCell(startX, startY);
            }
            return result;
        }

        public bool IsAreaLink(int x, int y)
        {
            var pathFinder = new Pathfinding(Game);
            return Game != null && Game.IsMapLoaded && Game.Map.IsAreaLink(x, y) && pathFinder.CanMoveTo(x, y);
        }

        public void SetClient(GameClient client)
        {
            Game = client;
            AI = null;
            Stop();

            if (client != null)
            {
                AI = new BattleAI(client);
                client.ConnectionOpened += Client_ConnectionOpened;
                client.ConnectionFailed += Client_ConnectionFailed;
                client.ConnectionClosed += Client_ConnectionClosed;
                client.BattleMessage += Client_BattleMessage;
                client.SystemMessage += Client_SystemMessage;
                client.ServerCommandException += Client_ServerCommandException;
                client.DialogOpened += Client_DialogOpened;
                client.TeleportationOccuring += Client_TeleportationOccuring;
                client.LogMessage += LogMessage;
                client.MoveToBattleWithNpc += Client_MoveToBattleWithNpc;
            }
            ClientChanged?.Invoke();
        }

        private void Client_MoveToBattleWithNpc(Npc battler)
        {
            _npcBattler = battler;
        }

        private void Client_ServerCommandException(string msg)
        {
            Stop();
            LogMessage("Server occurred an exception: " + msg, Brushes.Firebrick);
        }

        private void ExecuteNextAction()
        {
            try
            {
                bool executed = Script.ExecuteNextAction();
                if (!executed && Running != State.Stopped && !_actionTimeout.Update())
                {
                    LogMessage("No action executed: stopping the bot.");
                    Stop();
                }
                else if (executed)
                {
                    _actionTimeout.Set();
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                LogMessage("Error during the script execution: " + ex);
#else
                LogMessage("Error during the script execution: " + ex.Message);
#endif
                Stop();
            }
        }

        private void Client_ConnectionOpened()
        {
            ConnectionOpened?.Invoke();
            Game.SendAuthentication(Account.Name, Account.Password);
        }
        private void Client_ConnectionClosed(Exception ex)
        {
            if (ex != null)
            {
#if DEBUG
                LogMessage("Disconnected from the server: " + ex, Brushes.OrangeRed);
#else
                LogMessage("Disconnected from the server: " + ex.Message, Brushes.OrangeRed);
#endif
            }
            else
            {
                LogMessage("Disconnected from the server.", Brushes.OrangeRed);
            }
            ConnectionClosed?.Invoke();
            SetClient(null);
        }
        private void Client_ConnectionFailed(Exception ex)
        {
            if (ex != null)
            {
#if DEBUG
                LogMessage("Could not connect to the server: " + ex, Brushes.OrangeRed);
#else
                LogMessage("Could not connect to the server: " + ex.Message, Brushes.OrangeRed);
#endif
            }
            else
            {
                LogMessage("Could not connect to the server.", Brushes.OrangeRed);
            }
            ConnectionClosed?.Invoke();
            SetClient(null);
        }
        private void Client_SystemMessage(string message)
        {
            if (Script != null)
                Script.OnSystemMessage(message);
        }
        private void Client_BattleMessage(string message)
        {
            if (Script != null)
                Script.OnBattleMessage(message);
        }

        private void Client_DialogOpened(string message)
        {
            if (Script != null)
                Script.OnDialogMessage(message);
        }

        private void Client_TeleportationOccuring(string map, int x, int y)
        {
            string message = "Position updated: [" + map + "] (" + x + ", " + y + ")";
            if (Game.Map == null || Game.IsTeleporting)
            {
                message += " [OK]";
            }
            else if (Game.MapName != map)
            {
                message += " [WARNING, different map] /!\\";
                Script?.OnWarningMessage(true);
            }
            else
            {
                int distance = GameClient.DistanceBetween(x, y, Game.PlayerX, Game.PlayerY);
                if (distance < 8)
                {
                    message += " [OK, lag, distance=" + distance + "]";
                }
                else
                {
                    message += " [WARNING, distance=" + distance + "] /!\\";
                    Script?.OnWarningMessage(false, distance);
                }
                Script?.OnMovementLag(distance);
            }

            if (message.Contains("OK"))
            {
                LogMessage(message, Brushes.LimeGreen);
            }
            if (message.Contains("WARNING"))
            {
                LogMessage(message, Brushes.OrangeRed);
            }

            MovementResynchronizer.Reset();
        }

        private void Script_ScriptMessage(string message)
        {
            LogMessage(message);
        }
    }
}
