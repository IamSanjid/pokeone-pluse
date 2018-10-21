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

            if (Quests.Count > 0)
            {               

                if (_bot.Script?.IsLoaded == true)
                {
                    _bot.Script?.OnQuestUpdated(Quests[0].Name, Quests[0].Type.ToString(), Quests[0].Description);

                    if (_bot.Game.AutoCompleteQuest(Quests[0]))
                        _questTimeout.Set();

                    Quests.RemoveAt(0);
                }
            }

            if (_haveToMoveLink != null && !_bot.Game.IsInBattle)
            {
                _questTimeout.Set();
                _bot.Game?.ClearPath();
                _bot.MoveToCell(_haveToMoveLink.x, -_haveToMoveLink.z);
            }
        }
    }
}
