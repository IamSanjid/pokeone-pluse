using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Bot.Modules
{
    public class AutoLootBoxOpener
    {
        private bool _sentOpenLootBoxReq = false;

        Poke1Protocol.LootboxHandler _lootBoxHandler;

        private Poke1Protocol.ProtocolTimeout _lootBoxTimeOut = new Poke1Protocol.ProtocolTimeout();

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

        public AutoLootBoxOpener(BotClient bot)
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
                _bot.Game.RecievedLootBox += Game_RecievedLootBox;
                _bot.Game.LootBoxOpened += Game_LootBoxOpened;
            }
        }

        private void Game_LootBoxOpened(PSXAPI.Response.Payload.LootboxRoll[] arg1, PSXAPI.Response.LootboxType arg2)
        {
            _lootBoxHandler = null;
            _sentOpenLootBoxReq = false;
        }

        private void Game_RecievedLootBox(Poke1Protocol.LootboxHandler handler)
        {
            if (_bot.Game != null && IsEnabled)
            {
                _lootBoxHandler = handler;         
            }
        }

        public void Update()
        {
            _lootBoxTimeOut.Update();
            if (_bot.Game is null || _lootBoxTimeOut.IsActive) return;
            if (IsEnabled && _bot.Game != null && _bot.Game.RecievedLootBoxes != null && _bot.Game.IsLoggedIn && _bot.Game.IsMapLoaded)
            {
                if (_lootBoxHandler is null)
                    _lootBoxHandler = _bot.Game.RecievedLootBoxes;

                if (!_sentOpenLootBoxReq && _lootBoxHandler.Lootboxes.Count > 0)
                {
                    var loot = _lootBoxHandler.Lootboxes[0];
                    _lootBoxHandler.Lootboxes.RemoveAt(0);
                    if (loot.Remaining > 0 && !_bot.Game.IsInBattle)
                    {
                        _bot.Game.OpenLootBox(Poke1Protocol.ConvertLootBoxType.FromResponseType(loot.Type));
                        _sentOpenLootBoxReq = true;
                        _lootBoxTimeOut.Set(_lootBoxHandler.Lootboxes.Count > 0 ? _bot.Rand.Next(3500, 5000) :  _bot.Rand.Next(1500, 2000));
                    }
                }
            }
        }

        private void Client_ConnectionClosed(Exception ex)
        {
            _lootBoxHandler = null;
        }
    }
}
