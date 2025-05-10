using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SimpleTerrariaClone
{
    public class Tree
    {
        private Texture2D _texture;
        private int _tileSize;
        private int _xOffset;
        private int _yOffset;
        public int X { get; private set; }
        public int Y { get; private set; }

        // Scale factor to make trees smaller - use a much smaller value like 0.1f
        private float _scale;

        public Tree(Texture2D texture, int tileSize, int xOffset, int yOffset, float scale)
        {
            _texture = texture;
            _tileSize = tileSize;
            _xOffset = xOffset;
            _yOffset = yOffset;

            // Make sure scale is actually small enough to make a difference
            // If you wanted scale 0.01, make sure it's actually set to 0.01
            _scale = scale;
        }

        public void SetPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 cameraPosition)
        {
            // Calculate the position at the base of where the tree should be
            float baseX = X * _tileSize - _xOffset;
            float baseY = Y * _tileSize + _yOffset;

            // Calculate scaled dimensions
            float scaledWidth = _texture.Width * _scale;
            float scaledHeight = _texture.Height * _scale;

            // Position for drawing (centered on tile, bottom aligned)
            float drawX = baseX - (scaledWidth - _tileSize) / 2.0f;
            float drawY = baseY - scaledHeight + _tileSize;

            // Draw using source rectangle and destination rectangle to control scaling
            Rectangle sourceRect = new Rectangle(0, 0, _texture.Width, _texture.Height);
            Rectangle destRect = new Rectangle(
                (int)drawX,
                (int)drawY,
                (int)scaledWidth,
                (int)scaledHeight
            );

            spriteBatch.Draw(
                _texture,
                destRect,
                sourceRect,
                Color.White
            );
        }
    }
}