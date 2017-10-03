using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace DungeonGeek
{
    static class InstructionsScreen
    {


        #region Fields
        private static GameWindow window = new GameWindow();

        #endregion


        internal static void Initialize(GraphicsDevice gd)
        {
            string instructions = "Esc - Exit";
            string header = "Instructions";
            List<string>  gameInstructionList = new List<string>();
            gameInstructionList.AddRange(new string[]
            {
                "Arrow Keys / Number Pad (Numlock Off) - General movement (may hold key down)",
                "SHIFT + movement - Run until you find something (door, monster, loot, etc.)",
                "   - Also explores tunnel until the end or first intersection",
                "Move toward locked door or monster to attack / bash it",
                "DEL - Rest one turn (may hold key down)",
                "INS - Search for hidden doors & traps (3x chance to find than movement or resting)",
                "A,S,W,D - Pan camera (normal and map view)",
                "TAB - Pan back to hero position",
                "M - Toggle map view",
                "I - Opens inventory screen",
                "H - Opens message history screen (last 22 messages)",
                "< - Go down stairs",
                "ESC - Exit any open screen, or opens Game Menu",
                "? - Displays this help screen (Duh)"
                // PLANNING: Update instruction list with added features
            });

            window.Initialize(gd, gameInstructionList, header, instructions);
        }

        internal static void Draw(SpriteBatch spriteBatch, Rectangle viewPortBounds)
        {
            window.Draw(spriteBatch, viewPortBounds);
        }

        internal static bool ProcessPlayerInputs(Keys key)
        {
            if (key == Keys.Escape)
            {
                return true;
            }

            return false; // Does not allow inventory menu to exit yet
        }




    }
}
