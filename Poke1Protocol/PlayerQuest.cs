using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class PlayerQuest
    {
        public PSXAPI.Response.Quest QuestData { get; }
        public string Name { get; }
        public string Id { get; }
        public string SourceNPC { get; }
        public string SourceArea { get; }
        public string TargetArea { get; }
        public string TargetCompletedArea { get; }
        public Guid Target { get; }
        public bool AutoComplete { get; }
        public bool Completed { get; }
        public string Description { get; }
        public PSXAPI.Response.QuestType Type { get; }
        public PSXAPI.Response.Payload.QuestReward Reward { get; }
        public int Required { get; }
        public int Progress { get; }
        public int ProgressId { get; }
        public PSXAPI.Response.QuestProgressType ProgressType { get; }

        internal PlayerQuest(PSXAPI.Response.Quest data)
        {
            QuestData = data;
            Name = data.Name;
            Id = data.ID;
            SourceNPC = data.SourceNPC;
            SourceArea = data.SourceArea;
            TargetArea = data.TargetArea;
            TargetCompletedArea = data.TargetCompletedArea;
            Target = data.Target;
            AutoComplete = data.AutoComplete;
            Completed = data.Completed;
            Type = data.Type;
            Description = data.Description;
            Reward = data.Reward;
            Required = data.Required;
            Progress = data.Progress;
            ProgressId = data.ProgressID;
            ProgressType = data.ProgressType;
        }
    }
}
