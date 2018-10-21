using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public class PlayerInfos
    {
        public DateTime Expiration { get; set; }
        public DateTime Added { get; private set; }
        public DateTime Updated { get; set; }

        public int PosX { get; private set; }
        public int PosY { get; private set; }
        public Direction Direction { get; private set; }
        public bool IsAfk { get; private set; }
        public bool IsInBattle { get; private set; }
        public int PokemonPetId { get; private set; }
        public bool IsPokemonPetShiny { get; private set; }
        public bool IsOnground { get; private set; }
        public string GuildName { get; private set; }
        public string Name { get; private set; }
        public int Level { get; private set; }
        public bool IsMember { get; private set; }
        public bool IsStaff { get; private set; }
        public List<PSXAPI.Response.Payload.MapUserActionData> Actions { get; private set; }

        public PlayerInfos(PSXAPI.Response.Payload.MapUser data, DateTime expiration)
        {
            Name = data.Username;
            if (data.Data != null)
            {
                IsInBattle = data.Data.Battle;
                IsAfk = data.Data.Away;
                Direction = DirectionExtensions.FromPlayerDirectionResponse(data.Data.Direction);
                GuildName = data.Data.GuildName;
                Level = (int)data.Data.Level;
                IsMember = data.Data.MemberRank == PSXAPI.Response.MemberRank.Member;
                IsStaff = data.Data.StaffRank != PSXAPI.Response.StaffRank.None;
                PokemonPetId = data.Data.Follow;
                IsPokemonPetShiny = data.Data.FollowShiny;
            }
            Actions = data.Actions.ToList();
            Expiration = expiration;
            var action = Actions.LastOrDefault(ac => ac.Position != null);
            PosX = action.Position.X;
            PosY = action.Position.Y;
            if (Name.Contains("MyManIam"))
                Console.WriteLine("");
            
        }

        public void Update(PSXAPI.Response.Payload.MapUser data, DateTime expiration)
        {
            Name = data.Username;
            if (data.Data != null)
            {
                IsInBattle = data.Data.Battle;
                IsAfk = data.Data.Away;
                Direction = DirectionExtensions.FromPlayerDirectionResponse(data.Data.Direction);
                GuildName = data.Data.GuildName;
                Level = (int)data.Data.Level;
                IsMember = data.Data.MemberRank == PSXAPI.Response.MemberRank.Member;
                IsStaff = data.Data.StaffRank != PSXAPI.Response.StaffRank.None;
                PokemonPetId = data.Data.Follow;
                IsPokemonPetShiny = data.Data.FollowShiny;
            }
            Actions = data.Actions.ToList();
            Expiration = expiration;
            var action = Actions.LastOrDefault(ac => ac.Position != null);
            PosX = action.Position.X;
            PosY = action.Position.Y;
        }

        public bool IsExpired()
        {
            return DateTime.UtcNow > Expiration || Actions.Any(x => x.Action == PSXAPI.Response.Payload.MapUserAction.Leave);
        }
    }
}
