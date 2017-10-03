using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace DungeonGeek
{
    /// <summary>
    /// Provides common functionality used by all sub windows in the game
    /// </summary>
    class GameWindow
    {
        private static GraphicsDevice graphicsDevice;
        private Viewport viewPort;
        private static Texture2D pixel;
        private GameText headerText;
        private GameText currentText;
        private StringBuilder modifiedText = new StringBuilder();
        private SortedDictionary<string, GameText> sortedList;
        private GameText promptText;
        private List<string> bodySource;
        private static List<GameText> bodyText;
        private StringBuilder userResponse;
        private GameText userResponseText;
        private GameText instructionText;
        
        private static Color frameColor = Color.White;
        private static Color headerFontColor = Color.MintCream;
        private static Color screenInstructionFontColor = Color.Gold;
        private static Color normalFontColor = Color.White;
        private static Color selectedFontColor = Color.GreenYellow;
        private static bool initComplete = false;
        private Rectangle underline = new Rectangle();
        private bool selectable = false;
        private int selectedIndex = 0;
        private int viewableListItems;
        private int firstIndexToShowOnScreen; // Which item should be the first listed to fit a range of items in the window


        internal string SelectedText
        {
            get
            {
                if (bodySource == null) return string.Empty;
                else return bodySource[selectedIndex];
            }
        }
        internal string Instructions
        {
            get { return instructionText.Text; }
            set { instructionText.Text = value; }
        }

        internal int SelectedIndex
        {
            get { return selectedIndex; }
            set { selectedIndex = value; }
        }

        internal GameWindow(bool selectableList = false)
        {
            selectable = selectableList;
        }

        private static void Init(GraphicsDevice gd)
        {
            graphicsDevice = gd;
            Color[] colorData = { Color.White };
            pixel = new Texture2D(graphicsDevice, 1, 1);
            pixel.SetData(colorData);
            bodyText = new List<GameText>
            {
                new GameText("XXXXX",gd) // Allows for font size measurements to be taken
            };
            

            initComplete = true;
        }    

        internal void InitNewGame()
        {
            selectedIndex = -1;
            firstIndexToShowOnScreen = 0;
        }
        internal void Initialize(GraphicsDevice gd, List<string> listSource, string header = "", string instructions = "Esc - Exit")
        {
            viewPort = new Viewport();
            if (!initComplete) Init(gd);
            firstIndexToShowOnScreen = 0;
            if (header != "") headerText = new GameText(header, gd);
            instructionText = new GameText(instructions, new Point(), gd);
            bodySource = listSource;
        }

        
        internal void Initialize(GraphicsDevice gd, SortedDictionary<string, GameText> sortedListSource, string header = "", string instructions = "Esc - Exit")
        {
            viewPort = new Viewport();
            if (!initComplete) Init(gd);
            firstIndexToShowOnScreen = 0;
            if (header != "") headerText = new GameText(header, gd);
            instructionText = new GameText(instructions, new Point(), gd);
            sortedList = sortedListSource;
            bodySource = new List<string>();
            selectable = true; // The only class using the SortedDictionary is Inventory and it needs selectable enabled to work
        }

        internal void Initialize(GraphicsDevice gd, string prompt, StringBuilder userInput, string instructions = "Esc - Exit")
        {
            viewPort = new Viewport();
            if (!initComplete) Init(gd);
            firstIndexToShowOnScreen = 0;
            userResponse = userInput;
            userResponseText = new GameText(gd);
            instructionText = new GameText(instructions, new Point(), gd);
            promptText = new GameText(prompt, gd);
        }

        internal void Initialize(GraphicsDevice gd, string prompt, string instructions)
        {
            Initialize(gd, prompt, null, instructions);
        }
        
        internal void Draw(SpriteBatch spriteBatch, Rectangle viewPortBounds)
        {
            Color fontColor;
            viewPort.Bounds = viewPortBounds;
            graphicsDevice.Viewport = viewPort;

            spriteBatch.Begin();

            // Draw black canvas with frame over viewport
            Rectangle frame = new Rectangle(0, 0, viewPortBounds.Width, viewPortBounds.Height);
            Rectangle blackCanvas = new Rectangle(2, 2, frame.Width - 4, frame.Height - 4);
            spriteBatch.Draw(pixel, frame, frameColor);
            spriteBatch.Draw(pixel, blackCanvas, Color.Black);
            int nextTextTop = blackCanvas.Top + GameConstants.TOP_MARGIN;

            // Displays a header if used
            if (headerText != null)
            {
                // Show header text with underline
                headerText.Y = nextTextTop;
                headerText.ForeColor = headerFontColor;
                headerText.Scale = new Vector2(1.5f, 1.25f);
                headerText.X = blackCanvas.Left + GameConstants.HEAD_FOOT_LEFT;
                nextTextTop += (int)(headerText.Height * headerText.Scale.Y);
                headerText.Draw(spriteBatch);

                
                underline.X = headerText.X;
                underline.Y = nextTextTop;
                underline.Width = (int)(headerText.Width * headerText.Scale.X);
                underline.Height = (int)(headerText.Height * GameConstants.UNDERLINE_RATIO);
                spriteBatch.Draw(pixel, underline, headerFontColor);
                nextTextTop += underline.Height + (int)(headerText.Height * 0.25f) + GameConstants.LINE_SPACING;
            }

            // First, decide which list is provided and update bodySource if sorted dictionary
            if(sortedList != null)
            {
                bodySource.Clear();
                foreach (var kvPair in sortedList)
                    bodySource.Add(kvPair.Value.Text);
            }
            
            // Display body text when used (May only be a portion of the full list)
            if (bodySource != null)
            {
                // Count how many items will fit in the current view, leaving room at the bottom for instructions
                viewableListItems = (int)Math.Floor((decimal)(viewPortBounds.Height - nextTextTop) / (bodyText[0].Height + GameConstants.LINE_SPACING)) - 3;

                //If current selection is below range, move range down
                if (firstIndexToShowOnScreen < 0 && bodySource.Count > 0) firstIndexToShowOnScreen = 0;
                while (selectedIndex > firstIndexToShowOnScreen + viewableListItems - 1)
                    firstIndexToShowOnScreen++;
                //and the reverse if it is above the range
                while (selectedIndex < firstIndexToShowOnScreen)
                    firstIndexToShowOnScreen--;



                // Iterate through the list of text to display
                for (int i = firstIndexToShowOnScreen;
                    i > -1 && i < bodySource.Count && i < firstIndexToShowOnScreen + viewableListItems; i++)
                {
                    fontColor = selectedIndex == i && selectable ? selectedFontColor : normalFontColor;
                    if (bodyText.Count < i + 1) bodyText.Add(new GameText(graphicsDevice));
                    currentText = bodyText[i]; // Gives currentText a reference to an existing reusable object
                    FitCurrentTextToWindow(bodyText[i]); // changes currentText.Text
                    currentText = new GameText(bodySource[i], graphicsDevice)
                    {
                        X = GameConstants.LIST_LEFT,
                        Y = nextTextTop
                    };
                    nextTextTop += currentText.Height + GameConstants.LINE_SPACING;
                    currentText.ForeColor = fontColor;
                    currentText.Draw(spriteBatch);
                }
            }

            // Display prompt when used
            if (promptText != null)
            {
                promptText.Y = nextTextTop;
                promptText.ForeColor = normalFontColor;
                promptText.X = blackCanvas.Left + GameConstants.HEAD_FOOT_LEFT;
                nextTextTop += promptText.Height + GameConstants.LINE_SPACING;
                promptText.Draw(spriteBatch);
            }

            // Display user typed text when used
            if(userResponse != null)
            {
                userResponseText.Text = userResponse.ToString().Length == 0 ? " " : userResponse.ToString();
                userResponseText.ForeColor = normalFontColor;
                userResponseText.X = blackCanvas.X + blackCanvas.Width / 2 - userResponseText.Width / 2;
                userResponseText.Y = nextTextTop;
                nextTextTop += userResponseText.Height + GameConstants.LINE_SPACING;
                userResponseText.Draw(spriteBatch);

                underline.Y = nextTextTop;
                int minWidth = 100;
                underline.Width = MathHelper.Max(minWidth,(int)(userResponseText.Width * userResponseText.Scale.X));
                underline.Height = (int)(userResponseText.Height * GameConstants.UNDERLINE_RATIO);
                underline.X = blackCanvas.X + blackCanvas.Width / 2 - underline.Width / 2;
                spriteBatch.Draw(pixel, underline, userResponseText.ForeColor);
                nextTextTop += underline.Height + (int)(userResponseText.Height * 0.25f) + GameConstants.LINE_SPACING;
            }

            // Display current screen instructions
            instructionText.Y = (blackCanvas.Top + blackCanvas.Height - instructionText.Height - GameConstants.LINE_SPACING);
            instructionText.X = blackCanvas.Left + GameConstants.HEAD_FOOT_LEFT;
            instructionText.ForeColor = screenInstructionFontColor;
            instructionText.Draw(spriteBatch);

            spriteBatch.End();
        }

        internal void SelectNext()
        {
            if (bodySource != null && ++selectedIndex > bodySource.Count - 1) selectedIndex = 0;
        }

        internal void SelectPrev()
        {
            if (bodySource != null && --selectedIndex < 0) selectedIndex = bodySource.Count-1;
        }

        /// <summary>
        /// Reduces length of text to fit within window. This should only be necessary for magical
        /// names produced in excess of the limits as user name changes will be guarded against over
        /// run.
        /// </summary>
        /// <param name="textToFit"></param>
        private void FitCurrentTextToWindow(GameText textToFit)
        {
            modifiedText.Clear();
            modifiedText.Append(textToFit.Text);
            currentText.Text = modifiedText.ToString();

            // If text is too wide to fit window, append elipses and keep reducing it
            // until it fits
            if (currentText.Width > GameConstants.INV_MAX_TITLE_WIDTH)
            {
                modifiedText.Append("...");
                currentText.Text = modifiedText.ToString();

                while (currentText.Width > GameConstants.INV_MAX_TITLE_WIDTH)
                {
                    modifiedText.Remove(modifiedText.Length - 4, 1);
                    currentText.Text = modifiedText.ToString();
                }
            }
        }
    }
}
