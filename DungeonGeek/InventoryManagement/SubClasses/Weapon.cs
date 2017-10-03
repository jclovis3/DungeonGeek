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
    internal class Weapon : InventoryItem
    {
        internal enum SubClass { Stick, Knife, /*Bow, Sling*/} // To be expanded gradually
        
        
        #region Fields
        private CombatManager.DamageType damageType; // Slashing, Bludgeoning, etc...
        private CombatManager.CombatType combatType; // Melee or Ranged
        private KeyValuePair<CombatManager.DamageType, CombatManager.DieType>[] damageDice;
        private static Dictionary<SubClass, Texture2D> weaponSprites;
        private SubClass subClass;
        private string inventoryTitle;
        private bool wielded;

        #endregion




        #region Properties
        internal CombatManager.DamageType DamageType
        {
            get { return damageType; }
            set { damageType = value; }
        }

        internal CombatManager.CombatType CombatType
        {
            get { return combatType; }
            set { combatType = value; }
        }

        internal KeyValuePair<CombatManager.DamageType, CombatManager.DieType>[] DamageDice
        {
            get { return damageDice; }
            set { damageDice = value; }
        }

        internal override string InventoryTitle
        {
            get
            {
                return inventoryTitle;
            }
        }

        internal override string IdentifyText
        {
            get
            {
                return "This " + combatType + " weapon is a " + InventoryTitle;
            }
        }

        internal override bool IsSimilar(InventoryItem other)
        {
            throw new NotImplementedException();
        }

        internal bool Wielded
        {
            get { return wielded; }
            set { wielded = value; }
        }

        internal Texture2D Sprite
        {
            get { return weaponSprites[subClass]; }

        }
        #endregion




        #region Instance Constructor

        internal Weapon(SubClass weaponType, Point location)
        {
            Location = location;
            subClass = weaponType;
            switch (weaponType)
            {
                case SubClass.Stick:
                    // http://www.dandwiki.com/wiki/Cane_(3.5e_Equipment)
                    combatType = CombatManager.CombatType.Melee;
                    damageType = CombatManager.DamageType.Bludgeoning;
                    InventoryWeight = 3f;
                    damageDice = new KeyValuePair<CombatManager.DamageType, CombatManager.DieType>[]
                    {
                        new KeyValuePair<CombatManager.DamageType, CombatManager.DieType>
                        (CombatManager.DamageType.Bludgeoning, CombatManager.DieType.D6)
                    };
                    inventoryTitle = "medium stick (1d6 bludgeoning)";
                    break;
                case SubClass.Knife:
                    // http://www.dandwiki.com/wiki/Knife_(5e_Equipment)
                    combatType = CombatManager.CombatType.Melee;
                    damageType = CombatManager.DamageType.Slashing;
                    InventoryWeight = 0.25f;
                    damageDice = new KeyValuePair<CombatManager.DamageType, CombatManager.DieType>[]
                    {
                        new KeyValuePair<CombatManager.DamageType, CombatManager.DieType>
                        (CombatManager.DamageType.Slashing, CombatManager.DieType.D3)
                    };
                    inventoryTitle = "small knife (1d3 slashing)";
                    break;
                //case SubClass.Bow:
                    //break;
                //case SubClass.Sling:
                    //break;
                default:
                    break;
            }

            // Chance weapon is broken
            if (Utility.Rand.Next(10) == 0)
                Break();
        }

        internal Weapon(Point location, int floorNumber) : this(ChoseWeaponType(floorNumber), location) { }

        #endregion




        #region Static Methods

        internal static void LoadContent(ContentManager contentManager)
        {
            weaponSprites = new Dictionary<SubClass, Texture2D>();
            weaponSprites.Add(SubClass.Stick,
                contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_WEAPON + "stick_36x09"));
            weaponSprites.Add(SubClass.Knife,
                contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_WEAPON + "knife_24x35"));

            /*
            
            weaponSprites.Add(RangedWeaponTypes.Bow,
                contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_WEAPON + "tbd"));
            weaponSprites.Add(RangedWeaponTypes.Sling,
                contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_WEAPON + "tbd"));
            */
        }

        internal static SubClass ChoseWeaponType(int floorLevel)
        {
            // For now, just pick any weapon type randomly
            return (SubClass)Utility.Rand.Next(Enum.GetNames(typeof(SubClass)).Length);
            /* TODO: When more weapons are available, allow the floor number to affect
             * the probability of what type of weapon is chosen.
            */ 

        }

        #endregion




        #region Instance Methods
        internal string Break()
        {
            damageDice = new KeyValuePair<CombatManager.DamageType, CombatManager.DieType>[]
                    {
                        new KeyValuePair<CombatManager.DamageType, CombatManager.DieType>
                        (CombatManager.DamageType.Bludgeoning, CombatManager.DieType.D2)
                    };
            inventoryTitle = "broken " + subClass + " (1d2 bludgeoning)";

            return "Your " + subClass + " just broke.";
        }



        #endregion

    }
}
