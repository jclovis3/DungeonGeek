#define DEBUG

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonGeek
{
    /// <summary>
    /// Generates new levels randomly. Rooms are created with random number of rooms, and sizes.
    /// Loot is scattered randomly within them. Tunnels are drawn to ensure every room is connected
    /// to ensure a path always exists to the stairs. Monsters are also generated and placed randomly.
    /// Added in version 0.3 - Each type of magical consumable item will need to be created and stored once so
    /// that the discovery of the item's effects can be remembered. Rings are not included because they may
    /// each have a different strength or be cursed.
    /// </summary>
    static class FloorGenerator
    {
        /* PLANNING: Alternative map algorithm to be implemented when time permits:
         * This algorithm was tried once and found to be a bit problematic. For starters, each room
         * would have only one path to any other connected room. Somehow there spawned clusters of
         * rooms in their own connected networks that you couldn't get to from any other room outside
         * that network. Due to the single path problem, this algorithm has been abandoned.
         * Description was as follows:
         * Pick center most room first (of disconnected rooms). Detect closest neighbor (center point
         * to center point). Run first tunnel to this neighbor. Move both rooms to connected list.
         * With each room in connected list, calculate distance to every disconnected room. With the
         * room having the lowest distance, run a tunnel from it to the closer disconnected room. Move
         * the new room to the connected list, and repeat until all rooms are connected. The benifit
         * of this algorithm is that tunnels will not try to run across the map and work their way
         * around other disconnected rooms to reach the target. This should prevent any chance of
         * tunnel congestion. Should still look for tunnel intersection though.
        */

        /* Current map algoritm:
           Pick a disconnected room, tunnel to another random disconnected room. Move both to connected
           list. Pick a new disconnected room to be source, and a connected room to be target (both random)
           Tunnel from source to target, accepting any existing tunnel link or connected room found along
           the way. First iteration from disconnected to disconnected is only to provide general direction
           As of version 0.2, the discovery of any room along the way will complete the first tunnel.
           The bigest problem with this method is having to make a path around other disconnected rooms
           to connect from a connected room on one side of the map to a disconnected room on the other.
        */



        #region Fields

        static private Random rand = new Random();
        static private int floorNumber;
        static private int minRoomCount; // variable because it depends on size of floor
        static private int maxRoomCount; 
        static private int numberOfRooms;
        static private int floorWidth = GameConstants.FLOOR_WIDTH_START;
        static private int floorHeight = GameConstants.FLOOR_HEIGHT_START;
        static private FloorTile.Surfaces[,] floorPlan;
        static private List<Rectangle> rooms = new List<Rectangle>();
        static private bool[,] floorRevealed;

        /* 
         * loot scatter works as follows:
         * Scans through all lit and dark floor tiles and sets chance to drop loot for each
         * tile at DROP_LOOT_CHANCE. If roll passes, then list of lootOptionTypes is iterated through
         * testing a new roll with the individual lootProbablity value. If one passes, it is selected
         * as the loot to drop. If none passes, the iteration cycle repeats until one does.
         */
        static private List<InventoryItem> scatteredLoot;
        static private List<KeyValuePair<Point, int>> scatteredGold; // Location and amount of gold
        // Note, gold is not an Inventory Item and will not be picked up by the inventory.


        static private Dictionary<Type, float> lootProbability = new Dictionary<Type, float>();
        static private InventoryItem newLootItem = null;
        static private Monster newMonster = null;


        //static private List<Type> monsterOptionTypes = new List<Type>();

        


        #endregion



        #region Properties

        static internal bool[,] FloorRevealed
        {
            get { return floorRevealed; }
            set { floorRevealed = value; }
        }
        static internal int FloorWidth { get { return floorWidth; } }
        static internal int FloorHeight { get { return floorHeight; } }
        static internal int LevelNumber { get { return floorNumber; } }
        static internal FloorTile.Surfaces[,] FloorPlan
        {
            get { return floorPlan; }
        }
        static internal List<InventoryItem> ScatteredLoot
        {
            get { return scatteredLoot; }
            set { scatteredLoot = value; }
        }
        static internal List<KeyValuePair<Point, int>> ScatteredGold
        {
            get { return scatteredGold; }
            set { scatteredGold = value; }
        }
        #endregion



        #region Floor Generation
        /*   * Floor generation rules:
             
             * Low levels have all lit rooms and no hidden doors
             * As you increase in level, chance of dark room and hidden door increases
             * All rooms must be connected to starting room, even if hidden door is required to access
             * Small percentage of rooms will have lots of treasures, and lots of monsters (not roaming)
             * One room must have a stairs going down. After level 30, odds of finding stairs going up
             *    increase from zero to a percentage equal to 0.05 * the level number (so Level 50
             *    will have a 2.5% chance of having a stair going up) and shall bring the hero
             *    up 5 levels (decrementing the level number). This is so they have an option to fight
             *    easier monsters again. The floor plan will be new as will the loot be repopulated.
             * After level 80, the Golden Grail may be found with 20% chance and increasing 10% every
             *    level. If floor has the Golden Grail, it will be set next to stairs going up which
             *    from this point on, will bring the hero all the way out of the dungeon.
             *    Grail should be scattered somewhere within a large treasure/monster room.
             *    Grail also should not be something the hero can drop. Once hero has it, a grail
             *    should not be generated on any new floor. This allows hero to continue exploring and
             *    risk death with the grail in his hands.
             */


        static private void AdjustFloorSize()
        {
            // Produces a range of 80 to 400 tiles, capping out at floorLevel 108
            floorWidth = MathHelper.Min(
                GameConstants.FLOOR_WIDTH_START + (floorNumber * 3) - 3,
                GameConstants.FLOOR_WIDTH_MAX);

            floorHeight = MathHelper.Min(
                GameConstants.FLOOR_HEIGHT_START + (floorNumber * 3) - 3,
                GameConstants.FLOOR_HEIGHT_MAX);
        }


        /// <summary>
        /// Creates the floor structure and contents, wiping any existing floor data
        /// </summary>
        /// <exception cref="StackOverflowException">Thrown when unable to map tunnels</exception>
        static internal void CreateNewFloor(int level)
        {

            // Initialize values
            floorNumber = level;
            AdjustFloorSize();
            floorRevealed = new bool[floorWidth, floorHeight];

            // Define the number of rooms based on size of level (and DEBUG setting)
            minRoomCount = GameConstants.DEBUG_OPTION_SET_ROOM_COUNT > 0
                ? GameConstants.DEBUG_OPTION_SET_ROOM_COUNT
                : 1 + (int)Math.Ceiling((decimal)floorWidth * floorHeight / 800);

            maxRoomCount = GameConstants.DEBUG_OPTION_SET_ROOM_COUNT >0
                ? GameConstants.DEBUG_OPTION_SET_ROOM_COUNT
                :(int)Math.Floor((decimal)floorWidth * floorHeight / 400);

            numberOfRooms = rand.Next(minRoomCount, maxRoomCount + 1);

#if DEBUG
            if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 0)
                Debug.WriteLine(string.Format("Begin creating floor {0} x {1} with {2} rooms",floorWidth,floorHeight, numberOfRooms));
#endif
            // Clear floor, loot and monster lists
            rooms.Clear();
            floorPlan = new FloorTile.Surfaces[floorWidth, floorHeight];
            scatteredLoot = new List<InventoryItem>();
            scatteredGold = new List<KeyValuePair<Point, int>>();
            Monster.Monsters = new List<Monster>();
            if (lootProbability.Count == 0) PopulateLootProbabilities();
            // PLANNING: Loot probabilities may be determined by floor level, in which case it should
            // be called with every new level.

            // First create the boarder tiles so tunnels do not route through them.
            for(int x=0; x< floorWidth; x++)
            {
                floorPlan[x, 0] = FloorTile.Surfaces.Border;
                floorPlan[x, floorHeight - 1] = FloorTile.Surfaces.Border;
            }

            for(int y=1; y< floorHeight - 1; y++)
            {
                floorPlan[0, y] = FloorTile.Surfaces.Border;
                floorPlan[floorWidth - 1, y] = FloorTile.Surfaces.Border;
            }

            
            // Attempts to create the number of rooms determined above. If unable to find space
            // for all of them, accepts what it could and moves on.
            do
            {
                for (int i = 0; i < numberOfRooms; i++)
                {
                    if (!CreateRoom())
                    {
                        // Taking too long to find space for a room, reduce number
                        // of rooms to present quantity
                        numberOfRooms = i;
#if DEBUG
                        if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 0)
                            Debug.WriteLine(string.Format("Unable to create room. Number of rooms now {0}", numberOfRooms));
#endif
                        break;
                    }
                }
            }
            // If for some reason it fails to find space for 2 rooms, maybe if it tries again
            // it will pick smaller rooms the next time.
            while (numberOfRooms < 2);


            


            // Map tunnels to connect all rooms (or at least try up to 100 times)
            int tunnelAttempts = 0;
            while (!CreateTunnels()) // Sometimes the tunnel algorithm gets lockd up and needs a fresh start.
            {
                ClearAllTunnelsAndDoors();
#if DEBUG
                if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 0)
                    Debug.WriteLine(string.Format("Tunnels failed. Clearing all tunnels and trying again. (Attempt {0})",tunnelAttempts));
#endif
                if (tunnelAttempts++ > 100)
                {
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 0)
                        Debug.WriteLine("Too many attempts to create tunnels. Canceling floor creation and throwing StackOverflowExcepton.");
#endif
                    throw new StackOverflowException("All attempts to create tunnels have failed");
                }
                
            }

            ChangeUpDoors(); // Sets hidden and locked doors on level
            PlaceHero();
            PlaceStairsDown();

            // Scatter Inventory Items (aka 'loot'), Gold, and spawn monsters in same loop
            foreach (var room in rooms)
            {
                for (int x = room.Left; x < room.Right; x++)
                    for (int y = room.Top; y < room.Bottom; y++)
                    {
                        if (!(DungeonGameEngine.Hero.Location.X == x && DungeonGameEngine.Hero.Location.Y == y))
                        {
                            // Ensures loot, gold and monsters don't drop on the same spot
                            if (!RandomlyDropLoot(x, y))
                                if (!RandomlyDropGold(x, y))
                                    RandomlySpawnMonsters(x, y);
                        }
                    }
            }


#if DEBUG
            if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 0)
                Debug.WriteLine("Finished creating new floor");
#endif
        }
        #endregion



        #region Room Generation

        /// <summary>
        /// Adds a new room to the list and requests floor and wall tiles get set
        /// </summary>
        /// <returns></returns>
        static private bool CreateRoom()
        {
            int roomWidth = rand.Next(GameConstants.MIN_ROOM_FLOOR_DIMENSION,
                                      GameConstants.MAX_ROOM_FLOOR_DIMENSION + 1);
            int roomHeight = rand.Next(GameConstants.MIN_ROOM_FLOOR_DIMENSION,
                                       GameConstants.MAX_ROOM_FLOOR_DIMENSION + 1);

            Rectangle newRoom = FindRandomEmptySpace(roomWidth, roomHeight);
            if (newRoom.Width == 0) return false;
            rooms.Add(newRoom);
            AddFloorTiles(newRoom);
            return true;
        }


        /// <summary>
        /// Seeks void space on map for the size of the room, its surounding walls, and the
        /// required margin of space between rooms.
        /// </summary>
        /// <param name="width">Width of open floor space in room</param>
        /// <param name="height">Height of open floor space in room</param>
        /// <returns></returns>
        static private Rectangle FindRandomEmptySpace(int width, int height)
        { 
            bool foundSpace = false;
            int x=0; int y=0;
            Rectangle testSpace = new Rectangle();
            int iCounter = 0; // Ensures method can give up searching

            // Search for space
            while (!foundSpace && iCounter++ < 2000)
            {
                foundSpace = true;

                // Pick coordinate on the floorPlan for new room (without its walls)
                x = rand.Next(1 + GameConstants.MIN_MARGIN_BETWEEN_ROOMS,
                    floorWidth - 1 - width - GameConstants.MIN_MARGIN_BETWEEN_ROOMS);
                y = rand.Next(1 + GameConstants.MIN_MARGIN_BETWEEN_ROOMS,
                    floorHeight - 1 - height - GameConstants.MIN_MARGIN_BETWEEN_ROOMS);
                
                // Create test space to include floor space (width x height) + 2 for walls
                // (each dimension) + 2 for neighboring walls + margin on each side
                testSpace.X = x - 2 - GameConstants.MIN_MARGIN_BETWEEN_ROOMS;
                testSpace.Y = y - 2 - GameConstants.MIN_MARGIN_BETWEEN_ROOMS;
                testSpace.Width = width + 4 + GameConstants.MIN_MARGIN_BETWEEN_ROOMS *2;
                testSpace.Height = height + 4 + GameConstants.MIN_MARGIN_BETWEEN_ROOMS *2;

                // Checks to see if the new random test space intersects an existing room
                foreach (var room in rooms)
                {
                    if(room.Intersects(testSpace))
                    {
                        foundSpace = false;
                        break;
                    }
                }
            } // If test space was bad, repeats loop to try again

            // After predetermined number of attempts, either returns the new space if one was found
            // or returns an empty rectangle indicating that a space could not be found.
            if (!foundSpace) return new Rectangle(0, 0, 0, 0);
            return new Rectangle(x,y,width,height);
        }

        /// <summary>
        /// Sets the tileType to indicate what type of floor (Lit or dark) the room has and the
        /// wall spaces around the room.
        /// </summary>
        /// <param name="room"></param>
        static private void AddFloorTiles(Rectangle room)
        {
            FloorTile.Surfaces tileType;

            // Probability of having a lit room decreases with each level after level 5
            if (floorNumber > 5 && rand.NextDouble() < ((floorNumber * 0.05) - 0.1))
                tileType = FloorTile.Surfaces.DarkRoom;
            else tileType = FloorTile.Surfaces.LitRoom;

            // Add floor tiles of selected type
            for (int x = room.X; x < room.Right; x++)
                for (int y = room.Y; y < room.Bottom; y++)
                    floorPlan[x, y] = tileType;

            // Add walls outside the room dimensions
            tileType = FloorTile.Surfaces.Wall;

            // paint top and bottom walls
            for(int x=room.X-1; x<room.Right+1;x++)
            {
                floorPlan[x, room.Top - 1] = tileType;
                floorPlan[x, room.Bottom ] = tileType;
            }

            // paint left and right walls
            for(int y=room.Y;y<room.Bottom;y++)
            {
                floorPlan[room.Left - 1, y] = tileType;
                floorPlan[room.Right, y] = tileType;
            }
        }

        #endregion



        #region Hero and Monster placement

        /// <summary>
        /// Randomly places hero in a room (assumes before loot and monster placement)
        /// </summary>
        static private void PlaceHero()
        {
            
            Point heroLocation = new Point();
            int roomNumber = rand.Next(rooms.Count);
            heroLocation.X = rand.Next(rooms[roomNumber].Left, rooms[roomNumber].Right);
            heroLocation.Y = rand.Next(rooms[roomNumber].Top, rooms[roomNumber].Bottom);
            DungeonGameEngine.Hero.Location = heroLocation;
        }

        static private void RandomlySpawnMonsters(int x, int y, bool forceSpawn = false)
        {
            // Spawn monsters at random intervals
            if (forceSpawn || rand.NextDouble() < GameConstants.SPAWN_MONSTER_CHANCE)
            {
                Type monsterTypePicked = PickMonsterType(); 
                if (monsterTypePicked == typeof(Rat)) newMonster = new Rat(new Point(x, y));
                
                // ...

                if (newMonster != null) Monster.Monsters.Add(newMonster);

            }


        }

        static Type PickMonsterType()
        {

            // TODO: When more monsters are available, use the floor range property to pick monsters based on current level

            return typeof(Rat);
        }

        #endregion



        #region Loot

        /// <summary>
        /// Populates the lootProbability table for each type of loot.
        /// </summary>
        static internal void PopulateLootProbabilities()
        {
            // See note above declarations to see how probability works

            // Armor and rings are long lasting, so a low probability is best
            lootProbability.Add(typeof(Armor), 0.10f);
            lootProbability.Add(typeof(MagicalRing), 0.10f);

            // Consumables like these should be more common
            lootProbability.Add(typeof(Food), 0.20f);
            lootProbability.Add(typeof(MagicalScroll), 0.15f);
            
            // Weapons can break on a fumble, so they need to be higher than armor and rings
            lootProbability.Add(typeof(Weapon), 0.15f); 

        }


        /// <summary>
        /// Drops gold randomly in the selected place, unless forceDrop is true
        /// </summary>
        /// <param name="x">Location X value</param>
        /// <param name="y">Location Y value</param>
        /// <param name="forceDrop">set to true to drop 100 gold</param>
        /// /// <returns>True if gold dropped</returns>
        static internal bool RandomlyDropGold(int x, int y, bool forceDrop = false, int forceAmt=100)
        {
            // Determine if gold should drop
            if (forceDrop || rand.NextDouble() < GameConstants.DROP_GOLD_CHANCE)
            {
                // Determine how much to drop
                int amt;
                if (forceDrop) amt = forceAmt;
                else amt = rand.Next(floorNumber, floorNumber * 5 + 1);
                scatteredGold.Add(new KeyValuePair<Point, int>(new Point(x, y), amt));
                return true;
            }
            else return false;
        }
        
        
        /// <summary>
        /// Selects and drops loot based on probability. Location is determined before entering
        /// method and usually involves scanning through all spaces on the floor, but this method
        /// may also be used to garantee something drops and picks randomly what it is. For example,
        /// A trap that drops items on the hero, or a locked box with unkown contents until you open it.
        /// </summary>
        /// <param name="x">X coordinate of Location to drop</param>
        /// <param name="y">Y coordinate of Location to drop</param>
        /// <param name="forceDrop">Ignors chance of dropping loot and forces it to drop</param>
        /// <returns>True if loot dropped</returns>
        static private bool RandomlyDropLoot(int x, int y, bool forceDrop = false)
        {
            // Determine if loot should drop
            if (forceDrop || rand.NextDouble() < GameConstants.DROP_LOOT_CHANCE)
            {
                Type lootTypePicked = null;

                // Determine what to drop by first randomly picking a loot type from all the types
                // and then rolling against the probability until a type is picked.
                while (lootTypePicked == null)
                {
                    var lootOption = lootProbability.ElementAt(rand.Next(lootProbability.Count));

                    if (rand.NextDouble() < lootOption.Value)
                    {
                        lootTypePicked = lootOption.Key;
                        break;
                    }
                }



                // TODO: Add more loot types to be created after their classes are written
                // Create loot objects
                if (lootTypePicked == typeof(MagicalRing)) newLootItem = new MagicalRing(new Point(x, y));
                if (lootTypePicked == typeof(MagicalScroll)) newLootItem = MagicalScroll.GenerateNewScroll(new Point(x, y));
                if (lootTypePicked == typeof(Food)) newLootItem = new Food(new Point(x, y));
                // Type of armor is determined partially by the floor number
                if (lootTypePicked == typeof(Armor)) newLootItem = new Armor(new Point(x, y), floorNumber);
                if (lootTypePicked == typeof(Weapon)) newLootItem = new Weapon(new Point(x, y), floorNumber);
                // ...

                if (newLootItem != null) scatteredLoot.Add(newLootItem);
                return true;
            }
            else return false;
        }

        static private bool LootFoundAt(Point floorLocation)
        {
            foreach (var item in ScatteredLoot)
                if (item.Location == floorLocation) return true;
            return false;
        }
        #endregion



        #region Tunnel and door generation

        /// <summary>
        /// Resets all tunnel spaces to void, and all door spaces to walls. May be used if an attempt
        /// to map tunnels gets locked up and needs to start over.
        /// </summary>
        static private void ClearAllTunnelsAndDoors()
        {
            for (int x = 0; x < floorWidth; x++)
                for (int y = 0; y < floorHeight; y++)
                {
                    if (floorPlan[x, y] == FloorTile.Surfaces.Tunnel) floorPlan[x, y] = FloorTile.Surfaces.Void;
                    if (floorPlan[x, y] == FloorTile.Surfaces.HiddenDoor ||
                        floorPlan[x, y] == FloorTile.Surfaces.LockedDoor ||
                        floorPlan[x, y] == FloorTile.Surfaces.OpenDoor) floorPlan[x, y] = FloorTile.Surfaces.Wall;
                }
        }

        /// <summary>
        /// Maps the tunnels required to connect each room to a single network of connected rooms
        /// </summary>
        /// <returns></returns>
        static private bool CreateTunnels()
        {

#if DEBUG
            if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL >0)
                Debug.WriteLine("Begin CreateTunnels with {0} rooms", rooms.Count);
#endif

            List<Rectangle> connectedRooms = new List<Rectangle>(); 
            List<Rectangle> disconnectedRooms = new List<Rectangle>(rooms);

#if DEBUG
            if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
            {
                Debug.WriteLine("Rooms added to disconnectedRooms:");
                foreach (var room in disconnectedRooms)
                    Debug.WriteLine(room.ToString());
            }
#endif

            Rectangle sourceRoom;
            Rectangle targetRoom;
            int indexPick; // For picking a source or target room
            GameConstants.Direction4 direction; // Used to determine direction of tunnel or side of room to start on
            

            List<Point> currentTunnel = new List<Point>(); // used so we don't run into own tunnel
            Point currentPosition;
            bool currentTunnelCollapsed = false;
            int tunnelAttemptCounter = 0;

            /* How this works:
             * Loop requires selection of a disconnected room as source, in addition, the selection of a
             * connected room if available or else selection of disconnected room as target.
             * The target only gives a general direction to dig towards. If a connected room or tunnel
             * is found, the tunnel is complete. For the first loop, any disconnected room found along
             * the way will also be accepted.
            */
            while (disconnectedRooms.Count > 0 || currentTunnelCollapsed) 
            {

#if DEBUG
                if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                    Debug.WriteLine(string.Format("Top of while disconnectedRooms.Count>0 with tunnelAttemptCounter={0}", tunnelAttemptCounter));
#endif

                if (++tunnelAttemptCounter > 100)
                {
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 0)
                        Debug.WriteLine("tunnelAttemptCounter exceeded 100, exitting while loop");
#endif 
                    return false;
                }
                currentTunnel.Clear();
#if DEBUG
                if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                    Debug.WriteLine("currentTunnel cleared");
#endif
                currentTunnelCollapsed = false;

                // Select source from disconnected rooms
                indexPick = rand.Next(disconnectedRooms.Count);
                sourceRoom = disconnectedRooms[indexPick];
#if DEBUG
                if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                    Debug.WriteLine("Source selected: {0}", sourceRoom.ToString());
#endif
                // Select target from connected list if available, else disconnected
                if (connectedRooms.Count == 0)
                {
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                        Debug.WriteLine("No connected rooms present, selecting target from disconnected.");
#endif
                    do
                    {
                        indexPick = rand.Next(disconnectedRooms.Count);
                        targetRoom = disconnectedRooms[indexPick];
                    } while (targetRoom == sourceRoom);
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                        Debug.WriteLine(string.Format("Selected room {0}", targetRoom.ToString()));
#endif
                }
                else
                {
                    indexPick = rand.Next(connectedRooms.Count);
                    targetRoom = connectedRooms[indexPick];
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                        Debug.WriteLine(string.Format("Selected connected room {0}", targetRoom.ToString()));
#endif
                }

                // Pick the best wall to dig tunnel from to get from source to target
                GameConstants.Direction4 wallSide = AffinityDirection(sourceRoom.Center, targetRoom.Center);


                // Place door on chosen side of wall, then move outside the door
                Point startingDoor = SetStartingDoor(sourceRoom, wallSide); // place door on wall
#if DEBUG
                if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                    Debug.WriteLine(string.Format("Placed starting door at {0}", startingDoor.ToString()));
#endif
                
                currentPosition = Utility.GetNewPointFrom(wallSide, startingDoor); // move in direction as set by wallSide

                // Due to protected margin space, we know this spot is not blocked, so mark it
                floorPlan[currentPosition.X, currentPosition.Y] = FloorTile.Surfaces.Tunnel;
#if DEBUG
                if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                    Debug.WriteLine(string.Format("Marked {0} as a tunnel", currentPosition.ToString()));
#endif
                // Dig tunnel - Set starting values
                var tunnelComplete = false;
#if DEBUG
                if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                    Debug.WriteLine("Set tunnelComplete to False");
#endif
                FloorTile.Surfaces currentSurface;
                var foundRoom = new Rectangle();

                // Repeats process until the tunnel is finished
                while (!tunnelComplete)
                {
                    currentTunnel.Add(currentPosition);
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                        Debug.WriteLine(string.Format("Added {0} to currentTunnel", currentPosition.ToString()));
#endif
                    int loopCounter = 0; // Catches difficulty in finding a path and starts over
                    bool directionBlocked=false;
                    do {
                        // Uses affinity and random to pick a direction to dig towards
                        direction = PickDirection(currentPosition, targetRoom.Center);
#if DEBUG
                        if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                            Debug.WriteLine(string.Format("Set direction to {0}", direction));
#endif
                        // If unable to find a valid direction to move to, the tunnel is a failure
                        if (++loopCounter > (floorWidth + floorHeight) * 4)
                        {
                            currentTunnelCollapsed = true;
                            if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 0)
                                Debug.WriteLine(string.Format("Tunnel Collapsed after {0} tries", loopCounter));
                            break;
                        }
                        directionBlocked = TunnelBlocked(
                            currentPosition,
                            direction,
                            currentTunnel,      // blocked when running into own tunnel
                            targetRoom,         // not blocked if connected to target room
                            sourceRoom,         // blocked if wall from source room found
                            connectedRooms,     // not blocked if wall found from connected room
                            disconnectedRooms,  // blocked only if other connected rooms exist, otherwise not
                            out foundRoom);
#if DEBUG
                        if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                            Debug.WriteLine(string.Format("direction {0} blocked", directionBlocked ? "is" : "is not"));
#endif
                    } while (directionBlocked);

                    // If failed to dig current tunnel, reset tiles to void and restart loop
                    if (currentTunnelCollapsed)
                    {
#if DEBUG
                        if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                            Debug.WriteLine("Tunnel spaces reset to Void");
#endif
                        foreach (var space in currentTunnel)
                            floorPlan[space.X, space.Y] = FloorTile.Surfaces.Void;

                        floorPlan[startingDoor.X, startingDoor.Y] = FloorTile.Surfaces.Wall;
#if DEBUG
                        if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                            Debug.WriteLine(string.Format("starting door {0} reset to Wall", startingDoor.ToString()));
#endif
                        break;
                    }


                    // TunnelBlocked will return a rectangle with width > 0 as foundRoom if in the process
                    // of diging towards the target we find another room instead.
                    // This room will be connected if there are any, or disconnected if there are not.

                    // Reset target to be what ever room was found

                    if (foundRoom.Width > 0)
                    {
#if DEBUG
                        if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                            Debug.WriteLine(string.Format("Moving target room from {0} to {1}", targetRoom.ToString(), foundRoom.ToString()));
#endif
                        targetRoom = foundRoom;
                    }

                    // move into the direction chosen and identify the surface for this space
                    currentPosition = Utility.GetNewPointFrom(direction, currentPosition);
                    currentSurface = floorPlan[currentPosition.X, currentPosition.Y];
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                        Debug.WriteLine(string.Format("Moved onto a {0} at {1}", currentSurface, currentPosition.ToString()));
#endif
                    // Check to see if standing on a door or wall, or another connected tunnel.
                    // All doors should be open at this point. Any wall would have to be on a valid
                    // room.

                    if (currentSurface == FloorTile.Surfaces.Wall ||
                        currentSurface == FloorTile.Surfaces.Tunnel ||
                        currentSurface == FloorTile.Surfaces.OpenDoor ||
                        IsJoiningOtherTunnel(currentPosition, currentTunnel)) 
                        tunnelComplete = true;
                    else
                        // Its not blocked, and not entering into a room, make it a tunnel
                        floorPlan[currentPosition.X, currentPosition.Y] = FloorTile.Surfaces.Tunnel;
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                        Debug.WriteLine(string.Format("Tunnel {0}", tunnelComplete ? "complete" : "not complete - marking space as tunnel"));
#endif
                    // Finally, move both rooms into the connected list if they are in the
                    // disconnected list.
                    if (tunnelComplete)
                        foreach (var room in new Rectangle[] { sourceRoom, targetRoom })
                        {
#if DEBUG
                            if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                                Debug.WriteLine(string.Format("Checking room {0} to see if it was disconnected", room.ToString()));
#endif               
                            if (disconnectedRooms.Contains(room))
                            {
                                if(disconnectedRooms.Remove(room))
                                    connectedRooms.Add(room);
#if DEBUG
                                if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                                    Debug.WriteLine(string.Format("Moved room {0} from disconnected to connected list.", room.ToString()));
#endif               
                            }
                        }
                } // repeat while (!tunnelComplete)

                if (currentTunnelCollapsed) continue;

                // Depending on type of tile tunnel ended on, either change it to a door or tunnel.
                if (floorPlan[currentPosition.X, currentPosition.Y] == FloorTile.Surfaces.Wall)
                {
                    floorPlan[currentPosition.X, currentPosition.Y] = FloorTile.Surfaces.OpenDoor;
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                        Debug.WriteLine("Tunnel ended on a Wall. Changed it to an Open Door.");
#endif
                }
                    else if (floorPlan[currentPosition.X, currentPosition.Y] == FloorTile.Surfaces.Void)
                {
                    floorPlan[currentPosition.X, currentPosition.Y] = FloorTile.Surfaces.Tunnel;
#if DEBUG
                    if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 1)
                        Debug.WriteLine("Tunnel ended on a Void. Changed it to a Tunnel.");
#endif
                }

                tunnelAttemptCounter = 0; // Tunnel was completed, reset attempt counter for next tunnel.
#if DEBUG
                if (GameConstants.DEBUG_OPTION_LOGGING_LEVEL > 0)
                    Debug.WriteLine("Tunnel was completed. Reset attempt counter to 0.");
#endif
            } // repeat while (disconnectedRooms.Count > 0)

            return true;
            // end of createTunnels() method
        }

        /// <summary>
        /// Prevents running into own tunnel to ensure path makes its way towards an older
        /// tunnel, or wall/door at destination (or another connected) room. Also blocks entry
        /// onto room corners and map borders. If there are no connected rooms to start with, any room
        /// may be connected to and returned as the roomFound argument.
        /// </summary>
        /// <param name="currentPosition">Position to move from</param>
        /// <param name="direction">Direction to dig into</param>
        /// <param name="ownTunnel">path created by this dig session</param>
        /// <param name="connectedRooms">list of rooms already connected</param>
        /// <param name="disconnectedRooms">list of rooms yet to be connected</param>
        /// <param name="roomFound">Room connected to even if not the target</param>
        /// <param name="targetRoom">Room to aim for</param>
        /// <returns>true if path is blocked</returns>
        private static bool TunnelBlocked(Point currentPosition, GameConstants.Direction4 direction, List<Point> ownTunnel, Rectangle targetRoom, Rectangle sourceRoom, List<Rectangle> connectedRooms, List<Rectangle> disconnectedRooms, out Rectangle roomFound)
        {
            // Initialize OUT parameter values
            roomFound = new Rectangle();

            // Determine if connecting to disconnected rooms is allowed (there are no connected rooms)
            bool allowDisconnected = (connectedRooms.Count == 0);

            // Sets the location to check and gets the surface type
            var targetLocation = Utility.GetNewPointFrom(direction, currentPosition);
            var targetSurface = floorPlan[targetLocation.X, targetLocation.Y];

            // shortcuts, since all void spaces are free to tunnel and running
            // into own tunnel or border tiles is not allowed
            if (targetSurface == FloorTile.Surfaces.Void) return false;
            if (targetSurface == FloorTile.Surfaces.Border) return true;
            if (ownTunnel.Contains(targetLocation)) return true;

            
            // If surface is another door or wall
            if (targetSurface == FloorTile.Surfaces.Wall || targetSurface == FloorTile.Surfaces.HiddenDoor ||
                targetSurface == FloorTile.Surfaces.LockedDoor || targetSurface == FloorTile.Surfaces.OpenDoor)
            {
                // Get room surounded by found wall
                Rectangle joinToRoom = new Rectangle();
                foreach (var room in rooms)
                {
                    if (RoomWalls(room).Contains(targetLocation))
                    {
                        joinToRoom = room; // Inidicates that a connected room has been found
                        break;
                    }
                }

                // If found sourceRoom, it's a turnaround loop and must be blocked.
                if (joinToRoom == sourceRoom) return true;

                // If wall is a corner, return true
                if (joinToRoom.Width == 0 || IsCorner(joinToRoom, targetLocation)) return true;

                // Otherwise, if there are no connected rooms, then a disconnected room will do
                // But if there are, only connected rooms are valid.
                if (allowDisconnected || connectedRooms.Contains(joinToRoom))
                {
                    roomFound = joinToRoom;
                    return false;
                }

                // And if the room is not connected though there are connected rooms available, mark this wall segment as blocked.
                return true;
            }

            // By this point, target is not on a wall border, or own tunnel and thus is not blocked.
            return false;
        }

        /// <summary>
        /// Calculates the rectangle dimensions of the walls around a room.
        /// </summary>
        /// <param name="room"></param>
        /// <returns>Rectangle containing the room and walls</returns>
        private static Rectangle RoomWalls(Rectangle room)
        {
            Rectangle walls;
            walls.X = room.X - 1;
            walls.Y = room.Y - 1;
            walls.Width = room.Width + 2;
            walls.Height = room.Height + 2;
            return walls;
        }

        /// <summary>
        /// Determines if the location provided is a corner of the given room
        /// </summary>
        /// <param name="room">Room dimensions</param>
        /// <param name="location">Point to check for corner</param>
        /// <returns></returns>
        private static bool IsCorner(Rectangle room, Point location)
        {
            if (location.X == room.X - 1 && location.Y == room.Y - 1) return true;
            if (location.X == room.Right && location.Y == room.Y - 1) return true;
            if (location.X == room.X - 1 && location.Y == room.Bottom) return true;
            if (location.X == room.Right && location.Y == room.Bottom) return true;
            return false;
        }


        /// <summary>
        /// Determines if another tunnel (not the current one being dug) can be found in either of
        /// the four points vertically or horizontally of the current point.
        /// </summary>
        /// <param name="currentPosition">Center of search area</param>
        /// <param name="ownTunnel">List of points dug by current cycle</param>
        /// <returns>True if another tunnel is found, otherwise false</returns>
        private static bool IsJoiningOtherTunnel(Point currentPosition, List<Point> ownTunnel)
        {

            var testPoints = new Point[4];

            // Use int values of each Direction for indexes of array.
            int up = (int)GameConstants.Direction4.Up;
            int down = (int)GameConstants.Direction4.Down;
            int left = (int)GameConstants.Direction4.Left;
            int right = (int)GameConstants.Direction4.Right;

            // Load the 4 points around the current position
            testPoints[up].X = currentPosition.X;
            testPoints[up].Y = currentPosition.Y - 1;
            testPoints[right].X = currentPosition.X + 1;
            testPoints[right].Y = currentPosition.Y;
            testPoints[down].X = currentPosition.X;
            testPoints[down].Y = currentPosition.Y + 1;
            testPoints[left].X = currentPosition.X - 1;
            testPoints[left].Y = currentPosition.Y;

            // Check to see if a tunnel tile can be found on any of the four spaces
            Rectangle floorSpace = new Rectangle(0, 0, floorWidth, floorHeight);
            foreach (var testPoint in testPoints)
                if(floorSpace.Contains(testPoint) && // prevents overflow on the floorPlan array
                    floorPlan[testPoint.X, testPoint.Y] == FloorTile.Surfaces.Tunnel)
                    if (!ownTunnel.Contains(testPoint)) return true;
            return false;
        }


        /// <summary>
        /// Determines which direction to give preference towards to get from source to target. For
        /// example, if target is up and right of source, and the vertical distance is greater than
        /// the horizontal distance, than affinity should be up. Return values are 0(Up), 1(Right),
        /// 2(Down),3(Left).
        /// </summary>
        /// <param name="sourceX">X coordinate of source node</param>
        /// <param name="sourceY">Y coordinate of source node</param>
        /// <param name="targetX">X coordinate of target node</param>
        /// <param name="targetY">Y coordinate of target node</param>
        /// <returns>integer direction 0 to 4 as described above</returns>
        static private GameConstants.Direction4 AffinityDirection(Point source, Point target)
        {
            var distanceInDirection = new int[4];

            // Calculate distance toward target in each direction
            distanceInDirection[(int)GameConstants.Direction4.Up] = source.Y - target.Y;
            distanceInDirection[(int)GameConstants.Direction4.Left] = source.X - target.X;
            distanceInDirection[(int)GameConstants.Direction4.Down] = -distanceInDirection[(int)GameConstants.Direction4.Up];
            distanceInDirection[(int)GameConstants.Direction4.Right] = -distanceInDirection[(int)GameConstants.Direction4.Left];

            // Determine the greatest distance and return the direction
            return (GameConstants.Direction4)Array.IndexOf(distanceInDirection, distanceInDirection.Max());
        }

        /// <summary>
        /// Determines which of four directions should have great affinity and sets
        /// probability of selecting each direction, then returns the selected direction.
        /// As tunnel progresses towards target, affinity direction may change with each
        /// new source point. This ensures the general direction of the tunnel leads toward
        /// the target.
        /// </summary>
        /// <param name="source">Point to travel from</param>
        /// <param name="target">Point to travel to</param>
        /// <returns></returns>
        static private GameConstants.Direction4 PickDirection(Point source, Point target)
        {
            var directionProbability = new GameConstants.Range[4];

            // Set direction classifications
            var affinityDir = AffinityDirection(source, target);
            var altCounterClockwise = (affinityDir == GameConstants.Direction4.Up ? GameConstants.Direction4.Left : affinityDir - 1);
            var altClockwise = (affinityDir == GameConstants.Direction4.Left ? GameConstants.Direction4.Up : affinityDir + 1);
            var antiAffinityDir = (affinityDir > GameConstants.Direction4.Right ? affinityDir - 2 : affinityDir + 2);

            // Set probabilities
            directionProbability[(int)affinityDir].Low = 0;
            directionProbability[(int)affinityDir].High = 79;
            directionProbability[(int)altCounterClockwise].Low = 80;
            directionProbability[(int)altCounterClockwise].High = 89;
            directionProbability[(int)altClockwise].Low = 90;
            directionProbability[(int)altClockwise].High = 99;
            directionProbability[(int)antiAffinityDir].Low = -1; // No chance
            directionProbability[(int)antiAffinityDir].High = -100;

            var rnd = rand.Next(100);

            // Check each direction to see which the rnd value falls in between
            var direction = GameConstants.Direction4.Up;
            while (!(directionProbability[(int)direction].Low <= rnd && directionProbability[(int)direction].High >= rnd)) direction++;

            return direction;
        }

        /// <summary>
        /// Determines where to set the starting door for a tunnel and places it.
        /// </summary>
        /// <param name="room">Boundaries of room</param>
        /// <param name="wallSide">Which wall to place door</param>
        /// <returns></returns>
        static private Point SetStartingDoor(Rectangle room, GameConstants.Direction4 wallSide)
        {
            var doorLocation = new Point();

            // Determine which wall to place door
            if ((int)wallSide % 2 == 0) // Top and Bottom walls
            {
                doorLocation.X = rand.Next(room.Left, room.Right); // 
                doorLocation.Y = wallSide == GameConstants.Direction4.Up ? room.Top - 1 : room.Bottom;
            }
            else // Left and Right walls
            {
                doorLocation.Y = rand.Next(room.Top, room.Bottom);
                doorLocation.X = wallSide == GameConstants.Direction4.Left ? room.Left - 1 : room.Right;
            }

            // Place door
            floorPlan[doorLocation.X, doorLocation.Y] = FloorTile.Surfaces.OpenDoor;
            return doorLocation;
        }


        /// <summary>
        /// Iterates through all open doors to randomly change them to locked or hidden doors.
        /// Ensures that levels 1-5 will have none of these door types added by this method.
        /// </summary>
        static private void ChangeUpDoors()
        {
            GameConstants.Range hiddenProbability;
            GameConstants.Range lockedProbability;
            var doorList = new List<Point>();
            int rnd;

            // Range for hidden doors starts at 50, and locked doors starts at 75. The scope
            // of this range determines how likely the door will be hidden or locked.
            // If random number chosen does not fall within either range, then the door will
            // remain unlocked.
            hiddenProbability.Low = 50;
            lockedProbability.Low = 75;

            // As level increases, the odds should increase, up to a point.
            // Levels 1 - 5 have no chance of hidden or locked doors
            var probabilityRange = (floorNumber < 31 ? floorNumber - 6 : 24);
            hiddenProbability.High = hiddenProbability.Low + probabilityRange;
            lockedProbability.High = lockedProbability.Low + probabilityRange;


            // Iterate through each room to find doors only ad Open doors to the doorList
            foreach (var room in rooms)
            {
                for(int x=room.Left-1; x < room.Right+2; x++)
                {
                    if (GetTileAt(x, room.Top-1) == FloorTile.Surfaces.OpenDoor) doorList.Add(new Point(x, room.Top-1));
                    if (GetTileAt(x, room.Bottom+1) == FloorTile.Surfaces.OpenDoor) doorList.Add(new Point(x, room.Bottom+1));
                }

                for(int y=room.Height-1; y<room.Bottom+2; y++)
                {
                    if (GetTileAt(room.Left-1, y) == FloorTile.Surfaces.OpenDoor) doorList.Add(new Point(room.Left-1, y));
                    if (GetTileAt(room.Right+1, y) == FloorTile.Surfaces.OpenDoor) doorList.Add(new Point(room.Right+1, y));
                }
            }

            // For each door, pick random number and determine which range its in to set door tile
            foreach (var door in doorList)
            {
                
                rnd = rand.Next(100);
                if (rnd >= hiddenProbability.Low && rnd <= hiddenProbability.High)
                    floorPlan[door.X,door.Y] = FloorTile.Surfaces.HiddenDoor;
                if (rnd >= lockedProbability.Low && rnd <= lockedProbability.High)
                    floorPlan[door.X, door.Y] = FloorTile.Surfaces.LockedDoor;
            }
        }

        /// <summary>
        /// Locations a space to drop the stairs going down.
        /// </summary>
        static private void PlaceStairsDown()
        {
            int x; int y;
            do
            {
                int roomNumber = rand.Next(rooms.Count);
                x = rand.Next(rooms[roomNumber].Left, rooms[roomNumber].Right);
                y = rand.Next(rooms[roomNumber].Top, rooms[roomNumber].Bottom);
            } while (DungeonGameEngine.Hero.Location.X == x && DungeonGameEngine.Hero.Location.Y == y);

            floorPlan[x, y] = FloorTile.Surfaces.StairsDown;
        }

        
        #endregion



        #region Internally accessible methods

        /// <summary>
        /// Used to return a rectangle representing the map coordinates of any room
        /// containing the specified point.
        /// </summary>
        /// <param name="containingPoint">A Point within the room to be returned</param>
        /// <param name="foundRoom">The room containing the given point</param>
        /// <returns>True if room is found, otherwise false</returns>
        static internal bool GetRoomWithPoint(Point containingPoint, out Rectangle foundRoom)
        {
            foreach (var room in rooms)
                if (room.Contains(containingPoint))
                {
                    foundRoom = room;
                    return true;
                }

            foundRoom = new Rectangle();
            return false; 
        }

        /// <summary>
        /// Lights up all dark floor tiles in a room making the room a lit room
        /// </summary>
        internal static void LightRoom(Point startingPoint)
        {
            Rectangle currentRoom = new Rectangle();

            if (GetRoomWithPoint(startingPoint, out currentRoom))
            {


                // Expand space to include walls
                currentRoom.X--;
                currentRoom.Y--;
                currentRoom.Width += 2;
                currentRoom.Height += 2;

                for (int x = (currentRoom.X < 0 ? 0 : currentRoom.X);
                    x < (currentRoom.Right > FloorWidth ?
                    FloorWidth : currentRoom.Right); x++)

                    for (int y = (currentRoom.Y < 0 ? 0 : currentRoom.Y);
                        y < (currentRoom.Bottom > FloorHeight ?
                        FloorHeight : currentRoom.Bottom); y++)
                    {
                        FloorRevealed[x, y] = true;
                        if (floorPlan[x, y] == FloorTile.Surfaces.DarkRoom)
                            floorPlan[x, y] = FloorTile.Surfaces.LitRoom;
                    }
            }
        }


        static internal FloorTile.Surfaces GetTileAt(Point tileLocation)
        {
            return floorPlan[tileLocation.X, tileLocation.Y];
        }

        static internal FloorTile.Surfaces GetTileAt(Point startingPoint, GameConstants.Direction8 direction)
        {
            // If starting point is not within the boarders, return a Border because there is no Null option
            if (!(new Rectangle(1, 1, floorWidth-1, floorHeight-1).Contains(startingPoint))) return FloorTile.Surfaces.Border;
            return GetTileAt(PointInDirection(startingPoint, direction));
        }

        static internal FloorTile.Surfaces GetTileAt(int x, int y)
        {
            return GetTileAt(new Point(x, y));
        }

        internal static Point PointInDirection(Point startingPoint, GameConstants.Direction8 direction)
        { // TODO: This code would be could in a utility class because it is also used by the MagicalScroll
            Point newPoint = startingPoint;
            if (direction == GameConstants.Direction8.Down ||
                direction == GameConstants.Direction8.DownLeft ||
                direction == GameConstants.Direction8.DownRight) newPoint.Y++;
            if (direction == GameConstants.Direction8.Up ||
                direction == GameConstants.Direction8.UpLeft ||
                direction == GameConstants.Direction8.UpRight) newPoint.Y--;
            if (direction == GameConstants.Direction8.Left ||
                direction == GameConstants.Direction8.UpLeft ||
                direction == GameConstants.Direction8.DownLeft) newPoint.X--;
            if (direction == GameConstants.Direction8.Right ||
                direction == GameConstants.Direction8.UpRight ||
                direction == GameConstants.Direction8.DownRight) newPoint.X++;
            return newPoint;
        }

        /// <summary>
        /// Moves the draw rectangle x,y position in concurance with changed tile dimensions (map zooming)
        /// </summary>
        /// <param name="currentDrawRectangle">Rectangle to edit</param>
        /// <param name="currentSprite">graphic associated with the rectangle</param>
        /// <param name="tileWidth">scaled tile width</param>
        /// <param name="tileHeight">scaled tile height</param>
        static internal Rectangle TranslateRecToTile(Rectangle tileSize, Rectangle currentView, Point mapPos)
        {
            Rectangle currentDrawRectangle = tileSize; // sets width and height
            currentDrawRectangle.X = (mapPos.X - currentView.X) * tileSize.Width;
            currentDrawRectangle.Y = (mapPos.Y - currentView.Y) * tileSize.Height;
            return currentDrawRectangle;
        }

        static internal void DrawFloorTiles(SpriteBatch spriteBatch, Rectangle tileSize, Rectangle localView)
        {
            for (int x = 0; x < floorWidth; x++)
                for (int y = 0; y < floorHeight; y++)
                    if (floorRevealed[x, y] || GameConstants.DEBUG_MODE_REVEAL)
                    {
                        Point tileLocation = new Point(x, y);
                        bool alwaysLit = false; ;
                        var tile = GetTileAt(tileLocation);
                        if (tile == FloorTile.Surfaces.LitRoom ||
                            tile == FloorTile.Surfaces.Border ||
                            tile == FloorTile.Surfaces.StairsDown ||
                            tile == FloorTile.Surfaces.StairsUp ||
                            tile == FloorTile.Surfaces.HiddenDoor ||
                            tile == FloorTile.Surfaces.LockedDoor ||
                            tile == FloorTile.Surfaces.OpenDoor ||
                            tile == FloorTile.Surfaces.Wall) alwaysLit = true;

                        bool heroSighted = alwaysLit ||
                            Utility.InLineOfSight(
                            DungeonGameEngine.Hero.Location,
                            tileLocation,
                            Utility.GetVisibleDistance());

                        FloorTile.Draw(spriteBatch,
                            floorPlan[x, y],     // Tile type to draw
                            TranslateRecToTile( // Rectangle to draw in
                              tileSize,
                              localView,
                              tileLocation),
                            heroSighted);
                    }
        }

        static internal void DrawScatteredLoot(SpriteBatch spriteBatch, Rectangle tileSize, Rectangle localView)
        {
            foreach (var item in scatteredLoot)
            {
                if (floorRevealed[item.Location.X, item.Location.Y] || GameConstants.DEBUG_MODE_REVEAL)
                    item.Draw(spriteBatch, TranslateRecToTile(tileSize, localView, item.Location), localView);
            }

        }

        static internal void DrawScatteredGold(SpriteBatch spriteBatch, Rectangle tileSize, Rectangle localView, Texture2D sprite)
        {
            foreach(var kvPair in scatteredGold)
            {
                Point location = kvPair.Key;
                if (floorRevealed[location.X, location.Y] || GameConstants.DEBUG_MODE_REVEAL)
                {
                    Rectangle fittedRectangle = Utility.FittedSprite(sprite, TranslateRecToTile(tileSize, localView, location));
                    spriteBatch.Draw(sprite, fittedRectangle, Color.White);
                }
                   
            }
        }

        static internal bool TileIsPassible (FloorTile.Surfaces tile)
        {
            return (tile == FloorTile.Surfaces.DarkRoom ||
                    tile == FloorTile.Surfaces.LitRoom ||
                    tile == FloorTile.Surfaces.OpenDoor ||
                    tile == FloorTile.Surfaces.StairsDown ||
                    tile == FloorTile.Surfaces.StairsUp ||
                    tile == FloorTile.Surfaces.Tunnel);
        }

        /// <summary>
        /// Detects if hero is next to objects, doors, stairs that may stop him from running.
        /// Ignores spot where hero just left from.
        /// </summary>
        /// <returns>True if near an object worth stopping for</returns>
        static internal bool NearObject(Point heroLastLocation)
        {
            for (int x = DungeonGameEngine.Hero.Location.X - 1; x < DungeonGameEngine.Hero.Location.X + 2; x++)
                for (int y = DungeonGameEngine.Hero.Location.Y - 1; y < DungeonGameEngine.Hero.Location.Y + 2; y++)
                {
                    if (!(x == heroLastLocation.X && y == heroLastLocation.Y))
                    {
                        foreach (var item in scatteredLoot)
                            if (item.Location.X == x && item.Location.Y == y)
                                return true;
                        var tile = GetTileAt(x,y);
                        if (tile == FloorTile.Surfaces.LockedDoor ||
                            tile == FloorTile.Surfaces.OpenDoor ||
                            tile == FloorTile.Surfaces.StairsDown ||
                            tile == FloorTile.Surfaces.StairsUp)
                            return true;
                    }

                }
            return false;
        }


        /// <summary>
        /// Attempts to break a door down, changing it to an open door and moving the hero in
        /// </summary>
        /// <param name="doorLocation"></param>
        /// <returns>true if door bash successful.</returns>
        static internal bool BashDoor(Point doorLocation)
        {
            if (GetTileAt(doorLocation) == FloorTile.Surfaces.LockedDoor)
            {
                float bashModifier = (float)DungeonGameEngine.Hero.EffectiveStrength / 100;

                if (rand.NextDouble() < 0.05f + bashModifier)
                {
                    floorPlan[doorLocation.X, doorLocation.Y] = FloorTile.Surfaces.OpenDoor;
                    Monster.InitializeDistanceMap();
                    DungeonGameEngine.ProcessMessageQueue(false, "The door busts wide open.");
                    DungeonGameEngine.Hero.Location = doorLocation;
                    return true;
                }
                else
                {
                    DungeonGameEngine.ProcessMessageQueue(false, "The door is a little weaker now.");
                }
            }
                return false;
        }

        

        /// <summary>
        /// Applies CHANCE_TO_FIND in detecting hidden doors and traps around the hero.
        /// </summary>
        /// <param name="foundText">String of what was found for output message</param>
        /// <returns>True if something was detected, otherwise false.</returns>
        static internal bool NoticeHidden(out string message, int chanceMultiplier = 1)
        {
            // Check all spaces around hero. Note, if it were under the hero's foot, it
            // would have already been discovered (traps for instance). Removed logic to avoid checking
            // that space because it would affect all the other eight spaces by adding one more
            // comparison to each.
            // TODO: Remove debug option to findHidden();
            message = string.Empty;
            for (int x = DungeonGameEngine.Hero.Location.X - 1; x < DungeonGameEngine.Hero.Location.X + 2; x++)
                for (int y = DungeonGameEngine.Hero.Location.Y - 1; y < DungeonGameEngine.Hero.Location.Y + 2; y++)
                    if (x > 0 && y > 0 && x < floorWidth && y < floorHeight &&
                        GetTileAt(x,y) == FloorTile.Surfaces.HiddenDoor &&
                        rand.Next(100) < GameConstants.CHANCE_TO_FIND * chanceMultiplier)
                    {
                        floorPlan[x, y] = FloorTile.Surfaces.OpenDoor;
                        message = "hidden door";
                        return true;
                    }
            return false;
        }

        /// <summary>
        /// Supports decision to draw monsters by checking if hero can see them or has awareness of them.
        /// Also gives monster awareness of hero if within sight.
        /// </summary>
        /// <param name="monster"></param>
        /// <returns></returns>
        static internal bool WithinSightOf(Monster monster)
        {

            var tile = GetTileAt(DungeonGameEngine.Hero.Location);

            // If hero and monster are both in same room and it is lit, let everyone see each other
            Rectangle currentRoom;
            if (tile == FloorTile.Surfaces.LitRoom &&
                GetRoomWithPoint(DungeonGameEngine.Hero.Location, out currentRoom) &&
                currentRoom.Contains(monster.Location))
            {
                monster.LetMonsterSeeHero();
                return true;
            }


                // If hero is in dark room, door or tunnel, and monster is within lineOfSight
            int heroNightSightDistance = Utility.GetVisibleDistance(); // 0 if hero is blind
            int monsterDistance = (int)(Utility.Distance(
                DungeonGameEngine.Hero.Location, monster.Location));
            int heroSightTestDistance = MathHelper.Min(heroNightSightDistance, monsterDistance);
            int monsterSightTestDistance = MathHelper.Max(heroNightSightDistance, monsterDistance);
            
            if (tile == FloorTile.Surfaces.DarkRoom ||
                tile == FloorTile.Surfaces.Tunnel ||
                tile == FloorTile.Surfaces.OpenDoor)
            {
                bool heroSeesMonster = Utility.InLineOfSight(DungeonGameEngine.Hero.Location, monster.Location, heroSightTestDistance);

                if (Utility.InLineOfSight(DungeonGameEngine.Hero.Location, monster.Location, monsterSightTestDistance))
                    monster.LetMonsterSeeHero();
                if (heroSeesMonster) return true;
            }

            // When hero detects monsters magically, by this point they are not in sight so no need for LetMonsterSeeHero()
            /*if (InventoryEffectManager.HeroDetectsMonsters)
            {
                Rectangle senseRange = new Rectangle(
                    DungeonGameEngine.Hero.Location.X - GameConstants.DETECT_MONSTER_EFFECT_RANGE,
                    DungeonGameEngine.Hero.Location.Y - GameConstants.DETECT_MONSTER_EFFECT_RANGE,
                    GameConstants.DETECT_MONSTER_EFFECT_RANGE * 2,
                    GameConstants.DETECT_MONSTER_EFFECT_RANGE * 2);
                if (senseRange.Contains(DungeonGameEngine.Hero.Location)) return true;
            } */

            return false; 
        }

        /// <summary>
        /// Identifies spaces near hero that are to be revealed up upon entry into
        /// a lit room or movement in the dark.
        /// </summary>
        static internal void ShowLitSpaces()
        {
            int visibleDistance = Utility.GetVisibleDistance();

            Rectangle visibleSpace;

            // If hero is in a lit room, light up the room, otherwise, light up only the spaces next
            // to the hero (distance and visiblity of lit room affected by blindness and seeing in dark).
            if (GetTileAt(DungeonGameEngine.Hero.Location)
                == FloorTile.Surfaces.LitRoom && visibleDistance > 0)
            {
                if (GetRoomWithPoint(DungeonGameEngine.Hero.Location, out visibleSpace))
                {
                    // Expand space to include walls
                    visibleSpace.X--;
                    visibleSpace.Y--;
                    visibleSpace.Width += 2;
                    visibleSpace.Height += 2;
                }
            }
            else
            {
                visibleSpace = new Rectangle(DungeonGameEngine.Hero.Location.X - visibleDistance, DungeonGameEngine.Hero.Location.Y - visibleDistance, visibleDistance * 2 + 1, visibleDistance * 2 + 1);
            }

            // Cycle through floorRevealed elements to turn them on if within the new
            // visibleSpace rectangle.
            // BUG: Hero should not be able to see through walls
            // injected conditional values to prevent sending array out of range of the map.
            for (int x = (visibleSpace.X < 0 ? 0 : visibleSpace.X);
                x < (visibleSpace.Right > floorWidth ? floorWidth : visibleSpace.Right); x++)
                for (int y = (visibleSpace.Y < 0 ? 0 : visibleSpace.Y);
                    y < (visibleSpace.Bottom > floorHeight ? floorHeight : visibleSpace.Bottom); y++)
                // If blind, can only reveal non-floor tiles (walls, stiars, tunnels, etc.
                {
                    floorRevealed[x, y] = true;
                    if (!InventoryEffectManager.HeroBlind ||
                        !(GetTileAt(x, y) == FloorTile.Surfaces.DarkRoom ||
                        GetTileAt(x, y) == FloorTile.Surfaces.LitRoom)
                    ) floorRevealed[x, y] = true;
                }

            if (visibleDistance > 0)
                // TODO: Draw glowing light around hero on dark floor tiles
                return; // Then remove this
        }

        static internal void ScatterDroppedLoot(List<InventoryItem> droppedLoot, out string message)
        {
            message = string.Empty;
            while (droppedLoot.Count > 0)
            {
                // Locate free space around hero to drop an item, starting with space above 
                bool foundSpace = false;
                Point dropPoint = DungeonGameEngine.Hero.Location;
                for (GameConstants.Direction8 testDirection = GameConstants.Direction8.Up;
                    testDirection <= GameConstants.Direction8.UpLeft; testDirection++)
                {
                    var testPoint = Utility.GetNewPointFrom(testDirection, DungeonGameEngine.Hero.Location);
                    if (!(MovementBlockedByFloor(testPoint) || LootFoundAt(testPoint)))
                    {
                        dropPoint = testPoint;
                        foundSpace = true;
                        break;
                    }
                }

                if (foundSpace)
                {
                    //Drop the last item in the list
                    var item = droppedLoot.Last();
                    item.Location = dropPoint;
                    if(droppedLoot.Remove(item)) ScatteredLoot.Add(item);
                }
                else
                {
                    // return all remaining items to inventory and inform player
                    message="No room to drop anything.";
                    Inventory.AddRange(droppedLoot);
                    droppedLoot.Clear();
                }
            } 

        }

        /// <summary>
        /// Checks to see if new space is beyond the map boarders or is a non-walkable surface.
        /// </summary>
        /// <param name="newX"></param>
        /// <param name="newY"></param>
        /// <returns></returns>
        static internal bool MovementBlockedByFloor(Point checkPoint)
        {
            // Map boarder check
            if ((checkPoint.X <= 0) ||
                (checkPoint.Y <= 0) ||
                (checkPoint.X >= floorWidth - 1) ||
                (checkPoint.Y >= floorHeight - 1)) return true;

            // Tile type check
            if (!GameConstants.DEBUG_MODE_WALK_THROUGH_WALLS)
                if (!TileIsPassible(GetTileAt(checkPoint)))
                    return true;

            return false;
            
        }

        /// <summary>
        /// Checks to see if near tunnel intersection. Allows up to 2 neighboring tunnel
        /// spaces near hero (excluding diagonals) to not be in an intersection.
        /// </summary>
        /// <returns></returns>
        internal static bool NearTunnelIntersection()
        {
            var tunnelCount = 0;

            if (TunnelContinues(GameConstants.Direction8.Up, DungeonGameEngine.Hero.Location)) tunnelCount++;
            if (TunnelContinues(GameConstants.Direction8.Down, DungeonGameEngine.Hero.Location)) tunnelCount++;
            if (TunnelContinues(GameConstants.Direction8.Left, DungeonGameEngine.Hero.Location)) tunnelCount++;
            if (TunnelContinues(GameConstants.Direction8.Right, DungeonGameEngine.Hero.Location)) tunnelCount++;
            if (tunnelCount > 2)
                return true;
            else return false;

        }

        /// <summary>
        /// Checks to see if the tunnel continues in the direction indicated.
        /// </summary>
        /// <param name="direction">Direction from given point</param>
        /// <param name="fromPoint">Point to look from</param>
        /// <returns>True if tunnel continues in the indicated direction, otherwise false</returns>
        internal static bool TunnelContinues(GameConstants.Direction8 direction, Point fromPoint)
        {
            var newPoint = Utility.GetNewPointFrom(direction, fromPoint);
            return (GetTileAt(newPoint) == FloorTile.Surfaces.Tunnel);
        }



        #endregion



        #region DEBUG TOOLS
        // These tools are created to test features as they are added.

        // Added to test discovery of hidden doors. Retained for when traps are added. The idea is
        // that the immediate window can be used to set the hero next to the door so it can walk back
        // and fourth (or use search button when implemented) to find it and see that it is revealed
        // safely and the message queue is updated.
        internal static int FindHidden(out string message)
        {
            List<KeyValuePair<Point, FloorTile.Surfaces>> hiddenPlaces = new List<KeyValuePair<Point, FloorTile.Surfaces>>();
            message = string.Empty;
            for (int x = 0; x < floorWidth; x++)
                for (int y = 0; y < floorHeight; y++)
                    if (floorPlan[x, y] == FloorTile.Surfaces.HiddenDoor)
                        hiddenPlaces.Add(new KeyValuePair<Point, FloorTile.Surfaces>(new Point(x, y), floorPlan[x, y]));

            if (hiddenPlaces.Count == 0) message="No hidden objects found";
            else message="Found something at " + hiddenPlaces[0].Key.ToString();
            return hiddenPlaces.Count;
        }


        #endregion
    }
}
