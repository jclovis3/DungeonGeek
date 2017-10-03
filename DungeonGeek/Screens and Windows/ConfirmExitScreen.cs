using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System.Text;

namespace DungeonGeek
{
    static class ConfirmExitScreen
    {



        #region Fields
        private static GameWindow window = new GameWindow();
        private static string header = "";
        private static string prompt = "You will have to start over. Are you sure?";
        private static string instructions = "Y - Exit      N - Return to game";
        private static string purpose = string.Empty;

        #endregion

        #region Properties
        internal static string Purpose
        {
            get { return purpose; }
            set { purpose = value; }
        }

        #endregion


        internal static void Initialize(GraphicsDevice gd)
        {
            window.Initialize(gd, prompt, instructions);
        }

        internal static void Draw(SpriteBatch spriteBatch, Rectangle viewPortBounds)
        {
            window.Draw(spriteBatch, viewPortBounds);
        }

        internal static bool ProcessPlayerInputs(Keys key, out bool ConfirmQuit)
        {
            ConfirmQuit = false;
            if (key == Keys.Y)
            {
                ConfirmQuit = true;
                return true; // Screen closes
            }
            else if(key == Keys.N || key == Keys.Escape)
            { return true; }

            return false; // Does not allow screen to exit yet
        }

    }
}
