using System;
using Microsoft.Xna.Framework;


namespace DungeonGeek
{
    class Food : InventoryItem , IConsumable, IStackable
    {
        private const float UNIT_WEIGHT = 0.5f;
        private const string INV_TITLE_OF_UNIT = "Mystery Meat";

        private int baseCalories = 800;
        private int qty=1;
        private string inventoryTitle;
        
        #region Properties

        public int Qty { get { return qty; }  } // IStackable

        internal override string InventoryTitle
        {
            get
            {
                if (qty == 1) return INV_TITLE_OF_UNIT;
                else return INV_TITLE_OF_UNIT + " (" + qty + ")";
            }
        }

        internal override string IdentifyText
        {
            get
            {
                return "It is food and it gives you energy (or makes you fat if you consume too much).";
            }
        }


        #endregion


        #region Constructor
        internal Food(Point mapLocation)
        {
            inventoryTitle = INV_TITLE_OF_UNIT;
            IsStackable = true;
            InventoryWeight = UNIT_WEIGHT;
            qty = 1;
            Location = mapLocation;
            itemClass = GameConstants.ItemClasses.Food;

        }

        internal Food() : this(Point.Zero) { }

        #endregion


        #region Methods
        /// <summary>
        /// Picks a portion size and assigns an appropriate number of calories to the hero.
        /// </summary>
        /// <returns>Message to the player about the type of food eaten</returns>
        public string Consume()                     // IConsumable
        {
            int deviationEffect = rand.Next(3);
            int cal = baseCalories;
            string result=string.Empty;
            if(deviationEffect==0)
            {
                result = "That wasn't very filling";
                cal /= 2;
            } else if(deviationEffect == 1)
            {
                result = "That hit the spot";
            } else
            {
                result = "Such a large portion, you feel like you couldn't eat another bite.";
                cal *= 2;
            }
            DungeonGameEngine.Hero.Eat(cal);
            return result;
        }

        public IStackable Remove()                  // IStackable
        {
            if (qty > 1)
            {
                qty--;
                AdjustProperties();
                return new Food(Location);
            }
            else throw new ArithmeticException("A pile of food cannot exist with qty = 0");
        }

        public void Add(int qty = 1)                // IStackable
        {
            if (qty > 0) this.qty += qty;
            else throw new ArithmeticException("Cannot add a negative qty to a pile of food");
            AdjustProperties();
        }

        private void AdjustProperties()
        {
            InventoryWeight = qty * UNIT_WEIGHT;
            
        }

        internal override bool IsSimilar(InventoryItem other)
        {
            return (other != null && other.Class == Class);
        }

        
        #endregion
    }
}
