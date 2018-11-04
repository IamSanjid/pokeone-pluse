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

            var packet = @"Battle CgZ8c3BsaXQKCXxjaG9pY2V8fAo2fGNob2ljZXxtb3ZlIGFxdWF0YWlsIDMsIG1vdmUgY3V0IDMsIG1vdmUgc21hY2tkb3duIDF8Cjp8Y2hvaWNlfHxtb3ZlIGFzc3VyYW5jZSAzLCBtb3ZlIHNwaXR1cCAxLCBtb3ZlIGRvdWJsZWhpdCAyCmd8Y2hvaWNlfG1vdmUgYXF1YXRhaWwgMywgbW92ZSBjdXQgMywgbW92ZSBzbWFja2Rvd24gMXxtb3ZlIGFzc3VyYW5jZSAzLCBtb3ZlIHNwaXR1cCAxLCBtb3ZlIGRvdWJsZWhpdCAyCgF8Cit8bW92ZXxwMmM6IEthbmdhc2toYW58RG91YmxlIEhpdHxwMWI6IFBhcmFzCgZ8c3BsaXQKGXwtZGFtYWdlfHAxYjogUGFyYXN8MjAvNDgKGXwtZGFtYWdlfHAxYjogUGFyYXN8MjQvNTYKGXwtZGFtYWdlfHAxYjogUGFyYXN8MjAvNDgKGXwtZGFtYWdlfHAxYjogUGFyYXN8MjQvNTYKBnxzcGxpdAoZfC1kYW1hZ2V8cDFiOiBQYXJhc3wwIGZudAoZfC1kYW1hZ2V8cDFiOiBQYXJhc3wwIGZudAoZfC1kYW1hZ2V8cDFiOiBQYXJhc3wwIGZudAoZfC1kYW1hZ2V8cDFiOiBQYXJhc3wwIGZudAoXfC1oaXRjb3VudHxwMWI6IFBhcmFzfDIKEXxmYWludHxwMWI6IFBhcmFzCid8bW92ZXxwMmI6IEVrYW5zfFNwaXQgVXB8cDFhOiBXYXJ0b3J0bGUKKXxtb3ZlfHAyYTogS29mZmluZ3xBc3N1cmFuY2V8cDFjOiBHZW9kdWRlCgZ8c3BsaXQKG3wtZGFtYWdlfHAxYzogR2VvZHVkZXwyOS80OAobfC1kYW1hZ2V8cDFjOiBHZW9kdWRlfDMzLzU0Cht8LWRhbWFnZXxwMWM6IEdlb2R1ZGV8MjkvNDgKG3wtZGFtYWdlfHAxYzogR2VvZHVkZXwzMy81NAoYfGNhbnR8cDFhOiBXYXJ0b3J0bGV8cGFyCip8bW92ZXxwMWM6IEdlb2R1ZGV8U21hY2sgRG93bnxwMmE6IEtvZmZpbmcKBnxzcGxpdAoafC1kYW1hZ2V8cDJhOiBLb2ZmaW5nfDYvNDgKGnwtZGFtYWdlfHAyYTogS29mZmluZ3w2LzQ4Chp8LWRhbWFnZXxwMmE6IEtvZmZpbmd8OC81NQoafC1kYW1hZ2V8cDJhOiBLb2ZmaW5nfDgvNTUKAXwKB3x1cGtlZXAS+AwKAnAxEAYa7wwIBhABKAAoASgAOuIMCgJwMRICcDEangIKDXAxOiBXYXJ0b3J0bGUSEVdhcnRvcnRsZSwgTDMwLCBNGgk2NC83OSBwYXIgASoKCCwQMBgtID4oLDIIYXF1YXRhaWwyCHRhaWx3aGlwMgh3YXRlcmd1bjIId2l0aGRyYXc6B3RvcnJlbnRCAEoIcG9rZWJhbGxSDHRodWl0aHVpaG9hY1iQhsG7BmIhCglBcXVhIFRhaWwSCGFxdWF0YWlsGAQgCioGbm9ybWFsYioKCVRhaWwgV2hpcBIIdGFpbHdoaXAYHiAeKg9hbGxBZGphY2VudEZvZXNiIQoJV2F0ZXIgR3VuEgh3YXRlcmd1bhgZIBkqBm5vcm1hbGIeCghXaXRoZHJhdxIId2l0aGRyYXcYKCAoKgRzZWxmGoMCCglwMTogUGFyYXMSDVBhcmFzLCBMMjQsIEYaBTAgZm50IAEqCggqECMYIiAgKBAyA2N1dDIMcG9pc29ucG93ZGVyMglzdHVuc3BvcmUyBmFic29yYjoHZHJ5c2tpbkIASgdwb2tiYWxsUgx0aHVpdGh1aWhvYWNY9+66wwdiFgoDQ3V0EgNjdXQYHiAeKgZub3JtYWxiKQoNUG9pc29uIFBvd2RlchIMcG9pc29ucG93ZGVyGCMgIyoGbm9ybWFsYiMKClN0dW4gU3BvcmUSCXN0dW5zcG9yZRgeIB4qBm5vcm1hbGIcCgZBYnNvcmISBmFic29yYhgXIBkqBm5vcm1hbBqiAgoLcDE6IEdlb2R1ZGUSD0dlb2R1ZGUsIEwyNCwgTRoFMzMvNTQgASoKCDUQNRgUIBooFTIMc2VsZmRlc3RydWN0MglzbWFja2Rvd24yCnJvY2twb2xpc2gyB3JvbGxvdXQ6BnN0dXJkeUIASgdwb2tiYWxsUgx0aHVpdGh1aWhvYWNYuufCwAViLgoNU2VsZi1EZXN0cnVjdBIMc2VsZmRlc3RydWN0GAUgBSoLYWxsQWRqYWNlbnRiIwoKU21hY2sgRG93bhIJc21hY2tkb3duGAwgDyoGbm9ybWFsYiMKC1JvY2sgUG9saXNoEgpyb2NrcG9saXNoGBQgFCoEc2VsZmIeCgdSb2xsb3V0Egdyb2xsb3V0GBQgFCoGbm9ybWFsGv0BCgxwMTogU251YmJ1bGwSEFNudWJidWxsLCBMMTksIEYaBTU1LzU1KgoIJxAbGBggGigQMghoZWFkYnV0dDILdGh1bmRlcmZhbmcyBGJpdGUyBGxpY2s6B3J1bmF3YXlCAEoHcG9rYmFsbFIMdGh1aXRodWlob2FjWLLlkokFYiAKCEhlYWRidXR0EghoZWFkYnV0dBgPIA8qBm5vcm1hbGInCgxUaHVuZGVyIEZhbmcSC3RodW5kZXJmYW5nGA8gDyoGbm9ybWFsYhgKBEJpdGUSBGJpdGUYGSAZKgZub3JtYWxiGAoETGljaxIEbGljaxgeIB4qBm5vcm1hbBqMAgoOcDE6IEJlbGxzcHJvdXQSEkJlbGxzcHJvdXQsIEwxOCwgTRoFNDYvNDYqCgggEBIYIiATKBUyCXN0dW5zcG9yZTIGZ3Jvd3RoMgR3cmFwMgtzbGVlcHBvd2RlcjoLY2hsb3JvcGh5bGxCAEoHcG9rYmFsbFIMdGh1aXRodWlob2FjWMTG0HViIwoKU3R1biBTcG9yZRIJc3R1bnNwb3JlGB4gHioGbm9ybWFsYhoKBkdyb3d0aBIGZ3Jvd3RoGBQgFCoEc2VsZmIYCgRXcmFwEgR3cmFwGBQgFCoGbm9ybWFsYicKDFNsZWVwIFBvd2RlchILc2xlZXBwb3dkZXIYDyAPKgZub3JtYWwa/AEKCnAxOiBNZW93dGgSDk1lb3d0aCwgTDE2LCBGGgU0MS80MSoKCBoQExgRIBQoHjIHc2NyYXRjaDIEYml0ZTIHZmFrZW91dDIKZnVyeXN3aXBlczoGcGlja3VwQgBKB3Bva2JhbGxSDHRodWl0aHVpaG9hY1jD6c/iA2IeCgdTY3JhdGNoEgdzY3JhdGNoGCMgIyoGbm9ybWFsYhgKBEJpdGUSBGJpdGUYGSAZKgZub3JtYWxiHwoIRmFrZSBPdXQSB2Zha2VvdXQYCiAKKgZub3JtYWxiJQoLRnVyeSBTd2lwZXMSCmZ1cnlzd2lwZXMYDyAPKgZub3JtYWwatQwKAnAyEAYarAwgATqnDAoCcDISAnAyGpECCgtwMjogS29mZmluZxIPS29mZmluZywgTDI1LCBNGgQ4LzU1IAEqCgglEC4YIyAbKBgyCWFzc3VyYW5jZTIJY2xlYXJzbW9nMgZzbHVkZ2UyDHNlbGZkZXN0cnVjdDoIbGV2aXRhdGVCAEoIcG9rZWJhbGxYguTF2AFiIgoJQXNzdXJhbmNlEglhc3N1cmFuY2UYCCAKKgZub3JtYWxiIwoKQ2xlYXIgU21vZxIJY2xlYXJzbW9nGA4gDyoGbm9ybWFsYhwKBlNsdWRnZRIGc2x1ZGdlGBQgFCoGbm9ybWFsYi4KDVNlbGYtRGVzdHJ1Y3QSDHNlbGZkZXN0cnVjdBgFIAUqC2FsbEFkamFjZW50GvEBCglwMjogRWthbnMSDUVrYW5zLCBMMjUsIEYaBTQ5LzUyIAEqCggjEBsYGSAgKCAyBGFjaWQyBnNwaXR1cDIJc3RvY2twaWxlMgdzd2FsbG93OgppbnRpbWlkYXRlQgBKCHBva2ViYWxsWOuWzsABYiEKBEFjaWQSBGFjaWQYHiAeKg9hbGxBZGphY2VudEZvZXNiHQoHU3BpdCBVcBIGc3BpdHVwGAggCioGbm9ybWFsYiAKCVN0b2NrcGlsZRIJc3RvY2twaWxlGBQgFCoEc2VsZmIcCgdTd2FsbG93Egdzd2FsbG93GAogCioEc2VsZhr2AQoOcDI6IEthbmdhc2toYW4SEkthbmdhc2toYW4sIEwyOSwgRhoFOTkvOTkgASoKCDwQMxgcIC0oPjIEYml0ZTIJZG91YmxlaGl0MgRyYWdlMgltZWdhcHVuY2g6CWVhcmx5YmlyZEIASghwb2tlYmFsbFjg+8bTBWIYCgRCaXRlEgRiaXRlGBkgGSoGbm9ybWFsYiMKCkRvdWJsZSBIaXQSCWRvdWJsZWhpdBgJIAoqBm5vcm1hbGIYCgRSYWdlEgRyYWdlGBQgFCoGbm9ybWFsYiMKCk1lZ2EgUHVuY2gSCW1lZ2FwdW5jaBgUIBQqBm5vcm1hbBqOAgoIcDI6IE9uaXgSDE9uaXgsIEwyNSwgRhoFMCBmbnQqCggbEEwYFCAdKCgyCnJvY2twb2xpc2gyCGd5cm9iYWxsMglzbWFja2Rvd24yDGRyYWdvbmJyZWF0aDoIcm9ja2hlYWRCAEoIcG9rZWJhbGxYw9iklwViIwoLUm9jayBQb2xpc2gSCnJvY2twb2xpc2gYFCAUKgRzZWxmYiEKCUd5cm8gQmFsbBIIZ3lyb2JhbGwYBSAFKgZub3JtYWxiIwoKU21hY2sgRG93bhIJc21hY2tkb3duGA8gDyoGbm9ybWFsYikKDURyYWdvbiBCcmVhdGgSDGRyYWdvbmJyZWF0aBgTIBQqBm5vcm1hbBqCAgoKcDI6IE1lb3d0aBIOTWVvd3RoLCBMMjUsIE0aBTAgZm50KgoIGBAWGBkgGygyMgpmdXJ5c3dpcGVzMgdzY3JlZWNoMgtmZWludGF0dGFjazIFdGF1bnQ6CnRlY2huaWNpYW5CAEoIcG9rZWJhbGxYguix5gViJQoLRnVyeSBTd2lwZXMSCmZ1cnlzd2lwZXMYDyAPKgZub3JtYWxiHgoHU2NyZWVjaBIHc2NyZWVjaBgoICgqBm5vcm1hbGInCgxGZWludCBBdHRhY2sSC2ZlaW50YXR0YWNrGBMgFCoGbm9ybWFsYhoKBVRhdW50EgV0YXVudBgUIBQqBm5vcm1hbBqFAgoLcDI6IFJoeWhvcm4SD1JoeWhvcm4sIEwyNCwgTRoFMCBmbnQqCggxEDIYEyARKBEyCXNjYXJ5ZmFjZTIJc21hY2tkb3duMgVzdG9tcDIIYnVsbGRvemU6DGxpZ2h0bmluZ3JvZEIASghwb2tlYmFsbFjS3YCRA2IjCgpTY2FyeSBGYWNlEglzY2FyeWZhY2UYCiAKKgZub3JtYWxiIwoKU21hY2sgRG93bhIJc21hY2tkb3duGA8gDyoGbm9ybWFsYhoKBVN0b21wEgVzdG9tcBgUIBQqBm5vcm1hbGIlCghCdWxsZG96ZRIIYnVsbGRvemUYFCAUKgthbGxBZGphY2VudCoMdGh1aXRodWlob2FjKgx0aHVpdGh1aWhvYWMqDHRodWl0aHVpaG9hYyoMdGh1aXRodWlob2FjKgx0aHVpdGh1aWhvYWMqDHRodWl0aHVpaG9hY0ADWAFgIg==";
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
