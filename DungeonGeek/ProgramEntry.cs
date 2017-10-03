using System;

using System.Text;


namespace DungeonGeek
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class ProgramEntry
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            
            using (var game = new DungeonGameEngine())
            {
                
                

                game.Run();
            }
            
        }

        
    }
#endif
}
