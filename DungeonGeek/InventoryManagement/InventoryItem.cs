using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using System;
using System.Text;
using System.Collections.Generic;

namespace DungeonGeek
{
    /// <summary>
    /// Abstract class for any item the hero may find lying around that he can add to
    /// his/her inventory. Class includes methods for drawing the item, generating a
    /// random magical name, and all the shared properties of inventory items that allow
    /// them to be added to and worked with within a List.
    /// </summary>
    internal abstract class InventoryItem : IEquatable<InventoryItem>
    {
        #region Fields
        // protected fields are accessible from current and subclasses only
        protected static Random rand = InventoryEffectManager.RandomNumberGenerator;
        protected GameConstants.ItemClasses itemClass;
        protected bool discoveredEffect;
        private float inventoryWeight;
        private Point location;
        
        private bool isStackable=false;
        private int uniqueID;
        private static int nextID = 0;

        // graphic and drawing info
        protected string spriteFile;
        protected Texture2D sprite;

        #endregion



        #region Properties
        // internal properties are available anywhere within the same name space
        // Must have private variable to reference for property set/get methods


        internal abstract string InventoryTitle { get;} // name as shown in inventory

        internal int UniqueID
        {
            get { return uniqueID; }
        }

        internal static int GetNextID
        {
            get { return nextID++; }
        }

        internal float InventoryWeight // weight as it counts towards carry limit
        {
            get { return inventoryWeight; }
            set { inventoryWeight = value; }
        }
        
        internal Point Location // where object can be found on current map if not in inventory
        {
            get { return location; }
            set { location = value; }
        }
        
 
        internal bool IsStackable
        {
            get { return isStackable; }
            set { isStackable = value; }
        }

        internal GameConstants.ItemClasses Class
        {
            get { return itemClass; }
        }
        
        internal string SortingValue
        {
            get { return Class.ToString() + ":" + InventoryTitle + ":" + UniqueID; }
        }

        internal string DiscoveryText
        {
            get {return "You found " + InventoryTitle;}
        }

        internal abstract string IdentifyText {get;}

        #endregion



        #region Constructor


        protected InventoryItem()
        {
            uniqueID = GetNextID;
            //InventoryTitle = GenerateMagicalName();
            // TODO: Move this to the items that need it, not all items
        }

        
        #endregion



        #region Methods

        public override string ToString()
        {
            return InventoryTitle;
        }

        /// <summary>
        /// Needed for cloning objects from static lists. Generates a new ID for those objects.
        /// </summary>
        internal void AssignNewID()
        {
            uniqueID = GetNextID;
        }


        /// <summary>
        /// Creates a magical name using random words from the word list.
        /// </summary>
        /// <returns></returns>
        protected static string GenerateMagicalName()
        {
            string[] wordList = {"bik","lok","nuk","sac","do","voo","kel","nif",
                          "abice","croda","nebel","monji","nelba","baxtice","barlow",
                          "borna","owata","coflack","zeltona","yalto","lupidus","goat"};

            
            int numberOfWords = rand.Next(4, 8);
            StringBuilder magicalName = new StringBuilder();

            for (int wordNum = 0; wordNum < numberOfWords; wordNum++)
            {
                magicalName.Append(wordList[rand.Next(wordList.Length)]);
                if (wordNum < numberOfWords - 1) magicalName.Append(" ");
            }
            
            return magicalName.ToString(); ;
        }


        /// <summary>
        /// Updates draw rectangle for sprite based on currentView of level floor plan
        /// so that as the view pans to the right keeping centered on hero if possible, the
        /// drawRectangle moves to the left by the given number of pixels as determined by
        /// tileWidth. Same concept for vertical and tileHeight.
        /// </summary>
        /// <param name="currentView">Tiles in floor map within viewable area</param>
        /// <param name="tileWidth">pixel width of tile</param>
        /// <param name="tileHeight">pixel height of tile</param>


        internal void Draw(SpriteBatch spriteBatch, Rectangle currentDrawRectangle, Rectangle currentView)
        {
            if (currentView.Contains(Location))
            {
                // Because there are several different sprites used depending on the type of armor
                if (itemClass == GameConstants.ItemClasses.Armor)
                    sprite = (this as Armor).Sprite;
                else if (itemClass == GameConstants.ItemClasses.Weapon)
                    sprite = (this as Weapon).Sprite;
                else
                    sprite = Inventory.InventorySprites[itemClass];

                spriteBatch.Draw(sprite, Utility.FittedSprite(sprite,currentDrawRectangle), Color.White);
            }
        }

        

        /// <summary>
        /// Used to determine if two items being stackable are actually the same. For instance, the class
        /// alone doesn't work if two scrolls have different effects.
        /// </summary>
        /// <param name="other">The other item to compare to</param>
        /// <returns></returns>
        public bool Equals(InventoryItem other) //IEquatable<InventoryItem>
        {
            return UniqueID == other.UniqueID;
        }

        internal abstract bool IsSimilar(InventoryItem other);

        #endregion

    }
}
