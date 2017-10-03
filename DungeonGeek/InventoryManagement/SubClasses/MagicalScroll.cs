using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGeek
{

    /// <summary>
    /// Creates a magical scroll. Scrolls should be created once and stored in a list so that the
    /// discovery of what it does is retained. Any new scroll distributed should be copied from this
    /// list. The scroll is IStackable so scrolls of the same effect (and therefor same name) can be
    /// stacked.
    /// </summary>
    class MagicalScroll : InventoryItem, IConsumable, IStackable
    {

        const float UNIT_WEIGHT = 0.05f;

        #region Fields

        private Type effectType;
        // Should be either
        //      typeof(GameConstants.TemporaryEffects)
        //      typeof(GameConstants.InstantEffects)
        private GameConstants.TemporaryEffects tempEffect;  // When scroll is assigned a temporary effect, set it here
        private GameConstants.InstantEffects instantEffect; // When scroll is assigned an instant effect, set it here
        private int qty;                                    // Supports IStackable
        private string titleOfOneScroll;


        // Static retention of all scrolls to preserve discovered names and effects
        private static Dictionary<MagicalScroll, float> scrollProbability;

        #endregion


        public int Qty                                  // IStackable
        { get { return qty; } }

        internal override string InventoryTitle
        {
            get
            {
                if (qty > 1) return titleOfOneScroll + "(" + qty + ")";
                else return titleOfOneScroll;
            }
        }

        internal override string IdentifyText
        {
            get
            {
                if (!discoveredEffect) NameScroll();
                return "it is " + titleOfOneScroll;
            }
        }


        #region Constructors and Generators


        /// <summary>
        /// Constructor to create a scroll template of a predefined type for the scrollArchives.
        /// Declared private so a new scroll isn't created that doesn't have an original in the archive.
        /// </summary>
        /// <param name="effectType"></param>
        /// <param name="tempEffect"></param>
        /// <param name="instantEffect"></param>
        private MagicalScroll(Type effectType, GameConstants.TemporaryEffects tempEffect, GameConstants.InstantEffects instantEffect)
        {
            titleOfOneScroll = "a scroll called \"" + GenerateMagicalName() + "\"";
            discoveredEffect = false;
            InventoryWeight = UNIT_WEIGHT;
            Location = Point.Zero;
            qty = 0;
            itemClass = GameConstants.ItemClasses.Scroll;
            this.effectType = effectType;
            this.tempEffect = tempEffect;
            this.instantEffect = instantEffect;
        }

        
        /// <summary>
        /// Selects a scroll from the archives to copy, preserving any discovered effects for the
        /// new scroll.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        internal static MagicalScroll GenerateNewScroll(Point location = new Point())
        {
            MagicalScroll newScroll = null;
            KeyValuePair<MagicalScroll, float> kvPair;
            while (newScroll == null)
            {

                // Randomly pick a scroll, then roll against probability of selecting that scroll
                int i = rand.Next(scrollProbability.Count);
                kvPair = scrollProbability.ElementAt(i);
                if (kvPair.Value > rand.NextDouble())
                {
                    newScroll = (MagicalScroll)kvPair.Key.MemberwiseClone();
                    newScroll.qty = 1;
                    newScroll.Location = location;
                    newScroll.AssignNewID();
                }
            }
            return newScroll;
        }

        #endregion

        /// <summary>
        /// Resets the MagicalScroll static data to the state needed for a new game. All scrolls
        /// created in a previous game are deleted and the dictionary is repopulated with new scrolls
        /// each having a new unique magical name.
        /// </summary>
        internal static void InitNewGame()
        {
            scrollProbability = new Dictionary<MagicalScroll, float>();

            GameConstants.TemporaryEffects[] tempEffectList =
            {
                GameConstants.TemporaryEffects.Blind,
                GameConstants.TemporaryEffects.Confused,
                GameConstants.TemporaryEffects.Hastened,
                GameConstants.TemporaryEffects.ImprovedNightSight,
                GameConstants.TemporaryEffects.Observant,
                GameConstants.TemporaryEffects.SensesMonster,
                GameConstants.TemporaryEffects.SensesMonster,
                GameConstants.TemporaryEffects.Slowed
            };

            GameConstants.InstantEffects[] instantEffectList =
            {
                GameConstants.InstantEffects.VaporizeMonsters,
                GameConstants.InstantEffects.AscendStairs,
                GameConstants.InstantEffects.Identify,
                GameConstants.InstantEffects.RevealMap,
                GameConstants.InstantEffects.RestoreStrength,
                GameConstants.InstantEffects.RemoveCurse,
                GameConstants.InstantEffects.LightRoom
            };

            float[] instEffectProbability = { 0.02f, 0.03f, 0.10f, 0.15f, 0.10f, 0.10f, 0.10f };

            foreach (var selectedEffect in tempEffectList)
            {
                MagicalScroll newScroll = new MagicalScroll(typeof(GameConstants.TemporaryEffects),
                    selectedEffect, GameConstants.InstantEffects.Undefined);
                scrollProbability.Add(newScroll, 0.05f); // Each temp effect has even chance of selection
            }

            for (int i = 0; i < instantEffectList.Count(); i++)
            {
                MagicalScroll newScroll = new MagicalScroll(typeof(GameConstants.InstantEffects),
                    GameConstants.TemporaryEffects.Undefined, instantEffectList[i]);
                scrollProbability.Add(newScroll, instEffectProbability[i]);
            }


        }

        /// <summary>
        /// Triggers the discovery and effect of the scroll. Does not reduce qty because when qty
        /// reaches 1 before consumption, the Inventory will delete it from the list.
        /// </summary>
        /// <returns></returns>
        public string Consume()                         // IConsumable
        {
            if (!discoveredEffect)
            {
                NameScroll();
                DungeonGameEngine.ProcessMessageQueue(false, "It is " + titleOfOneScroll);
                discoveredEffect = true;
                
            }
            TriggerEffect();
            return "The scroll turned to ash as you finished reading it.";
        }

        internal static void UpdateDisoveredStatus(MagicalScroll newScroll)
        {
            if(!newScroll.discoveredEffect)
            {
                MagicalScroll archiveScroll = FindInArchives(newScroll);
                newScroll.discoveredEffect = archiveScroll.discoveredEffect;
                newScroll.titleOfOneScroll = archiveScroll.titleOfOneScroll;
            }
        }

        private static MagicalScroll FindInArchives(MagicalScroll newScroll)
        {
            foreach(var kvPair in scrollProbability)
                if (kvPair.Key.IsSimilar(newScroll)) return kvPair.Key;
            return null;
        }


        /// <summary>
        /// Removes a single item from the stack and returns it as a separate stack of 1
        /// </summary>
        /// <returns>Separate item removed</returns>
        public IStackable Remove()                      // IStackable
        {
            if (qty > 1)
            {
                qty--;
                
                MagicalScroll removedScroll = (MagicalScroll)MemberwiseClone();
                removedScroll.qty = 1;
                InventoryWeight = qty * UNIT_WEIGHT;
                return removedScroll;

            }
            else throw new ArithmeticException("A pile of food cannot exist with qty = 0");
        }


        /// <summary>
        /// Adds 1 or more to the stack
        /// </summary>
        /// <param name="qty">Number of items to add to the stack</param>
        public void Add(int qty = 1)                    // IStackable
        {
            if (qty > 0) this.qty += qty;
            else throw new ArithmeticException("Cannot add a negative qty to a pile of food");
            InventoryWeight = qty * UNIT_WEIGHT;
        }

        internal override bool IsSimilar(InventoryItem other)
        {
            if (other == null || other.Class != Class) return false;
            var otherScroll = other as MagicalScroll;
            if (otherScroll.effectType != effectType) return false;
            if (otherScroll.tempEffect != tempEffect) return false;
            if (otherScroll.instantEffect != instantEffect) return false;
            return true;
        }



        

        private void NameScroll()
        {
            if(effectType == typeof(GameConstants.TemporaryEffects))
            {
                switch (tempEffect)
                {
                    case GameConstants.TemporaryEffects.Hastened:
                        titleOfOneScroll = "a scroll of speed"; break;
                    case GameConstants.TemporaryEffects.Slowed:
                        titleOfOneScroll = "a scroll of slowness"; break;
                    case GameConstants.TemporaryEffects.Blind:
                        titleOfOneScroll = "a scroll of utter darkness"; break;
                    case GameConstants.TemporaryEffects.Confused:
                        titleOfOneScroll = "a scroll of confusion"; break;
                    case GameConstants.TemporaryEffects.ImprovedNightSight:
                        titleOfOneScroll = "a scroll of increased night vision"; break;
                    case GameConstants.TemporaryEffects.SensesMonster:
                        titleOfOneScroll = "a scroll monster awareness"; break;
                    case GameConstants.TemporaryEffects.Stuck:
                        titleOfOneScroll = "a scroll of paralysis"; break;
                    case GameConstants.TemporaryEffects.Observant:
                        titleOfOneScroll = "a scroll of perception"; break;
                }
            }
            else
            {
                switch (instantEffect)
                {
                    case GameConstants.InstantEffects.RestoreStrength:
                        titleOfOneScroll = "a scroll of restore strength"; break;
                    case GameConstants.InstantEffects.RemoveCurse:
                        titleOfOneScroll = "a scroll of remove curse"; break;
                    case GameConstants.InstantEffects.Identify:
                        titleOfOneScroll = "a scroll of identify"; break;
                    case GameConstants.InstantEffects.RevealMap:
                        titleOfOneScroll = "a scroll with a map on it"; break;
                    case GameConstants.InstantEffects.AscendStairs:
                        titleOfOneScroll = "a scroll of magical assending stairs"; break;
                    case GameConstants.InstantEffects.LightRoom:
                        titleOfOneScroll = "a scroll so bright it could light a room"; break;
                    case GameConstants.InstantEffects.VaporizeMonsters:
                        titleOfOneScroll = "a scroll of vaporize known monsters"; break;
                }
            } // end if(effectType == typeof(GameConstants.TemporaryEffects))

            

            // Update the name for the scrollArchives
            MagicalScroll archiveScroll = FindInArchives(this);
            archiveScroll.titleOfOneScroll = titleOfOneScroll;
            archiveScroll.discoveredEffect = true;
            
        }


        /// <summary>
        /// Calls any actions required to put the scroll's effect into motion when consumed
        /// </summary>
        /// <returns>string message to user when effect occurs</returns>
        private void TriggerEffect()
        {
            
            if (effectType == typeof(GameConstants.TemporaryEffects))
            {
                switch (tempEffect)
                {
                    case GameConstants.TemporaryEffects.Hastened:
                        InventoryEffectManager.HeroHastened = true; break;
                    case GameConstants.TemporaryEffects.Slowed:
                        InventoryEffectManager.HeroSlowed = true; break;
                    case GameConstants.TemporaryEffects.Blind:
                        InventoryEffectManager.HeroBlind = true; break;
                    case GameConstants.TemporaryEffects.Confused:
                        InventoryEffectManager.HeroConfused = true; break;
                    case GameConstants.TemporaryEffects.ImprovedNightSight:
                        InventoryEffectManager.HeroSeesInDark = true; break;
                    case GameConstants.TemporaryEffects.SensesMonster:
                        InventoryEffectManager.HeroDetectsMonsters = true; break;
                    case GameConstants.TemporaryEffects.Stuck:
                        InventoryEffectManager.HeroStuck = true; break;
                    case GameConstants.TemporaryEffects.Observant:
                        InventoryEffectManager.HeroObservant = true; break;
                }
            }
            else
            {
                switch (instantEffect)
                {
                    case GameConstants.InstantEffects.RestoreStrength:
                        DungeonGameEngine.Hero.RestoreStrength();
                        DungeonGameEngine.ProcessMessageQueue(false, "Your strength has been restored.");
                        break;
                    case GameConstants.InstantEffects.RemoveCurse:
                        Inventory.RemoveCurseItem(); break;
                    case GameConstants.InstantEffects.Identify:
                        Inventory.IdentifyItem(); break;
                    case GameConstants.InstantEffects.RevealMap:
                        RevealMap();
                        DungeonGameEngine.ProcessMessageQueue(false, "You suddenly feel as if you have been here before.");
                        break;
                    case GameConstants.InstantEffects.AscendStairs:
                        CreateUpStairs();
                        DungeonGameEngine.ProcessMessageQueue(false, "The scroll flies from your hands and transorms into stairs, going up.");
                        break;
                    case GameConstants.InstantEffects.LightRoom:
                        LightSpaces();
                        DungeonGameEngine.ProcessMessageQueue(false, "The scroll burned so brightly the walls are still glowing");
                        break;
                    case GameConstants.InstantEffects.VaporizeMonsters:
                        VaporizeMonsters();
                        DungeonGameEngine.ProcessMessageQueue(false, "The scroll says it is safe now, but do you believe it?");
                        break;
                    default:
                        break;
                }
                
            }
        }

        private void RevealMap()
        {
            for (int x = 0; x < FloorGenerator.FloorWidth; x++)
                for (int y = 0; y < FloorGenerator.FloorHeight; y++)
                    FloorGenerator.FloorRevealed[x, y] = true;
        }

        private void CreateUpStairs()
        {
            DungeonGameEngine.ProcessMessageQueue(false, "This scroll has not been implemented yet.");
        }

        /// <summary>
        /// Divides the algorithms for deciding how to light up an area from the scroll's effect
        /// </summary>
        private void LightSpaces()
        {
            Point startingPoint = DungeonGameEngine.Hero.Location;

            if (FloorGenerator.FloorPlan
                [startingPoint.X, startingPoint.Y] == FloorTile.Surfaces.DarkRoom )
                FloorGenerator.LightRoom(startingPoint);
            else
            if (FloorGenerator.FloorPlan
                [startingPoint.X, startingPoint.Y] == FloorTile.Surfaces.Tunnel)
                    LightTunnel();
            else
                if (FloorGenerator.FloorPlan
                [startingPoint.X, startingPoint.Y] == FloorTile.Surfaces.OpenDoor)
            {
                LightTunnel();

                // Check all four sides of door to find the room tile
                if (FloorGenerator.FloorPlan
                    [startingPoint.X, startingPoint.Y + 1] == FloorTile.Surfaces.DarkRoom)
                    startingPoint.Y++;
                else if (FloorGenerator.FloorPlan
                    [startingPoint.X, startingPoint.Y - 1] == FloorTile.Surfaces.DarkRoom)
                    startingPoint.Y--;
                else if (FloorGenerator.FloorPlan
                    [startingPoint.X, startingPoint.X + 1] == FloorTile.Surfaces.DarkRoom)
                    startingPoint.X++;
                else if (FloorGenerator.FloorPlan
                    [startingPoint.X, startingPoint.X - 1] == FloorTile.Surfaces.DarkRoom)
                    startingPoint.X--;

                FloorGenerator.LightRoom(startingPoint);
            }
        }

        

        private void LightTunnel()
        {
            // Cycle through each of 8 directions, lighting up every tile from hero until void or wall is reached
            // Doors should be lit up but the light shines on through it revealing a line of spaces inside.
            // Walls will also be lit but the light will stop on them.
            foreach (GameConstants.Direction8 direction in Enum.GetValues(typeof(GameConstants.Direction8)))
                LightTilesInRow(DungeonGameEngine.Hero.Location, direction);
        }

        /// <summary>
        /// Reveals any non-blocking tile in a straight line but stops at doors because the light in
        /// a lit room doesn't light up a hallway.
        /// </summary>
        /// <param name="startingPoint"></param>
        /// <param name="direction"></param>
        private void LightTilesInRow(Point startingPoint, GameConstants.Direction8 direction)
        {
            bool blocked = false;
            Point currentPoint = startingPoint;
            FloorTile.Surfaces currentSurface;
            while (!blocked)
            {
                currentSurface = FloorGenerator.GetTileAt(currentPoint);
                FloorGenerator.FloorRevealed[currentPoint.X, currentPoint.Y] = true;

                if(!FloorGenerator.TileIsPassible(currentSurface))
                    blocked = true;
                if (currentPoint == startingPoint)
                    blocked = false; // Allows effect if standing in doorway to split both ways
                if (blocked) break;

                // Move to next tile
                currentPoint = FloorGenerator.PointInDirection(currentPoint, direction);
            }
        }

        private void VaporizeMonsters()
        {
            DungeonGameEngine.ProcessMessageQueue(false, "This scroll has not been implemented yet.");
            // requires list of visible or detected monsters
        }


    }
}
