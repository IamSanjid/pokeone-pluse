using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public enum Regions
    {
        Kanto = 1,
        Johto = 2
    }
    public class PlayerStats
    {
        public string PlayerName { get; }
        public int[] Badges { get; }
        public PSXAPI.Response.Payload.Stats Data { get; }
        public PSXAPI.Response.Equip Equip { get; }
        public PSXAPI.Response.Style Style { get; }
        public int PetId { get; }
        public bool IsPetShiny { get; }
        public int KantoLevel { get; }
        public int JohtoLevel { get; }
        public Regions CurrentRegion { get; }
        internal PlayerStats(PSXAPI.Response.Stats stat)
        {
            Badges = stat.Badges;
            Data = stat.Data;
            Equip = stat.Equip;
            Style = stat.Style;
            PetId = stat.Follow;
            IsPetShiny = stat.FollowShiny;
            PlayerName = stat.Username;
            if (stat.LevelRegions.Length > 1)
            {
                KantoLevel = (int)stat.Levels[0];
                JohtoLevel = (int)stat.Levels[1];
            }
            else if (stat.LevelRegions.Length == 1)
            {
                KantoLevel = (int)stat.Levels[0];
                JohtoLevel = 0;
            }
            CurrentRegion = (Regions)stat.Region;
        }
    }
}
