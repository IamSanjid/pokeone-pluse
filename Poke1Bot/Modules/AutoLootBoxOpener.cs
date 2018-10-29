using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Bot.Modules
{
    public class AutoLootBoxOpener
    {

        Poke1Protocol.LootboxHandler _lootBoxHandler;

        private List<PSXAPI.Response.Lootbox> lootboxes = new List<PSXAPI.Response.Lootbox>();

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
                _lootBoxHandler = _bot.Game.RecievedLootBoxes;
                _bot.Game.ConnectionClosed += Client_ConnectionClosed;
                _bot.Game.ConnectionFailed += Client_ConnectionClosed;
                _bot.Game.LootBoxOpened += Game_LootBoxOpened;
                _bot.Game.RecievedLootBox += Game_RecievedLootBox;
            }
        }

        private void Game_RecievedLootBox(PSXAPI.Response.Lootbox obj)
        {
            lootboxes.Add(obj);
        }

        private void Game_LootBoxOpened(PSXAPI.Response.Payload.LootboxRoll[] arg1, PSXAPI.Response.LootboxType arg2)
        {
            _lootBoxHandler = null;
            _lootBoxTimeOut.Set();
        }

        public void Update()
        {
            _lootBoxTimeOut.Update();
            if (_bot.Game is null) return;

            if (IsEnabled && _bot.Game.IsLoggedIn && _bot.Game.IsMapLoaded)
            {
                if (_lootBoxHandler is null)
                    _lootBoxHandler = _bot.Game.RecievedLootBoxes;

                if (!_lootBoxTimeOut.IsActive && lootboxes.Count > 0)
                {
                    var loot = lootboxes[0];
                    lootboxes.RemoveAt(0);
                    if (loot.Remaining > 0 && !_bot.Game.IsInBattle)
                    {
                        _bot.Game.OpenLootBox(Poke1Protocol.ConvertLootBoxType.FromResponseType(loot.Type));
                        _lootBoxTimeOut.Set(lootboxes.Count > 0 ? _bot.Rand.Next(2500, 3000) :  _bot.Rand.Next(2000, 2500));
                    }
                }
                if (lootboxes.Count == 0)
                    _lootBoxTimeOut.Cancel();
            }
        }

        private void Client_ConnectionClosed(Exception ex)
        {
            _lootBoxHandler = null;
            lootboxes.Clear();
        }
    }
}
