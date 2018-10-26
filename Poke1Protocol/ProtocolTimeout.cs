using System;

namespace Poke1Protocol
{
    public class ProtocolTimeout
    {
        public bool IsActive { get; private set; }
        private DateTime _expirationTime;
        public int Interval { get; private set; }

        public bool Update()
        {
            if (IsActive && DateTime.UtcNow >= _expirationTime)
            {
                IsActive = false;
            }
            return IsActive;
        }

        public void Set(int milliseconds = 10000)
        {
            IsActive = true;
            _expirationTime = DateTime.UtcNow.AddMilliseconds(milliseconds);
            Interval = milliseconds;
        }

        public void Cancel()
        {
            IsActive = false;
        }
    }
}
