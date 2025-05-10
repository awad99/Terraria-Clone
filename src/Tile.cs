using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Project1
{
    public class Tileset
    {
        private Texture2D dirtTexture;
        private Texture2D grassTexture;
        private Texture2D stoneTexture;
        private Texture2D goldTexture;
        private Texture2D silverTexture;
        private int tileSize;
        private int dirtTilesPerRow;
        private int grassTilesPerRow;
        private int stoneTilesPerRow;
        private int goldTilesPerRow;
        private int silverTilesPerRow;

        // Constructor that takes all textures
        public Tileset(Texture2D dirtTexture, Texture2D grassTexture, Texture2D stoneTexture,
                      Texture2D goldTexture, Texture2D silverTexture, int tileSize)
        {
            this.dirtTexture = dirtTexture;
            this.grassTexture = grassTexture;
            this.stoneTexture = stoneTexture;
            this.goldTexture = goldTexture;
            this.silverTexture = silverTexture;
            this.tileSize = tileSize;

            // Calculate tiles per row for each texture
            this.dirtTilesPerRow = dirtTexture.Width / tileSize;
            this.grassTilesPerRow = grassTexture.Width / tileSize;
            this.stoneTilesPerRow = stoneTexture.Width / tileSize;
            this.goldTilesPerRow = goldTexture.Width / tileSize;
            this.silverTilesPerRow = silverTexture.Width / tileSize;
        }

        // Constructor for backward compatibility
        public Tileset(Texture2D texture, int tileSize)
        {
            this.dirtTexture = texture;
            this.tileSize = tileSize;
            this.dirtTilesPerRow = texture.Width / tileSize;
        }

        // Draw tile method with flags for different tile types
        public void DrawTile(SpriteBatch spriteBatch, int tileIndex, Vector2 position,
                            bool isGrass = false, bool isStone = false, bool isGold = false, bool isSilver = false)
        {
            if (isGrass && grassTexture != null)
            {
                // Use grass texture
                int wrappedIndex = tileIndex % grassTilesPerRow;
                Rectangle sourceRect = GetTileSourceRec(wrappedIndex, grassTilesPerRow, grassTexture);
                spriteBatch.Draw(grassTexture, position, sourceRect, Color.White);
            }
            else if (isStone && stoneTexture != null)
            {
                // Use stone texture
                int wrappedIndex = tileIndex % stoneTilesPerRow;
                Rectangle sourceRect = GetTileSourceRec(wrappedIndex, stoneTilesPerRow, stoneTexture);
                spriteBatch.Draw(stoneTexture, position, sourceRect, Color.White);
            }
            else if (isGold && goldTexture != null)
            {
                // Use gold texture
                int wrappedIndex = tileIndex % goldTilesPerRow;
                Rectangle sourceRect = GetTileSourceRec(wrappedIndex, goldTilesPerRow, goldTexture);
                spriteBatch.Draw(goldTexture, position, sourceRect, Color.White);
            }
            else if (isSilver && silverTexture != null)
            {
                // Use silver texture
                int wrappedIndex = tileIndex % silverTilesPerRow;
                Rectangle sourceRect = GetTileSourceRec(wrappedIndex, silverTilesPerRow, silverTexture);
                spriteBatch.Draw(silverTexture, position, sourceRect, Color.White);
            }
            else
            {
                // Default to dirt texture
                int wrappedIndex = tileIndex % dirtTilesPerRow;
                Rectangle sourceRect = GetTileSourceRec(wrappedIndex, dirtTilesPerRow, dirtTexture);
                spriteBatch.Draw(dirtTexture, position, sourceRect, Color.White);
            }
        }

        // Helper method to calculate source rectangle for a tile
        private Rectangle GetTileSourceRec(int tileIndex, int tilesInRow, Texture2D texture)
        {
            // Calculate tile position in texture atlas
            int x = (tileIndex % tilesInRow) * tileSize;
            int y = (tileIndex / tilesInRow) * tileSize;

            // Ensure we're within texture bounds - wrap if exceeding dimensions
            int textureWidthInTiles = texture.Width / tileSize;
            int textureHeightInTiles = texture.Height / tileSize;

            int tileX = tileIndex % textureWidthInTiles;
            int tileY = (tileIndex / textureWidthInTiles) % textureHeightInTiles;

            return new Rectangle(tileX * tileSize, tileY * tileSize, tileSize, tileSize);
        }
    }
}