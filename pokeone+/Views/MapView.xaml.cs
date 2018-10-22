using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Poke1Bot;
using Poke1Protocol;

namespace pokeone_plus
{
    /// <summary>
    /// Interaction logic for MapView.xaml
    /// </summary>
    public partial class MapView : UserControl
    {
        private BotClient _bot;
        private Dictionary<int, Brush> _colliderColors;

        private int _cellWidth = 16;

        private bool _isMapDirty;
        private UniformGrid _mapGrid;
        private bool _isPlayerDirty;
        private Shape _player;
        private bool _areNpcsDirty;
        private Shape[] _npcs;
        private bool _arePlayersDirty;
        private Shape[] _otherPlayers;

        private Point _lastDisplayedCell = new Point(-1, -1);
        private Point _playerPosition = new Point();

        private Point _startDragCell = new Point();
        private Shape _selectedRectangle;
        private bool _dragging;
        private string playerName { get; set; }
        public MapView(BotClient bot)
        {
            InitializeComponent();
            _bot = bot;
            _colliderColors = new Dictionary<int, Brush>
            {
                { 0, Brushes.White },
                { 2, Brushes.LightSkyBlue },
                { 3, Brushes.Gray },
                { 4, Brushes.Gray },
                { 5, Brushes.LightSkyBlue },
                { 6, Brushes.LightGreen },
                { 7, Brushes.White },
                { 8, Brushes.LightGreen },
                { 9, Brushes.White },
                { 10, Brushes.LightGray },
                //{ 12, Brushes.LightSkyBlue},
                //{ 16, Brushes.White },
                //{ 18, Brushes.White },
                //{ 19, Brushes.White },
                //{ 20, Brushes.White },
            };

            IsVisibleChanged += MapView_IsVisibleChanged;
            MouseDown += MapView_MouseDown;
            SizeChanged += MapView_SizeChanged;

            _selectedRectangle = new Rectangle
            {
                Stroke = Brushes.Red,
                Fill = Brushes.Transparent,
                StrokeThickness = 1
            };
        }

        private void MapView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Window parent = Window.GetWindow(this);
            if (parent is null) return;
            if (IsVisible)
            {
                if (_isMapDirty) RefreshMap();
                if (_isPlayerDirty) RefreshPlayer(!_areNpcsDirty || !_arePlayersDirty);
                if (_areNpcsDirty) RefreshNpcs();
                if (_arePlayersDirty) RefreshOtherPlayers();
                parent.KeyDown += Parent_KeyDown;
            }
            else
            {
                parent.KeyDown -= Parent_KeyDown;
            }
        }

        private void MapView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(this);
        }

        private void MapView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (IsVisible)
            {
                RefreshMap();
            }
        }

        private void Parent_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            if (e.Key == Key.Add)
            {
                _cellWidth += 2;
                if (_cellWidth > 64) _cellWidth = 64;
                RefreshMap();
            }
            else if (e.Key == Key.Subtract)
            {
                _cellWidth -= 2;
                if (_cellWidth < 4) _cellWidth = 4;
                RefreshMap();
            }
            else if (e.Key == Key.Up)
            {
                MovePlayer(Direction.Up);
            }
            else if (e.Key == Key.Down)
            {
                MovePlayer(Direction.Down);
            }
            else if (e.Key == Key.Left)
            {
                MovePlayer(Direction.Left);
            }
            else if (e.Key == Key.Right)
            {
                MovePlayer(Direction.Right);
            }
            else
            {
                e.Handled = false;
            }
        }

        private void MovePlayer(Direction direction)
        {
            lock (_bot)
            {
                if (_bot.Game != null &&
                    _bot.Game.IsMapLoaded &&
                    //_bot.Game.AreNpcReceived &&
                    _bot.Game.IsInactive &&
                    !_bot.Game.IsInBattle &&
                    _bot.Running != BotClient.State.Started)
                {
                    _bot.Game.Move(direction);
                }
            }
        }

        private void RetrieveCellInfo(int x, int y)
        {
            _lastDisplayedCell = new Point(x, y);

            StringBuilder logBuilder = new StringBuilder();

            if (_dragging && ((int)_startDragCell.X != x || (int)_startDragCell.Y != y))
            {
                int startX = (int)_startDragCell.X;
                int startY = (int)_startDragCell.Y;
                logBuilder.AppendLine($"Rectangle: ({Math.Min(x, startX)}, {Math.Min(y, startY)}, {Math.Max(x, startX)}, {Math.Max(y, startY)})");
                logBuilder.AppendLine();
            }

            logBuilder.AppendLine($"Cell: ({x},{y})");

            if (_bot.Game.Map.HasLink(x, y))
            {
                logBuilder.AppendLine("Link ID: " + _bot.Game.Map.Links.Find(s => s.x == x && s.z == -y).DestinationID);
            }
            PlayerInfos[] playersOnCell = _bot.Game.Players.Values.Where(player => player.PosX == x && player.PosY == y).ToArray();
            if (playersOnCell.Length > 0)
            {
                logBuilder.AppendLine($"{playersOnCell.Length} player{(playersOnCell.Length != 1 ? "s" : "")}:");
                foreach (PlayerInfos player in playersOnCell)
                {
                    logBuilder.Append("  " + player.Name);
                    if (player.IsInBattle) logBuilder.Append(" [in battle]");
                    if (player.IsMember) logBuilder.Append(" [member]");
                    if (player.IsAfk) logBuilder.Append(" [afk]");
                    logBuilder.Append("\n  Level: " + player.Level);
                    logBuilder.AppendLine();
                }
            }

            Npc[] npcsOnCell = _bot.Game.Map.Npcs.Where(npc => npc.PositionX == x && npc.PositionY == y).ToArray();
            if (npcsOnCell.Length > 0)
            {
                logBuilder.AppendLine($"{npcsOnCell.Length} npc{(npcsOnCell.Length != 1 ? "s" : "")}:");
                foreach (Npc npc in npcsOnCell)
                {
                    logBuilder.AppendLine("  ID: " + npc.Id);
                    if (npc.NpcName != string.Empty) logBuilder.AppendLine("    name: " + npc.NpcName);
                    logBuilder.AppendLine("    sprite: " + npc.Data.Settings.Sprite);
                    logBuilder.AppendLine("    Is Battler: " + npc.IsBattler);
                    logBuilder.AppendLine("    Is PC: " + (_bot.Game.Map.IsPc(npc.PositionX, npc.PositionY)));
                    logBuilder.AppendLine("    Is Cutable Tree: " + (_bot.Game.Map.IsCutTree(npc.PositionX, npc.PositionY)));
                    logBuilder.AppendLine("    Is Smashable Rock: " + (_bot.Game.Map.IsRockSmash(npc.PositionX, npc.PositionY)));
                }
            }

            logBuilder.Length -= Environment.NewLine.Length;
            TipText.Text = logBuilder.ToString();
        }

        public void RefreshMap()
        {
            if (!IsVisible)
            {
                _isMapDirty = true;
                return;
            }
            _isMapDirty = false;

            MapCanvas.Children.Clear();

            lock (_bot)
            {
                if (_bot.Game == null || _bot.Game.Map == null) return;

                UniformGrid grid = new UniformGrid();

                grid.Background = Brushes.White;

#if DEBUG
                _bot.LogMessage($"DimensionX:{_bot.Game.Map.DimensionX},DimensionY:{_bot.Game.Map.DimensionY},Width:{_bot.Game.Map.Width},Height:{_bot.Game.Map.Height}");
#endif

                int minX = Math.Max(_bot.Game.PlayerX - 25, 0);
                int maxX = Math.Min(_bot.Game.PlayerX + 25, _bot.Game.Map.Width);
                int minY = Math.Max(_bot.Game.PlayerY - 25, 0);
                int maxY = Math.Min(_bot.Game.PlayerY + 25, _bot.Game.Map.Height);

                grid.Columns = _bot.Game.Map.Width;
                grid.Rows = _bot.Game.Map.Height;
                grid.Width = grid.Columns * _cellWidth;
                grid.Height = grid.Rows * _cellWidth;

                // TODO: Draw only current area

                for (int y = 0; y < grid.Rows; ++y)
                {
                    for (int x = 0; x < grid.Columns; ++x)
                    {
                        Rectangle rect = new Rectangle();
                        int collider = _bot.Game.Map.GetCollider(x, y);
                        if (_bot.Game.Map.HasLink(x, y))
                        {
                            rect.Fill = Brushes.Gold;
                        }
                        else if (_colliderColors.ContainsKey(collider))
                        {
                            rect.Fill = _colliderColors[collider];
                        }
                        else
                        {
                            rect.Fill = Brushes.Black;
                        }

                        if (collider == 4 || collider == 22)
                        {
                            rect.Height = _cellWidth / 4;
                            rect.VerticalAlignment = VerticalAlignment.Top;
                        }

                        if (collider == 19 || collider == 20)
                        {
                            rect.Width = _cellWidth / 4;
                            rect.HorizontalAlignment = collider == 19 ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                        }

                        if (_bot.Game.Map.GetCellSideMoveable(collider))
                        {
                            rect.Fill = Brushes.Wheat;
                            rect.Height = _cellWidth / 4;
                            rect.VerticalAlignment = VerticalAlignment.Top;
                        }

                        if (_bot.Game.Map.IsGrass(x, y))
                            rect.Fill = Brushes.LightGreen;

                        if (_bot.Game.Map.IsCutTree(x, y))
                            rect.Fill = Brushes.DarkGreen;
                        if (_bot.Game.Map.IsRockSmash(x, y))
                            rect.Fill = Brushes.SandyBrown;

                        grid.Children.Add(rect);
                    }
                }


                _mapGrid = grid;
                MapCanvas.Children.Add(grid);

                _player = new Ellipse() { Fill = Brushes.Red, Width = _cellWidth, Height = _cellWidth };
                MapCanvas.Children.Add(_player);
                Panel.SetZIndex(_player, 100);

                RefreshPlayer(false);
                RefreshNpcs();
                RefreshOtherPlayers();

                _dragging = false;
                _selectedRectangle.Width = _cellWidth;
                _selectedRectangle.Height = _cellWidth;
                Tuple<double, double> drawingOffset = GetDrawingOffset();
                double deltaX = drawingOffset.Item1;
                double deltaY = drawingOffset.Item2;
                Canvas.SetLeft(_selectedRectangle, (_lastDisplayedCell.X + deltaX) * _cellWidth);
                Canvas.SetTop(_selectedRectangle, (_lastDisplayedCell.Y + deltaY) * _cellWidth);
                MapCanvas.Children.Add(_selectedRectangle);
            }
        }

        public void RefreshPlayer(bool refreshEntities)
        {
            if (!IsVisible)
            {
                _isPlayerDirty = true;
                return;
            }
            _isPlayerDirty = false;

            lock (_bot)
            {
                if (_bot.Game == null || _bot.Game.Map == null || _player == null) return;
                UpdatePlayerPosition();
                if (refreshEntities)
                {
                    UpdateNpcPositions();
                    UpdateOtherPlayerPositions();
                }
            }
        }

        private void UpdatePlayerPosition()
        {
            _playerPosition = new Point(_bot.Game.PlayerX, _bot.Game.PlayerY);

            Tuple<double, double> drawingOffset = GetDrawingOffset();
            double deltaX = drawingOffset.Item1;
            double deltaY = drawingOffset.Item2;

            Canvas.SetLeft(_mapGrid, deltaX * _cellWidth);
            Canvas.SetTop(_mapGrid, deltaY * _cellWidth);
            Canvas.SetLeft(_player, (_bot.Game.PlayerX + deltaX) * _cellWidth);
            Canvas.SetTop(_player, (_bot.Game.PlayerY + deltaY) * _cellWidth);
        }

        public void RefreshNpcs()
        {
            if (!IsVisible)
            {
                _areNpcsDirty = true;
                return;
            }
            _areNpcsDirty = false;

            lock (_bot)
            {
                if (_bot.Game == null || _bot.Game.Map == null || _mapGrid == null) return;

                if (_npcs != null)
                    foreach (Shape npc in _npcs)
                        MapCanvas.Children.Remove(npc);

                _npcs = new Shape[_bot.Game.Map.Npcs.Count];
                for (int i = 0; i < _npcs.Length; i++)
                {
                    Brush color;
                    if (_bot.Game.Map.IsCutTree(_bot.Game.Map.Npcs[i].PositionX, _bot.Game.Map.Npcs[i].PositionY))
                        color = Brushes.DarkGreen;
                    else if (_bot.Game.Map.IsRockSmash(_bot.Game.Map.Npcs[i].PositionX, _bot.Game.Map.Npcs[i].PositionY))
                        color = Brushes.SandyBrown;
                    else if (_bot.Game.Map.IsPc(_bot.Game.Map.Npcs[i].PositionX, _bot.Game.Map.Npcs[i].PositionY))
                        color = Brushes.DarkGray;
                    else
                        color = Brushes.DarkOrange;
                    _npcs[i] = new Ellipse() { Fill = color, Width = _cellWidth, Height = _cellWidth };
                    MapCanvas.Children.Add(_npcs[i]);
                }

                UpdateNpcPositions();
            }
        }

        private void UpdateNpcPositions()
        {
            if (_bot.Game.Map.Npcs.Count != _npcs.Length) return;

            Tuple<double, double> drawingOffset = GetDrawingOffset();
            double deltaX = drawingOffset.Item1;
            double deltaY = drawingOffset.Item2;

            for (int i = 0; i < _npcs.Length; i++)
            {
                Canvas.SetLeft(_npcs[i], (_bot.Game.Map.Npcs[i].PositionX + deltaX) * _cellWidth);
                Canvas.SetTop(_npcs[i], (_bot.Game.Map.Npcs[i].PositionY + deltaY) * _cellWidth);
            }
        }

        public void RefreshOtherPlayers()
        {
            if (!IsVisible)
            {
                _arePlayersDirty = true;
                return;
            }
            _arePlayersDirty = false;

            lock (_bot)
            {
                if (_bot.Game == null || _bot.Game.Map == null || _mapGrid == null) return;

                if (_otherPlayers != null)
                    foreach (Shape player in _otherPlayers)
                        MapCanvas.Children.Remove(player);

                _otherPlayers = new Shape[_bot.Game.Players.Count];
                for (int i = 0; i < _otherPlayers.Length; i++)
                {
                    _otherPlayers[i] = new Ellipse() { Fill = Brushes.ForestGreen, Width = _cellWidth, Height = _cellWidth };
                    MapCanvas.Children.Add(_otherPlayers[i]);
                }
                UpdateOtherPlayerPositions();
            }
        }

        private void UpdateOtherPlayerPositions()
        {
            if (_bot.Game.Players.Count != _otherPlayers.Length) return;

            Tuple<double, double> drawingOffset = GetDrawingOffset();
            double deltaX = drawingOffset.Item1;
            double deltaY = drawingOffset.Item2;

            int playerIndex = 0;
            foreach (PlayerInfos player in _bot.Game.Players.Values)
            {
                Canvas.SetLeft(_otherPlayers[playerIndex], (player.PosX + deltaX) * _cellWidth);
                Canvas.SetTop(_otherPlayers[playerIndex], (player.PosY + deltaY) * _cellWidth);
                playerIndex++;
            }
        }

        private Tuple<double, double> GetDrawingOffset()
        {
            double canFillX = Math.Floor(MapCanvas.ActualWidth / _cellWidth);
            double canFillY = Math.Floor(MapCanvas.ActualHeight / _cellWidth);

            double deltaX = -_playerPosition.X + canFillX / 2;
            double deltaY = -_playerPosition.Y + canFillY / 2;

            if (_mapGrid.Columns <= canFillX) deltaX = 0;
            if (_mapGrid.Rows <= canFillY) deltaY = 0;

            if (deltaX < -_mapGrid.Columns + canFillX) deltaX = -_mapGrid.Columns + canFillX;
            if (deltaY < -_mapGrid.Rows + canFillY) deltaY = -_mapGrid.Rows + canFillY;

            if (deltaX > 0) deltaX = 0;
            if (deltaY > 0) deltaY = 0;

            return new Tuple<double, double>(deltaX, deltaY);
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tuple<double, double> drawingOffset = GetDrawingOffset();
            double deltaX = drawingOffset.Item1;
            double deltaY = drawingOffset.Item2;
            int ingameX = (int)((e.GetPosition(this).X / _cellWidth - deltaX));
            int ingameY = (int)((e.GetPosition(this).Y / _cellWidth) - deltaY);

            lock (_bot)
            {
                if (_bot.Game != null &&
                    _bot.Game.IsMapLoaded &&
                    _bot.Game.AreNpcReceived &&
                    _bot.Game.IsInactive &&
                    !_bot.Game.IsInBattle &&
                    _bot.Running != BotClient.State.Started)
                {
                    Npc npcOnCell = _bot.Game.Map.Npcs.FirstOrDefault(npc => npc.PositionX == ingameX && npc.PositionY == ingameY);
                    if (npcOnCell == null)
                    {
                        _bot.MoveToCell(ingameX, ingameY);
                    }
                    else
                    {
                        _bot.TalkToNpc(npcOnCell);
                    }
                }
            }
        }

        private void MapCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tuple<double, double> drawingOffset = GetDrawingOffset();
            double deltaX = drawingOffset.Item1;
            double deltaY = drawingOffset.Item2;
            _startDragCell.X = (int)((e.GetPosition(this).X / _cellWidth - deltaX));
            _startDragCell.Y = (int)((e.GetPosition(this).Y / _cellWidth) - deltaY);
            _dragging = true;
        }

        private void MapCanvas_MouseEnter(object sender, MouseEventArgs e)
        {
            if (_bot.Game is null) return;
            FloatingTip.IsOpen = true;
            _selectedRectangle.Visibility = Visibility.Visible;

            Tuple<double, double> drawingOffset = GetDrawingOffset();
            double deltaX = drawingOffset.Item1;
            double deltaY = drawingOffset.Item2;
            int ingameX = (int)((e.GetPosition(this).X / _cellWidth - deltaX));
            int ingameY = (int)((e.GetPosition(this).Y / _cellWidth) - deltaY);
            if (_dragging)
            {
                int xOffset = ingameX - (int)_startDragCell.X;
                int yOffset = ingameY - (int)_startDragCell.Y;

                if (xOffset >= 0)
                    Canvas.SetLeft(_selectedRectangle, (_startDragCell.X + deltaX) * _cellWidth);
                else
                    Canvas.SetLeft(_selectedRectangle, (ingameX + deltaX) * _cellWidth);

                if (yOffset >= 0)
                    Canvas.SetTop(_selectedRectangle, (_startDragCell.Y + deltaY) * _cellWidth);
                else
                    Canvas.SetTop(_selectedRectangle, (ingameY + deltaY) * _cellWidth);

                _selectedRectangle.Width = _cellWidth * (Math.Abs(xOffset) + 1);
                _selectedRectangle.Height = _cellWidth * (Math.Abs(yOffset) + 1);
            }
            else
            {
                Canvas.SetLeft(_selectedRectangle, (ingameX + deltaX) * _cellWidth);
                Canvas.SetTop(_selectedRectangle, (ingameY + deltaY) * _cellWidth);
            }

            lock (_bot)
            {
                if (_bot.Game != null && _bot.Game.IsMapLoaded)
                    RetrieveCellInfo(ingameX, ingameY);
                else
                    TipText.Text = $"Cell: ({ingameX},{ingameY})";
            }

            Point currentPos = e.GetPosition(MapCanvas);
            FloatingTip.HorizontalOffset = currentPos.X + 20;
            FloatingTip.VerticalOffset = currentPos.Y;
        }

        private void MapCanvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging)
                return;
            _dragging = false;
            Tuple<double, double> drawingOffset = GetDrawingOffset();
            double deltaX = drawingOffset.Item1;
            double deltaY = drawingOffset.Item2;
            int ingameX = (int)((e.GetPosition(this).X / _cellWidth - deltaX));
            int ingameY = (int)((e.GetPosition(this).Y / _cellWidth) - deltaY);
            _selectedRectangle.Width = _cellWidth;
            _selectedRectangle.Height = _cellWidth;
            Canvas.SetLeft(_selectedRectangle, (ingameX + deltaX) * _cellWidth);
            Canvas.SetTop(_selectedRectangle, (ingameY + deltaY) * _cellWidth);
            if ((int)_startDragCell.X == ingameX && (int)_startDragCell.Y == ingameY)
            {
                Clipboard.SetDataObject($"{ingameX}, {ingameY}");
#if DEBUG
                _bot.LogMessage($"({ingameX},{ingameY})Collider:{_bot.Game.Map.Colliders[ingameX, ingameY]},TilesType:{_bot.Game.Map.TileTypes[ingameX, ingameY]}," +
                    $"TilesType2:{_bot.Game.Map.TileTypes2[ingameX, ingameY]},TilesHeight:{_bot.Game.Map.TileHeight[ingameX, ingameY]}," +
                    $"TilesWater:{_bot.Game.Map.TileWater[ingameX,ingameY]},TileZones:{_bot.Game.Map.TileZones[ingameX, ingameY]} || ISAREALINK{_bot.Game.Map.IsAreaLink(ingameX, ingameY)}");
                var obj = _bot.Game.Map.Objects.Find(o => o.x == ingameX && o.z == -ingameY);
                if (obj != null)
                    _bot.LogMessage($"Object = Name:{obj.Name} ID:{obj.ID} Tag:{obj.tag}");
                var area = _bot.Game.Map.CheckArea(ingameX, ingameY);
                _bot.LogMessage($"AreaName:{area?.AreaName}StartX:{area?.StartX}StartY:{area?.StartY}EndX:{area?.EndX}EndY:{area?.EndY}");

#endif
            }
            else
            {
                int x = ingameX;
                int y = ingameY;
                int startX = (int)_startDragCell.X;
                int startY = (int)_startDragCell.Y;
                Clipboard.SetDataObject($"{Math.Min(x, startX)}, {Math.Min(y, startY)}, {Math.Max(x, startX)}, {Math.Max(y, startY)}");
            }
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_bot.Game is null) return;

            Tuple<double, double> drawingOffset = GetDrawingOffset();
            double deltaX = drawingOffset.Item1;
            double deltaY = drawingOffset.Item2;
            int ingameX = (int)((e.GetPosition(this).X / _cellWidth - deltaX));
            int ingameY = (int)((e.GetPosition(this).Y / _cellWidth) - deltaY);
            if (_lastDisplayedCell.X != ingameX || _lastDisplayedCell.Y != ingameY)
            {
                if (_dragging)
                {
                    int xOffset = ingameX - (int)_startDragCell.X;
                    int yOffset = ingameY - (int)_startDragCell.Y;

                    if (xOffset >= 0)
                        Canvas.SetLeft(_selectedRectangle, (_startDragCell.X + deltaX) * _cellWidth);
                    else
                        Canvas.SetLeft(_selectedRectangle, (ingameX + deltaX) * _cellWidth);

                    if (yOffset >= 0)
                        Canvas.SetTop(_selectedRectangle, (_startDragCell.Y + deltaY) * _cellWidth);
                    else
                        Canvas.SetTop(_selectedRectangle, (ingameY + deltaY) * _cellWidth);

                    _selectedRectangle.Width = _cellWidth * (Math.Abs(xOffset) + 1);
                    _selectedRectangle.Height = _cellWidth * (Math.Abs(yOffset) + 1);
                }
                else
                {
                    Canvas.SetLeft(_selectedRectangle, (ingameX + deltaX) * _cellWidth);
                    Canvas.SetTop(_selectedRectangle, (ingameY + deltaY) * _cellWidth);
                }

                lock (_bot)
                {
                    if (_bot.Game != null && _bot.Game.IsMapLoaded)
                        RetrieveCellInfo(ingameX, ingameY);
                    else
                        TipText.Text = $"Cell: ({ingameX},{ingameY})";
                }
            }

            Point currentPos = e.GetPosition(MapCanvas);
            FloatingTip.HorizontalOffset = currentPos.X + 20;
            FloatingTip.VerticalOffset = currentPos.Y;
        }

        private void MapCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            FloatingTip.IsOpen = false;
            _selectedRectangle.Visibility = Visibility.Hidden;
        }

        public void Client_MapLoaded(string mapName)
        {
            Dispatcher.InvokeAsync(delegate
            {
                RefreshMap();
            });
        }

        public void Client_AreaUpdated()
        {
            
        }

        public void Client_PositionUpdated(string map, int x, int y)
        {
            Dispatcher.InvokeAsync(delegate
            {
                RefreshPlayer(true);
            });
        }

        public void Client_NpcReceived(List<Npc> npcs)
        {
            Dispatcher.InvokeAsync(delegate
            {
                RefreshNpcs();
            });
        }

        public void Client_PlayerEnteredMap(PlayerInfos player)
        {
            Dispatcher.InvokeAsync(delegate
            {
                RefreshOtherPlayers();
            });
        }

        public void Client_PlayerLeftMap(PlayerInfos player)
        {
            Dispatcher.InvokeAsync(delegate
            {
                RefreshOtherPlayers();
            });
        }

        public void Client_PlayerMoved(PlayerInfos player)
        {
            Dispatcher.InvokeAsync(delegate
            {
                RefreshOtherPlayers();
            });
        }
    }
}
