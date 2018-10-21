using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Poke1Bot.Scripting
{
    public abstract class BaseScript
    {
        public string Name { get; protected set; }
        public string Author { get; protected set; }
        public string Description { get; protected set; }
        public bool IsLoaded { get; protected set; }

        public event Action<string> ScriptMessage;

        public virtual void Initialize() { }
        public virtual void Start() { }
        public virtual void Stop() { }
        public virtual void Pause() { }
        public virtual void Resume() { }
        public virtual void Update() { }
        //public virtual void Relog(float seconds, string message, bool autoReconnect) { }

        public virtual void OnDialogMessage(string message) { }
        public virtual void OnBattleMessage(string message) { }
        public virtual void OnSystemMessage(string message) { }
        public virtual void OnLearningMove(string moveName, int pokemonIndex) { }
        public virtual void OnQuestUpdated(string name, string type, string questDescription) { }
        public virtual void OnWarningMessage(bool isDifferentMap, int distance = -1) { }
        public virtual void OnMovementLag(int distance) { }
        public abstract bool ExecuteNextAction();

        protected void LogMessage(string message)
        {
            ScriptMessage?.Invoke(message);
        }
    }
}
