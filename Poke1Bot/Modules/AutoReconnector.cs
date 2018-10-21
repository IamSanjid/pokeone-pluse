using Poke1Bot;
using System;

namespace Poke1Bot.Modules
{
    public class AutoReconnector
    {
        public const int MinDelay = 30;
        public const int MaxDelay = 50;

        public event Action<bool> StateChanged;

        private bool _isEnabled = false;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    StateChanged?.Invoke(value);
                }
            }
        }

        private BotClient _bot;
        public bool _reconnecting;
        private DateTime _autoReconnectTimeout;

        public AutoReconnector(BotClient bot)
        {
            _bot = bot;
            _bot.ClientChanged += Bot_ClientChanged;
        }

        private void Bot_ClientChanged()
        {
            if (_bot.Game != null)
            {
                _bot.Game.ConnectionClosed += Client_ConnectionClosed;
                _bot.Game.ConnectionFailed += Client_ConnectionClosed;
                _bot.Game.LoggedIn += Client_LoggedIn;
                _bot.Game.AuthenticationFailed += Client_AuthenticationFailed;
            }
        }

        public void Update()
        {
            if (IsEnabled == true && _reconnecting && (_bot.Game == null || !_bot.Game.IsConnected))
            {
                if (_autoReconnectTimeout < DateTime.UtcNow)
                {
                    _bot.LogMessage("Reconnecting...");
                    _bot.Login(_bot.Account);
                    _autoReconnectTimeout = DateTime.UtcNow.AddSeconds(_bot.Rand.Next(MinDelay, MaxDelay + 1));
                }
            }
        }

        private void Client_ConnectionClosed(Exception ex)
        {
            if (IsEnabled)
            {
                _reconnecting = true;
                int seconds = _bot.Rand.Next(MinDelay, MaxDelay + 1);
                _autoReconnectTimeout = DateTime.UtcNow.AddSeconds(seconds);
                _bot.LogMessage("Reconnecting in " + seconds + " seconds.");
            }
        }

        private void Client_LoggedIn()
        {
            if (_reconnecting)
            {
                _bot.Start();
                _reconnecting = false;
            }
        }

        private void Client_AuthenticationFailed(PSXAPI.Response.LoginError result)
        {
            if (result == PSXAPI.Response.LoginError.AccountNotFound || result == PSXAPI.Response.LoginError.WrongPassword
                || result == PSXAPI.Response.LoginError.Unsupported || result == PSXAPI.Response.LoginError.Banned
                || result == PSXAPI.Response.LoginError.NotVerified)
            {
                IsEnabled = false;
                _reconnecting = false;
            }
        }
    }
}
