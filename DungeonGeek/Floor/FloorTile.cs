using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System.Collections.Generic;

namespace DungeonGeek
{
    /// <summary>
    /// Maintains Dictionary of all floor tiles and performs draw functions as needed for each
    /// where they are needed.
    /// </summary>
    static class FloorTile
    {
        internal enum Surfaces { Void, LitRoom, DarkRoom, Wall, Tunnel, OpenDoor, LockedDoor, HiddenDoor, StairsDown, StairsUp, Border }

        // graphic and drawing info
        private static Texture2D currentSprite;
        private static Dictionary<Surfaces, Texture2D> tileList = new Dictionary<Surfaces, Texture2D>();
        private static Dictionary<Surfaces, string> fileList = new Dictionary<Surfaces, string>();
        private static Dictionary<Surfaces, Rectangle> drawRectangleList = new Dictionary<Surfaces, Rectangle>();
        static FloorTile()
        {
            string spritePath = @"Sprites\Floor and Wall Tiles\";
            fileList.Add(Surfaces.LitRoom, spritePath + "roomFloor_35x35");
            fileList.Add(Surfaces.Tunnel, spritePath + "tunnel_35x35");
            fileList.Add(Surfaces.Wall, spritePath + "wall_35x35");
            fileList.Add(Surfaces.OpenDoor, spritePath + "open_door_on_wall_35x35");
            fileList.Add(Surfaces.LockedDoor, spritePath + "closed_door_on_wall_35x35");
            fileList.Add(Surfaces.HiddenDoor, spritePath + "wall_35x35");
            fileList.Add(Surfaces.StairsDown, spritePath + "stairs_down_35x35 v2");
            fileList.Add(Surfaces.Border, spritePath + "Boarder");

        }
        internal static void LoadContent(ContentManager contentManager)
        {
            
            foreach (var fileEntry in fileList)
            {
                currentSprite =  contentManager.Load<Texture2D>(fileEntry.Value);
                tileList.Add(fileEntry.Key, currentSprite);
                drawRectangleList.Add(fileEntry.Key, new Rectangle(0, 0,currentSprite.Width,currentSprite.Height));
            }
            // Because dark room now uses same sprite file as lit room, only with applied 
            // tinting for effect, reuse the same sprite.
            currentSprite = tileList[Surfaces.LitRoom];
            tileList.Add(Surfaces.DarkRoom, currentSprite);
            drawRectangleList.Add(Surfaces.DarkRoom, new Rectangle(0, 0, currentSprite.Width, currentSprite.Height));
        }

        internal static void Draw(SpriteBatch spriteBatch, Surfaces tileType, Rectangle currentDrawRectangle, bool heroSighted)
        {
            if (tileType == Surfaces.Void) return; // No sprite for this anyway

            currentSprite = tileList[tileType];
            Color tint = heroSighted ? Color.White : Color.Gray;
            spriteBatch.Draw(tileList[tileType], currentDrawRectangle, tint); // White is full color with no tinting
        }



    }
}
