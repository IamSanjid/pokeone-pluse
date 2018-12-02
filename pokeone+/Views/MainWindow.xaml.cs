using Poke1Bot;
using Poke1Protocol;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
//using System.Windows.Shapes;

namespace pokeone_plus
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region CUSTOM_WINDOW
        private const int WM_SYSCOMMAND = 0x112;
        private uint TPM_LEFTALIGN = 0x0000;
        private uint TPM_RETURNCMD = 0x0100;
        private const UInt32 MF_ENABLED = 0x00000000;
        private const UInt32 MF_GRAYED = 0x00000001;
        internal const UInt32 SC_MAXIMIZE = 0xF030;
        internal const UInt32 SC_RESTORE = 0xF120;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags,
          int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem,
           uint uEnable);

        private bool PressedIcon;

        private void Icon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            PressedIcon = true;
            var helper = new WindowInteropHelper(this);
            var callingWindow = helper.Handle;
            var wMenu = GetSystemMenu(callingWindow, false);
            // Display the menu
            if (WindowState == WindowState.Maximized)
            {
                EnableMenuItem(wMenu, SC_MAXIMIZE, MF_GRAYED);
            }
            else
            {
                EnableMenuItem(wMenu, SC_MAXIMIZE, MF_ENABLED);
            }

            var command = WindowState != WindowState.Maximized ? TrackPopupMenuEx(wMenu, TPM_LEFTALIGN | TPM_RETURNCMD,
                (int)Application.Current.MainWindow.Left + 8, (int)Application.Current.MainWindow.Top + 30, callingWindow, IntPtr.Zero) 
                : TrackPopupMenuEx(wMenu, TPM_LEFTALIGN | TPM_RETURNCMD,
                (int)e.GetPosition(null).X, (int)e.GetPosition(null).Y, callingWindow, IntPtr.Zero);
            if (command == 0)
                return;

            PostMessage(callingWindow, WM_SYSCOMMAND, new IntPtr(command), IntPtr.Zero);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).Background = Brushes.RoyalBlue;
            Close();
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void BtnMiniMax_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                //TitleBar.btnMiniMaxIco.Icon = FontAwesome.WPF.FontAwesomeIcon.WindowMaximize;
                TitleBar.tipTextMiniMax.Text = "Maximize";
            }
            else
            {
                WindowState = WindowState.Maximized;
                //TitleBar.btnMiniMaxIco.Icon = FontAwesome.WPF.FontAwesomeIcon.WindowRestore;
                TitleBar.tipTextMiniMax.Text = "Restore Down";
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("PokeOne+.exe");
        }
        private void TabWindow_LostFocus(object sender, RoutedEventArgs e)
        {
            TabsWindow.Header.Background = (Brush)new BrushConverter().ConvertFromString("#2D2D30");
        }
        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!PressedIcon)
                DragMove();
            PressedIcon = false;
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private RichTextBox MessageTextBox => LogWindow.MessageTextBox;
        private void IntializeCustomeComponent()
        {
            TitleBar.btnMiniMax.Click += BtnMiniMax_Click;
            TitleBar.btnMinimize.Click += BtnMinimize_Click;
            TitleBar.btnClose.Click += BtnClose_Click;
            TitleBar.Title.Text = "PokeOne+";
            TitleBar.Icon.MouseDown += Icon_MouseDown;
            TabsWindow.WindowName.Text = "Tabs";
            LogWindow.WindowName.Text = "Output";
            TabsWindow.Left.Click += Left_Click;
            TabsWindow.Right.Click += Right_Click;
            LogWindow.MenuPosition.Click += MenuPosition_Click;
        }

        private void MenuPosition_Click(object sender, RoutedEventArgs e)
        {
            var ico = LogWindow.HiderIcon;
            if (ico.Icon == FontAwesome.WPF.FontAwesomeIcon.AngleDown)
            {
                LogWindow.HiderText.Text = "Show";
                LogWindow.HiderIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.AngleUp;
                LogWindowResizer.Visibility = Visibility.Hidden;
                LogWindowArea.Height = new GridLength(0, GridUnitType.Auto);
                LogWindow.Height = 22;
            }
            else
            {
                LogWindow.HiderText.Text = "Hide";
                LogWindow.HiderIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.AngleDown;
                LogWindowResizer.Visibility = Visibility.Visible;
                LogWindowArea.Height = new GridLength(1, GridUnitType.Star);
                MainViewArea.Height = new GridLength(2, GridUnitType.Star);
                LogWindow.Height = double.NaN;
            }
        }

        private void Right_Click(object sender, RoutedEventArgs e)
        {
            TabsWindow.Left.IsEnabled = true;
            TabsWindow.Right.IsEnabled = false;

            Grid.SetColumn(TabsWindow, 2);
            Grid.SetColumn(TabWindowResizer, 1);
            Grid.SetColumn(LogWindow, 0);
            Grid.SetColumn(LogWindowResizer, 0);
            Grid.SetColumn(MainView, 0);

            TabsWindow.Margin = new Thickness(0, 0, 5, 6);
            LogWindow.Margin = new Thickness(5, 6, 5, 6);
            MainView.Margin = new Thickness(6, 6, 6, 0);
        }

        private void Left_Click(object sender, RoutedEventArgs e)
        {
            TabsWindow.Left.IsEnabled = false;
            TabsWindow.Right.IsEnabled = true;

            Grid.SetColumn(TabsWindow, 0);
            Grid.SetColumn(TabWindowResizer, 0);
            Grid.SetColumn(LogWindow, 1);
            Grid.SetColumn(LogWindowResizer, 1);
            Grid.SetColumn(MainView, 1);

            TabsWindow.Margin = new Thickness(5, 0, 0, 6);
            LogWindow.Margin = new Thickness(0, 5, 6, 5);
            MainView.Margin = new Thickness(0, 6, 6, 0);
        }
        #endregion


        public BotClient Bot { get; }


        public TeamView Team { get; private set; }
        public InventoryView Inventory { get; private set; }
        public ChatView Chat { get; private set; }
        public PlayersView Players { get; private set; }
        public MapView Map { get; private set; }

        private FileLogger FileLog { get; }
        private Dictionary<string, TabItem> _viewTabs;
        private Dictionary<string, UserControl> _views;

        DateTime _refreshPlayers;
        int _refreshPlayersDelay;


        public MainWindow()
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
#endif
            Thread.CurrentThread.Name = "UI Thread";
            Bot = new BotClient();
            Bot.StateChanged += Bot_StateChanged;
            Bot.ClientChanged += Bot_ClientChanged;
            Bot.ConnectionClosed += Bot_ConnectionClosed;
            Bot.ConnectionOpened += Bot_ConnectionOpened;
            Bot.AutoReconnector.StateChanged += Bot_AutoReconnectorStateChanged;
            Bot.AutoLootBoxOpener.StateChanged += AutoLootBoxOpener_StateChanged;
            Bot.PokemonEvolver.StateChanged += PokemonEvolver_StateChanged;
            Bot.MessageLogged += Bot_MessageLogged;
            Bot.ColorMessageLogged += Bot_ColorMessageLogged;


            InitializeComponent();
            IntializeCustomeComponent();

            _viewTabs = new Dictionary<string, TabItem>();
            _views = new Dictionary<string, UserControl>();

            AutoEvolveSwitch.IsChecked = Bot.Settings.AutoEvolve;
            OpenLootBox.IsChecked = Bot.Settings.OpenLootBoxes;
            AutoReconnectSwitch.IsChecked = Bot.Settings.AutoReconnect;

            Bot.AutoReconnector.IsEnabled = Bot.Settings.AutoReconnect;
            Bot.AutoLootBoxOpener.IsEnabled = Bot.Settings.OpenLootBoxes;
            Bot.PokemonEvolver.IsEnabled = Bot.Settings.AutoEvolve;

            App.InitializeVersion();

            if (!string.IsNullOrEmpty(Bot.Settings.LastScript) && File.Exists(Bot.Settings.LastScript))
            {
                ReloadScriptMenuItem.Header = "Reload " + System.IO.Path.GetFileName(Bot.Settings.LastScript) + "\tCtrl+R";
                ReloadScriptMenuItem.IsEnabled = true;
            }
            else
            {
                ReloadScriptMenuItem.IsEnabled = false;
                Bot.Settings.LastScript = null;
            }

            Team = new TeamView(Bot);
            Inventory = new InventoryView(Bot);
            Chat = new ChatView(Bot);
            Players = new PlayersView(Bot);
            Map = new MapView(Bot);

            FileLog = new FileLogger();

            _refreshPlayers = DateTime.UtcNow;
            _refreshPlayersDelay = 5000;

            MainView.TabWindow.SelectionChanged += TabWindow_SelectionChanged;

            AddView("Team", Team, TabsWindow.TeamTab);
            AddView("Inventory", Inventory, TabsWindow.InventoryTab);
            AddView("Chat", Chat, TabsWindow.ChatTab);
            AddView("Players", Players, TabsWindow.PlayersTab);
            AddView("Map", Map, TabsWindow.MapTab);

            SetTitle(null);

            LogMessage("Running " + App.Name + " by " + App.Author + ", version " + App.Version);

            Task.Run(() => UpdateClients());
        }

        private void PokemonEvolver_StateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                Bot.Settings.AutoEvolve = value;
                if (AutoEvolveSwitch.IsChecked == value) return;
                AutoEvolveSwitch.IsChecked = value;
            });
        }

        private void AutoLootBoxOpener_StateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                Bot.Settings.OpenLootBoxes = value;
                if (OpenLootBox.IsChecked == value) return;
                OpenLootBox.IsChecked = value;
            });
        }

        private void Bot_AutoReconnectorStateChanged(bool value)
        {
            Dispatcher.InvokeAsync(delegate
            {
                Bot.Settings.AutoReconnect = value;
                if (AutoReconnectSwitch.IsChecked == value) return;
                AutoReconnectSwitch.IsChecked = value;
            });
        }

        private void TabWindow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedTab = MainView.TabWindow.SelectedItem as TabItem;
            if (selectedTab != null)
            {
                if (selectedTab.Content == Players)
                {
                    _refreshPlayersDelay = 200;
                }
                else
                    _refreshPlayersDelay = 5000;
            }
        }

        private void Bot_ColorMessageLogged(string msg, Brush color)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(msg, color);
            });
        }

        private void Bot_MessageLogged(string msg)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(msg);
            });
        }

        private void Client_PlayerAdded(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Client_PlayerUpdated(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Client_PlayerRemoved(PlayerInfos player)
        {
            if (_refreshPlayers < DateTime.UtcNow)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    Players.RefreshView();
                });
                _refreshPlayers = DateTime.UtcNow.AddMilliseconds(_refreshPlayersDelay);
            }
        }

        private void Bot_ConnectionOpened()
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null)
                    {
                        SetTitle(Bot.Account.Name);
                        //UpdateBotMenu();
                        LogoutMenuItem.IsEnabled = true;
                        LoginMenuItem.IsEnabled = false;
                        LoginButton.IsEnabled = true;
                        LoginButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.SignOut;
                        LogMessage("Connected, authenticating...", (Brush)new BrushConverter().ConvertFrom("#28d659"));
                    }
                }
            });
        }

        private void Bot_ConnectionClosed()
        {
            Dispatcher.InvokeAsync(delegate
            {
                //_lastQueueBreakPoint = null;
                LoginMenuItem.IsEnabled = true;
                LogoutMenuItem.IsEnabled = false;
                LoginButton.IsEnabled = true;
                LoginButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.SignIn;
                //UpdateBotMenu();
                StatusText.Text = "Offline";
                StatusText.Foreground = Brushes.Red;
                //Battle.ConnectionClosed();
            });
        }

        private void Bot_ClientChanged()
        {
            lock (Bot)
            {
                if (Bot.Game != null)
                {
                    //loot box
                    Bot.Game.RecievedLootBox += Client_RecievedLootBox;
                    Bot.Game.LootBoxOpened += Client_LootBoxOpened;
                    Bot.Game.LootBoxMessage += Client_LootBoxMessage;
                    // Login Stuff
                    Bot.Game.LoggedIn += Client_LoggedIn;
                    Bot.Game.AuthenticationFailed += Client_AuthenticationFailed;
                    Bot.Game.PositionUpdated += Client_PositionUpdated;
                    Bot.Game.PokemonsUpdated += Client_PokemonsUpdated;
                    Bot.Game.InventoryUpdated += Client_InventoryUpdated;
                    Bot.Game.DialogOpened += Client_DialogOpened;
                    //chat
                    Bot.Game.ChannelMessage += Chat.Client_ChannelMessage;
                    Bot.Game.RefreshChannelList += Chat.Client_RefreshChannelList;
                    Bot.Game.PrivateMessage += Chat.Client_PrivateMessage;
                    Bot.Game.LeavePrivateMessage += Chat.Client_LeavePrivateMessage;
                    //System/shop/time etc..
                    Bot.Game.LevelChanged += Client_LevelChanged;
                    Bot.Game.GameTimeUpdated += Client_PokeTimeUpdated;
                    Bot.Game.SystemMessage += Client_SystemMessage;
                    Bot.Game.PlayerRemoved += Client_PlayerRemoved;
                    Bot.Game.PlayerAdded += Client_PlayerAdded;
                    Bot.Game.PlayerUpdated += Client_PlayerUpdated;
                    Bot.Game.ShopOpened += Client_ShopOpened;
                    //Battle
                    Bot.Game.BattleStarted += Client_BattleStarted;
                    Bot.Game.BattleMessage += Client_BattleMessage;
                    Bot.Game.BattleEnded += Client_BattleEnded;
                    // Map Stuff
                    Bot.Game.MapLoaded += Map.Client_MapLoaded;
                    Bot.Game.PositionUpdated += Map.Client_PositionUpdated;
                    Bot.Game.LinksUpdated += Map.Client_LinksUpdated;
                    Bot.Game.PlayerAdded += Map.Client_PlayerEnteredMap;
                    Bot.Game.PlayerRemoved += Map.Client_PlayerLeftMap;
                    Bot.Game.PlayerUpdated += Map.Client_PlayerMoved;
                    Bot.Game.NpcReceieved += Map.Client_NpcReceived;
                }
            }
            Dispatcher.InvokeAsync(delegate
            {
                if (Bot.Game != null)
                {
                    FileLog.OpenFile(Bot.Account.Name);
                }
                else
                {
                    FileLog.CloseFile();
                }
            });
        }

        private void Client_LootBoxMessage(string text)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(text, Brushes.RoyalBlue);
            });
        }

        private void Client_LootBoxOpened(PSXAPI.Response.Payload.LootboxRoll[] rewards, PSXAPI.Response.LootboxType type)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (rewards != null)
                {
                    var builder = new StringBuilder();
                    builder.AppendLine($"You've opened {type} Loot Box! And received following things: ");
                    foreach(var reward in rewards)
                    {
                        builder.Append($"\t{reward.LootType}");
                        if (reward.LootType == PSXAPI.Response.Payload.LootType.Item)
                            builder.Append($"\t: {ItemsManager.Instance.ItemClass.items.ToList().Find(i => i.ID == reward.Num).Name}");
                        else if (reward.LootType == PSXAPI.Response.Payload.LootType.Gold)
                            builder.Append($"\t[PG]{reward.Num}");
                        else if (reward.LootType == PSXAPI.Response.Payload.LootType.Money)
                            builder.Append($"\t${reward.Num.ToString("#,##0")}");
                        else if (reward.LootType == PSXAPI.Response.Payload.LootType.Pokemon)
                            builder.Append($"\t: {PokemonManager.Instance.Names[reward.Num]}");
                    }
                    LogMessage(builder.ToString(), Brushes.RoyalBlue);
                }
            });            
        }

        private void Client_SystemMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                AddSystemMessage(message);
            });
        }

        private void Client_LevelChanged(PSXAPI.Response.Level preLevel, PSXAPI.Response.Level currentLevel)
        {
            Dispatcher.InvokeAsync(delegate
            {
                if (preLevel != null)
                {
                    if (currentLevel.UserLevel > preLevel.UserLevel)
                        LogMessage($"You've been leveled up to {currentLevel.UserLevel}!", (Brush)new BrushConverter().ConvertFrom("#28d659"));
                }
                LevelText.Text = currentLevel.UserLevel.ToString();
            });
        }

        private void Client_RecievedLootBox(PSXAPI.Response.Lootbox box)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage($"You've received a {box.Type} Loot Box!", Brushes.Tomato);
            });
        }

        private void Client_BattleStarted()
        {
            Dispatcher.InvokeAsync(delegate
            {
                StatusText.Text = "In battle";
                StatusText.Foreground = (Brush)new BrushConverter().ConvertFrom("#148CCF");
            });
        }

        private void Client_BattleMessage(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                message = Regex.Replace(message, @"\[.+?\]", "");
                LogMessage(message, Brushes.Aqua);
            });
        }

        private void Client_BattleEnded()
        {
            Dispatcher.InvokeAsync(delegate
            {
                StatusText.Text = "Online";
                StatusText.Foreground = (Brush)new BrushConverter().ConvertFrom("#28d659");
            });
        }

        private void Client_DialogOpened(string message)
        {
            Dispatcher.InvokeAsync(delegate
            {
                LogMessage(message, Brushes.DarkOrange);
            });
        }

        private void Client_InventoryUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                string money;
                string gold;
                IList<InventoryItem> items;
                lock (Bot)
                {
                    if (Bot.Game is null) return;
                    money = Bot.Game.Money.ToString("#,##0");
                    gold = Bot.Game.Gold.ToString("#,##0");
                    items = Bot.Game.Items.ToArray();
                }
                MoneyText.Text = money;
                GoldText.Text = gold;
                Inventory.ItemsListView.ItemsSource = items;
                Inventory.ItemsListView.Items.Refresh();
            });
        }

        private void Client_PokemonsUpdated()
        {
            Dispatcher.InvokeAsync(delegate
            {
                lock (Bot)
                {
                    if (Bot.Game != null)
                    {
                        IList<Pokemon> team;

                        team = Bot.Game.Team.ToArray();
                        Team.PokemonsListView.ItemsSource = team;
                        Team.PokemonsListView.Items.Refresh();
                    }
                }
            });
        }

        private void Client_PositionUpdated(string map, int x, int y)
        {
            Dispatcher.InvokeAsync(delegate
            {
                MapNameText.Text = map;
                PlayerPositionText.Text = "(" + x + "," + y + ")";
                AreaNameTip.Text = $"Area Name: {map}";
            });
        }

        private void Client_AuthenticationFailed(PSXAPI.Response.LoginError reason)
        {
            Dispatcher.InvokeAsync(delegate
            {
                var message = "";
                switch (reason)
                {
                    case PSXAPI.Response.LoginError.AlreadyLoggedIn:
                        message = "Already logged in";
                        break;
                    case PSXAPI.Response.LoginError.Banned:
                        message = "You are banned from PokeOne";
                        break;
                    case PSXAPI.Response.LoginError.NotVerified:
                        message = "Email not activated";
                        break;
                    case PSXAPI.Response.LoginError.WrongPassword:
                        message = "Invalid password";
                        break;
                    case PSXAPI.Response.LoginError.AccountNotFound:
                        message = "Invalid username";
                        break;
                    case PSXAPI.Response.LoginError.Unsupported:
                        message = "Outdated client, please wait for an update";
                        break;
                    case PSXAPI.Response.LoginError.Locked:
                        message = "Server locked for maintenance";
                        break;
                    case PSXAPI.Response.LoginError.Full:
                        message = "Server is full";
                        break;
                    default:
                        message = "Unexpected error";
                        break;
                }
                LogMessage("Authentication failed: " + message, Brushes.OrangeRed);
            });
        }

        private void Client_LoggedIn()
        {
            Dispatcher.InvokeAsync(delegate
            {
               // _lastQueueBreakPoint = null;
                LogMessage("Authenticated successfully!", (Brush)new BrushConverter().ConvertFrom("#28d659"));
                //UpdateBotMenu();
                StatusText.Text = "Online";
                StatusText.Foreground = (Brush)new BrushConverter().ConvertFrom("#28d659");
            });
        }

        private void Client_PokeTimeUpdated(string pokeTime, string weather)
        {
            lock (Bot)
            {
                Dispatcher.InvokeAsync(delegate
                {
                    if (Bot.Game != null)
                    {
                        PokeTimeText.Text = pokeTime;
                    }
                });
            }
        }

        private void Client_ShopOpened(Shop shop)
        {
            Dispatcher.InvokeAsync(delegate
            {
                var content = new StringBuilder();
                content.Append("Shop opened:");
                foreach (ShopItem item in shop.Items)
                {
                    content.AppendLine();
                    content.Append(item.Name);
                    content.Append(" ($" + item.Cost + ")");
                    if (item.TokenCost > 0)
                        content.Append(" - ([PG]" + item.TokenCost + ")");
                }
                LogMessage(content.ToString());
            });
        }

        private void Bot_StateChanged(BotClient.State state)
        {
            Dispatcher.InvokeAsync(delegate
            {
                //UpdateBotMenu();
                string stateText;
                if (state == BotClient.State.Started)
                {
                    stateText = "started";
                    StartScriptButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Pause;
                }
                else if (state == BotClient.State.Paused)
                {
                    stateText = "paused";
                    StartScriptButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
                }
                else
                {
                    stateText = "stopped";
                    StartScriptButtonIcon.Icon = FontAwesome.WPF.FontAwesomeIcon.Play;
                }
                if (stateText == "started")
                { LogMessage("Bot " + stateText, (Brush)new BrushConverter().ConvertFrom("#28d659")); }
                else
                { LogMessage("Bot " + stateText, Brushes.OrangeRed); }
            });
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Dispatcher.InvokeAsync(() => HandleUnhandledException(e.Exception.InnerException));
        }
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleUnhandledException(e.ExceptionObject as Exception);
        }

        private void HandleUnhandledException(Exception ex)
        {
            try
            {
                if (ex != null)
                {
                    File.WriteAllText("crash_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt",
                        App.Name + @" " + App.Version + @" crash report: " + Environment.NewLine + ex);
                }
                MessageBox.Show(App.Name + " encountered a fatal error. The application will now terminate." + Environment.NewLine +
                    "An error file has been created next to the application.", App.Name + " - Fatal error", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }
            catch(Exception)
            {
                //ignore
            }
        }

        private void UpdateClients()
        {
            lock (Bot)
            {
                if (Bot.Game != null)
                {
                    Bot.Game.Update();
                }
                Bot.Update();
            }
            Task.Delay(1).ContinueWith((previous) => UpdateClients());
        }

        private void AddSystemMessage(string message)
        {
            var bc = new BrushConverter();
            LogMessage("System: " + message, (Brush)bc.ConvertFrom("#28d659"));
        }

        private void LogMessage(string message, Brush color)
        {
            var text = "[" + DateTime.Now.ToLongTimeString() + "] " + message;
            AppendLineToRichTextBox(MessageTextBox, text, color);
            FileLog.Append(text);
        }
        private void LogMessage(string message)
        {
            var bc = new BrushConverter();
            LogMessage(message, (Brush)bc.ConvertFrom("#F1F1F1"));
        }
        private void LogMessage(string format, params object[] args)
        {
            LogMessage(string.Format(format, args));
        }

        private void AddView(string tabName, UserControl view, ListViewItem button)
        {
            var tab = new ButtonTabHeader();
            tab.TabName.Content = tabName;
            tab.ButtonClose.Click += (sender, arg) => CloseTab(tabName);
            tab.MouseDown += (sender, arg) =>
            {
                MainView.TabWindow.Focus();
                if (_viewTabs.ContainsKey(tabName))
                    _viewTabs[tabName].Focus();
            };
            view.MouseDown += (sender, arg) =>
            {
                MainView.TabWindow.Focus();
                if (_viewTabs.ContainsKey(tabName))
                    _viewTabs[tabName].Focus();
            };
            tab.Tag = tabName;
            button.Tag = tabName;
            var tabItm = new TabItem { Header = tab, Content = view };
            _viewTabs[tabName] = tabItm;
            _views[tabName] = view;
            MainView.TabWindow.Items.Add(tabItm);
            button.MouseDoubleClick += Button_DoubleClick;
            if (_viewTabs.Count > 0)
                MainView.VerticalLine.Background = (Brush)new BrushConverter().ConvertFromString("#007ACC");
        }

        private void Button_DoubleClick(object sender, RoutedEventArgs e)
        {
            var button = sender as ListViewItem;
            var tabName = button.Tag.ToString();
            if (!_viewTabs.ContainsKey(tabName))
            {
                AddView(tabName, _views[tabName], button);
                MainView.TabWindow.SelectedItem = _viewTabs[tabName];
            }
            else
            {
                MainView.TabWindow.SelectedItem = _viewTabs[tabName];
                if (_viewTabs.Count > 0)
                    MainView.VerticalLine.Background = (Brush)new BrushConverter().ConvertFromString("#007ACC");
            }
        }

        private void CloseTab(string tabName)
        {
            if (_viewTabs.ContainsKey(tabName))
            {
                MainView.TabWindow.Items.Remove(_viewTabs[tabName]);
                _viewTabs.Remove(tabName);
                if (_viewTabs.Count <= 0)
                    MainView.VerticalLine.Background = (Brush)new BrushConverter().ConvertFromString("#3f3f46");
            }
        }

        private void SetTitle(string username)
        {
            Title = username == null ? "" : username + " - ";
            Title += App.Name + " " + App.Version;
#if DEBUG
            Title += " (debug)";
#endif
            TitleBar.Title.Text = Title;
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var shouldLogin = false;
            lock (Bot)
            {
                if (Bot.Game == null || !Bot.Game.IsConnected)
                {
                    shouldLogin = true;
                }
                else
                {
                    Logout();
                }
            }
            if (shouldLogin)
            {
                OpenLoginWindow();
            }
        }
        private void Logout()
        {
            LogMessage("Logging out...", Brushes.OrangeRed);
            lock (Bot)
            {
                Bot.Logout(false);
            }
        }
        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            Logout();
        }

        private void StartScriptButton_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                if (Bot.Running == BotClient.State.Stopped)
                {
                    Bot.Start();
                }
                else if (Bot.Running == BotClient.State.Started || Bot.Running == BotClient.State.Paused)
                {
                    Bot.Pause();
                }
            }
        }

        private void StopScriptButton_Click(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.Stop();
            }
        }

        private async void LoadScriptButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadScript();
        }

        private void OpenLoginWindow()
        {
            var login = new LoginWindow(Bot) { Owner = this };
            var result = login.ShowDialog();
            if (result != true)
            {
                return;
            }

            LogMessage("Connecting to the server...", (Brush)new BrushConverter().ConvertFrom("#28d659"));
            LoginButton.IsEnabled = false;
            LoginMenuItem.IsEnabled = false;
            Login(login);
        }

        /// <summary>
        /// Login to the server manually.
        /// </summary>
        /// <param name="login"></param>
        private void Login(LoginWindow login)
        {
            var account = new Account(login.Username);
            lock (Bot)
            {
                account.Password = login.Password;

                if (login.HasProxy)
                {
                    account.Socks.Version = (SocksVersion)login.ProxyVersion;
                    account.Socks.Host = login.ProxyHost;
                    account.Socks.Port = login.ProxyPort;
                    account.Socks.Username = login.ProxyUsername;
                    account.Socks.Password = login.ProxyPassword;
                }
                Bot.Login(account);
            }
        }

        private async Task LoadScript(string filePath = null, bool startScriptInstant = false)
        {
            if (filePath == null)
            {
                var openDialog = new OpenFileDialog
                {
                    Filter = App.Name + " Scripts|*.lua;*.txt|All Files|*.*"
                };
                bool? result = openDialog.ShowDialog();

                if (!(result.HasValue && result.Value))
                    return;

                filePath = openDialog.FileName;
            }

            try
            {
                lock (Bot)
                {
                    Bot.Settings.LastScript = filePath;
                    ReloadScriptMenuItem.Header = "Reload " + Path.GetFileName(filePath) + "\tCtrl+R";
                    ReloadScriptMenuItem.IsEnabled = true;
                }
                await Bot.LoadScript(filePath);
                MenuPathScript.Header =
                    "Script: \"" + Bot.Script.Name + "\"" + Environment.NewLine + filePath;
                LogMessage("Script \"{0}\" by \"{1}\" successfully loaded", Bot.Script.Name, Bot.Script.Author);
                if (!string.IsNullOrEmpty(Bot.Script.Description))
                {
                    LogMessage(Bot.Script.Description);
                }
            }
            catch (Exception ex)
            {
                var filename = Path.GetFileName(filePath);
#if DEBUG
                LogMessage(string.Format("Could not load script {0}: " + Environment.NewLine + "{1}", filename, ex), Brushes.OrangeRed);
#else
                LogMessage(string.Format("Could not load script {0}: " + Environment.NewLine + "{1}", filename, ex.Message), Brushes.OrangeRed);
#endif
            }
        }

        private async void ReloadScript_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(Bot.Settings.LastScript))
                return;
            if (!File.Exists(Bot.Settings.LastScript))
            {
                Bot.Settings.LastScript = null;
                return;
            }

            await LoadScript(Bot.Settings.LastScript);
        }

        // Technique for updating column widths of a ListView's GridView manually
        public static void UpdateColumnWidths(GridView gridView)
        {
            // For each column...
            foreach (var column in gridView.Columns)
            {
                // If this is an "auto width" column...
                if (double.IsNaN(column.Width))
                {
                    // Set its Width back to NaN to auto-size again
                    column.Width = 0;
                    column.Width = double.NaN;
                }
            }
        }

        public static void AppendLineToRichTextBox(RichTextBox richTextBox, string message, Brush color = null)
        {
            Paragraph para;
            var r = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            var t = Regex.Replace(r.Text, @"\s+", "");
#if DEBUG
            Console.WriteLine(t.Length.ToString());
#endif
            if (t.Length > 0)
            {
                para = new Paragraph { Margin = new Thickness(0) };
            }
            else
            {
                para = richTextBox.Document.Blocks.FirstBlock as Paragraph;
                para.LineHeight = 10;
            }

            if (color != null)
            {
                para.Inlines.Add(new Run(message)
                {
                    Foreground = color
                });
            }
            else
            {
                para.Inlines.Add(new Run(message));
            }

            richTextBox.Document.Blocks.Add(para);

            var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            if (range.Text.Length > 12000)
            {
                var text = range.Text;
                text = text.Substring(text.Length - 10000, 10000);
                var index = text.IndexOf(Environment.NewLine, StringComparison.Ordinal);
                if (index != -1)
                {
                    text = text.Substring(index + Environment.NewLine.Length);
                }
                var lines = text.Lines();
                var ln = Convert.ToInt32(lines);
                ln = richTextBox.Document.Blocks.Count - ln;
                for (int i = 0; i <= richTextBox.Document.Blocks.Count - 1; i++)
                {
                    if (i <= ln && ln > 0) //Value to remove blocks
                    {
                        //Removing blocks
                        richTextBox.Document.Blocks.Remove(richTextBox.Document.Blocks.ToList()[i]);
                    }
                    else
                    {
                        if (i <= 48) //Default value
                        {
                            //Removing blocks
                            richTextBox.Document.Blocks.Remove(richTextBox.Document.Blocks.ToList()[i]);
                        }
                    }
                }
            }
            if (richTextBox.Selection.IsEmpty)
            {
                richTextBox.CaretPosition = richTextBox.Document.ContentEnd;
                richTextBox.ScrollToEnd();
            }
        }

        private void OpenLootBox_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoLootBoxOpener.IsEnabled = true;
            }
        }

        private void OpenLootBox_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoLootBoxOpener.IsEnabled = false;
            }
        }

        private void AutoReconnectSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoReconnector.IsEnabled = true;
            }
        }

        private void AutoReconnectSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.AutoReconnector.IsEnabled = false;
            }
        }

        private void AutoEvolveSwitch_Checked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.PokemonEvolver.IsEnabled = true;
            }
        }

        private void AutoEvolveSwitch_Unchecked(object sender, RoutedEventArgs e)
        {
            lock (Bot)
            {
                Bot.PokemonEvolver.IsEnabled = false;
            }
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) != null)
            {
                var file = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (file != null)
                {
                    await LoadScript(file[0]);
                }
            }
        }

        private void ReloadHotKey_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ReloadScript_Click(sender, null);
        }

        private void SourceCode_View_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://bit.ly/2qaXE4J");
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://www.paypal.me/purpleP1");
        }

        private void LuaApi_View_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://crazy3001.github.io/pokeoneplus-slate/");
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(App.Name + " version " + App.Version + ", by " + App.Author + "." + Environment.NewLine + App.Description, App.Name + " - About");
        }
    }
}
