﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Linq;


namespace RedKite
{
    public class TileMapper
    {
        static TileMapper _instance;

        public static TileMapper Instance
        {
            get
            {
                return _instance ?? (_instance = new TileMapper());
            }
        }

        public static Cell[] tileTypes =
        {
            new Cell(Cell.Type.Empty),
            new Cell(Cell.Type.Floor),
            new Cell(Cell.Type.Wall),
            new Cell(Cell.Type.OccupiedEnemy),
            new Cell(Cell.Type.OccupiedAlly),
            new Cell(Cell.Type.PassableProp)
        };

        public int H;
        public int W;
        public char[,] map;

        public Dictionary<int, Area> Areas { get; private set; }

        int areaCount;
        int roomIndex = 0;

        protected static readonly char TILE_VOID = ' ';
        //static char TILE_FLOOR = '.';
        protected static readonly char TILE_WALL = '#';
        protected static readonly char TILE_CORNER = '!';
        protected static readonly char TILE_HALL = '+';
        protected static readonly char TILE_SPAWN = '@';
        //static char TILE_ROOM_CORNER = '?';
        //static char TILE_VISITED = '$';
        //static char TILE_POPULAR = '&';
        protected static readonly char TILE_WALL_CORNER = '%';
        int failedDoors;
        int[] roomFailures = new int[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        //everything below is unity related
        public List<Vector3Int>[] RoomTiles { get; private set; }

        protected static System.Random rndState = new System.Random();
        static int rnd(int x) => rndState.Next() % x;

        public Cell[,] Tiles { get; protected set; }

        public int index = 0;

        PathFinder pathFinder;

        List<Enemy> enemies;
        List<Hero> heroes;
        List<Prop> props;

        public void Update()
        {
            TrackSprites();
        }

        public bool TryUpdateTile(Vector3Int pos, Cell.Type tileType, int room)
        {
            if (Tiles[pos.x, pos.y].TileType == Cell.Type.Floor)
            {
                if (tileType == Cell.Type.PassableProp)
                { 
                    Tiles[pos.x, pos.y] = tileTypes[5];

                    //max distance is 99 since all non passable objects are 100. Could get complicated once rooms get bigger.
                    if (Areas[room].ConnectedAreas.All(x=> pathFinder.IsReachable(PathFinder.Graph[(int)Areas[room].Floor.Center.x, (int)Areas[room].Floor.Center.y], PathFinder.Graph[(int)TileMapper.Instance.Areas[x].Floor.Center.x, (int)TileMapper.Instance.Areas[x].Floor.Center.y], 99)))
                         return true;
                    else
                    {
                        Tiles[pos.x, pos.y] = tileTypes[1];
                        return false;
                    }
                }
                else
                    return false;
            }
            else
                return false;
           
        }

        public void UpdateTile(Vector3Int pos, Cell.Type tileType)
        {
            Tiles[pos.x, pos.y] = tileTypes.First(x=> x.TileType == tileType);
        }
        void TrackSprites()
        {
            enemies = GameSpriteManager.Instance.Enemies;
            heroes = GameSpriteManager.Instance.Heroes;
            props = GameSpriteManager.Instance.Props;

            List<Vector2> occupiedTilesEnemy = new List<Vector2>();
            List<Vector2> occupiedTilesHeroes = new List<Vector2>();
            List<Vector2> occupiedTilesProps = new List<Vector2>();

            foreach (Unit unit in enemies)
                occupiedTilesEnemy.Add(new Vector2(unit.Destination.x, unit.Destination.y));

            foreach (Unit unit in heroes)
                occupiedTilesHeroes.Add(new Vector2(unit.Destination.x, unit.Destination.y));

            foreach (Prop prop in props)
                occupiedTilesProps.Add(new Vector2(prop.Coordinate.x, prop.Coordinate.y));

            for (int y = 0; y < Tiles.GetLength(1); y++)
            { 
                for(int x = 0; x < Tiles.GetLength(0); x++)
                {
                    if (Tiles[x, y].TileType == Cell.Type.OccupiedEnemy & !occupiedTilesEnemy.Contains(new Vector2(x, y)))
                        Tiles[x, y] = tileTypes[1];
                    else if (occupiedTilesEnemy.Contains(new Vector2(x, y)))
                        Tiles[x, y] = tileTypes[3];

                    if (Tiles[x, y].TileType == Cell.Type.OccupiedAlly & !occupiedTilesHeroes.Contains(new Vector2(x, y)))
                        Tiles[x, y] = tileTypes[1];
                    else if (occupiedTilesHeroes.Contains(new Vector2(x, y)))
                        Tiles[x, y] = tileTypes[4];

                    if (Tiles[x, y].TileType == Cell.Type.PassableProp & !occupiedTilesProps.Contains(new Vector2(x, y)))
                        Tiles[x, y] = tileTypes[1];
                    else if (occupiedTilesProps.Contains(new Vector2(x, y)))
                        Tiles[x, y] = tileTypes[5];
                }
            }
        }

        public virtual void Generate()
        {
            Area.Wall.cornerGraph = null;

            H = rndState.Next(40, 50);
            W = rndState.Next(40, 50);

            map = new char[W, H];

            areaCount = 4;
            Areas = new Dictionary<int,Area>();


            Tiles = new Cell[W, H];

            //check if this even does anything anymore? Probably not necessary now.

            RoomTiles = new List<Vector3Int>[areaCount];

            for (int i = 0; i < RoomTiles.Length; i++)
            {
                RoomTiles[i] = new List<Vector3Int>();
            }


            ClearMap();

            // generate tilemap data
            roomIndex = 0;

            AddSpawn();
            roomIndex++;

            for(int i = 0; i < 5; i++)
            { 
                for (int j = 0; j < 500; j++)
                {
                    if (AddArea())
                    {
                        roomIndex++;
                    }
                    if (roomIndex > areaCount - 1)
                        break;
                    if (j == 499)
                    {
                        roomIndex = 0;
                        Areas.Clear();
                        ClearMap();
                        AddSpawn();
                        roomIndex++;
                    }
                }
                if (roomIndex > areaCount - 1)
                    break;
            }

            FindAllOverlaps();

            SplitAllWalls();

            foreach (Area area in Areas.Values)
            {
                foreach(Area.Wall wall in area.Walls)
                {
                    foreach (Segment segment in wall.Overlaps.Where(x => x.IsPath == true & x.IsRemoved == false))
                    {
                        Vector3[] tiles = Utility.CoordRange(segment.Min, segment.Max);
                        foreach(Vector3 tile in tiles)
                        {
                            map[(int)tile.x,(int)tile.y] = (char)(area.RoomIndex + 48);
                        }
                    }
                }

            }

            foreach (Area area in Areas.Values)
            {
                foreach (Area.Wall wall in area.Walls)
                {
                    foreach (Segment segment in wall.Segments)
                    {
                        Vector3[] tiles = Utility.CoordRange(segment.Min, segment.Max);
                        foreach (Vector3 tile in tiles)
                        {
                            if (Utility.WithinBounds(tile, W, H))
                                map[(int)tile.x, (int)tile.y] = TILE_WALL;
                        }
                    }
                }

            }


            //place tiles
            for (int x = 0; x < W; x++)
            {
                for (int y = 0; y < H; y++)
                {
                    char c = map[x, y];
                    if (c != TILE_VOID & c != TILE_WALL & c != TILE_WALL_CORNER)
                    {
                        Tiles[x, y] = tileTypes[1];
                        if(c==TILE_SPAWN)
                        { 
                            RoomTiles[0].Add(new Vector3Int(x, y, 1));
                        }
                        if ((int)c >= 48 & (int)c <= 57)
                        { 
                            RoomTiles[(int)c-48].Add(new Vector3Int(x, y,1));
                        }

                        Tiles[x, y] = Tiles[x, y] = tileTypes[1];
                    }
                    else if (c == TILE_WALL | c == TILE_WALL_CORNER)
                    {
                        Tiles[x, y] = tileTypes[2];

                    }
                    else if (c == TILE_VOID)
                    {
                        Tiles[x, y] = tileTypes[0];
                    }
                }
            }
        }

        protected void ClearMap()
        {
            //instantiate void cells
            for (int y = 0; y < map.GetLength(1); y++)
                for (int x = 0; x < map.GetLength(0); x++)
                    map[x, y] = TILE_VOID;
        }

        void AddSpawn(int maxWidth = 8, int maxHeight = 10)
        {
            //room height and width including walls
            int h = (int)Mathf.Max(rnd(maxHeight) + 5, 8);
            int w = (int)Mathf.Max(rnd(maxWidth) + 5, 8);

            //
            int ry = rndState.Next(1, H - h);
            int rx = rndState.Next(1, W - w);

            Vector3 startPoint = new Vector3(rx, ry, 1);

            Area firstArea = new Area(Orient.North, roomIndex, startPoint, w - 2, h - 2);

            //draw floor tiles
            Vector3[] tileCoords = firstArea.GetCoords();

            foreach (Vector3 coord in tileCoords)
                map[(int)coord.x, (int)coord.y] = (char)(roomIndex + 48);

            firstArea.GenerateWalls();

            //draw wall tile
            Vector3[] wallCoords = firstArea.GetWallCoords();

            foreach (Vector3 coord in wallCoords)
                map[(int)coord.x, (int)coord.y] = TILE_CORNER;

            foreach (Area.Wall wall in firstArea.Walls)
            {
                map[(int)wall.Min.x, (int)wall.Min.y] = TILE_WALL_CORNER;
                map[(int)wall.Max.x, (int)wall.Max.y] = TILE_WALL_CORNER;
            }

            int randoIndex = rndState.Next(0, tileCoords.Length);
            Vector3 spawn = tileCoords[randoIndex];

            map[(int)spawn.x, (int)spawn.y] = TILE_SPAWN;


            Areas.Add(roomIndex, firstArea);

        }

        bool AddArea(int maxWidth = 10, int maxHeight = 10)
        {


            Area foundRoom;

            foundRoom = FindArea();

            if (foundRoom == null)
            {
                return false;
            }

            foundRoom.GenerateWalls();

            int foundPaths = FindPaths(foundRoom);

            if (foundPaths < 1)
            {
                failedDoors++;
                return false;
            }

            //draw found room first so that halls can overwrite walls till I can add segmentation

            Vector3[] floorCoords = foundRoom.GetCoords();

            foreach (Vector3 coord in floorCoords)
                map[(int)coord.x, (int)coord.y] = (char)(roomIndex + 48);

            Vector3[] wallCoords = foundRoom.GetWallCoords();

            foreach (Vector3 coord in wallCoords)
                if (map[(int)coord.x, (int)coord.y] != TILE_WALL_CORNER)
                    map[(int)coord.x, (int)coord.y] = TILE_WALL;

            foreach (Area.Wall wall in foundRoom.Walls)
            {
                map[(int)wall.Min.x, (int)wall.Min.y] = TILE_WALL_CORNER;
                map[(int)wall.Max.x, (int)wall.Max.y] = TILE_WALL_CORNER;
            }

            Areas.Add(roomIndex, foundRoom);

            return true;

        }

        void SplitAllWalls()
        {
            foreach (Area area in Areas.Values)
                foreach (Area.Wall wall in area.Walls)
                { 
                    wall.Split();
                }
        }

        void FindAllOverlaps()
        {
            foreach (Area area in Areas.Values)
                foreach (Area.Wall wall in area.Walls)
                {
                    wall.FindOverlaps();
                }
        }

        //SHOULD BE VOID
        int FindPaths(Area workingArea)
        {
            //instantiate return variable which will let us know how many doors we found.
            //could technically be a bool but I might find use for knowing the number of doors.
            int foundPaths = 0;

            //get random list of indices to jump through dictionary.
            List<int> allIndices = Enumerable.Range(0, Areas.Count).ToList();
            Utility.Shuffle(allIndices);

            foreach (int area in allIndices)
            {
                Utility.Shuffle(Areas[area].Walls);
                for (int oWall = 0; oWall < Areas[area].Walls.Count; oWall++)
                {
                    Area.Wall oldWall = Areas[area].Walls[oWall];
                    Utility.Shuffle(workingArea.Walls);
                    for (int nWall = 0; nWall < workingArea.Walls.Count; nWall++)
                    {
                        Area.Wall newWall = workingArea.Walls[nWall];
                        //make sure walls are facing eachther.
                        if (newWall.Orientation.Forward == oldWall.Orientation.Back)
                        {
                            //my left is your right if we are facing eachother. This eliminates the forward and backward dimension
                            //and compares the axis on which the walls are stagnant.
                            if (Vector3.Scale(newWall.Max , newWall.Orientation.Forward) == Vector3.Scale(oldWall.Max , oldWall.Orientation.Back))
                            {
                                //https://stackoverflow.com/questions/325933/determine-whether-two-date-ranges-overlap

                                Vector3 up = newWall.Orientation == Orient.North | newWall.Orientation == Orient.South ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0);
                                Vector3 down = up == new Vector3(0, 1, 0) ? new Vector3(0, -1, 0) : new Vector3(-1, 0, 0);


                                float diff0 = Utility.DirectedDist(newWall.Min, oldWall.Max);
                                float diff1 = Utility.DirectedDist(newWall.Max, oldWall.Min);


                                if (diff0 >= 0 & diff1 <= 0)
                                {
                                    //These walls are now deemed connected. If the room finds a door it will be passed on to all connected walls in it's list.
                                    newWall.ConnectedWalls.Add(oldWall);

                                    //find maxiest min
                                    float diff2 = Utility.DirectedDist(newWall.Min, oldWall.Min);
                                    Vector3 startCorner = diff2 >= 0 ? oldWall.Min :
                                        newWall.Min;

                                    Vector3 startCoord = startCorner + up;


                                    //find minniest max
                                    float diff3 = Utility.DirectedDist(newWall.Max, oldWall.Max);
                                    Vector3 endCorner = diff3 <= 0 ? oldWall.Max :
                                        newWall.Max;

                                    Vector3 endCoord = endCorner + down;

                                    //if less than 4 tiles this wall is a failure. 2 is sufficient technically but it looks shitty.
                                    //eliminate new wall where overlap. create micro removed paths to anticipate corner for old wall. There will 
                                    //only be one corner as there is no room width less than 6. The test is if the max corner of the new wall is less
                                    //than the old wall. If so, eliminate the max corner (as it's the new wall's) else the overlap end at the 
                                    //new wall min corner which is at start corner. Old wall path includes one corner at the end.
                                    if (Vector3.Distance(startCoord, endCoord) < 4)
                                    {
                                        break;
                                    }

                                    //since the door has a minimum upward movement of one we need to make sure not to move past the endcoord
                                    //in the event it was the chosen doorCoord
                                    Vector3[] doorRange = Utility.CoordRange(startCoord, endCoord + down);

                                    //50 pct chance of door rather than blown out wall
                                    double randoP;
                                    randoP = rndState.Next(2);
                                    //instantiate start of path default value and it's width. Not sure if I like assigning a variable that has
                                    //a 50 percent chance of getting blown away.
                                    int width;
                                    Vector3 doorCoord = startCoord;

                                    //door is created and rooms are seperated.
                                    if (randoP == 1)
                                    {
                                        //this works because the minimum tile count in the door range is 4. This should never fail.
                                        //keep an eye on it though. May need to throw a return statement in there if it doesn't work.
                                        width = 2;

                                        //shuffle indexes not the range.
                                        int[] randoIndexes = Enumerable.Range(0, doorRange.Length).ToArray();
                                        Utility.Shuffle<int>(randoIndexes);

                                        //don't spawn doors at corners
                                        foreach (int index in randoIndexes)
                                        {
                                            Vector3 coord = doorRange[index];
                                            Vector3 checkAhead = coord + up;
                                            Vector3 checkAhead2 = coord + up + up;
                                            Vector3 checkBehind = coord + down;

                                            //don't go off the map.
                                            if (!Utility.WithinBounds(checkAhead2, W, H) & Utility.WithinBounds(checkBehind, W, H))
                                            {
                                                continue;
                                            }
                                            else
                                            { 
                                                //ensure you are not touching a corner. Check once behind and twice in the expanding direction
                                                if (map[(int)coord.x, (int)coord.y] == TILE_WALL_CORNER |
                                                    map[(int)checkAhead.x, (int)checkAhead.y] == TILE_WALL_CORNER |
                                                    map[(int)checkBehind.x, (int)checkBehind.y] == TILE_WALL_CORNER |
                                                    map[(int)checkAhead2.x, (int)checkAhead2.y] == TILE_WALL_CORNER)
                                                {
                                                    continue;
                                                }
                                                else
                                                {
                                                    doorCoord = coord;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //wall is blown out.
                                        doorCoord = startCoord;
                                        //due to subtracted endcoord from doorRange
                                        width = doorRange.Length + 1;
                                    }

                                    //width is minus one because it includes the startCoord.
                                    newWall.Doors.Add(Areas[area].RoomIndex, new Area.Door(doorCoord, doorCoord + ((width - 1) * up),false));

                                    foundPaths++;


                                }
                            }
                        }
                    }
                }
            }
            return foundPaths;
        }
        Area FindArea()
        {
            List<int> allIndices = Enumerable.Range(0, Areas.Count).ToList();

            Utility.Shuffle(allIndices);

            foreach (int area in allIndices)
            {
                foreach (Area.Wall wall in Areas[area].Walls.OrderBy(x => rndState.Next()))
                {

                    bool isLong;

                    //because width and height are relative to orientation.
                    if (wall.Orientation == Orient.North | wall.Orientation == Orient.South)
                    {
                        isLong = Areas[area].Floor.TrueNE.y - Areas[area].Floor.TrueSW.y >
                            Areas[area].Floor.TrueNE.x - Areas[area].Floor.TrueSW.x ? false : true;
                    }
                    else
                    {
                        isLong = Areas[area].Floor.TrueNE.y - Areas[area].Floor.TrueSW.y >
                            Areas[area].Floor.TrueNE.x - Areas[area].Floor.TrueSW.x ? true : false;
                    }

                    int w = isLong ? rndState.Next(8, 10) : rndState.Next(10, 16);
                    int h = isLong ? rndState.Next(10, 16) : rndState.Next(8, 10);


                    Vector3 roomDims = new Vector3(w - 2, h - 2, 1);

                    Vector3 up = wall.Orientation == Orient.North | wall.Orientation == Orient.South ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0);
                    Vector3 down = up == new Vector3(0, 1, 0) ? new Vector3(0, -1, 0) : new Vector3(-1, 0, 0);

                    //pick random spot on "old wall"

                    int oldIndex = rndState.Next((int)Vector3.Distance(wall.Max + (down * 2), wall.Min + (up*2)));
                    Vector3[] oldRange = Utility.CoordRange(wall.Min + (up * 2), wall.Max + (down * 2));
                    Vector3 oldCoord = oldRange[oldIndex];

                    int justify;

                    //pick random spot in new wall
                    if (wall.Orientation == Orient.North | wall.Orientation == Orient.South)
                        justify = rndState.Next(1,(int)roomDims.x - 1);
                    else
                        justify = rndState.Next(1,(int)roomDims.y - 1);

                    //move it out one space from the wall and select as corner of new floor.
                    Vector3 startPoint = oldCoord + wall.Orientation.Forward + (wall.Orientation.Right * justify);

                    roomFailures[roomIndex]++;

                    Area testArea = new Area(wall.Orientation, roomIndex, startPoint, w - 2, h - 2);

                    //if the top right and bottom left are in bounds then the rest should be
                    if (!Utility.WithinBounds(testArea.Floor.BottomLeft + wall.Orientation.Left + wall.Orientation.Back, W, H))
                    {
                        continue;
                    }

                    if (!Utility.WithinBounds(testArea.Floor.TopRight + wall.Orientation.Right + wall.Orientation.Forward, W, H))
                    {
                        continue;
                    }


                    int totalTiles = 0;


                    int timeOut = 1;

                    //this is off by one row;
                    totalTiles = (w - 2) * (h - 2);

                    Vector3[] allCoords = testArea.GetCoords();

                    //make sure no floor tiles come in contact with any existing floor OR door tiles.
                    foreach (Vector3 tileCoord in allCoords)
                    {
                        if (((int)map[(int)tileCoord.x, (int)tileCoord.y] >= 40 |
                            map[(int)tileCoord.x, (int)tileCoord.y] == TILE_CORNER | map[(int)tileCoord.x, (int)tileCoord.y] == TILE_WALL_CORNER))
                        {
                            break;
                        }
                        if (timeOut < totalTiles)
                            timeOut++;
                        else
                        {
                            return testArea;
                        }
                    }
                }
            }

            return null;
        }

        public Vector3[] GetSpawnPoints()
        {
            List<Vector3> spawnPoints = Areas[0].GetCoords().ToList();

            spawnPoints.Shuffle();

            return spawnPoints.ToArray();
        }

    }

}