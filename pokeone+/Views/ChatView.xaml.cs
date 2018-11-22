using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Poke1Bot;
using Poke1Protocol;
using pokeone_plus.Utils;

namespace pokeone_plus
{
    /// <summary>
    /// Interaction logic for ChatView.xaml
    /// </summary>
    public partial class ChatView : UserControl
    {
        private Dictionary<string, TabItem> _channelTabs;
        private Dictionary<string, TabItem> _pmTabs;
        private Dictionary<string, TabItem> _channelPmTabs;
        private TabItem _localChatTab;

        private const string Pokemon_Link_Regex_Pattern = @"\[([poke=\w\-]+)\]\[([\w\-\s\.]+)\]\[\/pok\]";

        private BotClient _bot;
        public ChatView(BotClient bot)
        {
            InitializeComponent();
            _bot = bot;
            _localChatTab = new TabItem();
            _localChatTab.Header = "General";
            _localChatTab.Foreground = (Brush)new BrushConverter().ConvertFromString("#F1F1F1");
            _localChatTab.Content = new ChatPanel();
            TabControl.Items.Add(_localChatTab);
            _channelTabs = new Dictionary<string, TabItem>();
            AddChannelTab("Map", false);
            AddChannelTab("Party", false);
            AddChannelTab("Battle", false);
            AddChannelTab("Guild", false);
            _pmTabs = new Dictionary<string, TabItem>();
            _channelPmTabs = new Dictionary<string, TabItem>();
        }

        private void AddChannelTab(string tabName, bool closeAble = true)
        {
            ButtonTabHeader tabHeader = new ButtonTabHeader();

            tabHeader.TabName.Content = '#' + tabName;
            if (closeAble)
                tabHeader.CloseButton += () => CloseChannelTab(tabName);
            else
                tabHeader.ButtonClose.Visibility = Visibility.Collapsed;
            tabHeader.Tag = tabName;

            var tab = new TabItem { Header = tabHeader, Content = new ChatPanel(), Tag = tabName };

            _channelTabs[tabName] = tab;
            TabControl.Items.Add(tab);
        }

        public void Client_RefreshChannelList()
        {
            Dispatcher.InvokeAsync(delegate
            {
                IList<ChatChannel> channelList;
                lock (_bot)
                {
                    if (_bot.Game is null || _bot.Game?.Channels is null) return;
                    channelList = _bot.Game.Channels.Values.ToArray();
                }
                if (channelList.Count > 0 || channelList != null)
                {
                    foreach (ChatChannel channel in channelList)
                    {
                        if (!_channelTabs.ContainsKey(channel.Name) && channel.Name != "General")
                        {
                            AddChannelTab(channel.Name);
                        }
                    }
                    foreach (string key in _channelTabs.Keys.ToArray())
                    {
                        if (!(channelList.Any(e => e.Name == key)))
                        {
                            RemoveChannelTab(key);
                        }
                    }
                }
            });
        }

        private void CloseChannelTab(string channelName)
        {
            if (!_channelTabs.ContainsKey(channelName))
            {
                return;
            }
            if (_bot.Game != null && _bot.Game != null && _bot.Game.IsMapLoaded && _bot.Game.Channels.Any(e => e.Key == channelName && e.Value.Id != "default"))
            {
                _bot.Game.CloseChannel(channelName);
            }
            else
            {
                RemoveChannelTab(channelName);
            }
        }

        private void RemoveChannelTab(string tabName)
        {
            if ((_channelTabs[tabName].Header as ButtonTabHeader).ButtonClose.IsVisible)
            {
                TabControl.Items.Remove(_channelTabs[tabName]);
                _channelTabs.Remove(tabName);
            }
        }

        private void AddPmTab(string tabName)
        {
            ButtonTabHeader tabHeader = new ButtonTabHeader();

            tabHeader.TabName.Content = tabName;
            tabHeader.CloseButton += () => ClosePmTab(tabName);
            tabHeader.Tag = tabName;

            var tab = new TabItem { Header = tabHeader, Content = new ChatPanel(), Tag = tabName };

            _pmTabs[tabName] = tab;
            TabControl.Items.Add(tab);
        }

        private void ClosePmTab(string pmName)
        {
            if (!_pmTabs.ContainsKey(pmName))
            {
                return;
            }
            if (_bot.Game != null && _bot.Game != null && _bot.Game.IsMapLoaded && _bot.Game.Conversations.Contains(pmName))
            {
                _bot.Game.CloseConversation(pmName);
            }
            RemovePmTab(pmName);
        }

        private void RemovePmTab(string tabName)
        {
            TabControl.Items.Remove(_pmTabs[tabName]);
            _pmTabs.Remove(tabName);
        }

        public void Client_ChannelMessage(string channelName, string author, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddChannelMessage(channelName, author, message);
            });
        }

        public void Client_PrivateMessage(string conversation, string author, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                PlayNotification();
                AddPrivateMessage(conversation, author, message);
            });
        }

        public void Client_LeavePrivateMessage(string conversation, string leaver, string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (leaver == _bot.Game.PlayerName)
                {
                    return;
                }
                AddPrivateMessage(conversation, leaver, message);
            });
        }

        private void AddPrivateMessage(string conversation, string author, string message)
        {
            message = Regex.Replace(message, Pokemon_Link_Regex_Pattern, "[$2]");
            if (!_pmTabs.ContainsKey(conversation))
            {
                AddPmTab(conversation);
            }
            MainWindow.AppendLineToRichTextBox((_pmTabs[conversation].Content as ChatPanel).ChatBox,
                        "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message);
        }

        private void AddChannelMessage(string channelName, string author, string message)
        {

            message = Regex.Replace(message, Pokemon_Link_Regex_Pattern, "[$2]");
            if (!_channelTabs.ContainsKey(channelName) && channelName != "General")
            {
                AddChannelTab(channelName);
            }

            if (message.Contains("^emote+"))
            {
                int.TryParse(message.Replace("^emote+", ""), out int emoteNo);
                message = author + $" showing {emoteNo} no emote.";
                MainWindow.AppendLineToRichTextBox((_localChatTab.Content as ChatPanel).ChatBox,
                        "[" + DateTime.Now.ToLongTimeString() + "] " + message);
                return;
            }

            if (channelName == "General")
            {
                MainWindow.AppendLineToRichTextBox((_localChatTab.Content as ChatPanel).ChatBox,
                        "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message);
            }
            MainWindow.AppendLineToRichTextBox((_channelTabs[channelName].Content as ChatPanel).ChatBox,
                        "[" + DateTime.Now.ToLongTimeString() + "] " + author + ": " + message);
        }

        private void InputChatBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && _bot.Game != null && _bot.Game.IsMapLoaded)
            {
                SendChatInput(InputChatBox.Text);
                InputChatBox.Clear();
            }
        }

        private void SendChatInput(string text)
        {
            if (text == "" || text.Replace(" ", "") == "")
            {
                return;
            }
            lock (_bot)
            {
                if (_bot.Game == null || !_bot.Game.IsLoggedIn)
                {
                    return;
                }

                var tab = TabControl.SelectedItem as TabItem;
                text = Regex.Replace(text, @"\[(-|.{6})\]", "");
                if (text.Length == 0) return;
                if (tab == _localChatTab)
                {
                    if (text.StartsWith("/pm"))
                    {
                        var d = text.Split(' ');
                        if (d.Length >= 2)
                        {
                            var userName = d[1];
                            var msg = d.Length == 3 ? d[2] : "";
                            if (d.Length > 3)
                            {
                                var loc3 = 3;
                                while (loc3 < d.Length)
                                {
                                    d[2] = d[2] + (" " + d[loc3]);
                                    loc3 = loc3 + 1;
                                }
                                msg = d[2];
                            }
                            _bot.Game.SendPrivateMessage(userName, msg);
                            AddPrivateMessage(userName, _bot.Game.PlayerName, msg);
                        }
                        return;
                    }
                    _bot.Game.SendMessage("Map", text);
                }
                else if (_channelTabs.ContainsValue(tab))
                {
                    if (text.StartsWith("/pm"))
                    {
                        var d = text.Split(' ');
                        if (d.Length >= 2)
                        {
                            var userName = d[1];
                            var msg = d.Length == 3 ? d[2] : "";
                            if (d.Length > 3)
                            {
                                var loc3 = 3;
                                while (loc3 < d.Length)
                                {
                                    d[2] = d[2] + (" " + d[loc3]);
                                    loc3 = loc3 + 1;
                                }
                                msg = d[2];
                            }
                            _bot.Game.SendPrivateMessage(userName, msg);
                            AddPrivateMessage(userName, _bot.Game.PlayerName, msg);
                        }                       
                        return;
                    }
                    string channelName = tab.Tag.ToString();
                    if (_bot.Game.Channels.ContainsKey(channelName))
                    {
                        _bot.Game.SendMessage(channelName, text);
                    }
                }
                else if (_pmTabs.ContainsValue(tab))
                {
                    _bot.Game.SendPrivateMessage(tab.Tag.ToString(), text);
                    // Needs to add the pm to the tab coz no packet is recieved after sending the pm.
                    AddPrivateMessage(tab.Tag.ToString(), _bot.Game.PlayerName, text);
                }
            }
        }
        private void PlayNotification()
        {
            Window window = Window.GetWindow(this);
            if (!window.IsActive || !IsVisible)
            {
                IntPtr handle = new WindowInteropHelper(window).Handle;
                FlashWindowHelper.Flash(handle);

                if (File.Exists("Assets/message.wav"))
                {
                    using (SoundPlayer player = new SoundPlayer("Assets/message.wav"))
                    {
                        player.Play();
                    }
                }
            }
        }
    }
    //Helper Class
    //Special Class to find out text lines
    public static class StringExtentions
    {
        public static long Lines(this string s)
        {
            long count = 1;
            int position = 0;
            while ((position = s.IndexOf('\n', position)) != -1)
            {
                count++;
                position++;
            }
            return count;
        }

        public static int[] GetPokemonLinkBounds(this string text, int characterIndex)
        {
            int num = characterIndex;
            if (text.Length > 10 && characterIndex != -1 && characterIndex < text.Length)
            {
                if (characterIndex + 4 > text.Length)
                {
                    characterIndex -= 4;
                    if (characterIndex < 0)
                    {
                        characterIndex = 0;
                    }
                }
                int num2;
                if (text[characterIndex] == '[' && text[characterIndex + 1] == 'p' && text[characterIndex + 2] == 'o' && text[characterIndex + 3] == 'k' && text[characterIndex + 4] == '=')
                {
                    num2 = characterIndex;
                }
                else
                {
                    num2 = text.LastIndexOf("[pok=", characterIndex + 4);
                }
                if (num2 == -1)
                {
                    return null;
                }
                num2 += 5;
                int num3 = text.IndexOf("]", num2);
                if (num3 == -1)
                {
                    return null;
                }
                int num4 = text.IndexOf("[/pok]", num3);
                if (num4 == -1 || num <= num4 + 5)
                {
                    return new int[]
                    {
                    num2 - 5,
                    num4 + 6
                    };
                }
            }
            return null;
        }

        public static int GetPokemonLinkPosition(this string text, int characterIndex)
        {
            if (characterIndex != -1 && characterIndex < text.Length)
            {
                if (characterIndex + 5 > text.Length)
                {
                    characterIndex -= 6;
                    if (characterIndex < 0)
                    {
                        characterIndex = 0;
                    }
                    if (text.LastIndexOf("[/pok]", characterIndex) == -1)
                    {
                        return -1;
                    }
                }
                int num;
                if (text[characterIndex] == '[' && text[characterIndex + 1] == 'p' && text[characterIndex + 2] == 'o' && text[characterIndex + 3] == 'k' && text[characterIndex + 4] == '=')
                {
                    num = characterIndex;
                }
                else
                {
                    num = text.LastIndexOf("[pok=", characterIndex + 4);
                }
                if (num == -1)
                {
                    return -1;
                }
                num += 5;
                int num2 = text.IndexOf("]", num);
                if (num2 == -1)
                {
                    return -1;
                }
                int num3 = text.IndexOf("[/pok]", num2);
                if (num3 == -1 || characterIndex <= num3 + 5)
                {
                    return num - 4;
                }
            }
            return -1;
        }
    }
}
