using Poke1Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Bot.Modules
{
    public class QuestManager
    {

        private BotClient _bot;

        private ProtocolTimeout _questTimeout = new ProtocolTimeout();

        private List<PlayerQuest> Quests = new List<PlayerQuest>();

        public bool IsWorking => _questTimeout.IsActive;

        public PlayerQuest SelectedQuest { get; set; }

        private MAPAPI.Response.LINKData _haveToMoveLink;

        public QuestManager(BotClient bot)
        {
            _bot = bot;
            _bot.ClientChanged += Bot_ClientChanged;
        }

        private void Bot_ClientChanged()
        {
            if (_bot.Game != null)
            {
                _bot.Game.QuestsUpdated += Game_QuestsUpdated;
                _bot.Game.ReceivedPath += Game_ReceivedPath;
            }
        }

        private void Game_ReceivedPath(PSXAPI.Response.Path path)
        {
            _haveToMoveLink = null;
            foreach (var link in path.Links)
            {
                var getLink = _bot.Game.Map.Links.Find(l => l.ID == link);
                if (getLink != null)
                {
                    if (SelectedQuest is null is false)
                        SelectedQuest.UpdateRequests(false);
                    _haveToMoveLink = getLink;
                    break;
                }
            }
        }

        private void Game_QuestsUpdated(List<PlayerQuest> quests)
        {
            foreach(var quest in quests)
            {
                Quests.Add(quest);
            }
            SelectedQuest = null;
            _questTimeout.Cancel();
        }

        public void Update()
        {
            _questTimeout.Update();

            if (_bot.Game != null && _bot.Game.IsInBattle)
                _haveToMoveLink = null;

            if (_haveToMoveLink != null && !_bot.Game.IsInBattle && _bot.Game != null)
            {
                if (_bot.Game.PlayerX != _haveToMoveLink.x || _bot.Game.PlayerY != -_haveToMoveLink.z)
                {
                    _questTimeout.Set();
                    _bot.Game?.ClearPath();
                    _bot.MoveToCell(_haveToMoveLink.x, -_haveToMoveLink.z);
                    _haveToMoveLink = null;
                    return;
                }
                else
                    _haveToMoveLink = null;
            }

            if (Quests.Count > 0)
            {               

                if (_bot.Script?.IsLoaded == true)
                {
                    var quest = Quests[0];
                    Quests.RemoveAt(0);

                    _bot.Script?.OnQuestUpdated(quest.Name, quest.Type.ToString(), quest.Description);

                    if (_bot.Game.AutoCompleteQuest(quest))
                        _questTimeout.Set();

                    _questTimeout.Set();
                }
            }
        }
    }
}
