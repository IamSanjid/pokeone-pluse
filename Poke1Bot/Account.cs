using Newtonsoft.Json;

namespace Poke1Bot
{
    public class Account
    {
        public string Name { get; set; }
        public string Password { get; set; }
        public Socks Socks { get; set; }

        [JsonIgnore]
        public string FileName { get; set; }

        public Account(string name)
        {
            Name = name;
            Socks = new Socks();
        }
    }
}