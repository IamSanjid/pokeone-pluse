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

            var enc = Poke1Protocol.StringCipher.EncryptOrDecryptToBase64Byte("username;)", "db2a1b6e-34d9-46ae-b319-d58bfc71011d");

            var s64 = new PSXAPI.Request.Ack
            {
                Data = enc
            };

            Console.WriteLine(Encoding.UTF8.GetString(s64.Data));

            //var packet = @"InventoryPokemon CtEBChIJ8TZvO060XkYRhk8er86fOWQSjgEIowEQCxjPCyAkKgYIXRAZGBkqBggtECgYKCoGCCEQIxgjKgYIXxAUGBQwAjhoQAtSDAgTEAQYHyAJKAEwDloAYAJqEgm/i6xDrmcMTxGCrF/kjgIhyXISCb+LrEOuZwxPEYKsX+SOAiHJgAEEkAHDruOwAaIBBhACIAEwBqoBCwi+8s/AlfvlNhAFsAEDGgwIJBAMGBAgCygRMBEiBXhjb2RlKgV4Y29kZTIIS2VlbiBFeWU4swpAwA0Q////////////ARj///////////8BIP///////////wE=";
            //var packet = @"Transfer CAESEgnxNm87TrReRhGGTx6vzp85ZA==";
            //var packet = @"Reorder ChIJ8TZvO060XkYRhk8er86fOWQKEgkmzfVu9CvLSRGQRb3nB/7fDAoSCfYOcUXnX0tPEaMc+TIXvZ2yChIJYRN0DkiWxU0RgB2UOdBYu10KEgnc3Xc6Zt6VQRG0ts8aa5F9XQ==";            
            var packet = @"Battle CgZ8c3BsaXQKCXxjaG9pY2V8fAo3fGNob2ljZXxtb3ZlIGJpdGUgMywgbW92ZSB0YWNrbGUgMywgbW92ZSB0aHVuZGVyZmFuZyAxfApAfGNob2ljZXx8bW92ZSBjbGVhcnNtb2cgMywgbW92ZSBkcmFnb25icmVhdGggMSwgbW92ZSBkb3VibGVoaXQgMgpufGNob2ljZXxtb3ZlIGJpdGUgMywgbW92ZSB0YWNrbGUgMywgbW92ZSB0aHVuZGVyZmFuZyAxfG1vdmUgY2xlYXJzbW9nIDMsIG1vdmUgZHJhZ29uYnJlYXRoIDEsIG1vdmUgZG91YmxlaGl0IDIKAXwKLHxtb3ZlfHAyYjogT25peHxEcmFnb24gQnJlYXRofHAxYTogQmxhc3RvaXNlCgZ8c3BsaXQKHXwtZGFtYWdlfHAxYTogQmxhc3RvaXNlfDEzLzQ4Ch18LWRhbWFnZXxwMWE6IEJsYXN0b2lzZXwyNC84OAodfC1kYW1hZ2V8cDFhOiBCbGFzdG9pc2V8MTMvNDgKHXwtZGFtYWdlfHAxYTogQmxhc3RvaXNlfDI0Lzg4Cht8LXN0YXR1c3xwMWE6IEJsYXN0b2lzZXxwYXIKKXxtb3ZlfHAxYTogQmxhc3RvaXNlfEJpdGV8cDJjOiBLYW5nYXNraGFuCgZ8c3BsaXQKHnwtZGFtYWdlfHAyYzogS2FuZ2Fza2hhbnwzNS80OAoefC1kYW1hZ2V8cDJjOiBLYW5nYXNraGFufDM1LzQ4Ch58LWRhbWFnZXxwMmM6IEthbmdhc2toYW58NzMvOTkKHnwtZGFtYWdlfHAyYzogS2FuZ2Fza2hhbnw3My85OQotfG1vdmV8cDJjOiBLYW5nYXNraGFufERvdWJsZSBIaXR8cDFiOiBOb2N0b3dsCgZ8c3BsaXQKG3wtZGFtYWdlfHAxYjogTm9jdG93bHwwIGZudAobfC1kYW1hZ2V8cDFiOiBOb2N0b3dsfDAgZm50Cht8LWRhbWFnZXxwMWI6IE5vY3Rvd2x8MCBmbnQKG3wtZGFtYWdlfHAxYjogTm9jdG93bHwwIGZudAoZfC1oaXRjb3VudHxwMWI6IE5vY3Rvd2x8MQoTfGZhaW50fHAxYjogTm9jdG93bAorfG1vdmV8cDJhOiBLb2ZmaW5nfENsZWFyIFNtb2d8cDFjOiBTbnViYnVsbAoefC1zdXBlcmVmZmVjdGl2ZXxwMWM6IFNudWJidWxsCgZ8c3BsaXQKHHwtZGFtYWdlfHAxYzogU251YmJ1bGx8MCBmbnQKHHwtZGFtYWdlfHAxYzogU251YmJ1bGx8MCBmbnQKHHwtZGFtYWdlfHAxYzogU251YmJ1bGx8MCBmbnQKHHwtZGFtYWdlfHAxYzogU251YmJ1bGx8MCBmbnQKGnwtY2xlYXJib29zdHxwMWM6IFNudWJidWxsChR8ZmFpbnR8cDFjOiBTbnViYnVsbAoBfAoHfHVwa2VlcBKxDAoCcDEQCRqoDAgJEAEoACgBKAE6mwwKAnAxEgJwMRqEAgoNcDE6IEJsYXN0b2lzZRIRQmxhc3RvaXNlLCBMMjksIE0aCTI0Lzg4IHBhciABKgoIORBTGD8gRChGMgRiaXRlMgp3YXRlcnB1bHNlMgh3YXRlcmd1bjIIYXF1YXRhaWw6B3RvcnJlbnRCAEoIcG9rZWJhbGxSBmZpdmF0b1j1w92UBGIYCgRCaXRlEgRiaXRlGBYgGSoGbm9ybWFsYiAKC1dhdGVyIFB1bHNlEgp3YXRlcnB1bHNlIBQqA2FueWIhCglXYXRlciBHdW4SCHdhdGVyZ3VuGBkgGSoGbm9ybWFsYh8KCUFxdWEgVGFpbBIIYXF1YXRhaWwgCioGbm9ybWFsGooCCgtwMTogTm9jdG93bBIPTm9jdG93bCwgTDIyLCBNGgUwIGZudCABKgoIHxAbGDAgMCgiMgtwc3ljaG9zaGlmdDIFZ3Jvd2wyBnRhY2tsZTIIaHlwbm9zaXM6CGluc29tbmlhQgBKB3Bva2JhbGxSBmZpdmF0b1jJwNqiBGInCgxQc3ljaG8gU2hpZnQSC3BzeWNob3NoaWZ0GAogCioGbm9ybWFsYiMKBUdyb3dsEgVncm93bBgoICgqD2FsbEFkamFjZW50Rm9lc2IcCgZUYWNrbGUSBnRhY2tsZRgaICMqBm5vcm1hbGIgCghIeXBub3NpcxIIaHlwbm9zaXMYFCAUKgZub3JtYWwagwIKDHAxOiBTbnViYnVsbBIQU251YmJ1bGwsIEwxMywgRhoFMCBmbnQgASoKCB0QFBgPIBIoDzIIdGFpbHdoaXAyC3RodW5kZXJmYW5nMgRiaXRlMgRsaWNrOgdydW5hd2F5QgBKB3Bva2JhbGxSBmZpdmF0b1i5oJ/vAmIqCglUYWlsIFdoaXASCHRhaWx3aGlwGB4gHioPYWxsQWRqYWNlbnRGb2VzYicKDFRodW5kZXIgRmFuZxILdGh1bmRlcmZhbmcYDiAPKgZub3JtYWxiGAoEQml0ZRIEYml0ZRgZIBkqBm5vcm1hbGIYCgRMaWNrEgRsaWNrGB4gHioGbm9ybWFsGvkBCgtwMTogUmF0dGF0YRIPUmF0dGF0YSwgTDE3LCBGGgUwIGZudCoKCBwQERgPIA4oHjIEYml0ZTIDY3V0MgtxdWlja2F0dGFjazILZm9jdXNlbmVyZ3k6B3J1bmF3YXlCAEoHcG9rYmFsbFIGZml2YXRvWP2nimBiGAoEQml0ZRIEYml0ZRgZIBkqBm5vcm1hbGIWCgNDdXQSA2N1dBgdIB4qBm5vcm1hbGInCgxRdWljayBBdHRhY2sSC3F1aWNrYXR0YWNrGB4gHioGbm9ybWFsYiUKDEZvY3VzIEVuZXJneRILZm9jdXNlbmVyZ3kYHiAeKgRzZWxmGvYBCgpwMTogTWVvd3RoEg5NZW93dGgsIEwxNCwgTRoFMCBmbnQqCggVEBAYEiASKB4yB3NjcmF0Y2gyBGJpdGUyB2Zha2VvdXQyCmZ1cnlzd2lwZXM6BnBpY2t1cEIASgdwb2tiYWxsUgZmaXZhdG9Yl7+e9gZiHgoHU2NyYXRjaBIHc2NyYXRjaBgiICMqBm5vcm1hbGIYCgRCaXRlEgRiaXRlGBkgGSoGbm9ybWFsYh8KCEZha2UgT3V0EgdmYWtlb3V0GAogCioGbm9ybWFsYiUKC0Z1cnkgU3dpcGVzEgpmdXJ5c3dpcGVzGA8gDyoGbm9ybWFsGoECCg5wMTogQmVsbHNwcm91dBISQmVsbHNwcm91dCwgTDEzLCBGGgUzOS8zOSoKCBkQDxgZIBAoEDIIdmluZXdoaXAyBmdyb3d0aDIEd3JhcDILc2xlZXBwb3dkZXI6CGdsdXR0b255QgBKB3Bva2JhbGxSBmZpdmF0b1jNiLaWAmIhCglWaW5lIFdoaXASCHZpbmV3aGlwGBkgGSoGbm9ybWFsYhoKBkdyb3d0aBIGZ3Jvd3RoGBQgFCoEc2VsZmIYCgRXcmFwEgR3cmFwGBQgFCoGbm9ybWFsYicKDFNsZWVwIFBvd2RlchILc2xlZXBwb3dkZXIYDyAPKgZub3JtYWwarAwKAnAyEAkaowwgATqeDAoCcDISAnAyGpICCgtwMjogS29mZmluZxIPS29mZmluZywgTDI1LCBNGgU0NC81NSABKgoIIRA0GCMgHSgWMglhc3N1cmFuY2UyCWNsZWFyc21vZzIGc2x1ZGdlMgxzZWxmZGVzdHJ1Y3Q6CGxldml0YXRlQgBKCHBva2ViYWxsWOCEnK4BYiIKCUFzc3VyYW5jZRIJYXNzdXJhbmNlGAkgCioGbm9ybWFsYiMKCkNsZWFyIFNtb2cSCWNsZWFyc21vZxgMIA8qBm5vcm1hbGIcCgZTbHVkZ2USBnNsdWRnZRgTIBQqBm5vcm1hbGIuCg1TZWxmLURlc3RydWN0EgxzZWxmZGVzdHJ1Y3QYBSAFKgthbGxBZGphY2VudBqOAgoIcDI6IE9uaXgSDE9uaXgsIEwyNSwgTRoFNDkvNTIgASoKCB0QVRgUIBgoKDIKcm9ja3BvbGlzaDIIZ3lyb2JhbGwyCXNtYWNrZG93bjIMZHJhZ29uYnJlYXRoOgZzdHVyZHlCAEoIcG9rZWJhbGxY8PfuqQViIwoLUm9jayBQb2xpc2gSCnJvY2twb2xpc2gYEyAUKgRzZWxmYiEKCUd5cm8gQmFsbBIIZ3lyb2JhbGwYBCAFKgZub3JtYWxiIwoKU21hY2sgRG93bhIJc21hY2tkb3duGA8gDyoGbm9ybWFsYikKDURyYWdvbiBCcmVhdGgSDGRyYWdvbmJyZWF0aBgRIBQqBm5vcm1hbBr2AQoOcDI6IEthbmdhc2toYW4SEkthbmdhc2toYW4sIEwyOSwgRhoFNzMvOTkgASoKCDwQOBgcIDMoMzIEYml0ZTIJZG91YmxlaGl0MgRyYWdlMgltZWdhcHVuY2g6CWVhcmx5YmlyZEIASghwb2tlYmFsbFjQ2qmwBWIYCgRCaXRlEgRiaXRlGBkgGSoGbm9ybWFsYiMKCkRvdWJsZSBIaXQSCWRvdWJsZWhpdBgJIAoqBm5vcm1hbGIYCgRSYWdlEgRyYWdlGBQgFCoGbm9ybWFsYiMKCk1lZ2EgUHVuY2gSCW1lZ2FwdW5jaBgUIBQqBm5vcm1hbBr+AQoKcDI6IE1lb3d0aBIOTWVvd3RoLCBMMjUsIE0aBTAgZm50KgoIGBAWGBkgGSg3MgpmdXJ5c3dpcGVzMgdzY3JlZWNoMgtmZWludGF0dGFjazIFdGF1bnQ6BnBpY2t1cEIASghwb2tlYmFsbFjJreblBmIlCgtGdXJ5IFN3aXBlcxIKZnVyeXN3aXBlcxgPIA8qBm5vcm1hbGIeCgdTY3JlZWNoEgdzY3JlZWNoGCggKCoGbm9ybWFsYicKDEZlaW50IEF0dGFjaxILZmVpbnRhdHRhY2sYEyAUKgZub3JtYWxiGgoFVGF1bnQSBXRhdW50GBQgFCoGbm9ybWFsGu8BCglwMjogRWthbnMSDUVrYW5zLCBMMjUsIE0aBTAgZm50KgoIIxAbGBkgHCgjMgRhY2lkMgZzcGl0dXAyCXN0b2NrcGlsZTIHc3dhbGxvdzoKaW50aW1pZGF0ZUIASghwb2tlYmFsbFjeibWZAWIhCgRBY2lkEgRhY2lkGB4gHioPYWxsQWRqYWNlbnRGb2VzYh0KB1NwaXQgVXASBnNwaXR1cBgKIAoqBm5vcm1hbGIgCglTdG9ja3BpbGUSCXN0b2NrcGlsZRgUIBQqBHNlbGZiHAoHU3dhbGxvdxIHc3dhbGxvdxgKIAoqBHNlbGYagQIKC3AyOiBSaHlob3JuEg9SaHlob3JuLCBMMjQsIE0aBTAgZm50KgoIKBA3GBMgEygRMglzY2FyeWZhY2UyCXNtYWNrZG93bjIFc3RvbXAyCGJ1bGxkb3plOghyb2NraGVhZEIASghwb2tlYmFsbFjxlL2hBWIjCgpTY2FyeSBGYWNlEglzY2FyeWZhY2UYCiAKKgZub3JtYWxiIwoKU21hY2sgRG93bhIJc21hY2tkb3duGA8gDyoGbm9ybWFsYhoKBVN0b21wEgVzdG9tcBgUIBQqBm5vcm1hbGIlCghCdWxsZG96ZRIIYnVsbGRvemUYFCAUKgthbGxBZGphY2VudCoGZml2YXRvKgZmaXZhdG8qBmZpdmF0byoGZml2YXRvKgZmaXZhdG8qBmZpdmF0b0ADWAFgbw==";

            var data = packet.Split(" ".ToCharArray());

            byte[] array = Convert.FromBase64String(data[1]);
            var type = Type.GetType($"PSXAPI.Request.{data[0]}, PSXAPI");
            //goto RESP;
            if (type != null)
            {
                var proto = typeof(PSXAPI.Proto).GetMethod("Deserialize").MakeGenericMethod(new Type[]
                {
                    type
                }).Invoke(null, new object[]
                {
                    array
                }) as PSXAPI.IProto;

                //var s = proto as PSXAPI.Request.Ack;

                //string decodedString = Encoding.UTF8.GetString(s.Data);
                //Console.WriteLine(decodedString);
                if (proto is null)
                    goto RESP;

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

            if (_npcBattler != null && Game != null && Game.IsMapLoaded && Game.IsInactive && !Game.IsInBattle)
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
                _npcBattler = null;
                Game.TalkToNpc(target);
                return true;
            }
            var result = MoveToCell(target.PositionX, target.PositionY, 1);

            if (!result)
            {
                _npcBattler = null;
            }

            return result;
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
                _npcBattler = null;
                message += " [OK]";
            }
            else if (Game.MapName != map)
            {
                _npcBattler = null;
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
