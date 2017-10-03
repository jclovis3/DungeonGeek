using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace DungeonGeek
{
    static class GameMenuScreen
    {
        
        public enum ReturnActions { None, New, Instructions, Records, Quit}

        #region Fields
        private static GameWindow window = new GameWindow(true);
        
        private static int elapsedTime = 0;
        private static int currentDelay = 0;
        private static bool firstLoop = true;
        private static Keys lastKey = Keys.None;



        #endregion


        internal static void Initialize(GraphicsDevice gd)
        {
            string header = "Options:";
            string instructions = "Up/Down - Change selection     Enter - Select\nEsc - Return to game";
            List<string> menuList = new List<string>();
            menuList.AddRange(new string[]
            {
                "New game",
                "Keyboard commands",
                "Hall of Records",
                "Quit"
            });
            window.Initialize(gd, menuList, header, instructions);
        }

        internal static void Draw(SpriteBatch spriteBatch, Rectangle viewPortBounds)
        {
            window.Draw(spriteBatch, viewPortBounds);
        }

        internal static bool ProcessPlayerInputs(Keys key, GameTime gameTime, out ReturnActions action)
        {
            elapsedTime += gameTime.ElapsedGameTime.Milliseconds;
            action = ReturnActions.None;

            if (firstLoop && key != Keys.None) return false; // Forces keys to be released upon entry of first loop
            firstLoop = false;

            // If no key pressed, there should be no more delay in this class
            if (key == Keys.None || key != lastKey) currentDelay = GameConstants.NO_DELAY;


            if (key != Keys.None && elapsedTime > currentDelay)
            {

                elapsedTime = 0;
                

                // When a key is first pressed, the initial delay should be set
                if (key != lastKey) currentDelay = GameConstants.INITIAL_KEY_DELAY;

                // Once the initial delay is over, if the key is still held down, then repeating is allowed
                else currentDelay = GameConstants.HOLD_KEY_DELAY;




                if (key == Keys.Escape)
                {
                    action = ReturnActions.None;
                    firstLoop = true;
                    return true; // Screen closes
                }

                // Up and down keys allow selection to roll from end to end
                else if (key == Keys.Down)
                {
                    window.SelectNext();
                }
                else if (key == Keys.Up)
                {
                    window.SelectPrev();
                }
                else if (key == Keys.Enter)
                {
                    // {"Save game", "Load game", "Keyboard commands","Quit"};

                    switch (window.SelectedText)
                    {
                        case "New game":
                            action = ReturnActions.New;
                            return CloseWindow();
                        case "Keyboard commands":
                            action = ReturnActions.Instructions;
                            return CloseWindow();
                        case "Hall of Records":
                            action = ReturnActions.Records;
                            return CloseWindow();
                        case "Quit":
                            action = ReturnActions.Quit;
                            return CloseWindow();
                        default:
                            break;
                    }
                }
            }
            lastKey = key;
            return false; // Does not allow screen to exit yet
        }

        /// <summary>
        /// Used to illiminate redundant code in the ProcessPlayerInputs method
        /// </summary>
        /// <returns></returns>
        private static bool CloseWindow()
        {
            firstLoop = true;
            return true;
        }

    }
}
