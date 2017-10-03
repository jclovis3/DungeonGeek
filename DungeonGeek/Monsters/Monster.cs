using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace DungeonGeek
{
    internal abstract class Monster : ICombatant
    {
        #region Fields

        // static fields
        protected static Random rand = InventoryEffectManager.RandomNumberGenerator;
        private static List<Monster> monsters;
        // Route mapping variables to remain static outside of the recursive method
        private static bool foundTarget;
        private static int minHops;
        private static int[,] pathCounters;
        private static int unmappedValue = 999999999;

        // To keep the algorithm from slowing down, a maxDistance has to be assumed for monsters to
        // sniff out the hero from out in hallways if such an effect should be added. If not, this still
        // serves for within line of sight in any room size.
        private static int maxDetectRange = 20;

        // Monster generics
        protected GameConstants.monsterClasses monsterClass;
        protected Point location;

        // graphic and drawing info
        protected string spriteFile;
        protected Texture2D sprite;
        private static Dictionary<GameConstants.monsterClasses, Texture2D> monsterSprites;

        // Battle stats
        protected int _HP;   // current HP of monster
        protected int maxHP;
        protected int healRate; // HP per monster turn of healing, if any
        protected int _XP;   // XP Awarded to hero

        protected GameConstants.Range floorLevelRange;
        protected bool awake = true;
        protected bool discovered = false; // A discovered monster remains visible if it is asleep
        // When monster detected through magical effect, set discovered to true after it moves
        // each turn and only when in range of effect. That way when the effect wears off,
        // the move will make it undiscovered and not visible for drawing anymore
        protected bool chasingHero = false;
        protected bool roaming = true; // By default, if monster is awake, it should be roaming.
        // A monster may also be awake but standing still or guarding something.
        protected List<InventoryItem> loot = new List<InventoryItem>(); // What he has to take when dead
                                                                        // Note, monsters that steal will put what they take in this loot basket
        protected float goldDropChance = 0.125f; // To be changed by individual monster classes if special (like Lepreacons and dragons)
        protected int goldDropAmt;

        // DEBUG Variables
        private static GameText[,] pathCounterText;
        private static GraphicsDevice graphicsDevice;

        #endregion


        #region Properties    



        public int Max_HP
        { get { return maxHP; } }                          // ICmbatant
        // Reports such at "The rat hit you" and "You killed the skelleton" should be valid,
        // so the name would be simply "rat" or "skelleton"
        public abstract string EntityName { get; }    // ICmbatant

        public abstract CombatManager.AbilityScores AbilityScores { get; } // Added for future expansion into more D&D features
        public abstract CombatManager.AttackDelivery AttackDeliveryPacket { get; }
        public abstract CombatManager.DefenseDelivery DeffenseDeliveryPacket { get; }

        internal static List<Monster> Monsters
        {
            get { return monsters; }
            set { monsters = value; }
        }
        internal int XP { get { return _XP; } }
        internal Point Location { get { return location; } }
        internal GameConstants.Range FloorLevelRange { get { return floorLevelRange; } }

        /// <summary>
        /// Discloses weather the hero has discovered the monster through sight or other magical means.
        /// Read only because CheckDiscovered() sets this to true after also determining if the
        /// monster has detected the hero.
        /// </summary>
        internal bool Discovered { get { return discovered; } }

        internal int MaxDetectRange
        {
            get { return maxDetectRange; }
        }

        #endregion

        #region Constructors
        internal Monster()
        {
            // Sets a base level for any monster if their class doesn't specify an amount later.
            goldDropAmt = rand.Next(FloorGenerator.LevelNumber,FloorGenerator.LevelNumber * 5+1);
        }


        #endregion


        #region Instance Methods

        public override string ToString()
        {
            string actionState = string.Empty;
            if (!awake) actionState = "Sleeping ";
            else if (chasingHero) actionState = "Attacking ";
            else if (roaming) actionState = "Roaming ";
            else actionState = "Guarding ";

            return string.Format("{0} {1} ({2},{3})",
                actionState,
                monsterClass.ToString(),
                location.X,
                location.Y);

        }

        internal void Draw(SpriteBatch spriteBatch, Rectangle currentDrawRectangle, Rectangle currentView)
        {
            if (currentView.Contains(Location))
            {
                sprite = monsterSprites[monsterClass];
                spriteBatch.Draw(sprite, Utility.FittedSprite(sprite,currentDrawRectangle), Color.White);
            }

        }


        /// <summary>
        /// Allows each monster type to change it's state based on what happens when within sight of hero.
        /// Monsters may be triggered to start chasing the hero, or run towards a treasure to guard it.
        /// </summary>
        /// <returns>True if monster is visible to the hero</returns>
        internal abstract void LetMonsterSeeHero();

        /// <summary>
        /// Used to allow monster to move, either by roaming or dart straight for the hero and attack if close enough.
        /// </summary>
        /// <param name="heroLocation">Where the hero is located</param>
        /// <returns>Number of hitpoints monster strikes with, or -1 if didn't strike and 0 if missed</returns>
        internal void MoveOrAttack()
        {
            if (!awake) {return; }
            if (roaming) Roam();
            else if (!NextToHero(location))
                ChaseTarget();
            else
                Attack(DungeonGameEngine.Hero);

        }

        /// <summary>
        /// Sends attack message with added target for computation, then delivers results
        /// as messages to the player.
        /// </summary>
        /// <param name="target">The target of the attack (in this case, the hero)</param>
        public void Attack(ICombatant target)
        {
            var attackPacket = AttackDeliveryPacket;
            attackPacket.Target = target;
            var attackResult = CombatManager.DeliverAttack(attackPacket);

            if (attackResult.Critical)
                DungeonGameEngine.ProcessMessageQueue(false,
                    "The " + EntityName + " got a lucky shot inflicting extra damage.");
            else if (attackResult.Fumble)
            {
                DungeonGameEngine.ProcessMessageQueue(false,
                    "You are slightly amused to see the " + EntityName + " strike at your shadow.");
            }
            else if (attackResult.Hit)
                DungeonGameEngine.ProcessMessageQueue(false,
                    "The " + EntityName + " hit you.");
            else
                DungeonGameEngine.ProcessMessageQueue(false,
                    "The " + EntityName + " missed you.");

        }

        public CombatManager.DefenseDelivery Defend()
        {
            awake = true;
            chasingHero = true;
            return DeffenseDeliveryPacket;
        }

        public  abstract void TakeDamage(CombatManager.DamageResult results);

        internal static void Die(Monster deadMonster, ICombatant attacker)
        {
            // To prevent accidentally forgetting to place the call to EarnXP in each monster subclass
            // it was placed in this method with a requirement to pass the attacker.
            if (monsters.Remove(deadMonster))
            {
                CombatManager.AppendDeathMessage("You have killed the " + deadMonster.EntityName + ".");
                deadMonster.DropLoot();

                // Because loot is visible if the tile is revealed, this will explain why you can see it when the hero is blind.
                if (InventoryEffectManager.HeroBlind)
                    DungeonGameEngine.ProcessMessageQueue(false, "You hear the " + deadMonster.EntityName + " drop something.");
            }
            else
                throw new ArgumentOutOfRangeException("Dead monster was not in the monsters list.");
            attacker.EarnXP(deadMonster._XP);
        }

        private void DropLoot()
        {
            if (loot.Count > 0)
                foreach (var item in loot)
                {
                    // Drop all loot in the same tile the monster died in
                    // PLANNING: Maybe later scatter the loot a monster drops ... or maybe not...
                    item.Location = Location;
                    FloorGenerator.ScatteredLoot.Add(item);
                }
            else
                // Chance to drop gold
                if (rand.NextDouble() < goldDropChance)
                FloorGenerator.RandomlyDropGold(location.X, location.Y, true, goldDropAmt);
        }
        public void EarnXP(int xp) { } // Monsters won't gain XP in this game
            

        internal void Roam()
        {
            // TODO: Make another Move method with path finding to get a monster moving toward a given point
            // For when an effect triggers all monsters to detect the hero or behavior should be less eratic.
            // This method should actually be used in the HeroStrikable method to move towards the hero.
            bool monsterMoved = false;
            Point checkPoint;
            while(!monsterMoved)
            {
                GameConstants.Direction8 moveDirection = (GameConstants.Direction8)rand.Next(8);
                checkPoint = FloorGenerator.PointInDirection(location, moveDirection);
                if(FloorGenerator.TileIsPassible(FloorGenerator.GetTileAt(checkPoint)))
                {
                    location = checkPoint;
                    monsterMoved = true;
                }
            }
            CheckVisibility();
        }


        internal bool HeroStrikable()
        {
            
            
            if (NextToHero(location)) return true;
            return false;
        }

        private void CheckVisibility()
        {
            // If Hero escapes out a door while monster still inside, need to set detected to off
            if (!Utility.InLineOfSight(location, DungeonGameEngine.Hero.Location, Utility.GetVisibleDistance()))
                discovered = false;
        }

        #endregion



        #region Static Methods
        internal static void LoadContent(ContentManager contentManager)
        {
            monsterSprites = new Dictionary<GameConstants.monsterClasses, Texture2D>();
            monsterSprites.Add(
                GameConstants.monsterClasses.Rat,
                contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_MONSTER + "rat_sm_25x35"));

        }

        internal static void Initialize(GraphicsDevice graphics)
        {
            graphicsDevice = graphics;

        }

        internal static void DrawDiscoveredMonsters(SpriteBatch spriteBatch, Rectangle tileSize, Rectangle localView)
        {
            foreach (var monster in monsters)
            {
                if (monster.Discovered || GameConstants.DEBUG_MODE_REVEAL)
                    monster.Draw(spriteBatch, FloorGenerator.TranslateRecToTile(tileSize, localView, monster.Location), localView);
            }

        }

        internal static void DrawCounters(SpriteBatch spriteBatch, Rectangle tileSize, Rectangle currentView)
        {
            Rectangle currentDrawRectangle;
            if (GameConstants.DEBUG_MODE_DRAW_STEPS_TO_HERO)
                for (int x = 1; x < FloorGenerator.FloorWidth - 1; x++)
                    for (int y = 1; y < FloorGenerator.FloorHeight - 1; y++)
                    {
                        currentDrawRectangle = FloorGenerator.TranslateRecToTile(tileSize, currentView, new Point(x, y));
                        pathCounterText[x, y].Location = new Point(
                            currentDrawRectangle.Center.X - pathCounterText[x, y].Width / 2,
                            currentDrawRectangle.Center.Y - pathCounterText[x, y].Height / 2);
                        pathCounterText[x, y].Draw(spriteBatch);
                    }
        }
        
        /// <summary>
        /// Generates loot that a monster may cary limitted to their cary limit and chance of having loot
        /// </summary>
        /// <param name="lootProbability"></param>
        /// <param name="lootMaxWeight"></param>
        /// <returns></returns>
        protected static List<InventoryItem> StartingLoot(float lootProbability, float lootMaxWeight)
        {
            List<InventoryItem> newLoot = new List<InventoryItem>();
            if (rand.NextDouble() < lootProbability)
            {
                InventoryItem newLootItem = null;
                do
                {
                    int lootOption = rand.Next(3);

                    switch (lootOption)
                    {
                        case 0: newLootItem = new Food(); break;
                        case 1: newLootItem = new MagicalRing(); break;
                        case 2: newLootItem = MagicalScroll.GenerateNewScroll(); break;
                    }
                } while (newLootItem.InventoryWeight > lootMaxWeight);
                newLoot.Add(newLootItem);
            }
            return newLoot;
        }

        internal static Monster MonsterFoundAt(Point checkPoint)
        {
            foreach (var monster in monsters)
                if (monster.Location == checkPoint) return monster;
            return null;
        }

        internal static bool MonsterNextTo(Point checkPoint)
        {
            foreach (var tile in Utility.TilesAround(checkPoint))
                if (MonsterFoundAt(tile) != null) return true;
            return false;
        }





        /* Mapping the best route across the floor to the hero will require:
         *  1. Initialize int array with same size as floor plan using CreateMapForMonsters()
         *      a. First time, all values are 0
         *      b. If a tile is not walkable, mark the space as -1
         *      c. Each reset of the array will only reset values > 0 back to 0 using ResetPathFinder()
         *  2. Starting from hero (with value 1), cycle outward adding 1 to hop count in any space in the
         *      array not containing a value > 0 (or already blocked by -1). This continues until each
         *      frenzied monster has been identified with a value > 0 in the array (at which point there is no
         *      need to continue any furthur).
         *      a. To cycle outward, each point in the array should be ran through an outer loop, testing each space
         *          around it for a value > 0.
         *      b. When a value > 0 is detected, pick the lowest value found and add 1 for the current space.
         *  3. During a monster turn, each monster checks the points immediately around him to pick a
         *      point with the smallest number to move to. If this point is the hero, then the attack commences.
         *  4. The pathCounters array must be reset and recalculated with each move of the hero if, and
         *      only if any one monster is triggered to seek out the hero.
         *  
         */



        internal static void CalculateDistanceMap()
        {

            ResetPositiveValues(); // Ignores blocked tiles with value of -1

            // Step 2, set space with hero to 1
            if(GameConstants.DEBUG_MODE_WALK_THROUGH_WALLS &&
                FloorGenerator.MovementBlockedByFloor(DungeonGameEngine.Hero.Location))
            {
                // Setting hero's tile to 1 when he walks on a wall causes everything to freeze so we need to
                // abort drawing the distance map. Monsters will stop moving if chasing the hero is true.
                return;

            }
            else
                pathCounters[DungeonGameEngine.Hero.Location.X, DungeonGameEngine.Hero.Location.Y] = 1;

            int outerlayer = 1; // Actualy one higher than outer layer to avoid repeating math in the for loop or <= operator.
            int layer;
            
            do
            {
                outerlayer++;
                for(layer = 1; layer < outerlayer; layer++)
                    foreach(var checkPoint in PointsInLayer(layer,DungeonGameEngine.Hero.Location))
                    {
                        Rectangle floorBounds = new Rectangle(0, 0, FloorGenerator.FloorWidth, FloorGenerator.FloorHeight);
                        if (!floorBounds.Contains(checkPoint))
                            continue;

                        // Skip space if it's already blocked
                        if (pathCounters[checkPoint.X, checkPoint.Y] == -1)
                        {
                            if (GameConstants.DEBUG_MODE_DRAW_STEPS_TO_HERO)
                                pathCounterText[checkPoint.X, checkPoint.Y].Text = pathCounters[checkPoint.X, checkPoint.Y].ToString();
                            continue;
                        }


                        // First, check to see if space is still unmapped
                        if(pathCounters[checkPoint.X, checkPoint.Y] == unmappedValue)
                        {
                            // Check all spaces around current space to get smallest value > 0, then add one to it
                            var lowestPosValue = FindLowestPositiveValue(checkPoint.X, checkPoint.Y);
                            if (lowestPosValue > 0 && lowestPosValue < unmappedValue && lowestPosValue < pathCounters[checkPoint.X, checkPoint.Y] + 1)
                                pathCounters[checkPoint.X, checkPoint.Y] = lowestPosValue + 1;
                            
                            // Added in case DEBUG_MODE_WALK_THROUGH_WALLS enabled to
                            // close up the path left on the blocking tiles
                            if (GameConstants.DEBUG_MODE_WALK_THROUGH_WALLS &&
                                !FloorGenerator.TileIsPassible(FloorGenerator.GetTileAt(checkPoint)))
                                    pathCounters[checkPoint.X, checkPoint.Y] = -1; 
                        }

                        if (GameConstants.DEBUG_MODE_DRAW_STEPS_TO_HERO)
                        {
                            if (pathCounters[checkPoint.X, checkPoint.Y] > 99) pathCounterText[checkPoint.X, checkPoint.Y].Text = "XX";
                            else pathCounterText[checkPoint.X, checkPoint.Y].Text = pathCounters[checkPoint.X, checkPoint.Y].ToString();
                        }
                    }
            } while (!GridMapped(outerlayer) );
            
        }

        

        internal static bool MonsterHeroMutualDetection()
        {
            bool monsterSighted = false;

            // Must cycle through each monster to trigger them to notice the hero too.
            foreach (var monster in monsters)
            {
                if (FloorGenerator.WithinSightOf(monster) && !InventoryEffectManager.HeroBlind)
                {
                    monsterSighted = true;
                    monster.discovered = true;
                }
                if(InventoryEffectManager.HeroDetectsMonsters)
                    monster.discovered = true;
            }
            return monsterSighted;
        }

        /// <summary>
        /// Used after each hero move to allow all monsters to move about and attack.
        /// </summary>
        internal static void MoveMonsters()
        {
            CalculateDistanceMap();
            foreach (var monster in monsters)
                monster.MoveOrAttack();
            
        }

        private static int FindLowestPositiveValue(int x, int y)
        {
            int lowestValue = int.MaxValue;
            for (int a = x - 1; a < x + 2; a++)
                for (int b = y - 1; b < y + 2; b++)
                    if (!(a == x && b == y) && pathCounters[a, b] > 0 && pathCounters[a, b] < lowestValue)
                        lowestValue = pathCounters[a, b];
            return lowestValue;
        }

        /// <summary>
        /// To be called with each new floor, identifies blocked tiles in the pathCounters array with
        /// a value of -1. When hero discovers hidden doors or bashes open locked doors, call this
        /// again to update the map so new routes can be plotted.
        /// </summary>
        internal static void InitializeDistanceMap()
        {
            pathCounters = new int[FloorGenerator.FloorWidth, FloorGenerator.FloorHeight];

            // DEBUG Feature
            if(GameConstants.DEBUG_MODE_DRAW_STEPS_TO_HERO)
                pathCounterText = new GameText[FloorGenerator.FloorWidth, FloorGenerator.FloorHeight];

            for (int x = 0; x < FloorGenerator.FloorWidth; x++)
                for (int y = 0; y < FloorGenerator.FloorHeight; y++)
                {
                    pathCounters[x, y] = unmappedValue;
                    if(!FloorGenerator.TileIsPassible(FloorGenerator.GetTileAt(x,y))) pathCounters[x, y] = -1;
                    // DEBUG Feature
                    if(GameConstants.DEBUG_MODE_DRAW_STEPS_TO_HERO)
                        pathCounterText[x, y] = new GameText(graphicsDevice);
                }
        }
        

        /// <summary>
        /// Resets the pathCounters array preserving any -1 locations.
        /// </summary>
        private static void ResetPositiveValues()
        {
            for (int x = 0; x < FloorGenerator.FloorWidth; x++)
                for (int y = 0; y < FloorGenerator.FloorHeight; y++)
                    if (pathCounters[x, y] > 0)
                    {
                        pathCounters[x, y] = unmappedValue;
                        if (GameConstants.DEBUG_MODE_DRAW_STEPS_TO_HERO)
                            pathCounterText[x, y].Text = "XX";
                    }
        }

        /// <summary>
        /// Determines if any monsters exist who are chasing the hero that have yet to have the map
        /// calculated all the way back to its position.
        /// </summary>
        /// <returns></returns>
        private static bool GridMapped(int outerlayer)
        {
            // Some paths that bend back toward hero may not be mapped with this algorithm, but monsters in
            // these spaces should be confused by their senses as the direction they detect the hero would have
            // to be in the opposite direction as their way out of the room to get to him. This cut-off ensures the
            // loop exits when one quadrant of it has gotten larger than the floor.
            if (outerlayer > FloorGenerator.FloorWidth && outerlayer > FloorGenerator.FloorHeight)
                return true;

            foreach (var monster in monsters)
                if(monster.chasingHero && pathCounters[monster.Location.X,monster.Location.Y] == unmappedValue)
                        return false;
            return true;
        }

        internal static Point[] PointsInLayer(int layer, Point targetPoint)
        {
            Point[] pts = new Point[layer * 8];
            int i = 0;
            for (int x = targetPoint.X - layer; x <= targetPoint.X + layer; x++)
            {// Top and bottom rows
                pts[i++] = new Point(x, targetPoint.Y - layer);
                pts[i++] = new Point(x, targetPoint.Y + layer);
            }
            for (int y = targetPoint.Y - layer + 1; y < targetPoint.Y + layer; y++)
            {
                pts[i++] = new Point(targetPoint.X - layer, y);
                pts[i++] = new Point(targetPoint.X + layer, y);
            }
            return pts;
        }

        internal static bool NextToHero(Point currentLocation)
        {
            // To be next to the hero, the current position X and Y values cannot exceed
            // +- 1 from hero location values.
            if (Math.Abs(currentLocation.X - DungeonGameEngine.Hero.Location.X) <= 1 &&
                Math.Abs(currentLocation.Y - DungeonGameEngine.Hero.Location.Y) <= 1)
                return true;
            else return false;
        }

        internal void ChaseTarget()
        {
            // Find lowest path values around monster that is not occupied by another monster
            Point nextPosition = location;

            // To help randomize movements a little when several options exist with the same value
            List<Point> possibleNextSteps = new List<Point>();

            int lowestValue = int.MaxValue;
            for (int x = location.X - 1; x < location.X + 2; x++)
                for (int y = location.Y - 1; y < location.Y + 2; y++)
                    if (pathCounters[x, y] > 0 &&
                        pathCounters[x, y] <= lowestValue &&
                        pathCounters[x, y] < unmappedValue &&
                        FloorGenerator.TileIsPassible(FloorGenerator.GetTileAt(x, y)) &&
                        MonsterFoundAt(new Point(x,y))==null)
                    {
                        if (pathCounters[x, y] == lowestValue)
                            possibleNextSteps.Add(new Point(x, y));
                        else
                        {
                            lowestValue = pathCounters[x, y];
                            possibleNextSteps.Clear();
                            nextPosition.X = x; nextPosition.Y = y;
                            possibleNextSteps.Add(nextPosition);
                        }
                    }
            // Randomly pick one of the possible steps having the same value
            if (possibleNextSteps.Count > 0)
                location = possibleNextSteps[Utility.Rand.Next(possibleNextSteps.Count)];
            
            
                
            
            // If path mapping disabled due to DEBUG_MODE_WALK_THROUGH_WALLS
            // then location will not change as the mapping will halt before the monster.
            CheckVisibility();
        }

        

        #endregion



    }
}
