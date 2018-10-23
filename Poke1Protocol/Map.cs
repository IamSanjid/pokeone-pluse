using MAPAPI.Response;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Poke1Protocol
{
    public class Map
    {
        public event Action<List<Npc>> NpcReceieved;
        public event Action AreaUpdated;
        public enum MoveResult
        {
            Success,
            Fail,
            Jump,
            NoLongerSurfing,
            OnGround,
            NoLongerOnGround,
            Sliding,
            Icing
        }

        private readonly Dictionary<int, int> SliderValues = new Dictionary<int, int>
        {
            { 26, 0 },
            { 27, 1 },
            { 28, 2 },
            { 29, 3 }
        };

        private readonly Dictionary<int, bool> Sides = new Dictionary<int, bool>
        {
            { 16, true },
            { 17, true },
            { 18, true }
        };
        public List<LINKData> Links { get; }
        public MAPAPI.Response.MapDump MapDump { get; }
        public int[,] Colliders { get; }
        public int[,] TileTypes { get; }
        public int[,] TileTypes2 { get; }
        public int[,] TileHeight { get; }
        public int[,] TileWater { get; }
        public int[,] TileZones { get; }
        public List<MAPAPI.Response.MapObjectStruct> Objects { get; }
        public int DimensionX { get; private set; } = -1;
        public int DimensionY { get; private set; } = -1;
        public HeightTilesStruct[,] WallData { get; }
        public string MapWeather { get; }
        public int Width { get; }
        public int Height { get; }
        public bool IsOutside { get; }
        public string Region { get; }
        public bool IsSessioned { get; }
        public List<Npc> OriginalNpcs { get; }
        public List<Npc> Npcs { get; }
        public Area CurrentArea { get; set; }
        private readonly GameClient _client;
        public Map(byte[] content, bool isSessioned, GameClient client)
        {
            _client = client;
            MapDump = MAPAPI.Response.MapDump.Deserialize(content);
            Height = MapDump.height;
            Width = MapDump.width;
            MapWeather = MapDump.Settings.Weather;
            IsOutside = MapDump.Settings.CanMount;
            TileTypes = (int[,])MapDump.TileTypes.ToArray();
            TileTypes2 = (int[,])MapDump.TileTypes2.ToArray();
            TileHeight = (int[,])MapDump.TileHeights.ToArray();
            TileWater = (int[,])MapDump.TileWater.ToArray();
            Colliders = (int[,])MapDump.Colliders.ToArray();
            TileZones = (int[,])MapDump.TileZones.ToArray();
            WallData = (HeightTilesStruct[,])MapDump.WallData.ToArray();
            Objects = MapDump.Objects;
            Region = MapDump.Settings.Region;
            IsSessioned = isSessioned;
            Npcs = new List<Npc>();
            OriginalNpcs = new List<Npc>();
            if (MapDump.NPCs != null && MapDump?.NPCs.Count > 0)
            {
                foreach (var npc in MapDump.NPCs)
                {
                    OriginalNpcs.Add(new Npc(npc));
                }
                NpcReceieved?.Invoke(OriginalNpcs);
            }
            Links = MapDump.Links;

            if (_client.IsLoggedIn)
            {
                if (MapDump.Areas != null && MapDump.Areas.Count > 1)
                    foreach (MAPAPI.Response.Area area in MapDump.Areas)
                    {
                        if (_client.PlayerX >= area.StartX && _client.PlayerX <= area.EndX && _client.PlayerY >= area.StartY && _client.PlayerY <= area.EndY)
                        {
                                CurrentArea = area;
                                DimensionX = area.EndX;
                                DimensionY = area.EndY;
                                break;
                        }
                    }
                if (CurrentArea is null)
                {
                    DimensionX = Width;
                    DimensionY = Height;

                    if (MapDump.Areas.Count == 1)
                    {
                        CurrentArea = MapDump.Areas.FirstOrDefault();
                        if (CurrentArea != null)
                        {
                            CurrentArea.EndX = Width;
                            CurrentArea.EndY = Height;
                            AreaUpdated?.Invoke();
                            return;
                        }
                    }

                    CurrentArea = new Area
                    {
                        AreaName = MapDump.Settings.MapName,
                        EndX = Width,
                        EndY = Height,
                        StartX = 0,
                        StartY = 0
                    };
                }
            }            
        }

        public void UpdateArea()
        {
            if (_client.IsLoggedIn)
            {
                if (MapDump.Areas != null && MapDump.Areas.Count > 1)
                    foreach (MAPAPI.Response.Area area in MapDump.Areas)
                    {
                        if (_client.PlayerX >= area.StartX && _client.PlayerX <= area.EndX && _client.PlayerY >= area.StartY && _client.PlayerY <= area.EndY)
                        {
                            if (CurrentArea.AreaName.ToLowerInvariant() != area.AreaName.ToLowerInvariant())
                            {
                                AreaUpdated?.Invoke();
                                CurrentArea = area;
                                DimensionX = area.EndX;
                                DimensionY = area.EndY;
                            }
                            break;
                        }
                    }
                if (CurrentArea is null)
                {
                    DimensionX = Width;
                    DimensionY = Height;

                    if (MapDump.Areas.Count == 1)
                    {
                        CurrentArea = MapDump.Areas.FirstOrDefault();
                        if (CurrentArea != null)
                        {
                            CurrentArea.EndX = Width;
                            CurrentArea.EndY = Height;
                            AreaUpdated?.Invoke();
                            return;
                        }
                    }

                    CurrentArea = new Area
                    {
                        AreaName = MapDump.Settings.MapName,
                        EndX = Width,
                        EndY = Height,
                        StartX = 0,
                        StartY = 0
                    };
                    AreaUpdated?.Invoke();
                }                
            }
        }

        public bool IsAreaLink(int x, int y)
        {
            var area = CheckArea(x, y);
            var currentArea = CurrentArea;
            if (area?.AreaName.ToLowerInvariant() != CurrentArea?.AreaName.ToLowerInvariant())
            {
                if (area != null && currentArea != null)
                {
                    var isHori = currentArea.StartX > area.EndX || area.StartX > currentArea.EndX; // Horizontal
                    var isVerti = currentArea.EndY < area.StartY || currentArea.StartY > area.EndY; // Vertical
                    if (Colliders[x, y] == 0)
                    {
                        if ((isHori && (x == area.EndX || x == area.StartX)) || (isVerti && (y == area.StartY || y == area.EndY)))
                            return true;
                        else if ((currentArea.StartX == area.EndX && (x == area.EndX))
                            || (currentArea.StartY == area.EndY && (y == area.EndY)))
                            return true;
                        else if ((currentArea.EndX == area.StartX && (x == area.StartX))
                            || (currentArea.EndY == area.StartY && (y == area.StartY)))
                            return true;
                        else if ((currentArea.StartX == area.EndX && (x - 1 == area.EndX || x + 1 == area.EndX)) 
                            || (currentArea.StartY == area.EndY && (y - 1 == area.EndY || y + 1 == area.EndY)))
                            return true;
                        else if ((currentArea.EndX == area.StartX && (x - 1 == area.StartX || x + 1 == area.StartX))
                            || (currentArea.EndY == area.StartY && (y + 1 == area.StartY || y - 1 == area.StartY)))
                            return true;
                    }
                }
            }
            return false;
        }

        public int GetPerfectDimensionX(int dimX)
        {
            if (Width > dimX)
            {
                var eq = Width - dimX;

                if (MapDump.Areas.Count > 1)
                {
                    var LastMap = MapDump.Areas.LastOrDefault();
                    if (LastMap?.AreaName.ToLowerInvariant() == CurrentArea?.AreaName.ToLowerInvariant())
                    {
                        return dimX + eq;
                    }
                    else
                    {
                        int x = dimX;
                        while (!IsAreaLink(x, DimensionY) && Width > x)
                        {
                            x++;
                        }
                        eq = x - dimX;
                        return eq + dimX;
                    }
                }
                else
                {
                    return dimX + eq;
                }
            }
            return dimX;
        }

        public int GetPerfectDimensionY(int dimY)
        {
            if (Height > dimY)
            {
                var eq = Height - dimY;

                if (MapDump.Areas.Count > 1)
                {
                    var LastMap = MapDump.Areas.LastOrDefault();
                    if (LastMap?.AreaName.ToLowerInvariant() == CurrentArea?.AreaName.ToLowerInvariant())
                    {
                        return dimY + eq;
                    }
                    else
                    {
                        int y = dimY;
                        while (!IsAreaLink(DimensionX, y) && Height > y)
                        {
                            y++;
                        }
                        eq = y - dimY;
                        return eq + dimY;
                    }
                }
                else
                {
                    return dimY + eq;
                }
            }
            return dimY;
        }

        public int GetPerfectDimensionX()
        {
            if (Width > DimensionX)
            {
                var eq = Width - DimensionX;
                
                if (MapDump.Areas.Count > 1)
                {
                    var LastMap = MapDump.Areas.LastOrDefault();
                    if (LastMap?.AreaName.ToLowerInvariant() == CurrentArea?.AreaName.ToLowerInvariant())
                    {
                        return DimensionX + eq;
                    }
                    else
                    {
                        int x = DimensionX;
                        while (!IsAreaLink(x, DimensionY) && Width > x)
                        {
                            x++;
                        }
                        eq = x - DimensionX;
                        return eq + DimensionX;
                    }
                }
                else
                {
                    return DimensionX + eq;
                }             
            }
            return DimensionX;
        }

        public int GetPerfectDimensionY()
        {
            if (Height > DimensionY)
            {
                var eq = Height - DimensionY;

                if (MapDump.Areas.Count > 1)
                {
                    var LastMap = MapDump.Areas.LastOrDefault();
                    if (LastMap?.AreaName.ToLowerInvariant() == CurrentArea?.AreaName.ToLowerInvariant())
                    {
                        return DimensionY + eq;
                    }
                    else
                    {
                        int y = DimensionY;
                        while (!IsAreaLink(DimensionX, y) && Height > y)
                        {
                            y++;
                        }
                        eq = y - DimensionY;
                        return eq + DimensionY;
                    }
                }
                else
                {
                    return DimensionY + eq;
                }
            }
            return DimensionY;
        }

        public Area CheckArea(int x, int y)
        {
            if (_client.IsLoggedIn)
            {
                if (MapDump.Areas != null && MapDump.Areas.Count > 0)
                    foreach (MAPAPI.Response.Area area in MapDump.Areas)
                    {
                        if (x >= area.StartX && x <= area.EndX && y >= area.StartY && y <= area.EndY)
                        {
                            return area;
                        }
                    }
            }
            return null;
        }

        public bool IsInCurrentArea(int x, int y)
        {
            return (x >= CurrentArea.StartX && x <= CurrentArea.EndX && y >= CurrentArea.StartY && y <= CurrentArea.EndY) || (x >= 0 && x < Width && y >= 0 && y < Height);
        }

        public int GetCollider(int x, int y)
        {
            if (IsInCurrentArea(x, y))
            {
                return Colliders[x, y];
            }
            return -1;
        }
        public bool HasLink(int x, int y)
        {
            if (!IsInCurrentArea(x, y)) return false;
            return Links.Any(l => l.x == x && l.z == -y && l.DestinationID != Guid.Empty);
        }
        public bool CanInteract(int playerX, int playerY, int npcX, int npcY)
        {
            int distance = GameClient.DistanceBetween(playerX, playerY, npcX, npcY);
            if (distance != 1) return false;

            if (IsRockSmash(npcX, npcY) || IsCutTree(npcX, npcY)) return true;

            int playerCollider = GetCollider(playerX, playerY);
            int npcCollider = GetCollider(npcX, npcY);
            if ((playerCollider == 25 || playerCollider == 26 || playerCollider == 27 ||
                playerCollider == 28 || playerCollider == 29 || playerCollider == 0 ||
                playerCollider == 24 || playerCollider == 10 || playerCollider == 3 ||
                playerCollider == 2 || playerCollider == 6 || playerCollider == 5 ||
                playerCollider == 7 || playerCollider == 4 || playerCollider == 0 || (IsWater(playerX, playerY)
                && IsWater(npcX, npcY))))
            {
                return true;
            }
            return false;
        }
        public bool CanSurf(int positionX, int positionY, bool isOnGround)
        {
            int currentCollider = GetCollider(positionX, positionY);
            int collider = GetCollider(positionX, positionY - 1);
            if ((collider == 2 || collider == 15 || collider == 31 || collider == 32 || collider == 33 || collider == 34) && isOnGround)
            {
                return true;
            }

            collider = GetCollider(positionX, positionY + 1);
            if ((collider == 2 || collider == 15 || collider == 31 || collider == 32 || collider == 33 || collider == 34) && isOnGround)
            {
                return true;
            }

            collider = GetCollider(positionX - 1, positionY);
            if ((collider == 2 || collider == 15 || collider == 31 || collider == 32 || collider == 33 || collider == 34) && isOnGround)
            {
                return true;
            }

            collider = GetCollider(positionX + 1, positionY);
            if ((collider == 2 || collider == 15 || collider == 31 || collider == 32 || collider == 33 || collider == 34) && isOnGround)
            {
                return true;
            }

            return false;
        }
        public bool IsWater(int x, int y)
        {
            if (!IsInCurrentArea(x, y)) return false;
            return Colliders[x, y] == 2 || Colliders[x, y] == 15;
        }

        public bool IsGrass(int x, int y)
        {
            if (!IsInCurrentArea(x, y)) return false;
            return TileTypes[x, y] == 8 && !HasLink(x, y) && TileTypes2[x, y] <= 0
                    && (TileZones[x, y] == 6 || TileZones[x, y] == 1 || TileZones[x, y] == 2)
                    && GetCollider(x, y) <= 0;
        }

        public bool IsPc(int x, int y)
        {
            if (!IsInCurrentArea(x, y)) return false;
            return (GetCollider(x, y + 1) == 20 || GetCollider(x, y + 1) == 19
                    || Objects.Any(ob => ob.x == x && ob.z == -y && ob.Name.StartsWith("Shadow_Back")))
                    && OriginalNpcs.Any(n => n.PositionX == x && n.PositionY == y && n.NpcName.ToLowerInvariant().StartsWith("new"));
        }

        public bool IsNormalGround (int x, int y)
        {
            if (!IsInCurrentArea(x, y)) return false;
            return TileZones[x, y] == 0
                && !HasLink(x, y)
                && !IsGrass(x, y) && !IsWater(x, y);
        }

        public bool IsRockSmash(int x, int y)
        {
            if (!IsInCurrentArea(x, y)) return false;
            if (OriginalNpcs.Find(s => s.PositionX == x && s.PositionY == y
                   && s.NpcName.ToLowerInvariant().StartsWith(".rocksmash") && s.Data.Settings.Sprite == 11) != null)
                return true;
            return false;
        }

        public bool IsCutTree(int x, int y)
        {
            if (!IsInCurrentArea(x, y)) return false;
            return OriginalNpcs.Find(s => s.PositionX == x && s.PositionY == y
                && s.NpcName.ToLowerInvariant().StartsWith(".cut") && s.Data.Settings.Sprite == 9) != null;
        }

        public MoveResult CanMove(Direction direction, int destinationX, int destinationY, bool isOnGround, bool isSurfing, bool canUseCut, bool canUseSmashRock)
        {
            var newArea = CheckArea(destinationX, destinationY);
            if ((destinationX < 0 || destinationX > DimensionX
                || destinationY < 0 || destinationY > DimensionY) && !HasLink(destinationX, destinationY)
                && newArea?.AreaName.ToLowerInvariant() == CurrentArea?.AreaName.ToLowerInvariant())
            {
                return MoveResult.Fail;
            }

            if (OriginalNpcs.Any(npc => npc.PositionX == destinationX && npc.PositionY == destinationY && !npc.IsMoving && npc.CanBlockPlayer && npc.IsVisible))
                return MoveResult.Fail;

            if (direction != Direction.Down && GetCollider(destinationX, destinationY) == 4)
            {
                return MoveResult.Fail;
            }

            if (IsUnmoveableCellSide(direction, destinationX, destinationY))
                return MoveResult.Fail;

            
            int collider = GetCollider(destinationX, destinationY);

            if (!IsMovementValid(direction, collider, isOnGround, isSurfing, canUseCut, canUseSmashRock, destinationX, destinationY))
            {
                return MoveResult.Fail;
            }

            if (collider == 4)
            {
                return MoveResult.Jump;
            }

            if (isSurfing && !IsWater(destinationX, destinationY))
                return MoveResult.NoLongerSurfing;


            return MoveResult.Success;
        }

        public bool IsUnmoveableCellSide(Direction direction, int destinationX, int destinationY)
        {
            if (direction == Direction.Right)
                return (GetCellSideMoveable(GetCollider(destinationX - 1, destinationY)) || GetCellSideMoveable(GetCollider(destinationX, destinationY)));
            if (direction == Direction.Left)
                return (GetCellSideMoveable(GetCollider(destinationX + 1, destinationY)) || GetCellSideMoveable(GetCollider(destinationX, destinationY)));
            if (direction == Direction.Up)
                return (GetCellSideMoveable(GetCollider(destinationX, destinationY + 1)));
            if (direction == Direction.Down)
                return (GetCellSideMoveable(GetCollider(destinationX, destinationY)));
            return false;
        }

        public bool GetCellSideMoveable(int collider)
        {
            if (Sides.ContainsKey(collider))
                return Sides[collider];
            return false;
        }

        public bool ApplyMovement(Direction direction, MoveResult result, ref int destinationX, ref int destinationY, ref bool isOnGround, ref bool isSurfing)
        {
            bool success = false;
            switch (result)
            {
                case MoveResult.Success:
                    success = true;
                    break;
                case MoveResult.Jump:
                    success = true;
                    switch (direction)
                    {
                        case Direction.Down:
                            destinationY++;
                            break;
                        case Direction.Left:
                            destinationX--;
                            break;
                        case Direction.Right:
                            destinationX++;
                            break;
                    }
                    break;
                case MoveResult.Sliding:
                    success = true;
                    break;
                case MoveResult.Icing:
                    success = true;
                    break;
                case MoveResult.OnGround:
                    success = true;
                    isOnGround = true;
                    break;
                case MoveResult.NoLongerOnGround:
                    success = true;
                    isOnGround = false;
                    break;
                case MoveResult.NoLongerSurfing:
                    success = true;
                    isSurfing = false;
                    break;
            }
            return success;
        }
        public void ApplyCompleteIceMovement(Direction direction, ref int x, ref int y, ref bool isOnGround)
        {
            MoveResult result;
            do
            {
                int destinationX = x;
                int destinationY = y;
                bool destinationGround = isOnGround;
                bool isSurfing = false;
                direction.ApplyToCoordinates(ref destinationX, ref destinationY);
                result = CanMove(direction, destinationX, destinationY, destinationGround, false, false, false);
                if (ApplyMovement(direction, result, ref destinationX, ref destinationY, ref destinationGround, ref isSurfing))
                {
                    x = destinationX;
                    y = destinationY;
                    isOnGround = destinationGround;
                }
            }
            while (result == MoveResult.Icing);
        }
        public void ApplyCompleteSliderMovement(ref int x, ref int y, ref bool isOnGround)
        {
            Direction? slidingDirection = null;
            MoveResult result;
            do
            {
                int destinationX = x;
                int destinationY = y;
                bool destinationGround = isOnGround;
                bool isSurfing = false;

                int slider = GetSlider(destinationX, destinationY);
                if (slider != -1)
                {
                    slidingDirection = SliderToDirection(slider);
                }

                if (slidingDirection == null)
                {
                    break;
                }

                slidingDirection.Value.ApplyToCoordinates(ref destinationX, ref destinationY);
                result = CanMove(slidingDirection.Value, destinationX, destinationY, destinationGround, false, false, false);
                if (ApplyMovement(slidingDirection.Value, result, ref destinationX, ref destinationY, ref destinationGround, ref isSurfing))
                {
                    x = destinationX;
                    y = destinationY;
                    isOnGround = destinationGround;
                }
            }
            while (slidingDirection != null && result != MoveResult.Fail);
        }

        private bool IsMovementValid(Direction direction, int collider, bool isOnGround, bool isSurfing, bool canUseCut, bool canUseSmashRock, int destx = -1, int desty = -1)
        {
            if (collider < 0)
                collider = Colliders[destx, desty];

            if (collider == 1)
            {
                return false;
            }

            //check for other areas!
            var newArea = CheckArea(destx, desty);
            if (newArea != null && !IsUnmoveableCellSide(direction, destx, desty))
            {
                if (newArea?.AreaName.ToLowerInvariant() != CurrentArea?.AreaName.ToLowerInvariant())
                    return true;
            }

            if (collider == 2 && !isSurfing)
                return false;

            if (HasLink(destx, desty) && !(direction == Direction.Up && collider == 22))
                return true;
            
            switch (direction)
            {
                case Direction.Up:
                    if (isOnGround)
                    {
                        
                        if (collider == 18 || collider == 0 || collider == 20 || collider == 19 || collider == 16
                            || collider == 24 || collider == 15 || collider == 12 || collider == 11 || collider == 14)
                        {
                            return true;
                        }
                        if (isSurfing && (collider == 2))
                        {
                            return true;
                        }
                    }
                    else if (collider == 14 || collider == 15 || collider == 7 || collider == 8 || collider == 9 || collider == 0 || collider == 12 || collider == 11)
                    {
                        return true;
                    }
                    break;
                case Direction.Down:
                    if (isOnGround)
                    {
                        

                        if (collider == 0 || collider == 15 || collider == 6 || collider == 7 || collider == 8 || collider == 9 || collider == 4 || collider == 12 || collider == 11 || collider == 14)
                        {
                            return true;
                        }
                        if (isSurfing && (collider == 5 || collider == 2))
                        {
                            return true;
                        }
                    }
                    else if (collider == 7 || collider == 8 || collider == 9 || collider == 0 || collider == 12)
                    {
                        return true;
                    }
                    break;
                case Direction.Left:
                    if (isOnGround)
                    {
                        var collPre = GetCollider(_client.PlayerX, _client.PlayerY);
                        if (collPre == 20 || collPre == 19 || collider == 16 || collider == 18 || GetCellSideMoveable(collPre))
                            return false;
                        if (collider == 14 || collider == 15 || collider == 0 || collider == 6 || collider == 7 || collider == 8 || collider == 9 || collider == 4 || collider == 12 || collider == 11)
                        {
                            return true;
                        }
                        if (isSurfing && (collider == 5 || collider == 2))
                        {
                            return true;
                        }
                    }
                    else if (collider == 14 || collider == 15 || collider == 7 || collider == 8 || collider == 9 || collider == 0 || collider == 12 || collider == 11)
                    {
                        return true;
                    }
                    break;
                case Direction.Right:
                    if (isOnGround)
                    {
                        var collPre = GetCollider(_client.PlayerX, _client.PlayerY);
                        if (collPre == 19 || collPre == 20 || collider == 16 || collider == 18 || GetCellSideMoveable(collPre))
                            return false;
                        if (collider == 14 || collider == 15 || collider == 0 || collider == 6 || collider == 7 || collider == 8 || collider == 9 || collider == 3 || collider == 12 || collider == 11)
                        {
                            return true;
                        }
                        if (isSurfing && (collider == 5 || collider == 2))
                        {
                            return true;
                        }
                    }
                    else if (collider == 14 || collider == 15 || collider == 7 || collider == 8 || collider == 9 || collider == 0 || collider == 12 || collider == 11)
                    {
                        return true;
                    }
                    break;
            }
            if (isSurfing && (collider == 2 || TileWater[destx, desty] == 1))
            {
                return true;
            }
            if ((canUseCut && IsCutTree(destx, desty)) || (canUseSmashRock && IsRockSmash(destx, desty))) // Smashable Rocks and Cutable trees are now NPCs
                return true;
            return false;
        }
        public int GetSlider(int x, int y)
        {
            int tile = Colliders[x, y];
            if (SliderValues.ContainsKey(tile))
            {
                return SliderValues[tile];
            }
            tile = Colliders[x, y];
            if (SliderValues.ContainsKey(tile))
            {
                return SliderValues[tile];
            }
            tile = Colliders[x, y];
            if (SliderValues.ContainsKey(tile))
            {
                return SliderValues[tile];
            }
            return -1;
        }
        public static Direction? SliderToDirection(int slider)
        {
            switch (slider)
            {
                case 0:
                    return Direction.Up;
                case 1:
                    return Direction.Down;
                case 2:
                    return Direction.Left;
                case 3:
                    return Direction.Right;
                default:
                    return null;
            }
        }
    }
}
