using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace DungeonGeek
{
    /// <summary>
    /// Magical Rings, as opposed to Ordinary Rings, have magical enchantments.
    /// Named this way in case of added structure for ordinary ring later that you can
    /// enchant with a scroll. May add a disenchant scroll to disenchant the ring.
    /// </summary>
    internal class MagicalRing : InventoryItem,  IWearable, INamable
    {
        #region Fields

        private GameConstants.RingEffects magicalEffect;
        private int effectPower;     // Cursed items get negative values by default,
                                     // but a negative value does not imply isCurssed
        private bool isCursed;
        private bool isLeftHand;
        private bool equipped=false;
        private string inventoryTitle;


        #endregion



        #region Properties
        
        internal bool IsCursed
        {
            get { return isCursed; }
            set { isCursed = value; }
        }

        public string ReservedTextForTitle
        {
            // For when user renames the ring, this reserves enough space for other parts of the
            // inventory title
            get { return "a ring inscribed \"" + string.Empty + "\" (Right hand)"; }
        }

        public bool Equipped // IWearable
        {
            get {return equipped;}

            set {equipped = value;}
        }

        public string WornOn // IWearable
        {
            get
            { 
                if (!equipped) return string.Empty;
                else return isLeftHand ? "(Left hand)" : "(Right hand)" ;
            }
        }

        internal override string InventoryTitle
        {
            get
            {
                return inventoryTitle; ;
            }
        }

        internal override string IdentifyText
        {
            get
            {
                if (!discoveredEffect)
                {
                    discoveredEffect = true;
                    inventoryTitle = EffectText();
                }
                return "It is " + inventoryTitle;
            }
        }

        #endregion



        /// <summary>
        /// Creates instance of a Magical Ring treasure. If a name is not assigned, creates an
        /// inscription that looks like magic words or phrases.
        /// </summary>
        /// <param name="givenName">Name to give the ring.</param>
        /// <param name="mapLocation">Where on the map the item is located (x,y)</param>
        internal MagicalRing(string givenName, Point mapLocation = new Point())
        {   
            inventoryTitle = "a ring inscribed \"" + givenName + "\"";
            discoveredEffect = false;
            InventoryWeight = 0.10f;
            Location = mapLocation;
            itemClass = GameConstants.ItemClasses.MagicalRing;
            SetEffectType();
            SetEffectPower();
        }

        /// <summary>
        /// Overloaded constructor creates a magical name for the ring and chains it to the original
        /// constructor above.
        /// </summary>
        /// <param name="mapLocation">Where on the map the item is located (x,y)</param>
        internal MagicalRing(Point mapLocation) : this(GenerateMagicalName(), mapLocation) { }

        internal MagicalRing() : this(Point.Zero) { }

        private void SetEffectType()
        {
            int effectRoll = rand.Next(100);
            if (effectRoll < 30) magicalEffect = GameConstants.RingEffects.EnhanceArmor; // 30%
            else if (effectRoll < 40) magicalEffect = GameConstants.RingEffects.ExtendSight; // 10%
            else if (effectRoll < 70) magicalEffect = GameConstants.RingEffects.IncreaseHitPoints; // 30%
            else magicalEffect = GameConstants.RingEffects.IncreaseStrength; // 30%
            return;
        }

        private void SetEffectPower()
        {
            // TODO: Consider increasing effectPower based on player level to encourage trying on new rings
            int effectRoll = rand.Next(100);
            if (effectRoll < 10) { IsCursed = true; effectPower = -2; }
            else if (effectRoll < 25) { IsCursed = true; effectPower = -1; }
            else if (effectRoll < 50) { IsCursed = false; effectPower = 1; }
            else if (effectRoll < 80) { IsCursed = false; effectPower = 2; }
            else { IsCursed = false; effectPower = 3; }
        }

        private string EffectText()
        {
            string effectType = string.Empty;
            switch (magicalEffect)
            {
                case GameConstants.RingEffects.IncreaseStrength:
                    effectType = "Ring of Strength";
                    break;
                case GameConstants.RingEffects.IncreaseHitPoints:
                    effectType = "Ring of Constitution";
                    break;
                case GameConstants.RingEffects.EnhanceArmor:
                    effectType = "Ring of Armor Fortification";
                    break;
                case GameConstants.RingEffects.ExtendSight:
                    effectType = "Ring of Far Sight";
                    break;
            }

            return (isCursed ? "a cursed " : "a ") + effectType + (effectPower >= 0 ? " (+" : " (") + effectPower + ")";
        }


        public void Rename(string newName) // INamable
        {
            inventoryTitle = "a ring inscribed \"" + newName + "\"";
        }


        public string Equip(List<InventoryItem> replacesItems) // IWearable
        {
            // Catch null parameters
            if (replacesItems == null) throw new ArgumentNullException("No list was passed in parameters");

            bool leftHandFull = false;
            bool rightHandFull = false;
            MagicalRing oldRing = null;


            // Catch items of wrong class, and determine if items of MagicalRing class are
            // equiped on left or right hand
            foreach (var item in replacesItems)
            {
                if (item.Class != Class) throw new ArgumentException("List contained one or more items from the wrong class");
                oldRing = item as MagicalRing;
                if (oldRing.Equipped && oldRing.WornOn == "(Left hand)") leftHandFull = true;
                if (oldRing.Equipped && oldRing.WornOn == "(Right hand)") rightHandFull = true;
            }

            // If hands are full, require removal of one first
            if (leftHandFull && rightHandFull) return "Magical rings cannot share the same hand. Remove one first.";

            equipped = true;
            if (!leftHandFull) isLeftHand = true;
            else isLeftHand = false;


            var it = IdentifyText; // Causes item to be renamed if just discovered

            return "You put " + InventoryTitle + " on your " + (isLeftHand ? "left hand." : "right hand.");
        }

        public string Unequip() // IWearable
        {
            if (equipped)
            {
                if (isCursed && !GameConstants.DEBUG_MODE_ALLOW_REMOVE_CURSED)
                    return "The curse causes the ring to dig in as you attempt to remove it.";
                equipped = false;
            }
            return string.Empty;
        }

        internal override bool IsSimilar(InventoryItem other)
        {
            if (other == null || other.Class != Class) return false;
            MagicalRing otherRing = other as MagicalRing;
            if (otherRing.magicalEffect != magicalEffect && otherRing.effectPower != effectPower) return false;
            return true;
        }

    }
}
