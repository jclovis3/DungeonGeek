using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace DungeonGeek
{
    /// <summary>
    /// Holds all shared methods and data between classes that don't fit well being kept in
    /// any one other class. This is likely due to their frequency of use across multiple classes.
    /// </summary>
    static class Utility
    {
        private static Random rand = new Random();
        

        internal static Random Rand { get { return rand; } }
        


        internal static int GetVisibleDistance()
        {
            int visibleDistance = 1;
            if (InventoryEffectManager.HeroBlind)
                visibleDistance = 0;
            else if (InventoryEffectManager.HeroSeesInDark)
                visibleDistance = 3;
            return visibleDistance;
        }

        internal static Point GetNewPointFrom(GameConstants.Direction8 direction, Point fromPoint)
        {
            var newX = fromPoint.X;
            var newY = fromPoint.Y;
            switch (direction)
            {
                case GameConstants.Direction8.UpLeft: newX--; newY--; break;
                case GameConstants.Direction8.Up: newY--; break;
                case GameConstants.Direction8.UpRight: newX++; newY--; break;
                case GameConstants.Direction8.Right: newX++; break;
                case GameConstants.Direction8.DownRight: newX++; newY++; break;
                case GameConstants.Direction8.Down: newY++; break;
                case GameConstants.Direction8.DownLeft: newX--; newY++; break;
                case GameConstants.Direction8.Left: newX--; break;
                default: break;

            }
            return new Point(newX, newY);
        }

        internal static Point GetNewPointFrom(GameConstants.Direction4 direction, Point fromPoint)
        {
            return GetNewPointFrom(ConvertToDirection8(direction), fromPoint);
        }

        /// <summary>
        /// Returns an array of Points for the 8 tiles around a given point.
        /// </summary>
        /// <param name="center"></param>
        /// <returns>Array of Points</returns>
        internal static Point[] TilesAround(Point center)
        {
            Point[] suroundingPoints = new Point[8];
            int i = 0;
            for (int x = center.X - 1; x < center.X + 2; x++)
                for (int y = center.Y - 1; y < center.Y + 2; y++)
                    if (x != center.X && y != center.Y)
                        suroundingPoints[i++] = new Point(x, y);

            return suroundingPoints;
        }

        /// <summary>
        /// Determines the draw rectangle for a sprite preserving aspect ratio to fit within a tile
        /// </summary>
        /// <param name="sprite"></param>
        /// <param name="tile"></param>
        /// <returns></returns>
        internal static Rectangle FittedSprite(Texture2D sprite, Rectangle tile)
        {
            Rectangle fittedRec = new Rectangle();
            if (sprite.Width > sprite.Height)
            {
                fittedRec.Width = tile.Width;
                fittedRec.Height = (int)(((float)sprite.Height / sprite.Width) * tile.Height);
                fittedRec.X = tile.X;
                fittedRec.Y = tile.Center.Y - (fittedRec.Height / 2);
            }
            else
            {
                fittedRec.Height = tile.Height;
                fittedRec.Width = (int)(((float)sprite.Width / sprite.Height) * tile.Width);
                fittedRec.Y = tile.Y;
                fittedRec.X = tile.Center.X - (fittedRec.Width / 2);
            }
            return fittedRec;
        }

        internal static GameConstants.Direction8 ConvertToDirection8(GameConstants.Direction4 direction)
        {
            switch (direction)
            {
                case GameConstants.Direction4.Up:
                    return GameConstants.Direction8.Up;
                case GameConstants.Direction4.Right:
                    return GameConstants.Direction8.Right;
                case GameConstants.Direction4.Down:
                    return GameConstants.Direction8.Down;
                case GameConstants.Direction4.Left:
                    return GameConstants.Direction8.Left;
                default: throw new ArgumentException("Invalid Direction4 value received for conversion.");
            }
        }

        /// <summary>
        /// Standard distance formula to return the distance between two points
        /// </summary>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        /// <returns></returns>
        internal static double Distance(Point ptA, Point ptB)
        {
            return Math.Sqrt((Math.Pow(ptA.X - ptB.X,2) + Math.Pow(ptA.Y - ptB.Y,2)));
        }

        static internal bool InLineOfSight(Point startPt, Point endPt, int visibleDistance)
        {
            
            // for each point along the path from ptA to ptB, check the floor tile
            Vector2 path = new Vector2(endPt.X - startPt.X, endPt.Y - startPt.Y);
            Point checkPoint = Point.Zero;
            double targetDistance = Math.Floor(Distance(startPt, endPt));
            if (targetDistance > visibleDistance) return false;

            int testDistance = MathHelper.Min((int)targetDistance, visibleDistance);
            var theta = Math.Atan2(path.Y, path.X);
            for (int d = 1; d <= testDistance; d++)
            {
                var x = d * Math.Cos(theta);
                var y = d * Math.Sin(theta);
                checkPoint.X = (int)(startPt.X + x);
                checkPoint.Y = (int)(startPt.Y + y);
                // If outside boarder, space is not LOS
                if (checkPoint.X < 0 || checkPoint.Y < 0 ||
                    checkPoint.X > FloorGenerator.FloorWidth - 1 ||
                    checkPoint.Y > FloorGenerator.FloorHeight - 1)
                    return false;

                // If it's right next to you, such as monster on a door or you are on a door
                if (targetDistance <= 1) return true;

                // If it's a wall, void, or closed door, it's not LOS
                var tile = FloorGenerator.GetTileAt(checkPoint);
                if (!FloorGenerator.TileIsPassible(tile) ||
                    tile == FloorTile.Surfaces.OpenDoor)    // because this is also a light barrier
                    return false;

                // Keep checking until end of path reached
            }
            // Remaining spaces do not block monsters from being seen
            return true;
        }

        

        
    }
}
