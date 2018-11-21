using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Poke1Bot;

using Poke1Protocol;

namespace pokeone_plus
{
    /// <summary>
    /// Interaction logic for MapView.xaml
    /// </summary>
    public partial class MapView : UserControl
    {
        private readonly BotClient _bot;
        private readonly Dictionary<int, Color> _colliderColors;

        private int _cellWidth = 15;

        private bool _isMapDirty;

        private int _mapWidth;
        private int _mapHeight;

        private Point _playerPosition = new Point();

        private Point _startDragCell = new Point();
        private bool _dragging;

        private readonly Color _playerColor = Colors.Red;
        private readonly Color _otherPlayersColor = Colors.ForestGreen;
        private readonly Color _selectedRegionColor = Colors.Red;

        private WriteableBitmap _mapBmp;
        private Int32Rect _selectedRegion;

#if DEBUG
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private double _lastTime;
#endif

        public MapView(BotClient bot)
        {
            InitializeComponent();
            _bot = bot;
            _colliderColors = new Dictionary<int, Color>
            {
                { 0, Colors.White },
                { 2, Colors.LightSkyBlue },
                { 3, Colors.White },
                { 4, Colors.Gray },
                { 5, Colors.Gray },
                { 6, Colors.Gray },
                { 7, Colors.Gray },
                { 8, Colors.LightGreen },
                { 9, Colors.White },
                { 10, Colors.LightGray },
                { 11, Colors.White },
                { 12, Colors.White},
                { 13, Colors.WhiteSmoke },
                { 14, Colors.White },
                { 15, Colors.White },
                { 22, Colors.Wheat },
                { 24, Colors.White },
                { 25, Colors.White }
            };

            IsVisibleChanged += MapView_IsVisibleChanged;
            MouseDown += MapView_MouseDown;
            SizeChanged += MapView_SizeChanged;

            RenderOptions.SetBitmapScalingMode(MapImage, BitmapScalingMode.NearestNeighbor);
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            lock (_bot)
            {
                if (_bot.Game?.Map == null)
                    return;
            }

            Draw();
#if DEBUG
            double timeNow = _stopwatch.ElapsedMilliseconds;
            double elapsedMilliseconds = timeNow - _lastTime;
            FpsCounter.Text = string.Format("FPS: {0:0.0}", 1000.0 / elapsedMilliseconds);
            _lastTime = timeNow;
#endif
        }

        // completely redraw and resize the map (if needed)
        private void ResetMap()
        {
            MapImage.Source = null;
            _mapBmp = null;
            _isMapDirty = true;
        }

        private void Draw()
        {
            if (_mapBmp == null)
            {
                GetCamPosPixel(out int camX1, out int camY1, out int camX2, out int camY2);
                MapImage.Source = _mapBmp = BitmapFactory.New(camX2, camY2);
            }

            if (!_isMapDirty || _bot.Game is null || !_bot.Game.IsMapLoaded)
                return;

            using (_mapBmp.GetBitmapContext())
            {
                _mapBmp.Clear(Colors.White);
                DrawMap();
                DrawNpcs();
                DrawOtherPlayers();
                DrawPlayer();
                DrawSelection();
            }
            _isMapDirty = false;
        }

        private void DrawSelection()
        {
            if (!_selectedRegion.HasArea)
                return;

            _mapBmp.DrawRectangle(_selectedRegion.X,
                                  _selectedRegion.Y,
                                  _selectedRegion.X + _selectedRegion.Width,
                                  _selectedRegion.Y + _selectedRegion.Height,
                                  _selectedRegionColor);
        }

        private void DrawMap()
        {
            lock (_bot)
            {
                _playerPosition = new Point(_bot.Game.PlayerX, _bot.Game.PlayerY);
                var map = _bot.Game.Map;

                GetCamPosIngame(out int camX1, out int camY1, out int camX2, out int camY2);

                // if less than max dimension, draw one extra cell so cells that are part-way
                // inside the cam are drawn too
                if (camX2 < _mapWidth) camX2++;
                if (camY2 < _mapHeight) camY2++;

                int rectX = 0;
                int rectY = 0;
                int rectWidth = 0;
                int rectHeight = 0;

                // Draw colliders
                for (int y = camY1; y < camY2; ++y, rectY += _cellWidth, rectX = 0)
                    for (int x = camX1; x < camX2; ++x, rectX += _cellWidth)
                    {
                        var modifiedX = -1;
                        var modifiedY = -1;

                        Color rectColor;
                        int collider = map.GetCollider(x, y);
                        if (map.HasLink(x, y))
                            rectColor = Colors.Gold;
                        else if (_colliderColors.ContainsKey(collider))
                            rectColor = _colliderColors[collider];
                        else
                            rectColor = Colors.Black;

                        rectWidth = _cellWidth;
                        rectHeight = _cellWidth;
                        if ((collider == 4 || collider == 22 || collider == 7) && !_bot.Game.Map.HasLink(x, y))
                        {
                            rectHeight = _cellWidth / 4;
                            modifiedY = _cellWidth;
                            rectY += modifiedY;
                            //rect.VerticalAlignment = VerticalAlignment.Top;
                        }

                        if (collider == 6 || collider == 5)
                        {
                            rectWidth = _cellWidth / 4;
                            if (collider == 6)
                            {
                                modifiedX = _cellWidth;
                                rectX += modifiedX;
                            }
                        }

                        if (collider == 19 || collider == 20)
                        {
                            rectWidth = _cellWidth / 4;
                            if (collider == 20)
                            {
                                modifiedX = _cellWidth;
                                rectX += modifiedX;
                            }
                            //rect.HorizontalAlignment = collider == 19 ? HorizontalAlignment.Left : HorizontalAlignment.Right;
                        }

                        if (map.GetCellSideMoveable(collider))
                        {
                            rectColor = Colors.Wheat;
                            rectHeight = _cellWidth / 4;
                            if (collider == 16)
                            {
                                //drawing to rectangles
                                _mapBmp.FillRectangle(rectX, rectY, rectX + rectWidth, rectY + rectHeight, rectColor);
                                _mapBmp.FillRectangle(rectX, rectY, rectX + (rectWidth / 4), rectY + _cellWidth, rectColor);
                                continue;
                            }
                            if (collider == 18)
                            {
                                //drawing to rectangles
                                _mapBmp.FillRectangle(rectX, rectY, rectX + rectWidth, rectY + rectHeight, rectColor);
                                _mapBmp.FillRectangle(rectX + (_cellWidth - (rectWidth / 4)), rectY, rectX + (rectWidth / 4) + (_cellWidth - (rectWidth / 4)), rectY + _cellWidth, rectColor);
                                continue;
                            }
                            //rect.VerticalAlignment = VerticalAlignment.Top;
                        }

                        if (map.IsGoingToSlide(collider))
                        {
                            rectColor = Colors.MediumPurple;
                        }

                        if (map.IsGrass(x, y))
                        {
                            rectColor = Colors.LightGreen;
                            if (map.GetCollider(x - 1 , y) == 6)
                            {
                                rectWidth = _cellWidth - (_cellWidth / 4);
                                modifiedX = _cellWidth / 4;
                                rectX += modifiedX;
                            }
                        }
                        if (map.IsCutTree(x, y)) rectColor = Colors.DarkGreen;
                        if (map.IsRockSmash(x, y)) rectColor = Colors.SandyBrown;

                        if (rectColor != Colors.White)
                        {
                            _mapBmp.FillRectangle(rectX, rectY, rectX + rectWidth, rectY + rectHeight, rectColor);
                            if (modifiedX != -1)
                            {
                                rectX -= modifiedX;
                            }
                            if (modifiedY != -1)
                            {
                                rectY -= modifiedY;
                            }
                        }
                    }
            }
        }

        private void DrawPlayer()
        {
            GetDrawingOffset(out double deltaX, out double deltaY);

            var pX = ((int)_playerPosition.X + (int)deltaX) * _cellWidth;
            var pY = ((int)_playerPosition.Y + (int)deltaY) * _cellWidth;
            _mapBmp.FillEllipse(pX, pY, pX + _cellWidth, pY + _cellWidth, _playerColor);
        }

        private void GetCamPosIngame(out int camX1, out int camY1, out int camX2, out int camY2)
        {
            GetDrawingOffset(out double deltaX, out double deltaY);
            camX1 = (int)-deltaX;
            camY1 = (int)-deltaY;
            camX2 = Math.Min(camX1 + ((int)ActualWidth / _cellWidth), camX1 + _mapWidth);
            camY2 = Math.Min(camY1 + ((int)ActualHeight / _cellWidth), camY1 + _mapHeight);
        }

        private void GetCamPosPixel(out int camX1, out int camY1, out int camX2, out int camY2)
        {
            GetDrawingOffset(out double deltaX, out double deltaY);
            camX1 = (int)-deltaX * _cellWidth;
            camY1 = (int)-deltaY * _cellWidth;
            camX2 = Math.Min(camX1 + (int)ActualWidth, (_mapWidth + camX1) * _cellWidth);
            camY2 = Math.Min(camY1 + (int)ActualHeight, (_mapHeight + camY1) * _cellWidth);
        }

        private void DrawOtherPlayers()
        {
            lock (_bot)
            {
                GetDrawingOffset(out double deltaX, out double deltaY);
                GetCamPosIngame(out int camX1, out int camY1, out int camX2, out int camY2);

                foreach (PlayerInfos player in _bot.Game.Players.Values)
                {
                    if (player.PosX < camX1 || player.PosX > camX2 || player.PosY < camY1 || player.PosY > camY2)
                        continue;

                    int pX = (player.PosX + (int)deltaX) * _cellWidth;
                    int pY = (player.PosY + (int)deltaY) * _cellWidth;
                    _mapBmp.FillEllipse(pX, pY, pX + _cellWidth, pY + _cellWidth, _otherPlayersColor);
                }
            }
        }

        private void DrawNpcs()
        {
            lock (_bot)
            {
                GetDrawingOffset(out double deltaX, out double deltaY);

                var map = _bot.Game.Map;
                int camX1 = (int)-deltaX;
                int camY1 = (int)-deltaY;
                int camX2 = camX1 + (int)ActualWidth;
                int camY2 = camY1 + (int)ActualHeight;

                foreach (var npc in map.Npcs)
                {
                    if (npc.PositionX < camX1 || npc.PositionX > camX2 || npc.PositionY < camY1 || npc.PositionY > camY2)
                        continue;

                    Color color;
                    if (map.IsCutTree(npc.PositionX, npc.PositionY))
                        color = Colors.DarkGreen;
                    else if (map.IsRockSmash(npc.PositionX, npc.PositionY))
                        color = Colors.SandyBrown;
                    else if (map.IsPc(npc.PositionX, npc.PositionY))
                        color = Colors.DarkGray;
                    else
                        color = Colors.DarkOrange;

                    var npcX = (npc.PositionX + (int)deltaX) * _cellWidth;
                    var npcY = (npc.PositionY + (int)deltaY) * _cellWidth;
                    _mapBmp.FillEllipse(npcX, npcY, npcX + _cellWidth, npcY + _cellWidth, color);
                }
            }
        }

        private void MapView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Window parent = Window.GetWindow(this);

            if (parent is null) return;

            if (IsVisible)
            {
                CompositionTarget.Rendering += CompositionTarget_Rendering;
                parent.KeyDown += Parent_KeyDown;
            }
            else
            {
                CompositionTarget.Rendering -= CompositionTarget_Rendering;
                parent.KeyDown -= Parent_KeyDown;
            }
        }

        private void MapView_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Keyboard.Focus(this);
        }

        private void MapView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ResetMap();
        }

        private void Parent_KeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
#if DEBUG
            Console.WriteLine(e.Key.ToString());
#endif
            if (e.Key == Key.Add || e.Key == Key.OemPlus)
            {
                _cellWidth += 5;
                if (_cellWidth > 50) _cellWidth = 50;

                ResetMap();
            }
            else if (e.Key == Key.Subtract || e.Key == Key.OemMinus)
            {
                _cellWidth -= 5;
                if (_cellWidth < 5) _cellWidth = 5;

                ResetMap();
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
                    _bot.Game.AreNpcReceived &&
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

        private void GetDrawingOffset(out double deltaX, out double deltaY)
        {
            double canFillX = Math.Floor(ActualWidth / _cellWidth);
            double canFillY = Math.Floor(ActualHeight / _cellWidth);

            deltaX = -_playerPosition.X + canFillX / 2;
            deltaY = -_playerPosition.Y + canFillY / 2;

            if (_mapWidth <= canFillX) deltaX = 0;
            if (_mapHeight <= canFillY) deltaY = 0;

            if (deltaX < -_mapWidth + canFillX) deltaX = -_mapWidth + canFillX;
            if (deltaY < -_mapHeight + canFillY) deltaY = -_mapHeight + canFillY;

            if (deltaX > 0) deltaX = 0;
            if (deltaY > 0) deltaY = 0;
        }

        private void MapImage_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            lock (_bot)
            {
                if (_bot.Game?.Map != null
                    && _bot.Game.IsMapLoaded
                    && _bot.Game.AreNpcReceived
                    && _bot.Game.IsInactive
                    && !_bot.Game.IsInBattle
                    && _bot.Running != BotClient.State.Started)
                {
                    GetDrawingOffset(out double deltaX, out double deltaY);
                    int ingameX = (int)((e.GetPosition(this).X / _cellWidth) - deltaX);
                    int ingameY = (int)((e.GetPosition(this).Y / _cellWidth) - deltaY);

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

        private void MapImage_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            GetDrawingOffset(out double deltaX, out double deltaY);

            _startDragCell.X = (int)((e.GetPosition(this).X / _cellWidth) - deltaX);
            _startDragCell.Y = (int)((e.GetPosition(this).Y / _cellWidth) - deltaY);
            _dragging = true;
        }

        private void MapImage_MouseEnter(object sender, MouseEventArgs e)
        {
            lock (_bot)
            {
                if (_bot.Game?.Map == null)
                    return;
            }

            FloatingTip.IsOpen = true;

            GetDrawingOffset(out double deltaX, out double deltaY);

            int ingameX = (int)((e.GetPosition(this).X / _cellWidth) - deltaX);
            int ingameY = (int)((e.GetPosition(this).Y / _cellWidth) - deltaY);
            if (_dragging)
            {
                int xOffset = ingameX - (int)_startDragCell.X;
                int yOffset = ingameY - (int)_startDragCell.Y;

                if (xOffset >= 0)
                    _selectedRegion.X = ((int)_startDragCell.X + (int)deltaX) * _cellWidth;
                else
                    _selectedRegion.X = (ingameX + (int)deltaX) * _cellWidth;

                if (yOffset >= 0)
                    _selectedRegion.Y = ((int)_startDragCell.Y + (int)deltaY) * _cellWidth;
                else
                    _selectedRegion.Y = (ingameY + (int)deltaY) * _cellWidth;

                _selectedRegion.Width = _cellWidth * (Math.Abs(xOffset) + 1);
                _selectedRegion.Height = _cellWidth * (Math.Abs(yOffset) + 1);
            }
            else
            {
                _selectedRegion.X = (ingameX + (int)deltaX) * _cellWidth;
                _selectedRegion.Y = (ingameY + (int)deltaY) * _cellWidth;
                _selectedRegion.Width = _cellWidth - 1;
                _selectedRegion.Height = _cellWidth - 1;
            }

            lock (_bot)
            {
                if (_bot.Game != null && _bot.Game.IsMapLoaded)
                    RetrieveCellInfo(ingameX, ingameY);
                else
                    TipText.Text = $"Cell: ({ingameX},{ingameY})";
            }

            _isMapDirty = true; // Redraw the selection region

            Point currentPos = e.GetPosition(MapImage);
            FloatingTip.HorizontalOffset = currentPos.X + 20;
            FloatingTip.VerticalOffset = currentPos.Y;
        }

        private void MapImage_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _selectedRegion = Int32Rect.Empty; // Reset selection
            _isMapDirty = true;

            if (!_dragging)
                return;
            _dragging = false;
            GetDrawingOffset(out double deltaX, out double deltaY);
            int ingameX = (int)((e.GetPosition(this).X / _cellWidth) - deltaX);
            int ingameY = (int)((e.GetPosition(this).Y / _cellWidth) - deltaY);
            //_selectedRectangle.Width = _cellWidth;
            //_selectedRectangle.Height = _cellWidth;
            //Canvas.SetLeft(_selectedRectangle, (ingameX + deltaX) * _cellWidth);
            //Canvas.SetTop(_selectedRectangle, (ingameY + deltaY) * _cellWidth);
            if ((int)_startDragCell.X == ingameX && (int)_startDragCell.Y == ingameY)
            {
                Clipboard.SetDataObject($"{ingameX}, {ingameY}");
#if DEBUG
                lock (_bot)
                {
                    _bot.LogMessage($"({ingameX},{ingameY})Collider:{_bot.Game.Map.Colliders[ingameX, ingameY]},TilesType:{_bot.Game.Map.TileTypes[ingameX, ingameY]}," +
                        $"TilesType2:{_bot.Game.Map.TileTypes2[ingameX, ingameY]},TilesHeight:{_bot.Game.Map.TileHeight[ingameX, ingameY]}," +
                        $"TilesWater:{_bot.Game.Map.TileWater[ingameX, ingameY]},TileZones:{_bot.Game.Map.TileZones[ingameX, ingameY]} || ISAREALINK{_bot.Game.Map.IsAreaLink(ingameX, ingameY)}");
                    var obj = _bot.Game.Map.Objects.Find(o => o.x == ingameX && o.z == -ingameY);
                    if (obj != null)
                        _bot.LogMessage($"Object = Name:{obj.Name} ID:{obj.ID} Tag:{obj.tag}");
                    var area = _bot.Game.Map.CheckArea(ingameX, ingameY);
                    _bot.LogMessage($"AreaName:{area?.AreaName}StartX:{area?.StartX}StartY:{area?.StartY}EndX:{area?.EndX}EndY:{area?.EndY}");
                    var npc = _bot.Game.Map.Npcs.Find(n => n.PositionX == ingameX && n.PositionY == ingameY);
                    if (npc != null)
                        _bot.LogMessage($"NPC Data({ingameX},{ingameY}): SightAction:{npc.Data.Settings.SightAction} Facing:{npc.Data.Settings.Facing} LOS:{npc.LosLength}");
                }
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

        private void MapImage_MouseMove(object sender, MouseEventArgs e)
        {
            lock (_bot)
            {
                if (_bot.Game?.Map == null)
                    return;
            }

            GetDrawingOffset(out double deltaX, out double deltaY);

            int ingameX = (int)((e.GetPosition(this).X / _cellWidth) - (int)deltaX);
            int ingameY = (int)((e.GetPosition(this).Y / _cellWidth) - (int)deltaY);
            if (_selectedRegion.X != ingameX || _selectedRegion.Y != ingameY)
            {
                if (_dragging)
                {
                    int xOffset = ingameX - (int)_startDragCell.X;
                    int yOffset = ingameY - (int)_startDragCell.Y;

                    if (xOffset >= 0)
                        _selectedRegion.X = ((int)_startDragCell.X + (int)deltaX) * _cellWidth;
                    else
                        _selectedRegion.X = (ingameX + (int)deltaX) * _cellWidth;

                    if (yOffset >= 0)
                        _selectedRegion.Y = ((int)_startDragCell.Y + (int)deltaY) * _cellWidth;
                    else
                        _selectedRegion.Y = (ingameY + (int)deltaY) * _cellWidth;

                    _selectedRegion.Width = _cellWidth * (Math.Abs(xOffset) + 1);
                    _selectedRegion.Height = _cellWidth * (Math.Abs(yOffset) + 1);
                }
                else
                {
                    _selectedRegion.X = (ingameX + (int)deltaX) * _cellWidth;
                    _selectedRegion.Y = (ingameY + (int)deltaY) * _cellWidth;
                    _selectedRegion.Width = _cellWidth - 1;
                    _selectedRegion.Height = _cellWidth - 1;
                }

                lock (_bot)
                {
                    if (_bot.Game != null && _bot.Game.IsMapLoaded)
                        RetrieveCellInfo(ingameX, ingameY);
                    else
                        TipText.Text = $"Cell: ({ingameX},{ingameY})";
                }
                _isMapDirty = true; // Redraw the selection region
            }

            Point currentPos = e.GetPosition(MapImage);
            FloatingTip.HorizontalOffset = currentPos.X + 20;
            FloatingTip.VerticalOffset = currentPos.Y;
        }

        private void MapImage_MouseLeave(object sender, MouseEventArgs e)
        {
            FloatingTip.IsOpen = false;
            _selectedRegion = Int32Rect.Empty;
            _isMapDirty = true; // Redraw the selection region
        }

        public void Client_MapLoaded(string mapName)
        {
            lock (_bot)
            {
                // just to make things simpler
                _mapHeight = _bot.Game.Map.Height;
                _mapWidth = _bot.Game.Map.Width;
            }
            Dispatcher.InvokeAsync(delegate
            {
                ResetMap();
            });
        }

        public void Client_AreaUpdated()
        {

        }

        public void Client_PositionUpdated(string map, int x, int y)
        {
            _isMapDirty = true;
        }

        public void Client_NpcReceived(List<Npc> npcs)
        {
            _isMapDirty = true;
        }

        public void Client_PlayerEnteredMap(PlayerInfos player)
        {
            _isMapDirty = true;
        }

        public void Client_PlayerLeftMap(PlayerInfos player)
        {
            _isMapDirty = true;
        }

        public void Client_PlayerMoved(PlayerInfos player)
        {
            _isMapDirty = true;
        }
    }
}