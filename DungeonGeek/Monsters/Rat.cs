using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace DungeonGeek
{
    class Rat : Monster
    {
        private const float LOOT_PROBABILITY = 0.20f;
        private const float LOOT_MAX_WEIGHT = 0.5f;
        private CombatManager.AbilityScores abilityScores;
        private static CombatManager.AttackDelivery attackDeliveryPacket = new CombatManager.AttackDelivery();
        private static CombatManager.DefenseDelivery defenseDeliveryPacket = new CombatManager.DefenseDelivery();

        public override string EntityName
        { get { return "rat"; } }

        public override CombatManager.AbilityScores AbilityScores
        { get { return abilityScores; } }
        public override CombatManager.AttackDelivery AttackDeliveryPacket
        { get { return attackDeliveryPacket; } }
        public override CombatManager.DefenseDelivery DeffenseDeliveryPacket
        { get { return defenseDeliveryPacket; } }


        internal Rat(Point location)
        {
            //Stats referenced from: http://www.dandwiki.com/wiki/5e_SRD:Rat

            monsterClass = GameConstants.monsterClasses.Rat;
            this.location = location;

            abilityScores = new CombatManager.AbilityScores();
            abilityScores.Str = 2;
            abilityScores.Dex = 11;
            abilityScores.Con = 9;
            abilityScores.Int = 2;
            abilityScores.Wis = 10;
            abilityScores.Cha = 4;
            abilityScores.StrMod = -4;
            abilityScores.DexMod = 0;
            abilityScores.ConMod = -1;
            abilityScores.IntMod = -4;
            abilityScores.WisMod = 0;
            abilityScores.ChaMod = -3;


            maxHP = MathHelper.Clamp(Utility.Rand.Next(4),1,3); // "Hit Points 1 (1d4 - 1)" meaning, min (max)
            _HP = maxHP;
            _XP = 10; // Challenge rating of 0 with attack : http://www.dandwiki.com/wiki/5e_SRD:Creatures#Size_Categories

            // "Bite. Melee Weapon Attack: +0 to hit, reach 5 ft., one target. Hit: 1 piercing damage."
            attackDeliveryPacket.CombatType = CombatManager.CombatType.Melee;
            attackDeliveryPacket.DamageDice = new KeyValuePair<CombatManager.DamageType, CombatManager.DieType>[0];
            attackDeliveryPacket.DamageModifiers = new Dictionary<CombatManager.DamageType, int>(1); // Set Capacity to 1
            attackDeliveryPacket.DamageModifiers.Add(CombatManager.DamageType.Piercing, 1);  // This one
            attackDeliveryPacket.ToHitModifier = 0;
            attackDeliveryPacket.Attacker = this;

            
            defenseDeliveryPacket.ArmorBonus = 0;
            defenseDeliveryPacket.SizeModifier = 2; //"tiny" http://www.dandwiki.com/wiki/SRD:Attack_Roll
            defenseDeliveryPacket.DexterityModifier = abilityScores.DexMod;
            defenseDeliveryPacket.ShieldBonus = 0;
            



            floorLevelRange.Low = 1;
            floorLevelRange.High = 20;
            healRate = 1;
            if (rand.NextDouble() < 0.33f)
                awake = roaming = false;
            loot = StartingLoot(LOOT_PROBABILITY, LOOT_MAX_WEIGHT);
        }

        public override void TakeDamage(CombatManager.DamageResult results)
        {
            int hitsTaken = 0;
            // A rat has no resistances or weaknesses to any type of damage, so all damage is taken
            // off the rat's HP.
            foreach (var damagePair in results.DamageDealt)
            {
                hitsTaken += damagePair.Value;
            }
            if (hitsTaken > _HP)
                Die(this, results.Attacker);
            else
                _HP -= hitsTaken;
        }


        /// <summary>
        /// Called when hero is within line of sight of a monster to allow monster to either
        /// start chasing the hero or remain asleep.
        /// </summary>
        /// <returns></returns>
        internal override void LetMonsterSeeHero()
        {
            if (awake)
            {
                chasingHero = true;
                roaming = false;
            }
        }
    }
}
