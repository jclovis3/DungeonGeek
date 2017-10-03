using System;
using System.Collections.Generic;
using System.Linq;

namespace DungeonGeek
{
    /// <summary>
    /// Handles calculation of hero stat modifications and other affects related to equiped inventory
    /// and temporary effects from scrolls, potions, monster attacks, traps, etc.
    /// </summary>
    static class InventoryEffectManager
    {


        // TODO: After adding monsters, implement SensesMonster.


        #region fields
        private static List<string> outputMessageQueue = new List<string>();
        private static Random rand = new Random();

        // Hero state
        private static bool feelsFaint ; // Hero can loose consciousness for a time
        
        // time each effect lasts (-1 is indefinate until something else changes)
        private static Dictionary<GameConstants.TemporaryEffects, int> heroCurrentEffects;

        // Statements used when status of effect changes (loaded in constructor)
        private static Dictionary<GameConstants.TemporaryEffects, string> affectedStatement;
        private static Dictionary<GameConstants.TemporaryEffects, string> unaffectedStatement;

        

        #endregion



        #region Properties

        internal static Random RandomNumberGenerator
        {
            get { return rand; }
        }    // Read only

        internal static bool HasMessages
        {
            get { return outputMessageQueue.Count > 0; }
        }
        internal static List<string> ImportMessageQueue
        { // Passes the message queue up and clears it locally
            get
            {
                List<string> sendQueue = new List<string>(outputMessageQueue);
                outputMessageQueue.Clear();
                return sendQueue;
            }
        } // Read with clear only
        internal static bool FeelsFaint
        { // Used when hero is low on calories and needs food
            get { return feelsFaint; }
            set { feelsFaint = value; }
        }                 // Read/Write
        internal static bool HeroHastened
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.Hastened] != 0; }
            set
            {
                if (value && heroCurrentEffects[GameConstants.TemporaryEffects.Hastened] > -1)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Hastened] += rand.Next(GameConstants.LONGER_EFFECT_MIN, GameConstants.LONGER_EFFECT_MAX + 1);
                else if (!value && heroCurrentEffects[GameConstants.TemporaryEffects.Hastened] != 0)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Hastened] = 0;
                SendEffectMessage(GameConstants.TemporaryEffects.Hastened, value);

            }
        }               // Read/Write
        internal static bool HeroSlowed
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.Slowed] != 0 || HeroOverburdened; }
            set
            {
                if (value && heroCurrentEffects[GameConstants.TemporaryEffects.Slowed] > -1)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Slowed] += rand.Next(GameConstants.LONGER_EFFECT_MIN, GameConstants.LONGER_EFFECT_MAX + 1);
                else if (!value && heroCurrentEffects[GameConstants.TemporaryEffects.Slowed] != 0)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Slowed] = 0;
                SendEffectMessage(GameConstants.TemporaryEffects.Slowed, value);
            }
        }                 // Read/Write
        internal static bool HeroBlind
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.Blind] != 0; }
            set
            {
                if (value && heroCurrentEffects[GameConstants.TemporaryEffects.Blind] > -1)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Blind] += rand.Next(GameConstants.LONGER_EFFECT_MIN, GameConstants.LONGER_EFFECT_MAX + 1);
                else if (!value && heroCurrentEffects[GameConstants.TemporaryEffects.Blind] != 0)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Blind] = 0;
                SendEffectMessage(GameConstants.TemporaryEffects.Blind, value);


            }
        }                  // Read/Write
        internal static bool HeroConfused
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.Confused] != 0; }
            set
            {
                if (value && heroCurrentEffects[GameConstants.TemporaryEffects.Confused] > -1)
                {
                    heroCurrentEffects[GameConstants.TemporaryEffects.Confused] += rand.Next(GameConstants.SHORTER_EFFECT_MIN, GameConstants.SHORTER_EFFECT_MAX + 1);
                    SendEffectMessage(GameConstants.TemporaryEffects.Confused, value);
                }
                else if (!value && heroCurrentEffects[GameConstants.TemporaryEffects.Confused] != 0)
                {
                    heroCurrentEffects[GameConstants.TemporaryEffects.Confused] = 0;
                    SendEffectMessage(GameConstants.TemporaryEffects.Confused, value);
                }
                // else hero was already confused or already not confused, and don't send a message
                
            }
        }               // Read/Write
        internal static bool HeroOverburdened
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.Overburdened] != 0; }
            set
            {
                if (value) heroCurrentEffects[GameConstants.TemporaryEffects.Overburdened] = -1;
                else heroCurrentEffects[GameConstants.TemporaryEffects.Overburdened] = 0;
                SendEffectMessage(GameConstants.TemporaryEffects.Overburdened, value);
            }
        }           // Read/Write
        internal static bool HeroPoisoned
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.Poisoned] != 0; }
            set
            {
                if (value && heroCurrentEffects[GameConstants.TemporaryEffects.Poisoned] > -1)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Poisoned] += rand.Next(GameConstants.SHORTER_EFFECT_MIN, GameConstants.SHORTER_EFFECT_MAX + 1);
                else if (!value && heroCurrentEffects[GameConstants.TemporaryEffects.Poisoned] != 0)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Poisoned] = 0;
                SendEffectMessage(GameConstants.TemporaryEffects.Poisoned, value);
            }
        }               // Read/Write
        internal static bool HeroSeesInDark
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.ImprovedNightSight] != 0; }
            set
            {
                if (value && heroCurrentEffects[GameConstants.TemporaryEffects.ImprovedNightSight] > -1)
                    heroCurrentEffects[GameConstants.TemporaryEffects.ImprovedNightSight] += rand.Next(GameConstants.LONGER_EFFECT_MIN, GameConstants.LONGER_EFFECT_MAX + 1);
                else if (!value && heroCurrentEffects[GameConstants.TemporaryEffects.ImprovedNightSight] != 0)
                    heroCurrentEffects[GameConstants.TemporaryEffects.ImprovedNightSight] = 0;
                SendEffectMessage(GameConstants.TemporaryEffects.ImprovedNightSight, value);
            }
        }            // Read/Write
        internal static bool HeroDetectsMonsters
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.SensesMonster] != 0; }
            set
            {
                if (value && heroCurrentEffects[GameConstants.TemporaryEffects.SensesMonster] > -1)
                    heroCurrentEffects[GameConstants.TemporaryEffects.SensesMonster] += rand.Next(GameConstants.LONGER_EFFECT_MIN, GameConstants.LONGER_EFFECT_MAX + 1);
                else if (!value && heroCurrentEffects[GameConstants.TemporaryEffects.SensesMonster] != 0)
                    heroCurrentEffects[GameConstants.TemporaryEffects.SensesMonster] = 0;
                SendEffectMessage(GameConstants.TemporaryEffects.SensesMonster, value);
            }
        }        // Read/Write
        internal static bool HeroStuck
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.Stuck] != 0; }
            set
            {
                if (value && heroCurrentEffects[GameConstants.TemporaryEffects.Stuck] > -1)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Stuck] += rand.Next(GameConstants.SHORTER_EFFECT_MIN, GameConstants.SHORTER_EFFECT_MAX + 1);
                else if (!value && heroCurrentEffects[GameConstants.TemporaryEffects.Stuck] != 0)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Stuck] = 0;
                SendEffectMessage(GameConstants.TemporaryEffects.Stuck, value);
            }
        }                  // Read/Write
        internal static bool HeroConcious
        { // Used to determine if the world takes moves without the player
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.Fainted] == 0; }
        }               // Read only
        internal static bool HeroObservant
        {
            get { return heroCurrentEffects[GameConstants.TemporaryEffects.Observant] != 0; }
            set
            {
                if (value && heroCurrentEffects[GameConstants.TemporaryEffects.Observant] > -1)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Observant] += rand.Next(GameConstants.LONGER_EFFECT_MIN, GameConstants.LONGER_EFFECT_MAX + 1);
                else if (!value && heroCurrentEffects[GameConstants.TemporaryEffects.Observant] != 0)
                    heroCurrentEffects[GameConstants.TemporaryEffects.Observant] = 0;
                SendEffectMessage(GameConstants.TemporaryEffects.Observant, value);
            }
        }              // Read/Write

        #endregion



        #region Constructor
        static InventoryEffectManager()
        {
            // Load effects dictionaries
            affectedStatement = new Dictionary<GameConstants.TemporaryEffects, string>();
            unaffectedStatement = new Dictionary<GameConstants.TemporaryEffects, string>();
            foreach (var effect in Enum.GetValues(typeof(GameConstants.TemporaryEffects)).Cast<GameConstants.TemporaryEffects>())
            {
                
                switch (effect)
                {
                    case GameConstants.TemporaryEffects.Hastened:
                        {
                            affectedStatement.Add(effect, "You feel quick on your feet.");
                            unaffectedStatement.Add(effect, "Your feet don't feel so quick anymore.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.Slowed:
                        {
                            affectedStatement.Add(effect, "Why is everything moving so fast.");
                            unaffectedStatement.Add(effect, "The world slows down again.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.Blind:
                        {
                            affectedStatement.Add(effect, "You have gone blind.");
                            unaffectedStatement.Add(effect, "\"I see,\" said the blind man.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.Confused:
                        {
                            affectedStatement.Add(effect, "You can't seem to tell your left from your right anymore.");
                            unaffectedStatement.Add(effect, "You feel less confused now.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.Overburdened:
                        {
                            affectedStatement.Add(effect, "You are slowed by your weight. Energy is consumed twice as fast.");
                            unaffectedStatement.Add(effect, "Your knees are thanking you again.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.Poisoned:
                        {
                            affectedStatement.Add(effect, "The poison will sap at your health if you don't find a cure.");
                            unaffectedStatement.Add(effect, "You can heal naturally again.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.ImprovedNightSight:
                        {
                            affectedStatement.Add(effect, "The world seems a brighter place for a moment.");
                            unaffectedStatement.Add(effect, "Your night blindness has returned.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.SensesMonster:
                        {
                            affectedStatement.Add(effect, "You can smell monsters from a greater distance.");
                            unaffectedStatement.Add(effect, "Your sense of smell is normal again.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.Stuck:
                        {
                            affectedStatement.Add(effect, "You are stuck.");
                            unaffectedStatement.Add(effect, "You can move again.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.Fainted:
                        {
                            affectedStatement.Add(effect, "You have fainted.");
                            unaffectedStatement.Add(effect, "You have awaken.");
                            break;
                        }

                    case GameConstants.TemporaryEffects.Observant:
                        {
                            affectedStatement.Add(effect, "Eyes like an eagle, you notice anything out of place.");
                            unaffectedStatement.Add(effect, "The walls all seem the same again.");
                            break;
                        }
                }
                
            }
        }
        #endregion



        #region Internal Methods

        /// <summary>
        /// Resets values to the state needed for the start of a new game
        /// </summary>
        internal static void InitNewGame()
        {
            // Reset all effect counters
            heroCurrentEffects = new Dictionary<GameConstants.TemporaryEffects, int>();
            foreach (var effect in Enum.GetValues(typeof(GameConstants.TemporaryEffects)).Cast<GameConstants.TemporaryEffects>())

                // Effect timers
                heroCurrentEffects.Add(effect, 0);

            feelsFaint = false;

        }
        /// <summary>
        /// Pulls in required data for scrolls and potions to act on and makes them available
        /// 
        /// </summary>
        /// <param name="hero"></param>
        /// <param name="floorPlan"></param>
        /// <param name="floorRevealed"></param>
        /// <param name="floorWidth"></param>
        /// <param name="floorHeight"></param>
        internal static void AdvanceTurn()
        {

            // For each effect (a Dictionary key/value pair) in heroCurrentEffects
            for(int i=0; i<heroCurrentEffects.Count; i++)
            {
                // If the counter for this effect is > 0
                if (heroCurrentEffects.ElementAt(i).Value>0)
                {
                    // Decrement the counter. Then if it ==0
                    if (--heroCurrentEffects[(GameConstants.TemporaryEffects)i] == 0)
                        // Send the message for the effect expiring
                        SendEffectMessage(heroCurrentEffects.ElementAt(i).Key, false);

                }


            }

            CheckOverburdened();
            CheckHeroFaints();
        }

        internal static void AddMessageToQueue(string newMessage)
        {
            outputMessageQueue.Add(newMessage);
        }



        #endregion



        #region Private Methods

        private static void CheckHeroFaints()
        {
            if (heroCurrentEffects[GameConstants.TemporaryEffects.Fainted] == 0 &&
                feelsFaint && rand.NextDouble() < GameConstants.CHANCE_OF_FAINT)
            {
                outputMessageQueue.Add("You have fainted.");
                heroCurrentEffects[GameConstants.TemporaryEffects.Fainted] =
                    rand.Next(GameConstants.FAINT_TIME_MIN, GameConstants.FAINT_TIME_MAX + 1);
                
            }
        }

        internal static void KnockHeroUnconcious()
        {
            if (heroCurrentEffects[GameConstants.TemporaryEffects.Fainted] == 0)
            {
                DungeonGameEngine.ProcessMessageQueue(false, "You knocked yourself out");
                heroCurrentEffects[GameConstants.TemporaryEffects.Fainted] =
                    rand.Next(GameConstants.FAINT_TIME_MIN, GameConstants.FAINT_TIME_MAX + 1);
            }

        }

        private static void CheckOverburdened()
        {
            if (!HeroOverburdened && Inventory.Weight > DungeonGameEngine.Hero.EffectiveCarryWeight)
                HeroOverburdened = true;
            else if (HeroOverburdened && Inventory.Weight <= DungeonGameEngine.Hero.EffectiveCarryWeight)
                HeroOverburdened = false;
        }

        private static void HeroAwakens()
        {
            outputMessageQueue.Add("You are awake again.");
        }


        private static void SendEffectMessage(GameConstants.TemporaryEffects effect, bool isAffected)
        {
            if (isAffected)
                outputMessageQueue.Add(affectedStatement[effect]);
            else
                outputMessageQueue.Add(unaffectedStatement[effect]);
        }


       


        

        #endregion
    }
}
