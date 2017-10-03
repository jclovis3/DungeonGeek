namespace DungeonGeek
{
    internal interface ICombatant
    {


        int Max_HP { get; }    // Used to see if you get knocked unconcious
        string EntityName { get; } // for messages like "the rat hit you" or "the rat scored a critical hit on you."


        /// <summary>
        /// Constructs a CombatManager.AttackDelivery package and sends it to the CombatManager.
        /// </summary>
        void Attack(ICombatant target);

        /// <summary>
        /// CombatManager then orders the intended target to Defend, which results in a DefenseDelivery
        /// package being returned. Combat manager sends a new AttackResult package to the attacker.
        /// </summary>
        /// <returns></returns>
        CombatManager.DefenseDelivery Defend();

        /// <summary>
        /// CombatManager then calls to the target to take damage and process the results from the combat.
        /// </summary>
        /// <param name="result"></param>
        void TakeDamage(CombatManager.DamageResult results);

        /// <summary>
        /// If the defendant dies, it can pass a call to the attacker to earn XP
        /// </summary>
        /// <param name="XP"></param>
        void EarnXP(int XP);

    }
}
