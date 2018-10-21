using OneNetwork;
using System;
using System.Net;
using System.Net.Sockets;

namespace Poke1Protocol
{
    public class GameConnection : OneConnection
    {
        // game server port = 2012, map server port = 2015, map host = maps.poke.one, game host = game.poke.one
        private const int _serverPort = 2012;

        private bool _useSocks;
        private int _socksVersion;
        private string _socksHost;
        private int _socksPort;
        private string _socksUser;
        private string _socksPass;

        private const string _host = "95.183.48.120";
        public GameConnection()
            : base(new ANetwork())
        {
        }

        public GameConnection(int socksVersion, string socksHost, int socksPort, string socksUser, string socksPass)
            : this()
        {
            _useSocks = true;
            _socksVersion = socksVersion;
            _socksHost = socksHost;
            _socksPort = socksPort;
            _socksUser = socksUser;
            _socksPass = socksPass;
        }

        public async void Connect()
        {
            if (!_useSocks)
            {
                Connect(IPAddress.Parse(_host), _serverPort);
            }
            else
            {
                try
                {
                    Socket socket = await SocksConnection.OpenConnection(_socksVersion, _host, _serverPort, _socksHost, _socksPort, _socksUser, _socksPass);
                    Initialize(socket);
                }
                catch (Exception ex)
                {
                    Close(ex);
                }
            }
        }
    }
}
