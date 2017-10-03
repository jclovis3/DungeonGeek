using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DungeonGeek
{
    class Armor : InventoryItem, IWearable
    {
        #region Constants and enums
        internal enum ArmorTypes { Leather, Chain, Plate } // Only add new types when a separate graphic is ready to represent it
        #endregion



        #region Fields
        private static Dictionary<ArmorTypes, Texture2D> armorSprites;
        private ArmorTypes armorType;
        private int armorBonus;
        private bool canRust;
        private string inventoryTitle;
        private bool isCursed;
        private bool equipped = false;
        
        
        #endregion



        #region Properties

        internal int ArmorBonus
        {
            get
            {
                return armorBonus;
            }

            set
            {
                armorBonus = value;
            }
        }

        internal bool CanRust
        {
            get
            {
                return canRust;
            }
        }

        internal override string IdentifyText
        {
            get
            {
                if(!discoveredEffect)
                {
                    discoveredEffect = true;
                    inventoryTitle = QualityText();
                }
                return "This armor is " + inventoryTitle;
            }
        }                  // InventoryItem

        internal override string InventoryTitle
        {
            get
            {
                return inventoryTitle;
            }
        }                // InventoryItem
        
        internal Texture2D Sprite
        {
            get { return armorSprites[armorType]; }

        }

        public bool Equipped
        {
            get
            {
                return equipped;
            }

            set
            {
                equipped = value;
            }
        }

        public string WornOn
        {
            get
            {
                if (!equipped) return string.Empty;
                else return "(being worn)";
            }
        }
        #endregion


        #region Static Methods

        internal static void LoadContent(ContentManager contentManager)
        {
            armorSprites = new Dictionary<ArmorTypes, Texture2D>();
            armorSprites.Add(ArmorTypes.Leather,
                contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_ARMOR + "leather_armor_25x26"));
            armorSprites.Add(ArmorTypes.Chain,
                contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_ARMOR + "chain_armor_30x34"));
            armorSprites.Add(ArmorTypes.Plate,
                contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_ARMOR + "plate_armor_17x34"));


        }

        private static int CalculateArmorBonus(ArmorTypes type, bool curse)
        {
            // http://www.dandwiki.com/wiki/SRD:Armor
            switch (type)
            {
                case ArmorTypes.Leather:
                    return curse ? -1 : 2;
                case ArmorTypes.Chain:
                    return curse ? -2 : 5; // Chainmail
                case ArmorTypes.Plate:
                    return curse ? -3 : 8; // Full Plate
                default:
                    return 0;
            }
        }

        #endregion


        
        #region Instance Constructor
        internal Armor(Point location, int floorLevel)
        {
            itemClass = GameConstants.ItemClasses.Armor;
            Location = location;
            // Decide if cursed
            isCursed = (Utility.Rand.Next(8) == 0);
            // Leather can be found anywhere, Chain increases in chance first, then Plate later
            float leatherProbability = 0.25f;
            float chainProbability = MathHelper.Min(floorLevel < 5 ? 0 : 0.02f * floorLevel, 0.25f);
            float plateProbability = MathHelper.Min(floorLevel < 15 ? 0 : 0.005f * floorLevel, 0.25f);
            
            int newType = 0;
            while (newType == 0)
            {
                int testType = Utility.Rand.Next(3) + 1;
                switch(testType)
                {
                    case 1: if (Utility.Rand.NextDouble() < leatherProbability) newType = testType; break;
                    case 2: if (Utility.Rand.NextDouble() < chainProbability) newType = testType; break;
                    case 3: if (Utility.Rand.NextDouble() < plateProbability) newType = testType; break;
                }
            }
            
            switch(newType)
            {
                case 2: armorType = ArmorTypes.Chain; InventoryWeight = 40;  break;
                case 3: armorType = ArmorTypes.Plate; InventoryWeight = 50;  break;
                default: armorType = ArmorTypes.Leather; InventoryWeight = 15;  break;
            }
            armorBonus = CalculateArmorBonus(armorType, isCursed);
            canRust = (armorType == ArmorTypes.Chain || armorType == ArmorTypes.Plate);
            inventoryTitle = "some " + armorType + " armor";
        }
        
        #endregion
        
        
        
        #region Instance Methods
        internal override bool IsSimilar(InventoryItem other)
        {
            if (other == null || other.Class != Class) return false;
            Armor otherArmor = other as Armor;
            if (otherArmor.armorType != armorType &&
                otherArmor.armorBonus != armorBonus &&
                otherArmor.isCursed != isCursed) return false;
            return true;
        }    // InventoryItem

        
        
        private string QualityText()
        {
            StringBuilder qtyText = new StringBuilder();
            qtyText.Append(isCursed ? "a cursed suit of" : "a suit of");
            qtyText.Append(" " + armorType + " armor");
            qtyText.Append("(" + (armorBonus > 0 ? "+" : string.Empty) + armorBonus + ")");
            return qtyText.ToString();
        }

        public string Equip(List<InventoryItem> replacesItems)
        {
            // Catch null parameters
            if (replacesItems == null) throw new ArgumentNullException("No list was passed in parameters");

            Armor oldArmor = null;
            
            // Catch items of wrong class, and determine if items of MagicalRing class are
            // equiped on left or right hand
            foreach (var item in replacesItems)
            {
                if (item.Class != Class) throw new ArgumentException("List contained one or more items from the wrong class");
                oldArmor = item as Armor;
                if (oldArmor.Equipped) return "It doesn't fit over your existing armor.";
            }
            
            equipped = true;
            bool newlyDiscovered = !discoveredEffect;
            var it = IdentifyText; // Causes item to be renamed if just discovered

            inventoryTitle = QualityText();
            if (newlyDiscovered)
                return "You are now wearing " + QualityText();
            else
                return string.Empty;


        }   // IWearable

        public string Unequip()
        {
            if (equipped)
            {
                if (isCursed && !GameConstants.DEBUG_MODE_ALLOW_REMOVE_CURSED)
                    return "The cursed armor resists being pulled off.";
                inventoryTitle = QualityText();
                equipped = false;
            }
            return string.Empty;
        }                                  // IWearable

        #endregion
    }
}
