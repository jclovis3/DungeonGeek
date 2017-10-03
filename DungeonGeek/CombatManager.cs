using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGeek
{
    static class CombatManager
    {
        #region Enums and Structs

        // Enums added to this class because Interfaces cannot have Enums (they ought to though)

        

        internal enum DamageType
        {
            Slashing,           // http://www.dandwiki.com/wiki/SRD:Slashing_Weapon
            Bludgeoning,        // http://www.dandwiki.com/wiki/SRD:Bludgeoning_Weapon
            Piercing,           // http://www.dandwiki.com/wiki/SRD:Piercing_Weapon
            Rust,               // Reduces armor rating
            Poison,             // To cause target to be poisoned for a period of time
            Energy              // Blood sucking vampires and the like will drain you of energy
        }           
             

        internal enum CombatType { Melee, Ranged }

        internal enum DieType { D2=2, D3=3, D4=4, D6=6, D8=8, D10=10, D00=0, D12=12, D20=20, D100=100}
        // D00 is the 10-percentile die
        // https://easyrollerdice.com/blogs/rpg/dd-dice
        // For example, roll 1D00 & 1D10 to roll percent
        // Where percent = 1D00 value * 10 + 1D10 value



        public struct AbilityScores
        {
            public int Str;    // Strength
            public int Dex;    // Dexterity
            public int Con;    // Constitution
            public int Int;    // Inteligence
            public int Wis;    // Wisdom
            public int Cha;    // Charisma
            public int StrMod;
            public int DexMod;
            public int ConMod;
            public int IntMod;
            public int WisMod;
            public int ChaMod;
        }


        internal struct AttackDelivery
        {
            internal int ToHitModifier;

            // To support multiple types of damage, and damage requiring multiple dice, such as Fire
            // damage using 2d4 in addition to normal damage of 3d6, add 5 values to the array.
            internal KeyValuePair<DamageType,DieType>[] DamageDice;
            internal Dictionary<DamageType, int> DamageModifiers;
            internal CombatType CombatType;
            internal ICombatant Target;
            internal ICombatant Attacker;
        }

        internal struct DefenseDelivery
        {
            // AC = 10 + armor bonus + shield bonus + Dexterity modifier + size modifier
            // http://www.dandwiki.com/wiki/SRD:Armor_Class
            internal int ArmorBonus;
            internal int ShieldBonus;
            internal int DexterityModifier;
            internal int SizeModifier; // http://dungeons.wikia.com/wiki/DnDWiki:Size_modifier
                                       // http://www.dandwiki.com/wiki/SRD:Attack_Roll
        }

        internal struct AttackResult
        {
            internal bool Hit;
            internal bool Critical;     // Rolled max value
            internal bool Fumble;       // Rolled min value
        }

        internal struct DamageResult
        {
            internal AttackResult AttackResult;
            internal KeyValuePair<DamageType, int>[] DamageDealt;
            internal bool KnockedUnconcious;
            internal ICombatant Attacker;
        }

        #endregion

        static private List<string> deathMessages = new List<string>();


        #region Internal Methods
        static internal void AppendDeathMessage(string message)
        {
            deathMessages.Add(message);
        }

        static internal void PlayDeathMessages()
        {
            foreach (var msg in deathMessages)
                DungeonGameEngine.ProcessMessageQueue(false, msg);
            deathMessages.Clear();
        }

        static internal AttackResult DeliverAttack(AttackDelivery attackMessage)
        {
            // Check for valid message
            if (attackMessage.DamageDice == null ||
                attackMessage.Target == null ||
                attackMessage.Attacker == null)
                throw new ArgumentNullException("AttackDelivery message missing required information"); 


            ICombatant target = attackMessage.Target;
            DefenseDelivery defense = target.Defend();

            // Compute attack result data
            // AC = 10 + armor bonus + shield bonus + Dexterity modifier + size modifier
            // http://www.dandwiki.com/wiki/SRD:Armor_Class
            int AC = 10 + defense.ArmorBonus + defense.ShieldBonus + defense.DexterityModifier + defense.SizeModifier;
            AttackResult attackResult = TryToHit(AC, attackMessage.ToHitModifier);
            DamageResult damageResult;
            if (attackResult.Hit || attackResult.Critical)
                damageResult = RollDamage(attackMessage.DamageDice, attackMessage.DamageModifiers, attackResult.Critical);
            else
            {
                damageResult = new DamageResult();
                damageResult.DamageDealt = new KeyValuePair<DamageType, int>[0];
            }
            damageResult.AttackResult = attackResult;
            damageResult.Attacker = attackMessage.Attacker;
            
            
            // Determine if blow was strong enough to knock target unconcious (for now, more than half their max HP)
            if (attackMessage.CombatType == CombatType.Melee)
                foreach(var damagePair in damageResult.DamageDealt)
                    if(damagePair.Key == DamageType.Bludgeoning &&
                        damagePair.Value > target.Max_HP / 2)
                    {
                        // For now, you get a 25% chance of being knocked out if damage exceeds half your max.
                        // TODO: Research better calculation for KO savings throw.
                        damageResult.KnockedUnconcious = (Utility.Rand.Next(4) == 0);
                        break;
                    }

            
            
            target.TakeDamage(damageResult);

            return attackResult;
        }

        static internal AttackResult TryToHit(int vsArmorClass, int toHitModifier = 0)
        {

            /*
                AC deflects chance to hit, not the amount of damage. AC base 10 is without armor.
                Role 1D20 + modifiers to hit, then compare this with AC. To hit, roll must be >= AC.
                See http://www.dandwiki.com/wiki/SRD:Armor_Class

                Also, for combat and hit point adjustment, see
                http://www.dandwiki.com/wiki/SRD:Attack_Roll

             */

            AttackResult thisResult = new AttackResult();
            
            int roll = Utility.Rand.Next(20) + 1;
            thisResult.Critical = (roll == 20);
            thisResult.Fumble = (roll == 1);
            if (thisResult.Critical) thisResult.Hit = true;
            else if (thisResult.Fumble) thisResult.Hit = false;
            else thisResult.Hit = roll + toHitModifier >= vsArmorClass;
            return thisResult;
        }

        static internal DamageResult RollDamage(KeyValuePair<DamageType, DieType>[] DamageDice, Dictionary<DamageType, int> DamageModifiers, bool Critical)
        {
            // If Critical, all rolls happen twice
            List<KeyValuePair<DamageType, int>> unconsolidatedDamageResults = new List<KeyValuePair<DamageType, int>>();

            // Use a dictionary to compile damage type results
            Dictionary<DamageType, int> damageResults = new Dictionary<DamageType, int>();
               int rollCount = Critical ? 2 : 1;
            for (int round = 0; round < rollCount; round++)
            {
                foreach(var typeDiePair in DamageDice)
                {
                    int damage = 0;
                    var damageType = typeDiePair.Key;
                    var die = typeDiePair.Value;
                    if (die > 0)
                        damage += Utility.Rand.Next((int)die) + 1;
                    else
                        damage += 10 * Utility.Rand.Next((int)die); // D00 having range 00 to 90 with D10(1-10) = 1 to 100
                    
                    if (damageResults.ContainsKey(damageType))
                        damageResults[damageType] += damage;
                    else
                        damageResults.Add(damageType, damage);
                }
            }

            // Add modifiers
            foreach(var resultPair in DamageModifiers)
            {
                var damageType = resultPair.Key;
                if (damageResults.ContainsKey(damageType))
                    damageResults[damageType] += DamageModifiers[damageType];
                else
                    damageResults.Add(damageType, DamageModifiers[damageType]);
            }

            DamageResult result = new DamageResult();
            result.DamageDealt = damageResults.ToArray();
            return result;

        }


        #endregion
    }
}
