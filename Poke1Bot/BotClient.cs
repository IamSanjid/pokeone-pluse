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
            byte[] bytes = new byte[] { 0x50, 0x6a, 0x03, 0xc2, 0xcf, 0x68, 0x90, 0xe6, 0xba, 0x32, 0x3c, 0x1a, 0x08, 0x00, 0x45, 0x00, 0x00, 0x28, 0x78, 0x91, 0x40, 0x00, 0x80, 0x06, 0x00, 0x00, 0xc0, 0xa8, 0x01, 0x0e, 0x5f, 0xb7, 0x30, 0x44, 0xed, 0x6f, 0x07, 0xdc, 0x54, 0x7f, 0x6b, 0x80, 0x7d, 0x82, 0x1a, 0x41, 0x50, 0x10, 0x01, 0x00, 0x51, 0xcc, 0x00, 0x00 };
            var pc = Convert.ToBase64String(bytes);

            Console.WriteLine(pc);

            var packet = @"Battle CgZ8c3BsaXQKCXxjaG9pY2V8fAolfGNob2ljZXxtb3ZlIHRhY2tsZSAyLCBtb3ZlIGFic29yYiAyfAopfGNob2ljZXx8bW92ZSBhc3N1cmFuY2UgMSwgbW92ZSBzY3JhdGNoIDEKRXxjaG9pY2V8bW92ZSB0YWNrbGUgMiwgbW92ZSBhYnNvcmIgMnxtb3ZlIGFzc3VyYW5jZSAxLCBtb3ZlIHNjcmF0Y2ggMQoBfAonfG1vdmV8cDJiOiBNZW93dGh8U2NyYXRjaHxwMWE6IEhvb3Rob290CgZ8c3BsaXQKHHwtZGFtYWdlfHAxYTogSG9vdGhvb3R8MCBmbnQKHHwtZGFtYWdlfHAxYTogSG9vdGhvb3R8MCBmbnQKHHwtZGFtYWdlfHAxYTogSG9vdGhvb3R8MCBmbnQKHHwtZGFtYWdlfHAxYTogSG9vdGhvb3R8MCBmbnQKFHxmYWludHxwMWE6IEhvb3Rob290Cil8bW92ZXxwMmE6IEtvZmZpbmd8QXNzdXJhbmNlfHAxYjogQXJpYWRvcwoGfHNwbGl0Cht8LWRhbWFnZXxwMWI6IEFyaWFkb3N8MCBmbnQKG3wtZGFtYWdlfHAxYjogQXJpYWRvc3wwIGZudAobfC1kYW1hZ2V8cDFiOiBBcmlhZG9zfDAgZm50Cht8LWRhbWFnZXxwMWI6IEFyaWFkb3N8MCBmbnQKE3xmYWludHxwMWI6IEFyaWFkb3MKAXwKB3x1cGtlZXAS+QkKAnAxEAsa8AkICxABKAEoATrlCQoCcDESAnAxGogCCgxwMTogSG9vdGhvb3QSD0hvb3Rob290LCBMNSwgTRoFMCBmbnQgASoKCAcQCBgIIAsoCjIJZm9yZXNpZ2h0MgVncm93bDIGdGFja2xlMghoeXBub3NpczoHa2VlbmV5ZUIASgdwb2tiYWxsUgtyZWdlbmRyaWRlcli53Kj2BmIiCglGb3Jlc2lnaHQSCWZvcmVzaWdodBgoICgqBm5vcm1hbGIjCgVHcm93bBIFZ3Jvd2wYKCAoKg9hbGxBZGphY2VudEZvZXNiHAoGVGFja2xlEgZ0YWNrbGUYIiAjKgZub3JtYWxiIAoISHlwbm9zaXMSCGh5cG5vc2lzGBQgFCoGbm9ybWFsGqwCCgtwMTogQXJpYWRvcxIOQXJpYWRvcywgTDYsIE0aBTAgZm50IAEqCggPEAsYDSAQKAoyCnN0cmluZ3Nob3QyC3N3b3Jkc2RhbmNlMgt2ZW5vbWRyZW5jaDIGYWJzb3JiOgVzd2FybUIASgdwb2tiYWxsUgtyZWdlbmRyaWRlcli/seuKAWIuCgtTdHJpbmcgU2hvdBIKc3RyaW5nc2hvdBgoICgqD2FsbEFkamFjZW50Rm9lc2IlCgxTd29yZHMgRGFuY2USC3N3b3Jkc2RhbmNlGBQgFCoEc2VsZmIwCgxWZW5vbSBEcmVuY2gSC3Zlbm9tZHJlbmNoGBQgFCoPYWxsQWRqYWNlbnRGb2VzYhwKBkFic29yYhIGYWJzb3JiGBUgGSoGbm9ybWFsGuYBCgtwMTogUmF0dGF0YRIOUmF0dGF0YSwgTDYsIE0aBTAgZm50KgoIChAJGAkgCSgPMgZ0YWNrbGUyCHRhaWx3aGlwMgtxdWlja2F0dGFjazoEZ3V0c0IASgdwb2tiYWxsUgtyZWdlbmRyaWRlclis6YyPAmIcCgZUYWNrbGUSBnRhY2tsZRgiICMqBm5vcm1hbGIqCglUYWlsIFdoaXASCHRhaWx3aGlwGB4gHioPYWxsQWRqYWNlbnRGb2VzYicKDFF1aWNrIEF0dGFjaxILcXVpY2thdHRhY2sYHiAeKgZub3JtYWwaigIKDXAxOiBXYXJ0b3J0bGUSEVdhcnRvcnRsZSwgTDE3LCBNGgUwIGZudCoKCBoQIhgeICQoHDIEYml0ZTIIdGFpbHdoaXAyCHdhdGVyZ3VuMgh3aXRoZHJhdzoHdG9ycmVudEIASghwb2tlYmFsbFILcmVnZW5kcmlkZXJYhvHAwAdiGAoEQml0ZRIEYml0ZRgEIBkqBm5vcm1hbGIqCglUYWlsIFdoaXASCHRhaWx3aGlwGB4gHioPYWxsQWRqYWNlbnRGb2VzYiEKCVdhdGVyIEd1bhIId2F0ZXJndW4YDSAZKgZub3JtYWxiHgoIV2l0aGRyYXcSCHdpdGhkcmF3GCggKCoEc2VsZhqqAQoKcDE6IExlZHliYRINTGVkeWJhLCBMNSwgTRoFMTkvMTkqCggHEAkYCCANKAoyBnRhY2tsZTIKc3VwZXJzb25pYzoFc3dhcm1CAEoHcG9rYmFsbFILcmVnZW5kcmlkZXJYoM2hGGIcCgZUYWNrbGUSBnRhY2tsZRgjICMqBm5vcm1hbGIkCgpTdXBlcnNvbmljEgpzdXBlcnNvbmljGBQgFCoGbm9ybWFsGvsHCgJwMhALGvIHIAE67QcKAnAyEgJwMhr5AQoLcDI6IEtvZmZpbmcSD0tvZmZpbmcsIEwxMywgTRoFMzIvMzMgASoKCBIQHRgUIBAoDzIGdGFja2xlMgRzbW9nMgtzbW9rZXNjcmVlbjIJYXNzdXJhbmNlOghsZXZpdGF0ZUIASghwb2tlYmFsbFjEld3RAmIcCgZUYWNrbGUSBnRhY2tsZRgjICMqBm5vcm1hbGIYCgRTbW9nEgRzbW9nGBMgFCoGbm9ybWFsYiYKC1Ntb2tlc2NyZWVuEgtzbW9rZXNjcmVlbhgRIBQqBm5vcm1hbGIiCglBc3N1cmFuY2USCWFzc3VyYW5jZRgHIAoqBm5vcm1hbBrxAQoKcDI6IE1lb3d0aBIOTWVvd3RoLCBMMTUsIEYaBTMwLzM3IAEqCggSEA0YEiARKCAyB3NjcmF0Y2gyBGJpdGUyB2Zha2VvdXQyCmZ1cnlzd2lwZXM6BnBpY2t1cEIASghwb2tlYmFsbFjHv/mMBWIeCgdTY3JhdGNoEgdzY3JhdGNoGCIgIyoGbm9ybWFsYhgKBEJpdGUSBGJpdGUYGSAZKgZub3JtYWxiHwoIRmFrZSBPdXQSB2Zha2VvdXQYCCAKKgZub3JtYWxiJQoLRnVyeSBTd2lwZXMSCmZ1cnlzd2lwZXMYDyAPKgZub3JtYWwa4gEKCXAyOiBFa2FucxINRWthbnMsIEwxMywgRhoFMCBmbnQqCggSEBAYECATKBMyBHdyYXAyC3BvaXNvbnN0aW5nMgRiaXRlMgVnbGFyZToIc2hlZHNraW5CAEoIcG9rZWJhbGxYtffxgwJiGAoEV3JhcBIEd3JhcBgTIBQqBm5vcm1hbGInCgxQb2lzb24gU3RpbmcSC3BvaXNvbnN0aW5nGCMgIyoGbm9ybWFsYhgKBEJpdGUSBGJpdGUYGSAZKgZub3JtYWxiGgoFR2xhcmUSBWdsYXJlGB4gHioGbm9ybWFsGo0CCg1wMjogR3Jvd2xpdGhlEhFHcm93bGl0aGUsIEwxNCwgRhoFMCBmbnQqCggaEA8YGCATKBUyBWVtYmVyMgRsZWVyMgpvZG9yc2xldXRoMgtoZWxwaW5naGFuZDoJZmxhc2hmaXJlQgBKCHBva2ViYWxsWN+/nIwCYhoKBUVtYmVyEgVlbWJlchgZIBkqBm5vcm1hbGIhCgRMZWVyEgRsZWVyGB0gHioPYWxsQWRqYWNlbnRGb2VzYiUKC09kb3IgU2xldXRoEgpvZG9yc2xldXRoGCggKCoGbm9ybWFsYi0KDEhlbHBpbmcgSGFuZBILaGVscGluZ2hhbmQYFCAUKgxhZGphY2VudEFsbHkqC3JlZ2VuZHJpZGVyKgtyZWdlbmRyaWRlcioLcmVnZW5kcmlkZXIqC3JlZ2VuZHJpZGVyKgtyZWdlbmRyaWRlckACWAFgMA==";
            var data = packet.Split(" ".ToCharArray());

            byte[] array = Convert.FromBase64String(data[1]);
            var type = Type.GetType($"PSXAPI.Request.{data[0]}, PSXAPI");

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
                //Console.WriteLine($"MapLoad: {(proto as PSXAPI.Request.BattleBroadcast).RequestID}, ID: {(proto as PSXAPI.Request.BattleBroadcast)._Name.ToString()}");
            }
            else
            {
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

            if (_npcBattler != null && Game != null && Game.IsMapLoaded && Game.IsInactive)
            {
                Game.ClearPath();
                TalkToNpc(_npcBattler);
                return;
            }

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

        public bool TalkToNpc(Npc target)
        {
            bool canInteract = Game.Map.CanInteract(Game.PlayerX, Game.PlayerY, target.PositionX, target.PositionY);
            if (canInteract)
            {
                _npcBattler = null;
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
            var links = Game.Map.Links.OrderBy(link => GameClient.DistanceBetween(Game.PlayerX, Game.PlayerY, link.x, -link.z));
            foreach (var link in links)
                if (MoveToCell(link.x, -link.z))
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

        private bool IsInArea(MAPAPI.Response.Area area, int x, int y)
        {
            if (x >= area.StartX && x <= area.EndX && y >= area.StartY && y <= area.EndY)
            {
                return true;
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
