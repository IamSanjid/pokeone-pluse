using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class PlayerEffect
    {
        public Guid UID { get; }
        public string Name { get; }
        public TimeSpan TimeRemaining { get; }
        public int EndSteps { get; }
        public PSXAPI.Response.Payload.Effect EffectData { get; }
        public PSXAPI.Response.Payload.EffectUseType Type { get; }
        public DateTime StartedTime { get; }
        public TimeSpan TotalTime { get; }

        internal PlayerEffect(PSXAPI.Response.Payload.Effect data, DateTime startedTime)
        {
            EffectData = data;
            EndSteps = (int)data.EndSteps;
            Name = data.Name;
            TimeRemaining = data.TimeRemaining;
            TotalTime = data.TimeTotal;
            UID = data.UID;
            Type = data.Type;
            StartedTime = startedTime;
        }
    }
}
