

namespace DungeonGeek
{
    internal static class GameConstants
    {


        #region Enums and structs
        internal enum Direction4 { Up, Right, Down, Left }
        internal enum Direction8 { Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft }
        internal enum InputControlFocus { HallOfRecords, MainGameScreen, MainMenu, EnterName, Inventory, Instructions, MessageHistory, RenameItemScreen, ConfirmExitScreen }
        internal enum ItemClasses { Weapon, Armor, Potion, Scroll, MagicalRing, Food }
        
        internal enum monsterClasses { Rat }


        // NOTE: With any temporary or permant effect, strings needed to be added for ...
        //   Changes in status of temporary effects - InventoryEffectManager constructor
        //   Discovery of the effect and strength - MagicalRing or any other such sub class added
        internal enum TemporaryEffects // Intended for potions, scrolls and traps
        {
            Undefined,          // Default used when not set with a value
            Hastened,           // Moves twice for each monster move
            Slowed,             // Moves only every other turn while monsters continue to move
            Blind,              // Does not light up rooms or reveal floor tiles
            Confused,           // All moves are in random directions (and run is eratic)
            Overburdened,       // Slowed, but not relieved by time (set to -1 when used)
            Poisoned,           // Causes damage each turn instead of healing (same rate)
            ImprovedNightSight, // Visible area in dark rooms and tunnels exended by 2 tiles in all directions
            SensesMonster,      // Monsters on the map within DETECT_MONSTER_EFFECT_RANGE tiles are revealed each turn
            Stuck,              // Can't move due to trap, relieved by time
            Fainted,            // Fell asleep due to lack of food or other reasons
            Observant           // 5x chance to notice hidden (multiplied when searching)
        }
        
        internal enum InstantEffects // Intended for restoration, or other permanant changes not following the hero
            // May be used by potions and scrolls
        {
            Undefined,          // Default used when not set with a value
            RestoreStrength, // Restores strength if below max, but does not raise it any above that - before persistant modifiers
            RemoveCurse, // Removes curse on items allowing them to be removed
            Identify, // Properly identifies an item (renames it) without using it.
            RevealMap, // Reveals the entire floor
            AscendStairs, // Causes an upward stair case to appear on the map as a revealed tile and centers view momentarily.
            LightRoom, // Lightens a dark room, otherwise nothing
            VaporizeMonsters // Instantly vaporizes all monsters visible or detected through other effects (needs to be rare)


        }

        internal enum RingEffects // Intended for rings, or any other wearable item added later
        {
            IncreaseStrength,
            IncreaseHitPoints,
            EnhanceArmor,
            ExtendSight
        };

        internal struct Range // Used to represent a number range
        {
            public int Low;
            public int High;
        }
        
        #endregion



        #region Constants
                


        #region File locations
        // Content file locations
        internal const string FONT_PATH = @"Fonts\";
        internal const string FONT_GAMETEXT = FONT_PATH + @"Arial";
        // TODO: Add other fonts to avoid scaling them to make fonts look larger.
        internal const string SPRITE_PATH = @"Sprites\";
        internal const string SPRITE_PATH_MAGICAL = SPRITE_PATH + @"Magical Items\";
        internal const string SPRITE_PATH_ARMOR = SPRITE_PATH + @"Armor\";
        internal const string SPRITE_PATH_WEAPON = SPRITE_PATH + @"Weapons\";
        internal const string SPRITE_PATH_MISC = SPRITE_PATH + @"Misc\";
        internal const string SPRITE_PATH_MONSTER = SPRITE_PATH + @"Monsters\";
        // GameStorage
        internal const string FILE_SCORES = @"hof.dat";
        internal const string FILE_AUTOSAVE = @"asGame.dat";
        #endregion



        #region Floor and tile dimensions
        // Floor plan uses tiled references to track type of surface. Objects found
        // within the dungeon will have location relative to this floor plan.
        // Normal is 80x80 and 800x800 is too slow
        // 400x400 is pretty tight and is the recommended limit unless the max rooms calculation is decreased
        internal const int FLOOR_WIDTH_START = 80;
        internal const int FLOOR_HEIGHT_START = 80;
        internal const int FLOOR_WIDTH_MAX = 400;
        internal const int FLOOR_HEIGHT_MAX = 400;

        // Pixels each tile in floor plan will consume on screen at normal zoom level
        internal const int TILE_SIDE_NORMAL = 35;
        internal const int TILE_SIDE_MININUM = 4;
        #endregion



        #region Windows / Viewports
        // Size of Child Window and margins used within
        internal const int CHILD_WINDOW_WIDTH = 600;
        internal const int CHILD_WINDOW_HEIGHT = 500;

        // Size of Input Window
        internal const int INPUT_VIEW_WIDTH = CHILD_WINDOW_WIDTH - 10;
        internal const int INPUT_VIEW_HEIGHT = 100;

        // Size of Medium Window
        internal const int GAME_MENU_VIEW_WIDTH = 350;
        internal const int GAME_MENU_VIEW_HEIGHT = 250;
        
        #endregion



        #region Text and underlining alignment

        internal const int TOP_MARGIN = 5;
        internal const int HEAD_FOOT_LEFT = 5;
        internal const int LIST_LEFT = 10;
        internal const float UNDERLINE_RATIO = 0.15f;
        internal const int LINE_SPACING = 3; // Space between lines in pixels
        internal const int INV_MAX_TITLE_WIDTH = CHILD_WINDOW_WIDTH - 20;
        internal const int HERO_NAME_MAX_WIDTH = CHILD_WINDOW_WIDTH - 20;

        // Create some text to help restrict the length of a name entry.
        internal const string RESERVED_FOR_OBITUARY_TEXT = " killed by a Hob Goblin on level 150 with 888,888,888 gold";
        #endregion



        #region Timing
        // Loop timing (game time)
        internal const int NO_DELAY = 0;
        internal const int INITIAL_KEY_DELAY = 500;
        internal const int HOLD_KEY_DELAY = 20;
        internal const int MOUSE_HIDE_DELAY = 1500;

        // Turn counting
        internal const int FAINT_TIME_MIN = 3; // Small because it will occur frequently when feelsFaint.
        internal const int FAINT_TIME_MAX = 5;
        internal const int SHORTER_EFFECT_MIN = 10; // Stuck, Confused, Poisoned
        internal const int SHORTER_EFFECT_MAX = 20;
        internal const int LONGER_EFFECT_MIN = 50;  // Hastened, Slowed, Blind, ImprovedNightSight, Observant
        internal const int LONGER_EFFECT_MAX = 200;

        #endregion



        #region Game limits and settings
        // Game Control
        internal const int CHANCE_TO_FIND = 20;
        internal const int STAT_FULL_ENERGY = 3000; 
        internal const int ENERGY_FAINT_LEVEL = 500;
        internal const double CHANCE_OF_FAINT = 0.10;
        internal const string TITLE_BAR_TEXT = "Dungeon Geek";
        internal const int MESSAGE_HISTORY_RETENTION_LENGTH = 19;
        internal const int DETECT_MONSTER_EFFECT_RANGE = 50; // tile range around hero to detect monsters when effect is on
        #endregion
        


        #region Floor Generation
        internal const float DROP_LOOT_CHANCE = 0.003f; // Base chance per floor tile to drop anything before deciding what
                                                        // Normal is 0.003f

        internal const float DROP_GOLD_CHANCE = 0.002f;
        internal const float SPAWN_MONSTER_CHANCE = 0.006f;

        // Floor dimensions are the number of tiles in walkable floor space excluding walls and doors
        internal const int MIN_ROOM_FLOOR_DIMENSION = 5;
        internal const int MAX_ROOM_FLOOR_DIMENSION = 16; // normal 16
        internal const int MIN_MARGIN_BETWEEN_ROOMS = 1; // width of single tunnel/passage way
        #endregion



        #region Debug Options
        // DEBUG options                                            // Toggle       Use
        internal static bool DEBUG_MODE_WALK_THROUGH_WALLS = false; // Ctrl+Alt+W   Automatic
        internal static bool DEBUG_MODE_REVEAL = false;             // Ctrl+Alt+R   Automatic
        internal static bool DEBUG_MODE_ALLOW_FLOOR_JUMPING = false;// Ctrl+Alt+F   F2(down)/F3(up)
        internal static bool DEBUG_MODE_ALLOW_REMOVE_CURSED = false;// Ctrl+Alt+C   Inventory Screen
        internal static bool DEBUG_MODE_DRAW_STEPS_TO_HERO = false; // Ctrl+Alt+S   Automatic
        internal static bool DEBUG_MODE_ENABLE_FREE_LOOT = false;   // Ctrl+Alt+L   L
        internal const int DEBUG_OPTION_SET_ROOM_COUNT = 0; // Set to 0 to ignore
        internal const int DEBUG_OPTION_LOGGING_LEVEL = 0; // 0 - None, 1 - brief, 2 - verbose
        internal const bool DEBUG_OPTION_ENABLE_DEBUG_MODE_TOGGLE = true;
        
        #endregion


        #endregion Constants


    }
}
