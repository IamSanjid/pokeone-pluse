using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poke1Protocol
{
    public enum ExpGroup
    {
        Unset,
        Erratic,
        Fast,
        MediumFast,
        MediumSlow,
        Slow,
        Fluctuating
    }
    public static class ExpGroupExtention
    {
        public static ExpGroup FromName(string Name)
        {
            Name = Name.ToLowerInvariant().Replace("-", "");
            switch (Name)
            {
                case "unset":
                    return ExpGroup.Unset;
                case "erratic":
                    return ExpGroup.Erratic;
                case "fast":
                    return ExpGroup.Fast;
                case "mediumfast":
                    return ExpGroup.MediumFast;
                case "mediumslow":
                    return ExpGroup.MediumSlow;
                case "slow":
                    return ExpGroup.Slow;
                case "fluctuating":
                    return ExpGroup.Fluctuating;
                default:
                    return ExpGroup.Unset;
            }
        }
    }
    public class PokemonExperience
    {
        public int CurrentLevel { get; private set; }
        public int LastExperience { get; private set; }
        public int BaseLevelExperience { get; private set; }
        public int NextExperience { get; private set; }
        public ExpGroup ExpGroup { get; private set; }
        public int RemainingExperience => (NextExperience - LastExperience) - (BaseLevelExperience - LastExperience);
        public PokemonExperience(int currentLevel, int baseLevelExp, int nextExp, int lastExp, ExpGroup expGroup)
        {
            CurrentLevel = currentLevel;
            BaseLevelExperience = baseLevelExp;
            NextExperience = nextExp;
            LastExperience = lastExp;
            ExpGroup = expGroup;
        }
        public int CalculateLevelExp()
        {
            //Ahh Followed PokeOne's way.
            double num = (double)(CurrentLevel * CurrentLevel * CurrentLevel);
            int result;
            switch (ExpGroup)
            {
                case ExpGroup.Erratic:
                    if (CurrentLevel < 50)
                    {
                        result = (int)(num * (double)(100 - CurrentLevel) / 50.0);
                    }
                    else if (CurrentLevel < 68)
                    {
                        result = (int)(num * (double)(150 - CurrentLevel) / 100.0);
                    }
                    else if (CurrentLevel < 98)
                    {
                        result = (int)(num * (double)((1911 - 10 * CurrentLevel) / 3) / 500.0);
                    }
                    else
                    {
                        result = (int)(num * (double)(160 - CurrentLevel) / 100.0);
                    }
                    break;
                case ExpGroup.Fast:
                    result = (int)(0.8 * num);
                    break;
                case ExpGroup.MediumFast:
                    result = (int)num;
                    break;
                case ExpGroup.MediumSlow:
                    result = (int)(1.2 * num) - 15 * CurrentLevel * CurrentLevel + 100 * CurrentLevel - 140;
                    break;
                case ExpGroup.Slow:
                    result = (int)(1.25 * num);
                    break;
                case ExpGroup.Fluctuating:
                    if (CurrentLevel < 15)
                    {
                        result = (int)(num * (double)(((CurrentLevel + 1) / 3 + 24) / 50));
                    }
                    else if (CurrentLevel < 36)
                    {
                        result = (int)(num * (double)((CurrentLevel + 14) / 50));
                    }
                    else
                    {
                        result = (int)(num * (double)((CurrentLevel / 2 + 32) / 50));
                    }
                    break;
                default:
                    result = 0;
                    break;
            }
            return result;
        }
    }
}
