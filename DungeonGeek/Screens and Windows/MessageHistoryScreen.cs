using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace DungeonGeek
{
    static class MessageHistory
    {
        #region Fields
        private static GameWindow window = new GameWindow();
        private static string instructions = "Esc - Exit";
        private static string header = "Message History (New to old):";
        private static List<string> messageHistory = new List<string>();

        #endregion

        internal static void Add(string message)
        {
            messageHistory.Add(message);
            if (messageHistory.Count > GameConstants.MESSAGE_HISTORY_RETENTION_LENGTH)
                messageHistory.RemoveRange(0, messageHistory.Count - GameConstants.MESSAGE_HISTORY_RETENTION_LENGTH);
        }

        internal static void AddRange(IEnumerable<string> messages)
        {
            messageHistory.AddRange(messages);
        }

        internal static void Clear()
        {
            messageHistory.Clear();
        }

        internal static void Initialize(GraphicsDevice gd)
        {
            window.Initialize(gd, messageHistory, header, instructions);
        }

        internal static void Show(SpriteBatch spriteBatch, Rectangle viewPortBounds)
        {
            window.Draw(spriteBatch, viewPortBounds);
        }

        internal static bool ProcessPlayerInputs(Keys key)
        {
            if (key == Keys.Escape)
            {
                return true;
            }

            return false; // Does not allow history screen to close yet
        }




    }
}
