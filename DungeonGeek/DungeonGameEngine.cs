#define DEBUG
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DungeonGeek
{
    /// <summary>
    /// Runs the game loop and all major event cycles are launched from here.
    /// </summary>
    public class DungeonGameEngine : Game
    {

        // Planning Notes:
        // PLANNING: Add MORE monsters - at least a few low level ones with no special tricks to get started.
        // PLANNING: Finish implementing any scroll effects not fully implemented
        // PLANNING: Implement variable zoom by mouse scroll wheel and keyboard
        // PLANNING: Make hero and stairs grow and shrink repeatedly in map view (when not moving) to make them easier to see
        // PLANNING: Allow mouse click to set target run-to point
        // PLANNING: Provide pan to stairs feature after they are found
        // PLANNING: Create data load and save features
        // PLANNING: Get player name at the beginning to record high score list
        // PLANNING: After trying a scroll or potion, force renaming it
        // PLANNING: Implement Treasure/Monster rooms
        // PLANNING: Implement Golden Grail to be mixed up in very large treasure room somewhere
        // PLANNING: Refer to rules in D&D for armor class rating and calculations.





        #region Fields

        internal static DungeonGameEngine thisGame;
            
            
        // Graphics and game timing
        int elapsedTime = 0;
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        static private Texture2D goldSprite;
        static private Viewport statsViewPort;
        static private Viewport mainViewPort;
        static private Viewport smallWindow;
        static private Viewport mediumWindow;
        static private Viewport largeWindow;
        static private Viewport defaultViewPort;


        // Map view and manipulation
        static Rectangle localView; // Viewable area of map - moves with hero
        // localView helps to control what gets drawn by limiting all drawing to those objects that
        // are located within the localView area. This view is tile based, not pixel based.
        static int tileHeight = GameConstants.TILE_SIDE_NORMAL;
        static int tileWidth = GameConstants.TILE_SIDE_NORMAL;
        static Dictionary<Keys, GameConstants.Direction8> panningDirectionKeys = new Dictionary<Keys, GameConstants.Direction8>();


        static bool mapViewEnabled;
        static Rectangle lastWindowSize;

        // Game state data
        static private bool gameStarted = false;
        static private bool newGame = false;
        static private bool resetGetNameTiming;
        static Hero hero;
        private static int goldScore;
        static StringBuilder statsRow;
        static int floorLevel;
        //private int floorWidth = GameConstants.FLOOR_WIDTH_START;
        //private int floorHeight = GameConstants.FLOOR_HEIGHT_START;
        
        static List<string> outputMessageQueue = new List<string>();
        static Random rand = new Random();
        static GameText statsText;
        static GameText messageText;
        static GameText moreText;
        static bool evenCycle; // Used for hastened and slowed effect to trigger movement of hero or monsters every other cycle
        static bool gameEnding;
        


        // Keyboard handling
        static KeyboardState keyState;
        static bool rapidRepeat; // rapid repeat of same move
        static bool isShiftDown;
        static bool isRunning;
        static bool waitForKeyRelease = false;
        static GameConstants.Direction8 currentDirection = GameConstants.Direction8.Up;
        static Dictionary<Keys, GameConstants.Direction8> movementDirectionKeys = new Dictionary<Keys, GameConstants.Direction8>();
        static Keys lastNonStateKeyPressed = Keys.None;  // Stores a key other than Shift, Alt, Ctrl, CAPS_LOCK.
        static int currentDelay = GameConstants.NO_DELAY;
        static GameConstants.InputControlFocus screenHavingFocus = GameConstants.InputControlFocus.MainGameScreen; // used when inventory or other menus are open

        // Mouse handling
        static MouseState mouse;
        static MouseState oldMouse;
        static int mouseCursorTime = 0;
        #endregion


        #region Properties

        internal static DungeonGameEngine ThisGame
        {
            get { return thisGame; }
        }

        internal static Hero Hero
        {
            get { return hero; }
            set { hero = value; }
        }

        internal static bool GameEnding
        { set { gameEnding = value; } }

        



        #endregion



        #region Constructor
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        public DungeonGameEngine()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            thisGame = this;
        }

        #endregion
        



        #region Game Class Overrides and Events

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            IsMouseVisible = true;
            ShowAboutScreen();
            screenHavingFocus = GameConstants.InputControlFocus.MainMenu;

            // Adjust window to 3/4 of screen size and allow resizing
            graphics.PreferredBackBufferWidth = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width* 0.75);
            graphics.PreferredBackBufferHeight = (int)(GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height * 0.75);
            graphics.ApplyChanges();
            
            lastWindowSize = Window.ClientBounds;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += Window_ClientSizeChanged;
            Window.Title = GameConstants.TITLE_BAR_TEXT;


            // Initialize all graphhic components
            goldSprite = Content.Load<Texture2D>(GameConstants.SPRITE_PATH_MISC + "PileOfGold_35x17");
            GameMenuScreen.Initialize(graphics.GraphicsDevice);
            Inventory.Initialize(graphics.GraphicsDevice);
            Monster.Initialize(graphics.GraphicsDevice);
            RenameItemScreen.Initialize(graphics.GraphicsDevice);
            EnterNameScreen.Initialize(graphics.GraphicsDevice);
            InstructionsScreen.Initialize(graphics.GraphicsDevice);
            MessageHistory.Initialize(graphics.GraphicsDevice);
            ConfirmExitScreen.Initialize(graphics.GraphicsDevice);
            HallOfRecordsScreen.Initialize(graphics.GraphicsDevice);


            statsRow = new StringBuilder();
            statsText = new GameText(UpdatedStatText(),graphics.GraphicsDevice);
            messageText = new GameText(graphics.GraphicsDevice);
            moreText = new GameText("<MORE>",graphics.GraphicsDevice);
            
            statsViewPort = 
            mainViewPort = 
            largeWindow =
            mediumWindow = 
            smallWindow = 
            defaultViewPort = GraphicsDevice.Viewport;

            oldMouse = Mouse.GetState();


            movementDirectionKeys.Add(Keys.Up, GameConstants.Direction8.Up);
            movementDirectionKeys.Add(Keys.Down, GameConstants.Direction8.Down);
            movementDirectionKeys.Add(Keys.Left, GameConstants.Direction8.Left);
            movementDirectionKeys.Add(Keys.Right, GameConstants.Direction8.Right);
            movementDirectionKeys.Add(Keys.Home, GameConstants.Direction8.UpLeft);
            movementDirectionKeys.Add(Keys.PageUp, GameConstants.Direction8.UpRight);
            movementDirectionKeys.Add(Keys.End, GameConstants.Direction8.DownLeft);
            movementDirectionKeys.Add(Keys.PageDown, GameConstants.Direction8.DownRight);

            panningDirectionKeys.Add(Keys.A, GameConstants.Direction8.Left);
            panningDirectionKeys.Add(Keys.D, GameConstants.Direction8.Right);
            panningDirectionKeys.Add(Keys.W, GameConstants.Direction8.Up);
            panningDirectionKeys.Add(Keys.S, GameConstants.Direction8.Down);
            
            base.Initialize();
        }


        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // SpriteBatch is used to draw textures to the GPU in a single stream.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load content for each class using graphics
            Hero.Sprite = Content.Load<Texture2D>(Hero.SpriteFile);
            GameText.LoadContent(Content);
            FloorTile.LoadContent(Content);
            Inventory.LoadContent(Content);
            Monster.LoadContent(Content);
            

            
            
            UpdateViewPorts();
            

            


        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // REMINDER: Unload any non ContentManager content here
            base.UnloadContent();
        }

       
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            #region Mouse Cursor
            // Allow mouse cursor to show or hide in each form

            mouse = Mouse.GetState();
            if (mouse != oldMouse)
            {
                mouseCursorTime = 0;
                IsMouseVisible = true;
            }
            if (mouseCursorTime > GameConstants.MOUSE_HIDE_DELAY && mouse == oldMouse)
            {
                // Mouse has not moved for a short time, make it vanish
                IsMouseVisible = false;
            }
            oldMouse = mouse;

            #endregion



#if DEBUG
            if (GameConstants.DEBUG_MODE_WALK_THROUGH_WALLS || GameConstants.DEBUG_MODE_REVEAL)
                Window.Title = GameConstants.TITLE_BAR_TEXT + " Hero: " + hero.Location;
#endif           
            // Ensure maximize / restore event is handled
            if (lastWindowSize != Window.ClientBounds)
                Window_ClientSizeChanged(this,EventArgs.Empty);

            // Display next message if there is one
            ProcessMessageQueue();

            // Get new keyState and Shift status
            lastNonStateKeyPressed = GetKeyPressed();

            
            elapsedTime += gameTime.ElapsedGameTime.Milliseconds;
            mouseCursorTime += gameTime.ElapsedGameTime.Milliseconds;


            #region Automatic Movement and other updates

            // If user not holding a key down, then turn repeat delay off. If forced to wait for 
            // key release, then this signifies that the key has been released.
            if (lastNonStateKeyPressed == Keys.None)
            {
                waitForKeyRelease = false;
                currentDelay = GameConstants.NO_DELAY;
            }


            if(gameStarted)
            {
                // If hero is unconcious, cancel run if active, move the monsters and update effect timing
                // Effectively skips hero's turn to move or control game events.
                if (!InventoryEffectManager.HeroConcious && GameConstants.HOLD_KEY_DELAY < elapsedTime)
                {
                    isRunning = false;
                    elapsedTime = 0;
                    rapidRepeat = false;
                    waitForKeyRelease = true;
                    FinishTurn();
                }

                // Hero slowed causes skipping of any commands every other cycle
                else if (InventoryEffectManager.HeroSlowed && evenCycle)
                {
                    FinishTurn();
                }

                // Hero is concious. If running, control running interval with same interval as hold key
                else if (isRunning && GameConstants.HOLD_KEY_DELAY < elapsedTime)
                {
                    elapsedTime = 0;
                    currentDelay = GameConstants.NO_DELAY; // used when falling out of run cycle
                    rapidRepeat = false;
                    waitForKeyRelease = true;
                    HeroRuns(); // Used here to continue the running cycle once started
                }

            }
            
            

            // Handle message queue acknowledgements if more than one message in queue
            if (!waitForKeyRelease && lastNonStateKeyPressed == Keys.Space && outputMessageQueue.Count > 0 )
                ProcessMessageQueue(true); // Acknowledge message

            if (gameEnding && outputMessageQueue.Count == 0)
            {
                // Saves death message until last
                if (hero.DeathMessage != string.Empty)
                {
                    outputMessageQueue.Add(hero.DeathMessage);
                    hero.DeathMessage = string.Empty;
                }
                else
                {
                    screenHavingFocus = GameConstants.InputControlFocus.HallOfRecords;
                    gameStarted = false;
                    gameEnding = false;
                    DirectPlayerInputs(gameTime);
                }
                waitForKeyRelease = true;
            }

            #endregion



            #region Manual Movement

            // delay is 0 until key is pressed, so this should execute right
            // away when key initially pressed
            else if (!gameEnding && !isRunning && outputMessageQueue.Count < 2 && currentDelay < elapsedTime && !waitForKeyRelease)
            {
                elapsedTime = 0;

                // Decide where current inputs should be passed along to. If no other child window open,
                // movement, search and rest inputs will trigger monsters to move and other game turn advancements
                DirectPlayerInputs(gameTime);
                rapidRepeat = true;




                // Cycle delay in response to repeat state
                // First, ensure control is local, otherwise remove delay
                // if coming in with no delay and local control, next delay should be INITIAL_KEY_DELAY
                // if delay is already at INITIAL_KEY_DELAY and holding key, advance to RAPID_DELAY
                if (lastNonStateKeyPressed != Keys.None)
                {

                    if (screenHavingFocus != GameConstants.InputControlFocus.MainGameScreen)
                        currentDelay = GameConstants.NO_DELAY; // Allows timing control to be handled by other windows.
                    else if (currentDelay == GameConstants.NO_DELAY)
                        currentDelay = GameConstants.INITIAL_KEY_DELAY;
                    else if (rapidRepeat && currentDelay == GameConstants.INITIAL_KEY_DELAY)
                        currentDelay = GameConstants.HOLD_KEY_DELAY;
                    else if (!rapidRepeat) currentDelay = GameConstants.INITIAL_KEY_DELAY;
                }
                else
                {
                    currentDelay = GameConstants.NO_DELAY;
                    rapidRepeat = false;
                }

            }


            #endregion


            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // Reset window to a black screen (ignores viewport bounds)
            GraphicsDevice.Clear(Color.Black);

            if (gameStarted)
            {
                DrawMainView();
                DrawStatsView();
            }

            // Pass drawing to any open windows or screens
            switch (screenHavingFocus)
            {
                case GameConstants.InputControlFocus.Inventory:
                    Inventory.Draw(spriteBatch, largeWindow.Bounds);
                    break;
                case GameConstants.InputControlFocus.RenameItemScreen:
                    Inventory.Draw(spriteBatch, largeWindow.Bounds);
                    RenameItemScreen.Draw(spriteBatch, smallWindow.Bounds);
                    break;
                case GameConstants.InputControlFocus.EnterName:
                    EnterNameScreen.Draw(spriteBatch, smallWindow.Bounds);
                    break;
                case GameConstants.InputControlFocus.Instructions:
                    InstructionsScreen.Draw(spriteBatch, largeWindow.Bounds);
                    break;
                case GameConstants.InputControlFocus.MessageHistory:
                    MessageHistory.Show(spriteBatch, largeWindow.Bounds);
                    break;
                case GameConstants.InputControlFocus.MainMenu:
                    GameMenuScreen.Draw(spriteBatch, mediumWindow.Bounds);
                    break;
                case GameConstants.InputControlFocus.HallOfRecords:
                    HallOfRecordsScreen.Draw(spriteBatch, largeWindow.Bounds);
                    break;
                case GameConstants.InputControlFocus.ConfirmExitScreen:
                    ConfirmExitScreen.Draw(spriteBatch, smallWindow.Bounds);
                    break;
                    // Note: The rename item window is drawn from the Inventory window
                    // so as to be drawn on top of it
            }

            
            base.Draw(gameTime);
        }

        protected void DrawMainView()
        {
            GraphicsDevice.Viewport = mainViewPort;
            spriteBatch.Begin(); // requires an End

            Rectangle tileFootprint = new Rectangle(0, 0, tileWidth, tileHeight);

            FloorGenerator.DrawFloorTiles(spriteBatch, tileFootprint, localView);
            FloorGenerator.DrawScatteredLoot(spriteBatch, tileFootprint, localView);
            FloorGenerator.DrawScatteredGold(spriteBatch, tileFootprint, localView, goldSprite);
            Monster.DrawDiscoveredMonsters(spriteBatch, tileFootprint, localView);
            Hero.Draw(spriteBatch, tileFootprint, localView);

            
            if(GameConstants.DEBUG_MODE_DRAW_STEPS_TO_HERO)
                Monster.DrawCounters(spriteBatch, tileFootprint, localView);

            spriteBatch.End();


        }

        protected void DrawStatsView()
        {
            GraphicsDevice.Viewport = statsViewPort;
            spriteBatch.Begin();
            statsText.Draw(spriteBatch);
            if (outputMessageQueue.Count > 0) messageText.Draw(spriteBatch);
            if (outputMessageQueue.Count > 1 ||
                (outputMessageQueue.Count>0 && gameEnding)
                ) moreText.Draw(spriteBatch); // Drawn just a few pixels to the right of the message

            spriteBatch.End();

        }



        /// <summary>
        /// Event handler for user changing the size of the window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            lastWindowSize = Window.ClientBounds;
            graphics.PreferredBackBufferWidth = lastWindowSize.Width;
            graphics.PreferredBackBufferHeight = lastWindowSize.Height;
            graphics.ApplyChanges();
            UpdateViewPorts();
        }


        #endregion




        #region Game Play

        /// <summary>
        /// Resets all data to the point needed for starting a new game and calls the same function
        /// out of each class that requires it.
        /// </summary>
        private void InitNewGame(string heroName)
        {

            floorLevel = 1;
            goldScore = 0;
            hero = new Hero();
            hero.HeroName = heroName;
            MessageHistory.Clear();
            outputMessageQueue.Clear();
            

            mapViewEnabled =
            evenCycle =
            gameEnding =
            rapidRepeat = 
            isShiftDown =
            isRunning =
            waitForKeyRelease = false;

            Inventory.InitNewGame();
            InventoryEffectManager.InitNewGame();
            MagicalScroll.InitNewGame();
            GenerateMaps();

            statsText = new GameText(UpdatedStatText(), graphics.GraphicsDevice);
            statsText.ForeColor = Color.Yellow;
            statsText.BackColor = Color.Black;
            messageText.Y = statsText.Height;
            messageText.ForeColor = Color.White;
            messageText.BackColor = Color.Black;
            moreText.Y = statsText.Height;
            moreText.ForeColor = Color.Black;
            moreText.BackColor = Color.White;

            UpdateViewPorts();
        }

        /// <summary>
        /// Generates the line of text for displaying hero and game status data
        /// </summary>
        /// <returns>string of text to display</returns>
        private string UpdatedStatText()
        {
            statsRow.Clear();
            if (gameStarted)
            {
                statsRow.Append(string.Format("Au: {0}", goldScore));
                statsRow.Append(string.Format("     Floor: {0}", floorLevel));
                statsRow.Append(string.Format("     HP: {0}/{1}", Hero.HP, Hero.Max_HP));
                statsRow.Append(string.Format("     XP [{0}] {1}/{2}", Hero.XPLvl, Hero.XP, Hero.XpToNext));
                statsRow.Append(string.Format("     Armor: {0}", 10 + Inventory.TotalArmorModifiers()));
                statsRow.Append(string.Format("     Str: {0}/{1}", Hero.EffectiveStrength, Hero.BaseStrength));
                statsRow.Append(string.Format("     Energy: {0}/{1}", Hero.Energy, Hero.EnergyStorage));
            }
            else
                statsRow.Append(" ");
            return statsRow.ToString();

        }

        /// <summary>
        /// Accepts keyboard and mouse inputs and processes game logic in response to those inputs
        /// </summary>
        private void ProcessPlayerInputs()
        {

            


            #region Keyboard Input

            if (lastNonStateKeyPressed == Keys.None) return;

            // Allow message queue last message to clear from screen only if not running and not holding key
            if (outputMessageQueue.Count > 0 && !rapidRepeat)
                ProcessMessageQueue(true);

            #region DEBUG FEATURES
            // DEBUG FEATURE: Toggle debug features on and off during run time
            if (GameConstants.DEBUG_OPTION_ENABLE_DEBUG_MODE_TOGGLE &&
                (keyState.IsKeyDown(Keys.LeftControl) || keyState.IsKeyDown(Keys.RightControl)) &&
                (keyState.IsKeyDown(Keys.LeftAlt) || keyState.IsKeyDown(Keys.RightAlt)))
            {
                if (lastNonStateKeyPressed == Keys.W)
                    GameConstants.DEBUG_MODE_WALK_THROUGH_WALLS = !GameConstants.DEBUG_MODE_WALK_THROUGH_WALLS;
                else if (lastNonStateKeyPressed == Keys.R)
                    GameConstants.DEBUG_MODE_REVEAL = !GameConstants.DEBUG_MODE_REVEAL;
                else if (lastNonStateKeyPressed == Keys.F)
                    GameConstants.DEBUG_MODE_ALLOW_FLOOR_JUMPING = !GameConstants.DEBUG_MODE_ALLOW_FLOOR_JUMPING;
                else if (lastNonStateKeyPressed == Keys.C)
                    GameConstants.DEBUG_MODE_ALLOW_REMOVE_CURSED = !GameConstants.DEBUG_MODE_ALLOW_REMOVE_CURSED;
                else if (lastNonStateKeyPressed == Keys.S)
                {
                    GameConstants.DEBUG_MODE_DRAW_STEPS_TO_HERO = !GameConstants.DEBUG_MODE_DRAW_STEPS_TO_HERO;
                    Monster.InitializeDistanceMap();
                    Monster.CalculateDistanceMap();
                }
                    
                else if (lastNonStateKeyPressed == Keys.L)
                    GameConstants.DEBUG_MODE_ENABLE_FREE_LOOT = !GameConstants.DEBUG_MODE_ENABLE_FREE_LOOT;
                return;
            }

            // DEBUG FEATURE: Drop Loot at hero location
            if (GameConstants.DEBUG_MODE_ENABLE_FREE_LOOT && lastNonStateKeyPressed == Keys.L)
            {
                FloorGenerator.ScatteredLoot.Add(MagicalScroll.GenerateNewScroll(Hero.Location));
                FloorGenerator.ScatteredLoot.Add(new MagicalRing(Hero.Location));
                return;
            }


            // DEBUG FEATURE: Instantly jump up and down floor levels with F2 and F3
            if (GameConstants.DEBUG_MODE_ALLOW_FLOOR_JUMPING && lastNonStateKeyPressed == Keys.F2)
            {
                floorLevel++;
                GenerateMaps();
                ToggleView(true);
                return;
            }

            if (GameConstants.DEBUG_MODE_ALLOW_FLOOR_JUMPING && lastNonStateKeyPressed == Keys.F3)
            {
                // TODO: Remove after testing screen fonts
                for (int i = 0; i < GameConstants.MESSAGE_HISTORY_RETENTION_LENGTH; i++)
                {
                    MessageHistory.Add(string.Format("DEBUG INSERT LINE - {0}", i));
                    Inventory.Add(new MagicalRing(DungeonGameEngine.Hero.Location));
                } // END Remove


                floorLevel--;
                GenerateMaps();
                ToggleView(true);
                return;
            }

            // DEBUG FEATURE: F1 Checks the map for hidden doors and traps and tells you where one is
            string message = string.Empty;
            if (GameConstants.DEBUG_MODE_REVEAL && lastNonStateKeyPressed == Keys.F1 && !rapidRepeat)
            {
                FloorGenerator.FindHidden(out message);
                if (message != string.Empty) outputMessageQueue.Add(message);
                return;
            }

            #endregion


            #region Turn advancement actions

            Debug.Assert (InventoryEffectManager.HeroConcious);

            isRunning = false;

            // If hero is dead, then turn is over
            if (gameEnding) return;
            
            // Get movement direction, and move if a movement key is pressed
            foreach (var directionAssignment in movementDirectionKeys)
            {
                if (lastNonStateKeyPressed == directionAssignment.Key)
                {
                    isRunning = isShiftDown && !GameConstants.DEBUG_MODE_WALK_THROUGH_WALLS;
                    if (isRunning) rapidRepeat = false;
                    currentDirection = directionAssignment.Value;
                    if (isRunning) HeroRuns(); // Used here to start the running cycle
                    else
                    {
                        Move(); // Used in the HeroRuns method as well and calls FinishTurn with each move
                    }
                    return;
                }
            }

            // Press Delete to rest one turn - lets other monsters move and effects wear off
            if (lastNonStateKeyPressed == Keys.Delete)
            {
                // Clear last message from screen
                if (outputMessageQueue.Count > 0)
                    ProcessMessageQueue(true);
                // 2 additional healing cycles in addition to the one that fires with FinishTurn
                for(int cycle = 0; cycle <=2; cycle++)
                    if (Hero.HP < Hero.Max_HP && !InventoryEffectManager.HeroPoisoned)
                        Hero.Heal();
                FinishTurn();
                return;
            }

            // Press Insert to search 3 times in one turn
            else if (lastNonStateKeyPressed == Keys.Insert)
            {
                // Clear last message from screen
                if (outputMessageQueue.Count > 0 )
                {
                    ProcessMessageQueue(true);
                    waitForKeyRelease = true;
                }
                FinishTurn(3); // Passes number of searches along to tripple the chance using the same pass
                return;
            }

            #endregion



            #region Static Actions - No turn advancement
            // Defined as an action that does not cause monsters to move or game turn to increment



            // Pan camera with assigned keys
            foreach (var panDirection in panningDirectionKeys)
            {
                if (panDirection.Key == lastNonStateKeyPressed)
                {
                    PanViewManually(panDirection.Value);
                    return;
                }
            }

            // Center view back on hero without requiring movement or toggling the map view
            if (lastNonStateKeyPressed == Keys.Tab && !rapidRepeat)
            { PanViewWithHero(); return; }

            // Esc - Game menu or exit map view (in addition to M)
            else if (lastNonStateKeyPressed == Keys.Escape && !rapidRepeat)
            {
                if (mapViewEnabled)
                {
                    ToggleView();
                    return;
                }
                else if(screenHavingFocus != GameConstants.InputControlFocus.MainMenu)
                {
                    screenHavingFocus = GameConstants.InputControlFocus.MainMenu;
                    ProcessMessageQueue(true);
                    return;
                }
                
            }

            // Space - Clears any remaining messages even without MORE tag
            else if (lastNonStateKeyPressed == Keys.Space && !rapidRepeat)
            {
                ProcessMessageQueue(true);
                return;
            }

            // Comma - Descends stairs
            else if (lastNonStateKeyPressed == Keys.OemComma && !rapidRepeat &&
                FloorGenerator.FloorPlan[hero.Location.X, hero.Location.Y] == FloorTile.Surfaces.StairsDown)
            {
                floorLevel++;
                GenerateMaps();
                ToggleView(true); // Reset view back to normal
                return;
            }

            // M - Toggle Map view
            else if (lastNonStateKeyPressed == Keys.M && !rapidRepeat) 
            {
                ToggleView();
                return;
            }

            // I - Show inventory
            else if (lastNonStateKeyPressed == Keys.I && !rapidRepeat && screenHavingFocus != GameConstants.InputControlFocus.Inventory)
            {
                screenHavingFocus = GameConstants.InputControlFocus.Inventory;
                ProcessMessageQueue(true); // Clears message queue from screen so it doesn't hold up rapid key firing in the rename window
                return;
            }

            // H - Show Message History
            else if (lastNonStateKeyPressed == Keys.H && !rapidRepeat && screenHavingFocus != GameConstants.InputControlFocus.MessageHistory)
            {
                screenHavingFocus = GameConstants.InputControlFocus.MessageHistory;
                return;
            }


            // ? - Show Instructions
            else if (lastNonStateKeyPressed == Keys.OemQuestion && !rapidRepeat && screenHavingFocus != GameConstants.InputControlFocus.Instructions)
            {
                screenHavingFocus = GameConstants.InputControlFocus.Instructions;
                return;
            }

            


            #endregion Static Actions - No turn advancement
            

            

            #endregion

        }

        /// <summary>
        /// Handles decisions as to whether player inputs should affect the main
        /// game or be passed along to an open dialog such as the inventory,
        /// instructions or other such child windows.
        /// </summary>
        private void DirectPlayerInputs(GameTime gameTime)
        {
            
            // Stop hold key rapid fire if a message is still on screen.
            if (outputMessageQueue.Count > 0 && lastNonStateKeyPressed != Keys.None)
                waitForKeyRelease = true;
            
            switch (screenHavingFocus)
            {
                case GameConstants.InputControlFocus.RenameItemScreen: // Keyboard control passes through Inventory
                case GameConstants.InputControlFocus.Inventory:
                    {
                        // Update statText every cycle while inventory is open
                        statsText.Text = UpdatedStatText();
                        List<InventoryItem> dropList; // to receive items dropped by Inventory
                        bool showRenameItemScreen;
                        if (Inventory.ProcessPlayerInputs(lastNonStateKeyPressed, gameTime, out dropList, out showRenameItemScreen))
                        {
                            // When Inventory closes, restore local controls and update statText one last time
                            screenHavingFocus = GameConstants.InputControlFocus.MainGameScreen;
                            statsText.Text = UpdatedStatText();

                            // Due to detect monster scroll, updated detected states
                            Monster.MonsterHeroMutualDetection();

                            // Pull in any messages passed from Inventory Window
                            outputMessageQueue.AddRange(InventoryEffectManager.ImportMessageQueue);
                            if (lastNonStateKeyPressed != Keys.None && outputMessageQueue.Count > 0) waitForKeyRelease = true;
                        }

                        if (dropList.Count > 0)
                        {
                            string message;
                            FloorGenerator.ScatterDroppedLoot(dropList,out message);
                            if (message != string.Empty) outputMessageQueue.Add(message);
                        }

                        if (showRenameItemScreen)
                        {
                            currentDelay = GameConstants.NO_DELAY; // Timing is handled by the rename window
                            screenHavingFocus = GameConstants.InputControlFocus.RenameItemScreen;
                        }
                        else
                        {
                            currentDelay = GameConstants.INITIAL_KEY_DELAY;
                            if (screenHavingFocus != GameConstants.InputControlFocus.MainGameScreen)
                                screenHavingFocus = GameConstants.InputControlFocus.Inventory;
                        }
                        break;
                    }
                case GameConstants.InputControlFocus.Instructions:
                    {
                        if (InstructionsScreen.ProcessPlayerInputs(lastNonStateKeyPressed))
                        {
                            // Because the main menu allows passing to the instruction screen before
                            // a game has even started, we need to pass control back to the main menu
                            // if this is the case
                            if (!gameStarted)
                                screenHavingFocus = GameConstants.InputControlFocus.MainMenu;
                            
                            // But if the game is running, restore local controls when Inventory closes
                            else
                                screenHavingFocus = GameConstants.InputControlFocus.MainGameScreen;
                        }
                        break;
                    }
                case GameConstants.InputControlFocus.MessageHistory:
                    {
                        if (MessageHistory.ProcessPlayerInputs(lastNonStateKeyPressed))
                        {
                            // Restore local controls when Inventory closes
                            screenHavingFocus = GameConstants.InputControlFocus.MainGameScreen;
                        }
                        break;
                    }
                case GameConstants.InputControlFocus.MainMenu:
                    {
                        GameMenuScreen.ReturnActions action;
                        if(GameMenuScreen.ProcessPlayerInputs(lastNonStateKeyPressed, gameTime, out action))
                        {
                            if (action == GameMenuScreen.ReturnActions.New)
                            {
                                if (gameStarted)
                                {
                                    screenHavingFocus = GameConstants.InputControlFocus.ConfirmExitScreen;
                                    ConfirmExitScreen.Purpose = "New";
                                }
                                else RestartGame();
                            }
                            else if (action == GameMenuScreen.ReturnActions.Quit)
                            {
                                if (gameStarted)
                                {
                                    screenHavingFocus = GameConstants.InputControlFocus.ConfirmExitScreen;
                                    ConfirmExitScreen.Purpose = "Quit";
                                }
                                else
                                    Exit();
                            }
                            else if (action == GameMenuScreen.ReturnActions.Instructions)
                                screenHavingFocus = GameConstants.InputControlFocus.Instructions;
                            else if (action == GameMenuScreen.ReturnActions.Records)
                                screenHavingFocus = GameConstants.InputControlFocus.HallOfRecords;

                            // Moves focus back to the game screen if game was started, otherwise
                            // it remains on the main menu.
                            else if (gameStarted)
                                screenHavingFocus = GameConstants.InputControlFocus.MainGameScreen;
                        }

                        break;
                    }
                case GameConstants.InputControlFocus.EnterName:
                    {
                        string response;
                        if (EnterNameScreen.ProcessPlayerInputs(lastNonStateKeyPressed, gameTime, resetGetNameTiming, out response))
                        {
                            // If screen exits with a name, continue starting the new name, otherwise return to main menu
                            if (response == string.Empty)
                                screenHavingFocus = GameConstants.InputControlFocus.MainMenu;
                            else
                            {
                                screenHavingFocus = GameConstants.InputControlFocus.MainGameScreen;
                                InitNewGame(response);
                            }
                        }
                        resetGetNameTiming = false;
                        break;
                    }
                case GameConstants.InputControlFocus.ConfirmExitScreen:
                    {
                        bool ConfirmExit = false;
                        if(ConfirmExitScreen.ProcessPlayerInputs(lastNonStateKeyPressed,out ConfirmExit))
                        {
                            if (ConfirmExit)
                            {
                                if (ConfirmExitScreen.Purpose == "Quit")
                                    GameOver("quit"); // To add the score in the hall of records if high enough
                                else if(ConfirmExitScreen.Purpose == "New")
                                {
                                    RestartGame();
                                }
                                
                            }
                            else
                            {
                                if (gameStarted)
                                    screenHavingFocus = GameConstants.InputControlFocus.MainGameScreen;
                                else
                                    screenHavingFocus = GameConstants.InputControlFocus.MainMenu;
                            }
                            
                        }
                        break;
                    }
                case GameConstants.InputControlFocus.HallOfRecords:
                    {
                        if (HallOfRecordsScreen.ProcessPlayerInputs(lastNonStateKeyPressed))
                        {
                                screenHavingFocus = GameConstants.InputControlFocus.MainMenu;
                        }
                        break;
                    }
                default: ProcessPlayerInputs(); break;
            }
        }

        private Keys GetKeyPressed()
        {
            Keys rtnVal = Keys.None;
            keyState = Keyboard.GetState();
            isShiftDown = (keyState.IsKeyDown(Keys.LeftShift) ||
                           keyState.IsKeyDown(Keys.RightShift));

            if (keyState.GetPressedKeys().Length != 0)
                foreach(var key in keyState.GetPressedKeys())
                {
                    // Only allow first non-state change key to be passed
                    if (key != Keys.LeftShift && key != Keys.RightShift &&
                    key != Keys.LeftAlt && key != Keys.RightAlt &&
                        key != Keys.LeftControl && key != Keys.RightControl &&
                        key != Keys.LeftWindows && key != Keys.RightWindows &&
                        key != Keys.CapsLock)
                    {
                        rtnVal = key;
                        break; 
                    }
                }
            return rtnVal;
        }
        
        /// <summary>
        /// Calls upon the FloorGenerator to create a map, loot and monsters. Monster class then gets
        /// to calculate the steps needed to get from any walkable tile to the hero.
        /// </summary>
        private void GenerateMaps()
        {
            if(newGame)
            {
                gameStarted = true;
                // Intro messages
                outputMessageQueue.Add("When you see the MORE prompt, press space.");
                outputMessageQueue.Add("It is shown when there are more messages in queue.");
                outputMessageQueue.Add("Use arrow keys or numeric keypad (NumLock off) to move.");
                outputMessageQueue.Add("Press ? for a complete list of commands.");
                outputMessageQueue.Add("This messages clears when you move.");
                newGame = false;
            }


            bool floorComplete = true;
            int floorTryCounter = 0;
            do
            {
                try
                {
                    FloorGenerator.CreateNewFloor(floorLevel);
                }
                catch (StackOverflowException)
                {
                    // Unable to map tunnels given current room configuration. Try again a few more times.
                    floorComplete = false;
                }
            } while (!floorComplete && ++floorTryCounter<10);

            if (!floorComplete) throw new StackOverflowException("Unable to create new floor after 10 tries");
            // TODO: When game saving is enabled, allow player to save before closing from this exception.

            // Translate the new floor map into values used in counting steps
            // to the hero from anywhere on the map
            Monster.InitializeDistanceMap();
            Monster.MonsterHeroMutualDetection();
            statsText.Text = UpdatedStatText();
        }

        private void PickUpGold()
        {
            int pickUpAmt = 0;

            // If multiple stacks of gold should exist, they are summed up before reporting to player
            for (int i=FloorGenerator.ScatteredGold.Count-1; i>=0; i--)
                if(FloorGenerator.ScatteredGold[i].Key == hero.Location)
                {
                    pickUpAmt += FloorGenerator.ScatteredGold[i].Value;
                    FloorGenerator.ScatteredGold.RemoveAt(i);
                }
            
            if(pickUpAmt>0)
            {
                goldScore += pickUpAmt;
                outputMessageQueue.Add("You found " + pickUpAmt + " gold.");
            }
            
        }


        
        /// <summary>
        /// Places the first message in the outputMessageQue into the messageText object for display.
        /// If user acknowledges message from MORE prompt, moves the message to the history.
        /// </summary>
        /// <param name="ack">Call with true to acknowledge the current message</param>
        internal static void ProcessMessageQueue(bool ack = false, string message = "")
        {
            if (message != "")
                outputMessageQueue.Add(message);
            if (outputMessageQueue.Count > 0)
            {
                if (ack)
                {
                    MessageHistory.Add(outputMessageQueue[0]);
                    outputMessageQueue.RemoveAt(0);
                    waitForKeyRelease = outputMessageQueue.Count > 0;
                }
                else
                {
                    messageText.Text = outputMessageQueue[0];
                    moreText.X = messageText.Width + 3;
                    isRunning = false;
                }
            }
            else if (lastNonStateKeyPressed == Keys.None) waitForKeyRelease = false;
        }

        /// <summary>
        /// Displays a basic menu for events such as Loading, Saving, Quitting.
        /// </summary>
        /// <returns>True if staying in game, else false</returns>

        internal void GameOver(string reason)
        {
            if (gameStarted && !gameEnding) HallOfRecordsScreen.QualifyResults(hero.HeroName, floorLevel, true, reason, goldScore);
            gameEnding = true;
            
        }

        private void RestartGame()
        {
            if (gameStarted) HallOfRecordsScreen.QualifyResults(hero.HeroName, floorLevel, true, "quit", goldScore);

            gameStarted = false;
            newGame = true;
            resetGetNameTiming = true;
            screenHavingFocus = GameConstants.InputControlFocus.EnterName;
            EnterNameScreen.Value = string.Empty;
        }

        #endregion




        #region Movement


        /// <summary>
        /// Consolidates all 8 movement methods into one, passes the new location to be
        /// checked to see if it is blocked, and moves only if it is not. If move is successfull,
        /// monsters are given a chance to move and if there are output messages queued up, they
        /// get a chance to be viewed and acknowledged. Each of these actions were appended to the
        /// move method because they are also required for each step when running.
        /// </summary>
        /// <param name="direction">Direction to move</param>
        /// <returns>True is moved, false if blocked</returns>
        private bool Move()
        {
            // Clear last message from screen if not near monster
            if(outputMessageQueue.Count>0 && !Monster.MonsterHeroMutualDetection())
                ProcessMessageQueue(true);

            // If hero is confused, choose a random direction instead
            if (InventoryEffectManager.HeroConfused)
            {
                do
                    currentDirection = (GameConstants.Direction8)rand.Next
                        (Enum.GetValues(typeof(GameConstants.Direction8)).Length);
                while (FloorGenerator.MovementBlockedByFloor(Utility.GetNewPointFrom(currentDirection, hero.Location)));
            }

            var newPoint = Utility.GetNewPointFrom(currentDirection, hero.Location);
            bool runMayContinue = true;
            var blockingMonster = Monster.MonsterFoundAt(newPoint);

            // If newPoint is a monster, attack only if walking but stop running either way
            if (blockingMonster != null)
            {
                if (!isRunning)
                {
                    hero.Attack(blockingMonster);
                    CombatManager.PlayDeathMessages();
                    FinishTurn();
                }
                runMayContinue = false;
            }

            // If newPoint is a locked door, Bash it if walking, or stop running
            else if (FloorGenerator.GetTileAt(newPoint) == FloorTile.Surfaces.LockedDoor)
            {
                if (!isRunning)
                {
                    FloorGenerator.BashDoor(newPoint);
                    FinishTurn();
                }
                else runMayContinue = false;
            }

            // If hero stuck, stop running and finish turn
            else if (InventoryEffectManager.HeroStuck)
            {
                runMayContinue = false;
                FinishTurn();
            }

            // If floor tile allows passage, move onto it and continue if running
            else if (!FloorGenerator.MovementBlockedByFloor(newPoint))
            {
                hero.Location = newPoint; // Move hero
                FinishTurn();
            }
            else runMayContinue = false;
            
                

            return runMayContinue;
        }

        /// <summary>
        /// Supports tunnel running by catching changes in tunnel direction as long
        /// as hero is not near an intersection.
        /// </summary>
        /// <param name="currentDirection"></param>
        /// <returns>New direction to follow tunnel with</returns>
        private void UpdateDirectionWithTunnel()
        {
            // enum Direction {Up, UpRight, Right, DownRight, Down, DownLeft, Left, UpLeft}
            var cameFrom = (int)currentDirection - 4;
            if (cameFrom < 0) cameFrom += 8;
            for (int direction = 0; direction < 8; direction+=2) // skip diagonals
                if (direction != cameFrom &&
                    FloorGenerator.TunnelContinues((GameConstants.Direction8)direction, hero.Location))
                {
                    currentDirection = (GameConstants.Direction8)direction;
                    return;
                }
        }

        /// <summary>
        /// Hero runs straight when in a room until any obstical is found.
        /// Hero runs through a tunnel until a split in the tunnel or any obstical is found.
        /// This entire run happens during a single game loop
        /// </summary>
        /// <param name="direciton">Direction to start running</param>
        private void HeroRuns()
        {
            // Determine if tunnel runner
            bool isTunnelRunner = (FloorGenerator.GetTileAt(hero.Location)
                == FloorTile.Surfaces.Tunnel);

            bool isStopped = false;
            var heroLastLocation = hero.Location;

            // Can't handle running through tunnels on diagonals (evaluates to odd numbered int).
            if (isTunnelRunner && (int)currentDirection % 2 == 1)
                isStopped = true;

            if (!Move() ||
                Monster.MonsterHeroMutualDetection() ||
                FloorGenerator.NearObject(heroLastLocation))
                isStopped = true;

            PanViewWithHero();

            // For in tunnels
            if (isTunnelRunner && FloorGenerator.NearTunnelIntersection())
                isStopped = true;
            if (!isStopped && isTunnelRunner)
                UpdateDirectionWithTunnel();
            isRunning = !isStopped;
            
        }

        private void FinishTurn(int searchCount=1)
        {
            int chanceMultiplier = searchCount * (InventoryEffectManager.HeroObservant ? 5 : 1);
            string message;
            if (FloorGenerator.NoticeHidden(out message, searchCount))
            {
                outputMessageQueue.Add("You found a " + message);
                waitForKeyRelease = true;
            }

            List<string> messages;
            Hero.PickUpLoot(out messages);
            if (messages.Count>0) // anything the hero is standing on
            {
                outputMessageQueue.AddRange(messages);
                messages.Clear();
                waitForKeyRelease = true; // Interupts rapid fire movement
            }

            PickUpGold();

            if (Hero.HP < Hero.Max_HP && !InventoryEffectManager.HeroPoisoned)
                Hero.Heal();

            if (!Hero.BurnCalories()) { GameOver("starved to death."); return; }

            if (InventoryEffectManager.HeroPoisoned && !Hero.TakePoisonDmg())
                {
                outputMessageQueue.Add("The poison has killed you.");
                GameOver("killed by poison");
                return;
            }

            // Monsters get to move every turn if hero is not hastended, and every other turn if he is
            if (!InventoryEffectManager.HeroHastened || (InventoryEffectManager.HeroHastened && !evenCycle))
            {
                Monster.MoveMonsters();
            }

            Monster.MonsterHeroMutualDetection();

            InventoryEffectManager.AdvanceTurn();
            statsText.Text = UpdatedStatText();
            PanViewWithHero();
            evenCycle = !evenCycle;
            outputMessageQueue.AddRange(InventoryEffectManager.ImportMessageQueue);
            if (lastNonStateKeyPressed != Keys.None && outputMessageQueue.Count > 0) waitForKeyRelease = true;
        }


        #endregion

        

        #region Visibility


        /// <summary>
        /// Sets the range of tiles on the map that can be viewed within the game window
        /// </summary>
        private void PanViewWithHero()
        {
            // margins are the number of tiles around the hero that should be in the window
            int marginX = (int)Math.Floor((decimal)(localView.Width / 2));
            int marginY = (int)Math.Floor((decimal)(localView.Height / 2));

            // Determines if hero is too close to map edges to remain centered, and centers if not
            if (DungeonGameEngine.Hero.Location.X < marginX) localView.X = 0;
            else if (DungeonGameEngine.Hero.Location.X > FloorGenerator.FloorWidth - marginX - 1)
                localView.X = FloorGenerator.FloorWidth - localView.Width;
            else localView.X = DungeonGameEngine.Hero.Location.X - marginX;

            if (DungeonGameEngine.Hero.Location.Y < marginY) localView.Y = 0;
            else if (DungeonGameEngine.Hero.Location.Y > FloorGenerator.FloorHeight - marginY - 1)
                localView.Y = FloorGenerator.FloorHeight - localView.Height;
            else localView.Y = DungeonGameEngine.Hero.Location.Y - marginY;

            // With new localView, all loot, traps, monsters, etc. need to be updated
            FloorGenerator.ShowLitSpaces(); 
        }

        private void PanViewManually(GameConstants.Direction8 moveDirection)
        {
            switch (moveDirection)
            {
                case GameConstants.Direction8.Up: localView.Y--; break;
                case GameConstants.Direction8.Down: localView.Y++; break;
                case GameConstants.Direction8.Left: localView.X--; break;
                case GameConstants.Direction8.Right: localView.X++; break;
                default: return;
            }

            localView.X = MathHelper.Clamp(localView.X, 0, FloorGenerator.FloorWidth - localView.Width);
            localView.Y = MathHelper.Clamp(localView.Y, 0, FloorGenerator.FloorHeight - localView.Height);

            // with new localView, all loot, traps, monsters, etc. need to be updated
            FloorGenerator.ShowLitSpaces(); 
        }

        private void UpdateViewPorts()
        {
            // Adjust full viewport (default)
            defaultViewPort.Width = Window.ClientBounds.Width;
            defaultViewPort.Height = Window.ClientBounds.Height;

            // Leave room for message output below stats by invoking with twice the height
            // Can't just add the messageText height because when it is empty, height is 0.
            statsViewPort.Height = statsText.Height * 2;

            // Change tile sizes based on new view mode
            if (mapViewEnabled)
            {
                // Calculate tile size to fit entire map on screen
                tileWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width 
                    / FloorGenerator.FloorWidth;
                tileHeight = (GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height
                    - statsViewPort.Height)
                    / FloorGenerator.FloorHeight;

                // Set both to smallest dimension to keep the tile square
                // Left width and height as the way to refer to a tile dimensions in case later
                // it should be decided that they need to be rectangle or some other shape.
                if (tileWidth < tileHeight) tileHeight = tileWidth;
                else tileWidth = tileHeight;

                // Prevent tile size from being set too small
                if (tileWidth < GameConstants.TILE_SIDE_MININUM ||
                    tileHeight < GameConstants.TILE_SIDE_MININUM)
                {
                    tileWidth = GameConstants.TILE_SIDE_MININUM;
                    tileHeight = GameConstants.TILE_SIDE_MININUM;
                }

            }
            else
            {
                tileWidth = GameConstants.TILE_SIDE_NORMAL;
                tileHeight = GameConstants.TILE_SIDE_NORMAL;
            }

            // Allow statsViewPort to consume the entire width of the screen
            statsViewPort.Width = Window.ClientBounds.Width;

            // Set size of mainViewPort
            mainViewPort.Width = Window.ClientBounds.Width;
            mainViewPort.Height = Window.ClientBounds.Height - statsViewPort.Height;

            // Reduce each dimension of mainViewPort to meet mapSize if too wide/tall
            mainViewPort.Width = MathHelper.Min(mainViewPort.Width,
                tileWidth * FloorGenerator.FloorWidth);
            mainViewPort.Height = MathHelper.Min(mainViewPort.Height,
                tileHeight * FloorGenerator.FloorHeight);

            // Center main viewport
            mainViewPort.X = Window.ClientBounds.Width / 2 - mainViewPort.Width / 2;
            mainViewPort.Y = (Window.ClientBounds.Height - statsViewPort.Height) / 2 - (mainViewPort.Height / 2) + statsViewPort.Height;
            
            localView.Width = mainViewPort.Width / tileWidth;
            localView.Height = mainViewPort.Height / tileHeight;

            // Set large window Size and Location
            largeWindow.Width = MathHelper.Min(GameConstants.CHILD_WINDOW_WIDTH, Window.ClientBounds.Width);
            largeWindow.Height = MathHelper.Min(GameConstants.CHILD_WINDOW_HEIGHT, Window.ClientBounds.Height);
            largeWindow.X = mainViewPort.Width / 2 - largeWindow.Width / 2;
            largeWindow.Y = mainViewPort.Height / 2 - largeWindow.Height / 2 + statsViewPort.Height;

            // Center small Window when used
            smallWindow.Width = GameConstants.INPUT_VIEW_WIDTH;
            smallWindow.Height = GameConstants.INPUT_VIEW_HEIGHT;
            smallWindow.X = largeWindow.Bounds.Center.X - smallWindow.Width / 2;
            smallWindow.Y = largeWindow.Bounds.Center.Y - smallWindow.Height / 2;

            // Center medium Window when used
            mediumWindow.Width = GameConstants.GAME_MENU_VIEW_WIDTH;
            mediumWindow.Height = GameConstants.GAME_MENU_VIEW_HEIGHT;
            mediumWindow.X = largeWindow.Bounds.Center.X - mediumWindow.Width / 2;
            mediumWindow.Y = largeWindow.Bounds.Center.Y - mediumWindow.Height / 2;

            // Attempt to keep hero in center of view if not too close to edges of map
            if(gameStarted) PanViewWithHero(); 
        }

        /// <summary>
        /// Toggles between normal view and map view (full level)
        /// </summary>
        /// <param name="setLocalView"></param>
        private void ToggleView(bool setLocalView = false)
        {
            mapViewEnabled = !mapViewEnabled && !setLocalView; // toggle view mode
            UpdateViewPorts();
        }

        #endregion




        #region Help
        /// <summary>
        /// Shows modal dialog of about contents. Called from within DungeonGameEngine to decrease
        /// delay after closing the dialog.
        /// </summary>
        internal static void ShowAboutScreen()
        {
            StringBuilder aboutText = new StringBuilder();
            aboutText.Append("Dungeon Geek\n");
            Version version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
            aboutText.Append("Version " + version + "\n"); // Edit values in AssemblyInfo.cs with any change here
            aboutText.Append("\n");
            aboutText.Append("Explore a randomly generated dungeon\n");
            aboutText.Append("Concept inspired by Rogue, a game created by Jon Lane and released for DOS in 1983.");
            System.Windows.Forms.MessageBox.Show(aboutText.ToString());

            /*
             * Version History:
             * v0.1 - Basic map generation and hero movement through map. Included one loot item
             *        for picking up and adding to inventory. Map panning and zooming.
             *        Started: 8/17/2017
             *        Closed: 8/26/2017
             *        
             * v0.2 - Improved movement features. Added inventory screen with features to drop, equip
             *        unequip, consume, and rename items. Added food along with food consumption, starvation
             *        fainting and penalties for getting too fat. Updated camera pan ability to look around
             *        without moving hero and recenter back on hero afterward. Implemented a screen to show
             *        keyboard commands and one to show the message history in case you miss something.
             *        Started: 8/30/2017
             *        Closed: 9/10/2017
             *       
             * v0.3 - TBD...
             *
             * */
        }

        #endregion

        
    }


}
