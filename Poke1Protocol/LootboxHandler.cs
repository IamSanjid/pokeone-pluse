using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PSXAPI;

namespace Poke1Protocol
{
    public class LootboxHandler
    {
        public event Action<PSXAPI.Response.Payload.LootboxRoll[], PSXAPI.Response.LootboxType> BoxOpened;
        public event Action<string> LootBoxMessage;
        public event Action<LootboxHandler> RecievedBox;

        public List<PSXAPI.Response.Lootbox> Lootboxes { get; private set; } = new List<PSXAPI.Response.Lootbox>();

        public PSXAPI.Response.LootboxType LootboxType { get; private set; }
        public TimeSpan DailyTime { get; private set; }
        public DateTime LastUpdated { get; private set; }
        public DateTime BoxLastUpdated { get; private set; }
        public PSXAPI.Response.DailyLootbox DailyBox { get; private set; }

        private uint NormalBoxes = 0u;

        private uint SmallBoxes = 0u;

        private uint oldAmount = 0u;

        public PSXAPI.Response.Payload.LootboxRoll[] Rewards;

        private DateTime lastMsg;
        public int TotalLootBoxes { get; private set; } = 0;

        public void HandleLootBoxes(PSXAPI.Response.Lootbox[] boxes)
        {
            if (boxes != null)
            {
                for (int i = 0; i < boxes.Length; i++)
                {

                    if (boxes[i].Remaining > 0 && boxes[i].Action != PSXAPI.Response.LootboxAction.Opened)
                        Lootboxes.Add(boxes[i]);

                    if (boxes[i].Type == PSXAPI.Response.LootboxType.Normal)
                    {
                        NormalBoxes = boxes[i].Remaining;
                    }
                    else if (boxes[i].Type == PSXAPI.Response.LootboxType.Small)
                    {
                        SmallBoxes = boxes[i].Remaining;
                    }
                    if (boxes[i].Action == PSXAPI.Response.LootboxAction.Opened)
                    {
                        if (boxes[i].Rolls != null)
                        {
                            Rewards = boxes[i].Rolls;
                            BoxOpened?.Invoke(Rewards, boxes[i].Type);
                        }                      
                    }
                }
            }
            // PokeOne's way....
            int count = (int)(NormalBoxes + SmallBoxes - oldAmount);
            if (count > (long)((ulong)oldAmount))
            {
                LootBoxMessage?.Invoke("You gained Loot Box x" + count + ".");
                RecievedBox?.Invoke(this);
            }
            TotalLootBoxes = (int)(NormalBoxes + SmallBoxes);

            oldAmount = NormalBoxes + SmallBoxes;
            UpdateFreeLootBox();
        }
        public void HandleLootbox(PSXAPI.Response.Lootbox boxes)
        {
            HandleLootBoxes(new PSXAPI.Response.Lootbox[]
            {
                boxes
            });
        }

        public void HandleDaily(PSXAPI.Response.DailyLootbox lootPacket, TimeSpan? time = null)
        {
            
            if (lootPacket != null)
            {
                DailyBox = lootPacket;
                BoxLastUpdated = DateTime.UtcNow;
                LootboxType = lootPacket.Type;
            }
            if (time.GetValueOrDefault() != null)
            {
                if (time.GetValueOrDefault().TotalSeconds > 0.0)
                {
                    DailyTime = time.GetValueOrDefault();
                    LastUpdated = DateTime.UtcNow;
                    lastMsg = DateTime.UtcNow;
                }
                else
                    DailyTime = new TimeSpan(0L);
            }
            else
                DailyTime = new TimeSpan(0L);
            if (DailyTime.TotalSeconds <= 0.0)
                DailyTime = DailyBox.Timer;
            UpdateFreeLootBox();
        }
        //PokeOne's way I don't care.
        public void UpdateFreeLootBox()
        {
            string text = "";
            TimeSpan t = DateTime.UtcNow - LastUpdated;
            t = DailyTime - t;
            if ((DailyBox == null && t.TotalSeconds <= 0.0) || (DailyBox != null && DailyBox.Type == PSXAPI.Response.LootboxType.None))
            {
                if (DailyTime.TotalSeconds > 0.0)
                {
                    text += t.FormatTimeString();
                    //if (t.Days > 0)
                    //{
                    //    if (t.Days > 1)
                    //    {
                    //        text = text + t.Days.ToString() + " days";
                    //    }
                    //    else
                    //    {
                    //        text = text + t.Days.ToString() + " day";
                    //    }
                    //}
                    //else if (t.Hours > 0)
                    //{
                    //    if (t.Hours > 1)
                    //    {
                    //        text = text + t.Hours.ToString() + " hours";
                    //    }
                    //    else
                    //    {
                    //        text = text + t.Hours.ToString() + " hour";
                    //    }
                    //}
                    //else if (t.Minutes > 0)
                    //{
                    //    if (t.Minutes > 1)
                    //    {
                    //        text = text + t.Minutes.ToString() + " minutes";
                    //    }
                    //    else
                    //    {
                    //        text = text + t.Minutes.ToString() + " minute";
                    //    }
                    //}
                    //else
                    //{
                    //    text += "< 1 minute";
                    //}
                    if (t.TotalSeconds > 0.0)
                    {
                        LootBoxMessage?.Invoke("Daily Reset\n" + text);
                    }
                }
            }
            else if (DailyBox != null)
            {
                t = DateTime.UtcNow - BoxLastUpdated;
                t = DailyBox.Timer - t;
                if (DailyTime.TotalSeconds > 0.0)
                {
                    text += t.FormatTimeString();
                    //if (t.Days > 0)
                    //{
                    //    if (t.Days > 1)
                    //    {
                    //        text = text + t.Days.ToString() + " days";
                    //    }
                    //    else
                    //    {
                    //        text = text + t.Days.ToString() + " day";
                    //    }
                    //}
                    //else if (t.Hours > 0)
                    //{
                    //    if (t.Hours > 1)
                    //    {
                    //        text = text + t.Hours.ToString() + " hours";
                    //    }
                    //    else
                    //    {
                    //        text = text + t.Hours.ToString() + " hour";
                    //    }
                    //}
                    //else if (t.Minutes > 0)
                    //{
                    //    if (t.Minutes > 1)
                    //    {
                    //        text = text + t.Minutes.ToString() + " minutes";
                    //    }
                    //    else
                    //    {
                    //        text = text + t.Minutes.ToString() + " minute";
                    //    }
                    //}
                    //else
                    //{
                    //    text += "< 1 minute";
                    //}
                    if (t.TotalSeconds > 0.0)
                    {
                        //if (text.Contains("1 minute") && !text.Contains("minutes"))
                            //LootBoxMessage?.Invoke("Next Free Lootbox in " + text);
                    }
                }
            }
        }
    }
    public static class TimeSpanExtention
    {
        public static string FormatTimeString(this TimeSpan obj)
        {
            var sb = new StringBuilder();
            if (obj.Hours != 0)
            {
                sb.Append(obj.Hours);
                sb.Append(" ");
                if (obj.Hours > 1)
                    sb.Append("hours");
                else
                    sb.Append("hour");
                sb.Append(" ");
            }

            if (obj.Minutes != 0 || sb.Length != 0)
            {
                sb.Append(obj.Minutes);
                sb.Append(" ");
                if (obj.Minutes > 1)
                    sb.Append("minutes");
                else
                    sb.Append("minute");
                sb.Append(" ");
            }
            if (obj.Seconds != 0 || sb.Length != 0)
            {
                sb.Append(obj.Seconds);
                sb.Append(" ");
                if (obj.Minutes > 1)
                    sb.Append("seconds");
                else
                    sb.Append("second");
                sb.Append(" ");
            }
            if (sb.Length == 0)
            {
                sb.Append("< 1 minute");
            }
            // if (obj.Milliseconds != 0 || sb.Length != 0)
            //{
            //    sb.Append(obj.Milliseconds);
            //    sb.Append(" ");
            //    sb.Append("Milliseconds");
            //    sb.Append(" ");
            //}
            // if (sb.Length == 0)
            //{
            //    sb.Append(0);
            //    sb.Append(" ");
            //    sb.Append("Milliseconds");
            //}
            return sb.ToString();
        }
    }
    public static class ConvertLootBoxType
    {
        public static PSXAPI.Request.LootboxType FromResponseType(PSXAPI.Response.LootboxType lootboxType)
        {
            switch (lootboxType)
            {
                case PSXAPI.Response.LootboxType.Normal:
                    return PSXAPI.Request.LootboxType.Normal;
                case PSXAPI.Response.LootboxType.Small:
                    return PSXAPI.Request.LootboxType.Small;
                default:
                    return PSXAPI.Request.LootboxType.Small;
            }
        }
    }
}
