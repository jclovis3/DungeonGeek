using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Text;

namespace DungeonGeek
{

    /// <summary>
    /// Handles sprite font loading and string output in XNA/MonoGame.
    /// </summary>
    class GameText
    {
        
        #region Fields
        static private SpriteFont font;
        private string text;
        private string formattedText;
        private Point location;
        private Vector2 scale;
        private int maxWidth = 0;
        private bool transparentBackground = false;
        Color foreColor = Color.White;
        Color backColor = Color.Black;
        Texture2D pixel;

        

        #endregion



        #region Properties

        public string Text
        {
            set { text = value; FitToWidth(); }
            get { return text; }
        }

        public int X
        {
            set { location.X = value; }
            get { return location.X; }
        }

        public int Y
        {
            set { location.Y = value; }
            get { return location.Y; }
        }

        public Point Location
        {
            set { location = value; }
            get { return location; }
        }

        public int Height
        {
            get { return (int)font.MeasureString(formattedText).Y; }
        }

        public int Width
        {
            get { return (int)font.MeasureString(formattedText).X; }
            set
            {
                maxWidth = value > 0 ? value : 0;
                FitToWidth();
            }
        }

        public Color ForeColor
        {
            get { return foreColor; }
            set { foreColor = value; }
        }

        public Color BackColor
        {
            get { return backColor; }
            set { backColor = value; }
        }

        public bool TransparentBackground
        {
            get { return transparentBackground; }
            set { transparentBackground = value; }
        }

        public float Spacing
        {
            get { return font.Spacing; }
            set { font.Spacing = value; }
        }

        public int LineSpacing
        {
            get { return font.LineSpacing; }
            set { font.LineSpacing = value; }
        }

        public Vector2 Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        #endregion



        #region Constructors
        public GameText (string text, Point location, GraphicsDevice graphicsDevice)
        {
            Text = text;
            FitToWidth();
            Location = location;
            Color[] colorData = {Color.White};
            pixel = new Texture2D(graphicsDevice, 1, 1);
            pixel.SetData(colorData);
            scale = new Vector2(1f, 1f);
        }

        public GameText(string text, GraphicsDevice graphicsDevice) : this(text, new Point(), graphicsDevice) { }

        public GameText(GraphicsDevice graphicsDevice) : this(string.Empty, new Point(), graphicsDevice) { }

        #endregion



        #region Methods

        static public void LoadContent(ContentManager content)
        {
            font = content.Load<SpriteFont>(GameConstants.FONT_GAMETEXT);
        }


        /// <summary>
        /// Draws text in active view port. Used during an open SpriteBatch session.
        /// </summary>
        /// <param name="spriteBatch">SpriteBatch to render to</param>
        public void Draw(SpriteBatch spriteBatch)
        {

            // Draw background color first
            if(!transparentBackground)
                spriteBatch.Draw(pixel, new Rectangle(X, Y, Width, Height), backColor);

            // Draw text onto background
            spriteBatch.DrawString(font, formattedText, new Vector2(location.X,location.Y),
                foreColor,0,new Vector2(),scale,SpriteEffects.None,0);
            
        }

        private void FitToWidth()
        {
            formattedText = text;
            if (maxWidth <= 0 || Width <= maxWidth) return;

            StringBuilder testText = new StringBuilder();
            StringBuilder currentLine = new StringBuilder();
            string[] wordList = text.Split(' ');
            bool firstWord = true;

            foreach(var word in wordList)
            {
                int addedLineLength = (int)font.MeasureString(currentLine.ToString() + " " + word).Length();
                if (addedLineLength < maxWidth)
                {
                    if (!firstWord) currentLine.Append(" ");
                    firstWord = false;
                    currentLine.Append(word);
                }
                else
                {
                    testText.Append(currentLine.ToString() + "\n");
                    currentLine.Clear();
                    currentLine.Append(word);
                }
            }
            testText.Append(currentLine.ToString());
            formattedText = testText.ToString();

        }

        public override string ToString()
        {
            return Text;
        }
        #endregion
    }
}
