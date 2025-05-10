using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SimpleTerrariaClone
{
    public class Logo
    {
        private Texture2D logoTexture;
        private Texture2D backgroundTexture;
        private Rectangle logoRect;
        private Rectangle playButtonRect;
        private Rectangle characterOptionsRect;

        private bool showOptions;
        private bool showCharacterOptions;
        private Color playButtonColor;
        private Color characterButtonColor;

        private SpriteFont font;

        public bool IsActive { get; private set; } = true;

        public Logo(Texture2D logo, Texture2D background, SpriteFont font)
        {
            this.logoTexture = logo;
            this.backgroundTexture = background;
            this.font = font;

            // Initialize button colors
            playButtonColor = Color.White;
            characterButtonColor = Color.White;

            // Set initial state
            showOptions = true;
            showCharacterOptions = false;

            // Calculate positions based on screen size
            ResetPositions();
        }

        public void ResetPositions(int screenWidth = 800, int screenHeight = 600)
        {
            // Logo is positioned at the top center of the screen
            logoRect = new Rectangle(
                screenWidth / 2 - logoTexture.Width / 2,
                50,
                logoTexture.Width,
                logoTexture.Height
            );

            // Play button positioned below the logo
            playButtonRect = new Rectangle(
                screenWidth / 2 - 100,
                logoRect.Bottom + 50,
                200,
                50
            );

            // Character options button below play button
            characterOptionsRect = new Rectangle(
                screenWidth / 2 - 100,
                playButtonRect.Bottom + 20,
                200,
                50
            );
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive) return;

            MouseState mouseState = Mouse.GetState();
            Point mousePosition = new Point(mouseState.X, mouseState.Y);

            // Check if mouse is hovering over buttons and change colors
            if (showOptions)
            {
                // Play button hover effect
                if (playButtonRect.Contains(mousePosition))
                {
                    playButtonColor = Color.Yellow;
                    if (mouseState.LeftButton == ButtonState.Pressed)
                    {
                        IsActive = false; // Start the game
                    }
                }
                else
                {
                    playButtonColor = Color.White;
                }

                // Character options button hover effect
                if (characterOptionsRect.Contains(mousePosition))
                {
                    characterButtonColor = Color.Yellow;
                    if (mouseState.LeftButton == ButtonState.Pressed)
                    {
                        showCharacterOptions = !showCharacterOptions;
                    }
                }
                else
                {
                    characterButtonColor = Color.White;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsActive) return;

            // Draw background
            spriteBatch.Draw(
                backgroundTexture,
                new Rectangle(0, 0, spriteBatch.GraphicsDevice.Viewport.Width, spriteBatch.GraphicsDevice.Viewport.Height),
                Color.White
            );

            // Draw logo
            spriteBatch.Draw(logoTexture, logoRect, Color.White);

            if (showOptions)
            {
                // Draw play button
                DrawButton(spriteBatch, playButtonRect, "Play", playButtonColor);

                // Draw character options button
                DrawButton(spriteBatch, characterOptionsRect, "Character Options", characterButtonColor);

                // If character options is toggled, draw the options panel
                if (showCharacterOptions)
                {
                    DrawCharacterOptions(spriteBatch);
                }
            }
        }

        private void DrawButton(SpriteBatch spriteBatch, Rectangle rect, string text, Color color)
        {
            // Draw button background
            spriteBatch.Draw(
                backgroundTexture, // Using background texture as button background
                rect,
                new Rectangle(0, 0, 10, 10), // Just using a small part of the texture
                color * 0.7f // Semi-transparent
            );

            // Draw button text
            Vector2 textSize = font.MeasureString(text);
            Vector2 textPosition = new Vector2(
                rect.X + (rect.Width - textSize.X) / 2,
                rect.Y + (rect.Height - textSize.Y) / 2
            );

            spriteBatch.DrawString(
                font,
                text,
                textPosition,
                color
            );
        }

        private void DrawCharacterOptions(SpriteBatch spriteBatch)
        {
            // Draw character options panel
            Rectangle optionsPanel = new Rectangle(
                characterOptionsRect.X - 100,
                characterOptionsRect.Bottom + 10,
                400,
                200
            );

            // Panel background
            spriteBatch.Draw(
                backgroundTexture,
                optionsPanel,
                new Rectangle(0, 0, 10, 10),
                Color.Black * 0.8f
            );

            // Draw options title
            spriteBatch.DrawString(
                font,
                "Character Options",
                new Vector2(optionsPanel.X + 10, optionsPanel.Y + 10),
                Color.White
            );

            // Draw character options (placeholders)
            string[] options = new string[] { "Skin Color", "Hair Style", "Clothes" };
            for (int i = 0; i < options.Length; i++)
            {
                spriteBatch.DrawString(
                    font,
                    options[i],
                    new Vector2(optionsPanel.X + 20, optionsPanel.Y + 50 + i * 40),
                    Color.White
                );
            }
        }
    }
}