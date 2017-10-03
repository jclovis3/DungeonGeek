using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace DungeonGeek
{
    static class Inventory
    {
        enum SelectionActions { None, Identify, RemoveCurse}
        // TODO: Redesign this class to use the new GameWindow class for content display. This will require changing
        // how items are selected and displayed first.
        #region Fields
        private static GameWindow window = new GameWindow();
        private static SortedDictionary<string, GameText> inventoryList = new SortedDictionary<string, GameText>(); // sorting string, text to view
        private static Dictionary<int,InventoryItem> inventoryContents = new Dictionary<int, InventoryItem>(); // item's UniqueID, Item
        private static Dictionary<string, string> inputInstructions; // key pressed, what it does
        private static float weight;
        //private static Viewport viewPort;
        //private static Texture2D pixel;
        private static GraphicsDevice graphicsDevice;

        // These are an attempt to prevent creating new objects with every draw cycle
        //private static GameText currentText;
        //private static GameText headerText;
        //private static GameText instructionsText;
        private static InventoryItem currentItem;
        //private static List<GameText> listTextObjects;
        private static StringBuilder modifiedText = new StringBuilder();
        private static StringBuilder instructions = new StringBuilder();

        //private static Color selectedFontColor = Color.GreenYellow;
        //private static Color normalFontColor = Color.White;
        //private static Color instructionFontColor = Color.Gold;
        //private static Color headerFontColor = Color.MintCream;
        private static int selectedIndex;
        //private static int firstIndexToShowOnScreen; // Which item should be the first listed to fit a range of items in the window
        private static SelectionActions currentSelectionAction; // Used when an action requires you to select an item after the action is triggered
        private static List<InventoryItem> dropList = new List<InventoryItem>();
        private static List<InventoryItem> nothingToDrop = new List<InventoryItem>();
        //private static int viewableListItems;
        private static Dictionary<GameConstants.ItemClasses, Texture2D> inventorySprites;
        private static bool usingRenameItemScreen = false;
        private static bool closingWindow = false;
        private static string response;
        private static int elapsedTime = 0;
        private static int currentDelay = 0;
        private static bool firstLoop = true;
        private static Keys lastKey = Keys.None;
        private static bool resetInputWindow = false;
        private static string searchString;


        #endregion



        #region Properties

        internal static float Weight
        {
            get { return weight; }
        }

        internal static Dictionary<GameConstants.ItemClasses, Texture2D> InventorySprites
        {
            get { return inventorySprites; }
        }

        internal static Weapon CurrentWeapon
        {
            get
            {
                foreach (var kvPair in inventoryContents)
                    if (kvPair.Value is Weapon && (kvPair.Value as Weapon).Wielded)
                        return kvPair.Value as Weapon;
                return null;
            }
        }

        #endregion



        #region Methods

        /// <summary>
        /// Loads each InventoryItem sprite into a list for reuse in the Draw method.
        /// </summary>
        /// <param name="contentManager">Content Manager passed from the Game class</param>
        internal static void LoadContent(ContentManager contentManager)
        {
            inventorySprites = new Dictionary<GameConstants.ItemClasses, Texture2D>
            {
                {
                    GameConstants.ItemClasses.MagicalRing,
                    contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_MAGICAL + "ring_30x25")
                },
                {
                    GameConstants.ItemClasses.Scroll,
                    contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_MAGICAL + "scroll_31x31")
                },
                {
                    GameConstants.ItemClasses.Food,
                    contentManager.Load<Texture2D>(GameConstants.SPRITE_PATH_MISC + "Food_35x20")
                }
            };
            Armor.LoadContent(contentManager);
            Weapon.LoadContent(contentManager);
        }

        /// <summary>
        /// Creates objects for reuse within the Draw or Input loops which the game engine calls
        /// at an estimate rated of 60 fps. These objects should not be created new with each loop.
        /// Also sets instructions for window use regardless of inventory quantity.
        /// </summary>
        /// <param name="gd"></param>
        internal static void Initialize(GraphicsDevice gd)
        {
            inputInstructions = new Dictionary<string, string>
            {
                { "Up/Down", "Change selection" },
                { "Esc", "Exit" },
                { "Del", string.Empty },
                { "Enter", string.Empty },
                { "R", string.Empty }
            };
            graphicsDevice = gd;
            string header = "Inventory:";
            RebuildInstructions(); // Formats instructions string based on content of inputInstructions
            window.Initialize(gd, inventoryList, header, instructions.ToString());
            
        }

        internal static void InitNewGame()
        {
            inventoryList.Clear();
            inventoryContents.Clear();
            selectedIndex = -1;
            window.InitNewGame();
            //firstIndexToShowOnScreen = 0;
            currentSelectionAction = SelectionActions.None;
            weight = 0;
        }

        /// <summary>
        /// Draws the Inventory screen and any text to be displayed on it using a black canvas inside
        /// a boarder which runs around the edges of the frame.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch object doing the drawing from a Game class</param>
        /// <param name="viewPortBounds">Area which contains the fram and all text.</param>
        internal static void Draw(SpriteBatch spriteBatch, Rectangle viewPortBounds)
        {
            if (firstLoop) UpdateCurrentItem();
            RebuildInstructions();
            window.Instructions = instructions.ToString();
            window.Draw(spriteBatch, viewPortBounds);
        }

        private static void RebuildInstructions()
        {
            // Rebuild Instruction line
            instructions.Clear();
            for (int i = 0; i < inputInstructions.Count; i++)
                if (inputInstructions.ElementAt(i).Value != string.Empty)
                {
                    if (instructions.Length > 0)
                        instructions.Append("   ");
                    instructions.Append(inputInstructions.ElementAt(i).Key + " - " + inputInstructions.ElementAt(i).Value);
                }
            
        }
        
        /// <summary>
        /// Uses the searchValue to locate an InventoryItem from within the
        /// inventoryContents Dictionary
        /// </summary>
        /// <param name="searchValue">Key used in the Dictionary</param>
        /// <returns>The item if found, otherwise null</returns>
        private static InventoryItem GetItem(string searchValue)
        {
            foreach (var item in inventoryContents)
                if (item.Value.SortingValue == searchValue)
                    return item.Value;

            return null;
        }

        /// <summary>
        /// Used by the status text to show the effective armor rating after adding it up for
        /// all equipped items in the inventory. Applies effect modifiers as well.
        /// </summary>
        /// <returns></returns>
        internal static int TotalArmorModifiers()
        {
            int modifier = 0;
            foreach (var kvPair in inventoryContents)
            {
                var item = kvPair.Value;
                if (item is Armor && (item as Armor).Equipped)
                    modifier += (item as Armor).ArmorBonus;
            }
            return modifier; // TODO: Add any effect modifiers when they exist (such as resistance to rust).
        }

        

        /// <summary>
        /// Takes player keyboard input and either updates selection change or activates an action on
        /// the selected item. If the rename window is open, control is passed over and directed to
        /// that window. Handles timing for when player holds a key while using Inventory window.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="gameTime"></param>
        /// <param name="dropped"></param>
        /// <param name="showRenameItemScreen"></param>
        /// <returns></returns>
        internal static bool ProcessPlayerInputs(Keys key, GameTime gameTime,
            out List<InventoryItem> dropped, out bool showRenameItemScreen)
        {
            dropped = nothingToDrop; // Prevents creating a new list with every loop
            showRenameItemScreen = usingRenameItemScreen;
            elapsedTime += gameTime.ElapsedGameTime.Milliseconds;

            if (firstLoop) UpdateCurrentItem();
            if (firstLoop && key != Keys.None) return false; // Forces keys to be released upon entry of first loop
            firstLoop = false;

            

            // If no key pressed, or input passes directly to the RenameScreen, there should be no more delay in this class
            if (key == Keys.None || key != lastKey || usingRenameItemScreen) currentDelay = GameConstants.NO_DELAY;


            #region Using Rename Item Screen
            if (usingRenameItemScreen)
            {
                Debug.Assert(currentItem != null);
                string reservedText = (GetItem(inventoryList.ElementAt(selectedIndex).Key) as INamable).ReservedTextForTitle;
                
                if (RenameItemScreen.ProcessPlayerInputs(key, gameTime, resetInputWindow, reservedText, out response))
                {
                    // Screen closed
                    usingRenameItemScreen = false;
                    firstLoop = true;
                }

                resetInputWindow = false;
                if (response != string.Empty)
                    Rename(currentItem,true);

                lastKey = key;
                return false; // The inventory window stays open

            }
            #endregion

            #region Key press for Inventory Screen
            else if (key != Keys.None && elapsedTime > currentDelay)
            {

                elapsedTime = 0;
                
                // When a key is first pressed, the initial delay should be set
                if (key != lastKey) currentDelay = GameConstants.INITIAL_KEY_DELAY;

                // Once the initial delay is over, if the key is still held down, then repeating is allowed
                else currentDelay = GameConstants.HOLD_KEY_DELAY;

                switch (key)
                {
                    case Keys.Up:
                        selectedIndex--;
                        window.SelectPrev();
                        break;
                    case Keys.Down:
                        selectedIndex++;
                        window.SelectNext();
                        break;
                    case Keys.Enter:
                        if (currentItem != null &&
                            currentSelectionAction == SelectionActions.None)
                        {
                            if (currentItem is IWearable && !(currentItem as IWearable).Equipped)
                                Equip(currentItem);
                            else if (currentItem is Weapon && !(currentItem as Weapon).Wielded)
                                Wield(currentItem);
                            else if (currentItem is IConsumable) Consume(currentItem);
                        }
                        else if (currentSelectionAction == SelectionActions.Identify)
                            IdentifyItem(true);

                        break;
                    case Keys.Delete:
                        if (currentItem != null && currentSelectionAction == SelectionActions.None)
                        {
                            if (currentItem is IWearable && (currentItem as IWearable).Equipped)
                                Unequip(currentItem);
                            else if (currentItem is Weapon && (currentItem as Weapon).Wielded)
                                Unwield(currentItem);
                            else Drop(currentItem);
                        }
                        break;
                    case Keys.R:
                        if (currentItem != null &&
                            currentSelectionAction == SelectionActions.None &&
                            currentItem is INamable)
                            Rename(currentItem);
                        break;
                    case Keys.Escape:
                        if (currentSelectionAction == SelectionActions.Identify)
                        {
                            DungeonGameEngine.ProcessMessageQueue(false, "It is your breath (and not a very good one at that).");
                            currentSelectionAction = SelectionActions.None;
                        }
                        closingWindow = true;
                        break;
                }
                
                UpdateCurrentItem();
                UpdateKeyboardInstructions();
            }
            #endregion

            lastKey = key;
            if(closingWindow)
            {
                dropped = dropList.ToList();
                dropList.Clear();
                firstLoop = true; // Reset for next time window opens
                closingWindow = false;
                return true;
            } else
            return false; // Does not allow inventory menu to exit yet
        }

        /// <summary>
        /// Dynamicaly adjusts keyboard instructions based on the item selected. Does not change base
        /// options to change selection or exit the screen.
        /// </summary>
        private static void UpdateKeyboardInstructions()
        {
            if (currentItem != null)
            {
                // Delete key action
                if (currentSelectionAction == SelectionActions.None)
                {
                    if (currentItem is IWearable && (currentItem as IWearable).Equipped)
                        inputInstructions["Del"] = "Unequip";
                    else if (currentItem.Class == GameConstants.ItemClasses.Weapon &&
                        (currentItem as Weapon).Wielded)
                        inputInstructions["Del"] = "Put away";
                    else
                        inputInstructions["Del"] = "Drop";
                }
                else inputInstructions["Del"] = string.Empty;


                // Enter key action
                if (currentSelectionAction == SelectionActions.None)
                {
                    if (currentItem is IWearable &&
                        !(currentItem as IWearable).Equipped)
                        inputInstructions["Enter"] = "Equip";
                    else if (currentItem.Class == GameConstants.ItemClasses.Food)
                        inputInstructions["Enter"] = "Eat";
                    else if (currentItem.Class == GameConstants.ItemClasses.Scroll)
                        inputInstructions["Enter"] = "Use";
                    else if (currentItem.Class == GameConstants.ItemClasses.Potion)
                        inputInstructions["Enter"] = "Drink";
                    else if (currentItem.Class == GameConstants.ItemClasses.Weapon &&
                        !(currentItem as Weapon).Wielded)
                            inputInstructions["Enter"] = "Wield";
                    else
                        inputInstructions["Enter"] = string.Empty;
                }
                else if (currentSelectionAction == SelectionActions.Identify)
                {
                    inputInstructions["Enter"] = "Identify";

                }
                else inputInstructions["Enter"] = string.Empty;


                // R key action
                if (currentSelectionAction == SelectionActions.None && currentItem is INamable)
                    inputInstructions["R"] = "Rename";
                else
                    inputInstructions["R"] = string.Empty;
            }
            else
            {
                inputInstructions["Del"] = string.Empty;
                inputInstructions["Enter"] = string.Empty;
                inputInstructions["R"] = string.Empty;

            }
            
            
        }

        /// <summary>
        /// Sets the currentItem marker depending on the state of the Inventory List. An empty list
        /// will have the marker set to -1. If the marker is already at -1 when a new item is added,
        /// the marker will be set to index 0 pointing to the new item.
        /// </summary>
        private static void UpdateCurrentItem()
        {
            if (inventoryList.Count > 0)
            {
                selectedIndex = MathHelper.Clamp(selectedIndex, 0, inventoryList.Count - 1);
                window.SelectedIndex = selectedIndex;
                searchString = inventoryList.ElementAt(selectedIndex).Key;
                currentItem = GetItem(searchString);
            }
            else
            {
                selectedIndex = -1;
                window.SelectedIndex = -1;
                currentItem = null;
            }
            UpdateKeyboardInstructions();

        }

        /// <summary>
        /// Adds picked up items to the inventory and stacks any stackable
        /// items if one already exists of the same item class.
        /// </summary>
        /// <param name="item">InventoryItem being picked up or added through any other means.</param>
        internal static void Add(InventoryItem item)
        {
            if (inputInstructions == null)
                throw new InvalidOperationException("Inventory not Initialized.");
            weight += item.InventoryWeight;

            // First detect if the item has been discovered before and update it's name from archives
            if (item is MagicalScroll) MagicalScroll.UpdateDisoveredStatus(item as MagicalScroll);

            bool itemExistsInInventory = false;
            if(item is IStackable && inventoryContents.Count>0)
                // If identical item already in inventory, then add the new item qty to the inventory
                foreach(var kvPair in inventoryContents)
                {
                    var existingStack = kvPair.Value;
                    if(item.IsSimilar(existingStack))
                        {
                            // Preserve objects from Dictionaries before replacing them
                            var listKey = existingStack.SortingValue;
                            var existingGameText = inventoryList[listKey];
                        

                            // Update quantity
                            (existingStack as IStackable).Add((item as IStackable).Qty);

                            // Update GameText to show new quantity
                            existingGameText.Text = existingStack.InventoryTitle;

                            // Remove old item from dictionary and replace with new item (because key changed).
                            inventoryList.Remove(listKey);
                            inventoryList.Add(existingStack.SortingValue, existingGameText);
                            itemExistsInInventory = true;
                            break;
                        }
                }
                    
            if(!itemExistsInInventory)
            {   // Otherwise, add the new item to the inventory instead
                inventoryContents.Add(item.UniqueID, item);
                inventoryList.Add(item.SortingValue, new GameText(item.InventoryTitle, graphicsDevice));
            }
            
            if (inventoryList.Count == 1) UpdateCurrentItem();
        }
        /// <summary>
        /// Overload to Add that allows adding multiple items at once to the inventory.
        /// </summary>
        /// <param name="items">IEnumerable object of InventoryItems to add</param>
        internal static void AddRange(IEnumerable<InventoryItem> items)
        {
            foreach (var item in items)
                Add(item);
        }


        /// <summary>
        /// Drops a single item from the inventory. Stackable items are separated in this way.
        /// dropList will carry these items back to the main game engine where they can be placed
        /// around the hero.
        /// </summary>
        /// <param name="item"></param>
        /// <returns>True if item dropped successfully</returns>
        private static bool Drop(InventoryItem item)
        {
            if (item == null) return false;

            InventoryItem itemRemoved = null;
            bool dropFromList = true;
            if (item is IStackable && (item as IStackable).Qty>1)
            {   // To remove only one item from the stack and drop it on the floor
                // leaving the stack in the inventory with adjusted values

                IStackable itemStack = item as IStackable;
                // Preserve old inventoryList Dictionary values on item
                var oldItemSortingValue = item.SortingValue;
                var oldItemInventoryGameText = inventoryList[oldItemSortingValue];
                
                // Remove old item from inventoryList (not contents though)
                inventoryList.Remove(oldItemSortingValue);

                // Get the consumed item by removing it from the stack
                itemRemoved = (InventoryItem)((item as IStackable).Remove());

                // Update the GameText object using the new InventoryTitle (incorperates qty change)
                oldItemInventoryGameText.Text = item.InventoryTitle;

                // Insert the updated data back into the Dictionary (because the key is different, we couldn't just change it)
                inventoryList.Add(item.SortingValue, oldItemInventoryGameText);

                dropFromList = false;
            }

            if(dropFromList && inventoryContents.Remove(item.UniqueID))
            {
                // Item is last in the stack, or is not stackable so drop it from the list
                inventoryList.Remove(item.SortingValue);
                weight -= item.InventoryWeight;
                dropList.Add(item);
                return true;
            } else if(!dropFromList && itemRemoved != null)
            {
                // Item is not last from stack, but still need to drop an item on the floor
                weight -= itemRemoved.InventoryWeight;
                dropList.Add(itemRemoved);
            }
            else return false;  // Was not able to drop item from list

            UpdateCurrentItem();
            return true; // Drop was successful
        }

        /// <summary>
        /// Separates a consumable from the stack (if stackable) and consumes it triggering its effect.
        /// The weight is adjusted with the loss of the item and the item is removed from the list if it
        /// is the last of its kind in the stack or otherwise not stackable. Consumed items do not get dropped
        /// to the floor.
        /// </summary>
        /// <param name="item">Item to be consumed.</param>
        private static void Consume(InventoryItem item)
        {
            if (item == null || !(item is IConsumable)) return;


            IConsumable consumedItem;
            bool lastOfStack = false;

            

            if (item is IStackable && (item as IStackable).Qty > 1)
            {
                // Preserve old inventoryList Dictionary values on item
                var oldItemSortingValue = item.SortingValue;
                var oldItemInventoryGameText = inventoryList[oldItemSortingValue];

                // Remove old item from inventoryList (not contents though)
                inventoryList.Remove(oldItemSortingValue);

                // Get the consumed item by removing it from the stack
                consumedItem = (IConsumable)((item as IStackable).Remove());

                // Update the GameText object using the new InventoryTitle (incorperates qty change)
                oldItemInventoryGameText.Text = item.InventoryTitle;

                // Insert the updated data back into the Dictionary (because the key is different, we couldn't just change it)
                inventoryList.Add(item.SortingValue, oldItemInventoryGameText);
            }
                
            else
            {
                consumedItem = item as IConsumable;
                lastOfStack = true;
            }


            string listKey = item.SortingValue; // Preserve old key in case using an item renames it.

            // Consume the item and get the string response from it for the message log
            // Note, that in order for remaining items in the stack to get the new name, Consume must be
            // called on the stack, not the item being tossed away.
            string attemptResponse = (item as IConsumable).Consume();
            if (attemptResponse != string.Empty)
                InventoryEffectManager.AddMessageToQueue(attemptResponse);
            if(InventoryEffectManager.HasMessages && currentSelectionAction == SelectionActions.None)
                closingWindow = true;

            // If last in the stack, remove the stack and update inventory weight
            if (lastOfStack && inventoryContents.Remove(item.UniqueID))
            {
                inventoryList.Remove(listKey);
                weight -= item.InventoryWeight;
            } else // otherwise just update the inventory weight and text
            if (!lastOfStack)
            {
                weight -= (consumedItem as InventoryItem).InventoryWeight;
                if (item.SortingValue != listKey)
                {
                    GameText textItem = inventoryList[listKey];
                    textItem.Text = item.InventoryTitle;
                    inventoryList.Remove(listKey);
                    inventoryList.Add(item.SortingValue, textItem);
                }
            }

            else throw new IndexOutOfRangeException("Item to be removed not found in the Inventory's Contents List");
            UpdateCurrentItem();
        }

        /// <summary>
        /// Attempts to put on a wearable InventoryItem if there is space. Does not remove items to do so.
        /// </summary>
        /// <param name="item">Item to put on</param>
        private static void Equip(InventoryItem item)
        {
            if (item == null || !(item is IWearable)) return;
            var wornItem = item as IWearable;
            if (wornItem.Equipped) return;

            // Identify all items of the same class that are equiped. Most classes will have only
            // one item equiped at a time, but rings may have more than one. This list is used to
            // handle removing and replacing one worn item with another.
            List<InventoryItem> replaces = new List<InventoryItem>();
            foreach (var kvPair in inventoryContents) // UniqueID, InventoryItem
            {
                if (kvPair.Value.Class != item.Class) continue;
                // If they are the same class, then assume IWearable
                if ((kvPair.Value as IWearable).Equipped)
                    replaces.Add(kvPair.Value);
            }

            
            string attemptResponse = wornItem.Equip(replaces);
            if (attemptResponse != string.Empty)
            {
                InventoryEffectManager.AddMessageToQueue(attemptResponse);
                closingWindow = true;
            }
            GameText inventoryText = inventoryList[searchString];
            inventoryText.Text = item.InventoryTitle + wornItem.WornOn;
            inventoryList.Remove(searchString);
            inventoryList.Add(item.SortingValue, inventoryText);
        }

        private static void Wield(InventoryItem item)
        {
            if (item == null || !(item is Weapon)) return;
            var chosenWeapon = item as Weapon;
            if (chosenWeapon.Wielded) return;

            Unwield(CurrentWeapon);

            var chosenWeaponGameText = inventoryList[chosenWeapon.SortingValue];
            chosenWeapon.Wielded = true;
            chosenWeaponGameText.Text = chosenWeapon.InventoryTitle + " (wielding)";
        }

        private static void Unwield(InventoryItem item)
        {
            if (item == null || !(item is Weapon)) return;
            var oldWeapon = item as Weapon;
            if (!oldWeapon.Wielded) return;
            var oldWeaponGameText = inventoryList[oldWeapon.SortingValue];
            oldWeapon.Wielded = false;
            oldWeaponGameText.Text = oldWeapon.InventoryTitle;
        }

        

        /// <summary>
        /// Attempts to unequip an item if it is not cursed. Cursed items are blocked from removal.
        /// </summary>
        /// <param name="item">Item to remove from body</param>
        private static void Unequip( InventoryItem item)
        {
            if (item == null || !(item is IWearable)) return;
            var wornItem = item as IWearable;
            if (!wornItem.Equipped) return;

            string attemptResponse = wornItem.Unequip();
            if (attemptResponse != string.Empty)
            {
                InventoryEffectManager.AddMessageToQueue(attemptResponse);
                closingWindow = true;
            }
            inventoryList[searchString].Text = item.InventoryTitle + wornItem.WornOn; // Because removal may fail
        }

        /// <summary>
        /// Allows player to rename some items
        /// </summary>
        /// <param name="item">Item to be renamed</param>
        /// <param name="receivedResponse">Result message returned to the player</param>
        private static void Rename(InventoryItem item ,bool receivedResponse = false)
        {
            if (!receivedResponse)
            {
                usingRenameItemScreen = true;
                resetInputWindow = true;
                RenameItemScreen.Value = string.Empty;
            }
            else
            {
                string itemKey = inventoryList.ElementAt(selectedIndex).Key;
                if(item is INamable)
                {
                    GameText gameText = inventoryList[itemKey];
                    inventoryList.Remove(itemKey);
                    (item as INamable).Rename(response);
                    if(item is IWearable)
                    {
                        gameText.Text = item.InventoryTitle + (item as IWearable).WornOn;
                    } else
                        gameText.Text = item.InventoryTitle;
                    inventoryList.Add(item.SortingValue, gameText);

                }
                usingRenameItemScreen = false;
            }
        }

        internal static void RemoveCurseItem(bool selected = false)
        {
            InventoryEffectManager.AddMessageToQueue("Remove Curse effect not implemented yet.");
        }

        internal static void IdentifyItem(bool selected = false)
        {
            if (!selected)
            {
                DungeonGameEngine.ProcessMessageQueue(false, "What do you want to identify?");
                currentSelectionAction = SelectionActions.Identify;
            }
                
            else
            {
                DungeonGameEngine.ProcessMessageQueue(true); // Acknowledge the "What do you want to identify" prompt

                // Get old text before item gets renamed
                var listKey = currentItem.SortingValue;
                
                // Retrieving this property should cause the item to be identified as well
                DungeonGameEngine.ProcessMessageQueue(false, currentItem.IdentifyText);
                currentSelectionAction = SelectionActions.None;

                // Update inventory list text
                GameText textItem = inventoryList[listKey];
                textItem.Text = currentItem.InventoryTitle;
                inventoryList.Remove(listKey);
                inventoryList.Add(currentItem.SortingValue, textItem);

                closingWindow = true;
            }

        }


        #endregion Methods


    }
}
