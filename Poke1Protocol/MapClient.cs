using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class MapClient
    {
        private const string MapExtension = ".psx";

        public Dictionary<string, Map> _cache = new Dictionary<string, Map>();

        private readonly Timer timer;

        public bool IsConnected { get; private set; }
        public bool IsLoggedIn { get; private set; }

        public event Action ConnectionOpened;
        public event Action<Exception> ConnectionFailed;
        public event Action<Exception> ConnectionClosed;
        public event Action<string, Map> MapLoaded;
        public event Action<List<Npc>> MapNpcReceived;

        private MapConnection _connection;
        private readonly GameClient _client;
        public MapClient(MapConnection connection, GameClient client)
        {
            _client = client;
            _connection = connection;
            _connection.PacketReceived += OnPacketReceived;
            _connection.Connected += OnConnected;
            _connection.Disconnected += OnDisconnected;

            timer = new Timer(new TimerCallback(Timer), null, TimeSpan.FromSeconds(30.0), TimeSpan.FromSeconds(30.0));
        }

        public void DownloadMap(string mapName)
        {
            if (_cache.ContainsKey(mapName.ToLowerInvariant()))
            {
#if DEBUG
                Console.WriteLine("[Map] Loaded from cache: " + mapName);
#endif
                var cache = _cache[mapName.ToLowerInvariant()];
                MapLoaded?.Invoke(RemoveExtension(mapName), new Map(MAPAPI.Response.MapDump.Serialize(cache.MapDump), cache.IsSessioned, _client));
                return;
            }
#if DEBUG
            Console.WriteLine("[Map] Requested: " + mapName);
#endif

            SendProto(new MAPAPI.Request.RequestMap
            {
                MapName = mapName
            });
        }

        public void Open()
        {
#if DEBUG
            Console.WriteLine("[+++] Connecting to the map server");
#endif
            _connection.Connect();
        }

        public void Update()
        {
            _connection.Update();
        }

        public void Close()
        {
            _connection.Close();
        }

        private void Timer(object obj)
        {
            if (IsConnected)
            {
                SendProto(new MAPAPI.Request.Ping());
            }
        }
        public void SendProto(MAPAPI.IProto proto)
        {
            string text = Convert.ToBase64String(MAPAPI.Proto.Serialize(proto));
            text = proto._Name + " " + text;
            SendPacket(text);
        }
        public void SendPacket(string packet)
        {
            _connection.Send(packet);
        }

        private void OnPacketReceived(string packet)
        {
            var data = packet.Split(" ".ToCharArray());

            byte[] array = Convert.FromBase64String(data[1]);
            var type = Type.GetType($"MAPAPI.Response.{data[0]}, MAPAPI");

            if (type is null)
            {
                Console.WriteLine("Received Unknown Response: " + data[0]);
            }
            else
            {
                MAPAPI.IProto item = typeof(MAPAPI.Proto).GetMethod("Deserialize").MakeGenericMethod(new Type[]
                {
                    type
                }).Invoke(null, new object[]
                {
                    array
                }) as MAPAPI.IProto;

                switch (item)
                {
                    case MAPAPI.Response.MapServerMap _:
                        FinishMapDownloading((MAPAPI.Response.MapServerMap)item);
                        break;
                    case MAPAPI.Response.DeleteMap DM:
                        if (_cache.ContainsKey(DM.MapName.ToLowerInvariant()))
                        {
                            _cache.Remove(DM.MapName.ToLowerInvariant());
                        }
                        break;
                    case MAPAPI.Response.DebugMessage msg:
#if DEBUG
                        Console.WriteLine(msg);
#endif
                        IsLoggedIn = true;
                        break;
                }
            }
        }

        private void FinishMapDownloading(MAPAPI.Response.MapServerMap mapProto)
        {
            string name = mapProto.MapName;

            byte[] data = MAPAPI.CompressionHelper.DecompressBytes(mapProto.MapData);
            Map map = new Map(data, mapProto.IsSession, _client);
            map.NpcReceieved += Map_NpcReceieved;

            if (!_cache.ContainsKey(name.ToLowerInvariant()))
            {
                _cache.Add(name.ToLowerInvariant(), map);
            }

#if DEBUG
            Console.WriteLine("[Map] Received: " + RemoveExtension(name));
#endif
            MapLoaded?.Invoke(RemoveExtension(name), map);
        }

        private void Map_NpcReceieved(List<Npc> obj)
        {
            MapNpcReceived?.Invoke(obj);
        }

        private void OnConnected()
        {
            IsConnected = true;
            ConnectionOpened?.Invoke();
            Login();
        }

        private void Login()
        {
            SendProto(new MAPAPI.Request.Login
            {
                Username = "Client",
                Password = "#PGSEONEp0k326783&^@#dgg4G@$W",
            });
        }

        private void OnDisconnected(Exception ex)
        {
            if (!IsConnected)
            {
#if DEBUG
                Console.WriteLine("[---] Map connection failed");
#endif
                ConnectionFailed?.Invoke(ex);
            }
            else
            {
                IsConnected = false;
#if DEBUG
                Console.WriteLine("[---] Map connection closed");
#endif
                ConnectionClosed?.Invoke(ex);
            }
        }

        public static string RemoveExtension(string name)
        {
            return name.Replace(MapExtension, "");
        }
    }
}
