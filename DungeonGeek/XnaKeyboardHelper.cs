using Microsoft.Xna.Framework.Input;

namespace DungeonGeek
{
    static class XnaKeyboardHelper
    {   // Code borrowed (and altered) from http://roy-t.nl/2010/02/11/code-snippet-converting-keyboard-input-to-text-in-xna.html

        static char[] shiftedNumKeys = { ')', '!', '@', '#', '$', '%', '^', '&', '*', '(' };
        
        
        
        
        /// <summary>
        /// Tries to convert keyboard input to characters and prevents repeatedly returning the 
        /// same character if a key was pressed last frame, but not yet unpressed this frame.
        /// </summary>
        /// <param name="keyboard">The current KeyboardState</param>
        /// <param name="oldKeyboard">The KeyboardState of the previous frame</param>
        /// <param name="keyList">When this method returns, contains the converted string of characters (used in case of fast typing).
        /// Else contains the null, (000), character.</param>
        /// <returns>True if conversion was successful</returns>
        public static bool TryConvertKeyboardInput(KeyboardState keyboard, KeyboardState oldKeyboard, out char key)
        {
            Keys[] keys = keyboard.GetPressedKeys();
            
            bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
            bool caps = keyboard.CapsLock;
            bool numLock = keyboard.NumLock;
            Keys lastKey=Keys.None;
            if (keys.Length > 0) 
            {
                foreach (var keyStroke in keys)
                {
                    // If key is a repeat of the previous, ignore.
                    if (keyStroke == lastKey) continue;
                    lastKey = keyStroke;

                    // enum Keys sets A = 65 to Z=90, and top row numbers 0 = 48 to 9=57, etc.
                    // enum Keys numPad0 = 96 to numPad9 = 105 (48 higher than regular numbers)
                    // use this to simplify algorithm
                    // ASCII A = 65, a = 97, 0 = 48 : lowercase = uppercase + 32


                    // Letters
                    for (int i = 65; i < 91; i++)
                        if (keyStroke == (Keys)i)
                        { key = ((char)((shift || caps) ? i : i + 32)); return true; }

                    // Numbers
                    for (int i = 48; i < 58; i++)
                    {
                        if ((!shift && keyStroke == (Keys)i) || (numLock && keyStroke == (Keys)(i + 48)))
                        { key = ((char)i); return true; }

                        if (shift && keyStroke == (Keys)i)
                        { key = shiftedNumKeys[i - 48]; return true; }
                    }

                    switch (keyStroke)
                    {


                        //Special keys
                        case Keys.OemTilde: if (shift) { key =('~'); } else { key =('`'); } return true;
                        case Keys.OemSemicolon: if (shift) { key =(':'); } else { key =(';'); } return true;
                        case Keys.OemQuotes: if (shift) { key =('"'); } else { key =('\''); } return true;
                        case Keys.OemQuestion: if (shift) { key =('?'); } else { key =('/'); } return true;
                        case Keys.OemPlus: if (shift) { key =('+'); } else { key =('='); } return true;
                        case Keys.OemPipe: if (shift) { key =( '|'); } else { key =( '\\'); } return true;
                        case Keys.OemPeriod: if (shift) { key =( '>'); } else { key =( '.'); } return true;
                        case Keys.OemOpenBrackets: if (shift) { key =( '{'); } else { key =( '['); } return true;
                        case Keys.OemCloseBrackets: if (shift) { key =( '}'); } else { key =( ']'); } return true;
                        case Keys.OemMinus: if (shift) { key =( '_'); } else { key =( '-'); } return true;
                        case Keys.OemComma: if (shift) { key =( '<'); } else { key =( ','); } return true;
                        case Keys.Space: key =( ' '); return true;
                        case Keys.Decimal: key =('.'); return true; // Number pad decimal key when !NumLock
                        case Keys.Divide: key =('/'); return true;
                        case Keys.Multiply: key =('*'); return true;
                        case Keys.Subtract: key =('-'); return true;
                        case Keys.Add: key =('+'); return true;

                    }
                }


                
            }

            key = (char)0;
            return false;
        }




    }
}
