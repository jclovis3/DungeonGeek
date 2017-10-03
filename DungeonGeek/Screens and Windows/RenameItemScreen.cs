using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Text;

namespace DungeonGeek
{
    static class RenameItemScreen
    {


        #region Fields
        private static GameWindow window = new GameWindow();
        private static GameText userResponseText;
        private static StringBuilder userResponse = new StringBuilder();
        private static KeyboardState keyboardState;
        private static KeyboardState oldKeyboard;
        private static int elapsedTime = 0;
        private static int currentDelay = 0;
        private static bool firstLoop = true;
        private static Keys lastKey = Keys.None;


        #endregion


        internal static string Value
        {
            // Can't just make a new object because the GameWindow is already referencing the current one.
            set { userResponse.Clear(); userResponse.Append(value); }
            get { return userResponse.ToString(); }

        }

        internal static void Initialize(GraphicsDevice gd)
        {
            oldKeyboard = Keyboard.GetState();
            string prompt = "What would you call it?";
            string instructions = "Enter - Except    Esc - Cancel";
            window.Initialize(gd, prompt, userResponse, instructions);
            userResponseText = new GameText(gd); // Used solely for measuring text length during input
        }

        internal static void Draw(SpriteBatch spriteBatch, Rectangle viewPortBounds)
        {
            window.Draw(spriteBatch, viewPortBounds);
        }

        internal static bool ProcessPlayerInputs(Keys key, GameTime gameTime, bool resetTiming, string reservedTitleText, out string response)
        {
            // Handle own delay control
            elapsedTime += gameTime.ElapsedGameTime.Milliseconds;
            response = string.Empty;
            if (resetTiming) firstLoop = true;
            if (firstLoop && key != Keys.None) return false; // Forces keys to be released upon entry of first loop
            firstLoop = false;

            // If no key being held (last key was released), there should be no more delay
            if (key == Keys.None || lastKey != key)
                currentDelay = GameConstants.NO_DELAY;

            // After the delay has elapsed, process the key if pressed
            if (key != Keys.None && elapsedTime > currentDelay)
            {
                elapsedTime = 0;



                // When a key is first pressed, the initial delay should be set
                if (key != lastKey)
                    currentDelay = GameConstants.INITIAL_KEY_DELAY;
                
                // Once the initial delay is over, if the key is still held down, then repeating is allowed
                else
                    currentDelay = GameConstants.HOLD_KEY_DELAY;
                       
                switch (key)
                {
                    case Keys.Escape: return true;
                    case Keys.Enter:
                        {
                            response = userResponse.ToString();
                            return true; // Allow window to close
                        }
                    case Keys.Back:
                        {
                            if (userResponse.Length > 0)
                                userResponse.Remove(userResponse.Length - 1, 1);
                            break;
                        }
                    default:
                        {
                            // Use the XNA Keyboard Helper to get typed input
                            keyboardState = Keyboard.GetState();
                            char newKey;

                            if (XnaKeyboardHelper.TryConvertKeyboardInput(keyboardState, oldKeyboard, out newKey))
                            {
                                // Prevent text entry from being wider than what inventory screen can handle
                                userResponseText.Text = reservedTitleText + userResponse.ToString() + newKey;
                                if (userResponseText.Width <= GameConstants.INV_MAX_TITLE_WIDTH)
                                    userResponse.Append(newKey);
                            }
                            oldKeyboard = keyboardState;
                            break;
                        }
                }
                
            }
            lastKey = key;
            return false; // Does not allow window to close yet
        }




    }
}
