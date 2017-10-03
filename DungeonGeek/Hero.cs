using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;

namespace DungeonGeek
{
    class Hero : ICombatant
    {
        #region Fields

        
        // graphic and drawing info
        static private string spriteFile;
        static private Texture2D sprite;


        // Game data
        private Point location;
        private string heroName = "John Doe";
        private int xpLevel = 1;
        private long xp = 0;
        private int hp; // set to baseHP in the constructor
        private CombatManager.AbilityScores abilityScores;

        // Daily calorie requirement. Excess energy penalizes strength by 1 for
        // every 1000 calories over being full. Being overburdened consumes calories twice as fast.
        // Reducing effective strength due to being too fat can cause overburdened state if inventory
        // is already near full, thus making the fat man burn off excess calories faster.
        private int energy = GameConstants.STAT_FULL_ENERGY;
        private bool fat = false;
        private bool hungry = false;


        // Base values are starting values and shall be computed with methods
        // for effective values of the same. They increase with XP level.

        private int baseCarryWeight = 66; // All weights are in pounds - Table from http://www.dandwiki.com/wiki/SRD:Carrying_Capacity

        private string deathMessage=string.Empty;
        private int max_HP;
        private int hitDie = 10;
        private int baseHealCycle = 30;
        private int healCycle = 30-1; // Number of turns required to heal one point == base - xpLevel
        // NOTE: Armor will be calculated from Inventory items being worn
        // and magican enhancements and curses



        #endregion

        #region Properties

        internal Point Location // where here is on map; (0,0) is top left corner
        {
            get { return location; }
            set { location = value; }
        }

        internal string HeroName
        {
            get { return heroName; }
            set { heroName = value; }
        }

        public string EntityName            // ICombatant
        {
            get { return "you"; }
        }
        internal int HP
        {
            get { return hp; }
        }

        public int Max_HP
        {
            get { return max_HP; }
        }               // ICombatant
        internal int XPLvl
        {
            get { return xpLevel; }
        }

        internal long XP
        {
            get { return xp; }
        }


        internal int BaseStrength
        {
            get { return abilityScores.Str; }
        }

        internal int Energy
        {
            get { return energy; }
        }

        internal int EnergyUseRate
        {
            get { return InventoryEffectManager.HeroOverburdened ? 2 : 1; }
        }

        internal int EnergyStorage // Will include mofifiers when those effects are added
        {
            get { return GameConstants.STAT_FULL_ENERGY; }

        }

        internal int EffectiveCarryWeight
        {
            get
            {
                // TODO: Replace "totalMod = 0" with method to screen for
                // modifiers from inventory items and temporary magical effects
                int totalMod = 0;

                // No effects added yet to modify carry weight limit

                return CalculateBaseCarryWeight(abilityScores.Str + totalMod);
            }
        }

        internal int BaseCarryWeight
        {
            get
            {
                return CalculateBaseCarryWeight(abilityScores.Str);
            }
        }

        internal int EffectiveStrength
        {
            get
            {
                // TODO: Replace strengthModifiers with method to screen for
                // modifiers from inventory items and temporary magical effects
                int totalMod = abilityScores.StrMod;
                if (fat)
                    totalMod -= (int)((energy - EnergyStorage) / 1000);

                return (abilityScores.Str + abilityScores.StrMod) < 0 ? 0 : abilityScores.Str + totalMod;
            }
        }

        internal string DeathMessage { get { return deathMessage; } set { deathMessage = value; } }

        internal long XpToNext
        {
            // http://www.dandwiki.com/wiki/5e_SRD:Gaining_a_Level
            get
            {
                int[] lvlReqSet = new int[]
                    {0, 300, 900, 2700, 6500, 14000, 23000, 34000, 48000, 64000, 85000, 100000,
                        120000, 140000, 165000, 195000, 225000, 265000, 305000, 355000 };
                if (XPLvl < 1) return 0;
                else if (XPLvl > 20) return int.MaxValue;
                else return lvlReqSet[XPLvl]; // ie level 1 hero needs 300 xp to gain level 2


            }
        }

        internal static string SpriteFile
        { get { return spriteFile; } }

        internal static Texture2D Sprite { set { sprite = value; } }

        #endregion


        static Hero()
        {
            spriteFile = GameConstants.SPRITE_PATH_MISC + "Hero_28x34";
        }

        internal Hero()
        {
            Location = new Point(0,0);
            
            abilityScores = new CombatManager.AbilityScores();
            abilityScores.Str = 10;
            abilityScores.Dex = 10;
            abilityScores.Con = 10;
            abilityScores.Int = 10;
            abilityScores.Wis = 10;
            abilityScores.Cha = 10;
            abilityScores.StrMod = 0;
            abilityScores.DexMod = 0;
            abilityScores.ConMod = 0;
            abilityScores.IntMod = 0;
            abilityScores.WisMod = 0;
            abilityScores.ChaMod = 0;
            max_HP = hitDie + abilityScores.ConMod;
            hp = max_HP;
            // TODO: Create input screen to gather name
            // heroName = Interaction.InputBox("Dust thou have a name?","Who are you","John Doe");
            if (heroName.Length < 1) heroName = "John Doe";
        }


        #region Methods

        #region Game Class Support
        

        internal void Draw(SpriteBatch spriteBatch, Rectangle tileSize, Rectangle localView)
        {
            Rectangle fittedRectangle = Utility.FittedSprite(sprite, FloorGenerator.TranslateRecToTile(tileSize, localView, Location));

            spriteBatch.Draw(sprite, fittedRectangle, Color.White); // White is full color with no tinting
        }
        #endregion

        internal bool AttemptPickupItem(InventoryItem item)
        {
            if (item.InventoryWeight < (EffectiveCarryWeight - Inventory.Weight))
            {
                Inventory.Add(item);
                return true;
            }
            else
            {
                DungeonGameEngine.ProcessMessageQueue(false, "You need to drop something first.");
                return false;
            }
        }

        /// <summary>
        /// Checks the ground below the hero's feet and picks up the item if there is room.
        /// Works on stacks of items if on the same space as well. 
        /// </summary>
        internal void PickUpLoot(out List<string> messages)
        {
            // Created pickupList as class variable so as not to create
            // and dispose of a new object with every move. Can't alter the scatteredLoot
            // list while iterating through it so a secondary list was needed.
            messages = new List<string>();
            
            for(int i= FloorGenerator.ScatteredLoot.Count-1; i>=0; i--)
            {
                var item = FloorGenerator.ScatteredLoot[i];
                if (item.Location == Location && AttemptPickupItem(item))
                {
                    FloorGenerator.ScatteredLoot.RemoveAt(i);
                    messages.Add(item.DiscoveryText);
                }
            }
        }

        /// <summary>
        /// Subtracts hpHit from hitpoints after adjusting for armor rating.
        /// </summary>
        /// <param name="hpHit"></param>
        /// <returns>true if hero survives hit, otherwise false</returns>
        internal bool TakeHit(int hpHit)
        {
            hp -= hpHit;
            if (hp <= 0)
            {
                hp = 0;
                return false;
            }
            else return true;
        }

        /// <summary>
        /// Poison has a 1 in 4 chance of doing a point of damage
        /// </summary>
        /// <returns></returns>
        internal bool TakePoisonDmg()
        {
            if (Utility.Rand.Next(4)==0 && --hp <= 0)
            {
                hp = 0;
                return false;
            }
            return true;

        }

        internal void Heal(int healAmt = 0)
        {
            // PLANNING: Potion idea : Healing - Calls heal with a specified amount
            // Unlike D&D campaigns involving short and long rests and limitting to the number of
            // hit dice you have to use this algorithm allows healing slowly while walking about.
            // Using the rest command should call this method more often.
            if (hp < max_HP)
            {
                if (healAmt > 0)
                {
                    hp += healAmt;
                    if (baseHealCycle - xpLevel < 1) healCycle = 1;
                    else healCycle = baseHealCycle - xpLevel;
                }
                else if (--healCycle <= 0)
                    {
                        hp++;
                        if (baseHealCycle - xpLevel < 1) healCycle = 1;
                        else healCycle = baseHealCycle - xpLevel;
                }
            }
            if (hp > max_HP) hp = max_HP;
            
        }


        /// <summary>
        /// Decrements energy counter by current rate and sends messages when status changes.
        /// Loss of all calories results in returning false and the hero should die of starvation.
        /// </summary>
        /// <returns>True if hero survives starvation</returns>
        internal bool BurnCalories()
        {
            energy -= EnergyUseRate;
            return SurvivesEnergyChange();
        }

        internal void Eat(int calories)
        {
            energy += calories;
            SurvivesEnergyChange();
        }

        internal void RestoreStrength()
        {
            // Removes any changes made from attacks or other effects.
            abilityScores.StrMod = (int)Math.Floor((double)((abilityScores.Str - 10) / 2));
        }

        private bool SurvivesEnergyChange()
        {
            if (fat && energy <= EnergyStorage + 1000)
            {
                fat = false;
                DungeonGameEngine.ProcessMessageQueue(false,"You are feeling much leaner and stronger again.");
            }
            else if (!fat && energy > EnergyStorage + 1000)
            {
                fat = true;
                DungeonGameEngine.ProcessMessageQueue(false, "You are beginning to feel fat (and not as strong).");
            }
            else if (energy > GameConstants.ENERGY_FAINT_LEVEL * 2)
            {
                InventoryEffectManager.FeelsFaint = false;
                hungry = false;
            }
            else if (energy > GameConstants.ENERGY_FAINT_LEVEL)
            {
                InventoryEffectManager.FeelsFaint = false;
                if (!hungry) DungeonGameEngine.ProcessMessageQueue(false, "You are beginning to feel hungry.");
                hungry = true;
            }
            else if (energy > 0)
            {
                if (!InventoryEffectManager.FeelsFaint) DungeonGameEngine.ProcessMessageQueue(false, "You feel faint from lack of food.");
                InventoryEffectManager.FeelsFaint = true;
            }
            else
            {
                return false;
            }
            return true;
        }

        public void Attack(ICombatant target)
        {
            // Construct attack message
            CombatManager.AttackDelivery attackMessage = new CombatManager.AttackDelivery();
            
            var currentWeapon = Inventory.CurrentWeapon;
            if(currentWeapon != null)
            {
                attackMessage.CombatType = currentWeapon.CombatType;
                attackMessage.DamageDice = currentWeapon.DamageDice;
            }
            else
            {
                // http://www.dandwiki.com/wiki/SRD:Unarmed_Strike
                attackMessage.CombatType = CombatManager.CombatType.Melee;
                attackMessage.DamageDice = new KeyValuePair<CombatManager.DamageType, CombatManager.DieType>[]
                {
                    new KeyValuePair<CombatManager.DamageType, CombatManager.DieType>
                    (CombatManager.DamageType.Bludgeoning, CombatManager.DieType.D3)
                };
            }
            attackMessage.CombatType = CombatManager.CombatType.Melee;
            attackMessage.Attacker = this;
            attackMessage.Target = target;
            attackMessage.ToHitModifier = 0;
            attackMessage.DamageModifiers = new Dictionary<CombatManager.DamageType, int>(0); // Set Capacity to 1


            // Send the attack
            CombatManager.AttackResult attackResult = CombatManager.DeliverAttack(attackMessage);


            // Process the attack results
            if (attackResult.Critical)
                DungeonGameEngine.ProcessMessageQueue(false,
                    "You struck a Critical blow to the " + target.EntityName + ".");
            else if (attackResult.Fumble)
            {
                // Not all fumbles have to cause trouble
                if(Utility.Rand.Next(4)==0)
                {
                    // But this one did, so what happened
                    var fumbleAction = Utility.Rand.Next(100);
                    if(fumbleAction<70)
                    {
                        // Either you hit yourself
                        DungeonGameEngine.ProcessMessageQueue(false,
                            "You swing wildly and miss, hitting yourself in the head in the process.");
                        if (Utility.Rand.Next(2) == 0)
                            InventoryEffectManager.HeroConfused = true;
                        else
                            InventoryEffectManager.KnockHeroUnconcious();
                    }
                    else
                    {
                        // Or you broke your weapon
                        if(currentWeapon != null)
                            DungeonGameEngine.ProcessMessageQueue(false, currentWeapon.Break());
                    }
                }
            }
            else if (attackResult.Hit)
                DungeonGameEngine.ProcessMessageQueue(false,
                    "You hit the " + target.EntityName + ".");
            else
            {
                DungeonGameEngine.ProcessMessageQueue(false,
                    "You missed the " + target.EntityName + ".");
            }
        }                           // ICombatant

        public CombatManager.DefenseDelivery Defend()
        {
            CombatManager.DefenseDelivery deffenseMessage = new CombatManager.DefenseDelivery();
            deffenseMessage.ArmorBonus = Inventory.TotalArmorModifiers();
            deffenseMessage.DexterityModifier = abilityScores.DexMod;
            deffenseMessage.ShieldBonus = 0; // TODO: If adding shields, use IShield to assist with separation between armor and shields
            deffenseMessage.SizeModifier = 0; // "medium"  http://www.dandwiki.com/wiki/SRD:Attack_Roll

            return deffenseMessage;
        }                   // ICombatant

        public void TakeDamage(CombatManager.DamageResult results)
        {
            // If you're hit
            if (results.AttackResult.Critical || results.AttackResult.Hit)
            {
                int hitsTaken = 0;
                int poisonDamage = 0;
                bool energyDrained = false;
                // Sort out the damage types
                foreach (var damagePair in results.DamageDealt)
                {
                    switch (damagePair.Key)
                    {
                        case CombatManager.DamageType.Slashing:
                        case CombatManager.DamageType.Bludgeoning:
                        case CombatManager.DamageType.Piercing:
                            hitsTaken += damagePair.Value;
                            break;
                        case CombatManager.DamageType.Rust:
                            // TODO: Implement damage to armor in Inventory class
                            break;
                        case CombatManager.DamageType.Poison:
                            poisonDamage += damagePair.Value;
                            // TODO: Research actual poison damage handling, but for now just set Poisoned
                            // For now, TakePoisonDmg randomly causes HP damage while poisoned and healing is stopped.
                            InventoryEffectManager.HeroPoisoned = true;
                            break;
                        case CombatManager.DamageType.Energy:
                            energy -= damagePair.Value;
                            energyDrained = true;
                            break;
                        default:
                            break;
                    }
                }

                // If you died from regular damage
                if (!TakeHit(hitsTaken))
                {
                    deathMessage = "The " + results.Attacker.EntityName + " has killed you.";
                    DungeonGameEngine.ThisGame.GameOver("killed by a " + results.Attacker.EntityName);
                }

                // If your energy was drained...
                if (energyDrained)
                {
                    // And you survived the drainage...
                    if (SurvivesEnergyChange())
                        DungeonGameEngine.ProcessMessageQueue(false,
                        "You feel a sudden loss of energy.");
                    else
                    { // or if you die from the drainage
                        CombatManager.AppendDeathMessage(
                        "You had all your remaining energy sucked right out of you.");
                        DungeonGameEngine.ThisGame.GameOver("drained to death");
                    }
                }
            }
        }      // ICombatant

        public void EarnXP(int XP)
        {
            xp += XP;
            // PLANNING: Scroll Idea: FastLearner - Doubles XP gained from every kill.

            if (xp >= XpToNext) GainXPLevel();
        }                                      // ICombatant

        private void GainXPLevel()
        {
            xpLevel++;

            // http://www.dandwiki.com/wiki/5e_SRD:Gaining_a_Level
            abilityScores.Str += 1; if (abilityScores.Str > 20) abilityScores.Str = 20;
            abilityScores.Dex += 1; if (abilityScores.Dex > 20) abilityScores.Dex = 20;
            abilityScores.Con += 1; if (abilityScores.Con > 20) abilityScores.Con = 20;
            abilityScores.Int += 1; if (abilityScores.Int > 20) abilityScores.Int = 20;
            abilityScores.Wis += 1; if (abilityScores.Wis > 20) abilityScores.Wis = 20;
            abilityScores.Cha += 1; if (abilityScores.Cha > 20) abilityScores.Cha = 20;
            abilityScores.StrMod = (int)Math.Floor((double)((abilityScores.Str - 10)/2));
            abilityScores.DexMod = (int)Math.Floor((double)((abilityScores.Dex - 10) / 2));
            abilityScores.ConMod = (int)Math.Floor((double)((abilityScores.Con - 10) / 2));
            abilityScores.IntMod = (int)Math.Floor((double)((abilityScores.Int - 10) / 2));
            abilityScores.WisMod = (int)Math.Floor((double)((abilityScores.Wis - 10) / 2));
            abilityScores.ChaMod = (int)Math.Floor((double)((abilityScores.Cha - 10) / 2));
            // Assuming human fighter class with hit die of 1d10
            int hpIncrese = Utility.Rand.Next(hitDie) + 1 + abilityScores.ConMod;
            max_HP += hpIncrese;
            hp += hpIncrese; // So you don't have to heal to catch back up to your new max, but preserves existing damage
        }

        private static int CalculateBaseCarryWeight(int strLevel)
        {
            // Table from http://www.dandwiki.com/wiki/SRD:Carrying_Capacity using Medium load high value
            int[] carryChart = new int[] {6, 13, 20, 26, 33, 40, 46, 53, 60, 66, 76, 86, 100,
                116, 133, 153, 173, 200, 233, 266, 306, 346, 400, 466, 533, 613, 693, 800, 933};
            if (strLevel < 1) return 0;
            else if (strLevel < 30) return carryChart[strLevel - 1];
            else return CalculateBaseCarryWeight(strLevel - 10) * 4;
        }
        #endregion Methods
    }
}
