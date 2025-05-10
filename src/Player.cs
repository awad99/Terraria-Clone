using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimpleTerrariaClone;
using static System.Formats.Asn1.AsnWriter;

namespace Project1
{
    public class Player
    {
        // Player properties
        private Vector2 _position;
        public Vector2 Position
        {
            get { return _position; }
            set { _position = value; }
        }

        private Vector2 _velocity;
        public Vector2 Velocity
        {
            get { return _velocity; }
            set { _velocity = value; }
        }
        private const int verticalMargin = 8; // Match value from CollisionBox

        public int NetworkId { get; set; } = -1;
        public int CurrentAnimationFrame => currentFrame;

        private bool _tileModified = false;
        private TileModification _lastTileChange;

        public bool IsOnGround { get; set; }

        // Physics constants - MODIFIED VALUES
        private const float MoveSpeed = 150f;
        private const float JumpStrength = -300f; // Reduced jump strength
        private const float Gravity = 1400f; // Increased gravity
        private const float MaxFallSpeed = 500f; // Increased max fall speed

        private Color highlightColor = Color.Yellow;
        private float highlightAlpha = 0.8f;
        // Frame constants for animation
        private const int IDLE_FRAME = 0;
        private const int WALK_START_FRAME = 1;
        private const int WALK_END_FRAME = 8;
        private const int JUMP_FRAME = 9;

        private const float FastFallSpeed = 800f;


        static public int FrameWidth => frameWidth;
        static public int FrameHeight => frameHeight;

        // Animation properties
        private Texture2D spriteSheet;
        private int currentFrame;
        private float frameTime;
        private float frameDelay = 0.1f; // Time between frame changes
        private SpriteEffects spriteEffect = SpriteEffects.None;

        private bool isFastFalling = false;
        // Player states
        private bool isMoving;
        private bool isJumping = false;

        static private readonly int frameWidth = 40;
        static private readonly int frameHeight = 56;
        private int framesCount;
        private Rectangle[] frames;

        // Position offset constants for drawing
        private const float MapYOffset = 10f;
        private const float MapAdditionalYOffset = 30f;
        static public  float DrawYOffset = 460f; // Very large Y offset for drawing the player much lower
        public const float DrawXOffset = -480f; // Negative X offset to draw player more to the left

        // Drawing scale
        private float scale = 1.0f;

        // Map reference for collision
        private Map map;
        private int tileSize;



        private MouseState previousMouseState;
        private Map.Tiles selectedTileType = Map.Tiles.dirt; // Default tile type
        private const float TileInteractionRange = 80f; // 8 tiles range
        private const int CollisionHorizontalMargin = 6;
        private const int CollisionVerticalMargin = 10;
        private Rectangle CollisionBox
        {
            get
            {
                int horizontalMargin = 4;
                int verticalMargin = 8; // Increased vertical margin
                return new Rectangle(
                    (int)_position.X + horizontalMargin,
                    (int)_position.Y + verticalMargin, // Start lower
                    (int)(frameWidth * scale) - (horizontalMargin * 2),
                    (int)(frameHeight * scale) - verticalMargin // Reduce height
                );
            }
        }

        public void SetAnimationFrame(int frame)
        {
            currentFrame = frame;
        }
        public Player(Texture2D spriteSheet, Map map, int tileSize)
        {
            this.spriteSheet = spriteSheet;
            this.map = map;
            this.tileSize = tileSize;

            // Adjust initial position to properly align with ground
            _position = new Vector2(100, Map.surfaceLevel * tileSize - frameHeight);

            _velocity = Vector2.Zero;
            IsOnGround = false;

            framesCount = spriteSheet.Height / frameHeight;
            InitializeAnimationFrames();

            // Initialize mouse state
            previousMouseState = Mouse.GetState();
        }

        private void InitializeAnimationFrames()
        {
            int count = spriteSheet.Height / frameHeight;
            frames = new Rectangle[count];
            for (int i = 0; i < count; i++)
                frames[i] = new Rectangle(0, i * frameHeight, frameWidth, frameHeight);
        }

        public void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            HandleInput(dt);
            ApplyPhysics(dt);
            CheckMapCollision();
            UpdateAnimation(dt);
            HandleTileInteraction(); // Added tile interaction
        }

        private void HandleInput(float deltaTime)
        {
            KeyboardState keyState = Keyboard.GetState();

            // Reset horizontal velocity
            _velocity.X = 0;
            isMoving = false;

            // Movement
            if (keyState.IsKeyDown(Keys.A) || keyState.IsKeyDown(Keys.Left))
            {
                _velocity.X = -MoveSpeed;
                isMoving = true;
                spriteEffect = SpriteEffects.None;
            }

            if (keyState.IsKeyDown(Keys.D) || keyState.IsKeyDown(Keys.Right))
            {
                _velocity.X = MoveSpeed;
                isMoving = true;
                spriteEffect = SpriteEffects.FlipHorizontally;
            }

            // Jump - only allow jumping when firmly on ground
            if ((keyState.IsKeyDown(Keys.W) || keyState.IsKeyDown(Keys.Up) ||
                 keyState.IsKeyDown(Keys.Space)) && IsOnGround)
            {
                _velocity.Y = JumpStrength;
                IsOnGround = false;
                isJumping = true;
            }

            isFastFalling = keyState.IsKeyDown(Keys.S) || keyState.IsKeyDown(Keys.Down);

            // Update the selected tile type
            UpdateSelectedTileType(keyState);
        }

        private void ApplyPhysics(float deltaTime)
        {

            if (!IsOnGround)
            {
                _velocity.Y += Gravity * deltaTime;

                // Limit fall speed based on fast falling
                float currentMaxFallSpeed = isFastFalling ? FastFallSpeed : MaxFallSpeed;
                if (_velocity.Y > currentMaxFallSpeed)
                    _velocity.Y = currentMaxFallSpeed;
            }
            else
            {
                // Set velocity to small downward value when on ground to keep grounded
                _velocity.Y = 10f;
                isJumping = false;
            }

            // Apply velocity to position
            _position += _velocity * deltaTime;
        }


        public bool HasModifiedTile()
        {
            bool result = _tileModified;
            _tileModified = false;
            return result;
        }

        public TileModification GetLastTileChange()
        {
            return _lastTileChange;
        }

        public struct TileModification
        {
            public int X;
            public int Y;
            public int TileType;
        }
        // Call this when the player modifies a tile
        public void ModifyTile(int x, int y, int tileType)
        {
            // Implement tile modification logic here
            _lastTileChange = new TileModification { X = x, Y = y, TileType = tileType };
            _tileModified = true;
        }
    

        private void HandleTileInteraction()
        {
            MouseState currentMouseState = Mouse.GetState();
            Vector2 mousePosScreen = new Vector2(currentMouseState.X, currentMouseState.Y);

            // Convert to world coordinates with proper offsets
            Point tileCoords = map.ScreenToTileCoordinates(
                (int)(mousePosScreen.X - DrawXOffset + map.xOffset),
                (int)(mousePosScreen.Y - DrawYOffset - map.yOffset)
            );

            if (map.IsTileInRange(Position, tileCoords, TileInteractionRange))
            {
                // Left click to break
                if (currentMouseState.LeftButton == ButtonState.Pressed)
                {
                    if (map.GetTileType(tileCoords.X, tileCoords.Y) != Map.Tiles.None)
                    {
                        map.RemoveTile(tileCoords.X, tileCoords.Y);
                    }
                }

                // Right click to place
                if (currentMouseState.RightButton == ButtonState.Pressed)
                {
                    if (map.GetTileType(tileCoords.X, tileCoords.Y) == Map.Tiles.None &&
                        !IsPlayerOccupyingTile(tileCoords))
                    {
                        map.PlaceTile(tileCoords.X, tileCoords.Y, selectedTileType);
                    }
                }
            }

            previousMouseState = currentMouseState;
        }

        private bool IsPlayerOccupyingTile(Point tileCoords)
        {
            Rectangle playerBounds = CollisionBox;
            Rectangle tileBounds = new Rectangle(
                tileCoords.X * tileSize,
                tileCoords.Y * tileSize,
                tileSize,
                tileSize
            );

            // Check collision with 1 tile buffer around player
            int buffer = tileSize * 2;
            Rectangle expandedPlayerBounds = new Rectangle(
                playerBounds.X - buffer,
                playerBounds.Y - buffer,
                playerBounds.Width + buffer * 2,
                playerBounds.Height + buffer * 2
            );

            return expandedPlayerBounds.Intersects(tileBounds);
        }
        private void DrawTileHighlight(SpriteBatch spriteBatch)
        {
            MouseState mouse = Mouse.GetState();

            // Convert screen coordinates to tile coordinates using Map's method
            Point tileCoords = map.ScreenToTileCoordinates(mouse.X, mouse.Y);

            // Calculate if tile is within interaction range using Map's method
            bool isInRange = map.IsTileInRange(_position, tileCoords, TileInteractionRange);

            // Only highlight if within interaction range
            if (isInRange)
            {
                // Use Map's DrawTileHighlight method which handles the correct conversion
                map.DrawTileHighlight(spriteBatch, tileCoords, highlightColor, highlightAlpha);
            }
        }

        private bool IsPlayerAtTile(Point tileCoords)
        {
            Rectangle playerBounds = CollisionBox;

            // Expand bounds by 1 tile to prevent adjacent placement
            int buffer = tileSize;
            Rectangle expandedBounds = new Rectangle(
                playerBounds.X - buffer,
                playerBounds.Y - buffer,
                playerBounds.Width + 2 * buffer,
                playerBounds.Height + 2 * buffer
            );

            Rectangle tileRect = new Rectangle(
                tileCoords.X * tileSize,
                tileCoords.Y * tileSize,
                tileSize,
                tileSize
            );

            return expandedBounds.Intersects(tileRect);
        }


        private void UpdateSelectedTileType(KeyboardState keyState)
        {
            // Tile selection hotkeys (1-5 keys)
            if (keyState.IsKeyDown(Keys.D1)) selectedTileType = Map.Tiles.dirt;
            if (keyState.IsKeyDown(Keys.D2)) selectedTileType = Map.Tiles.grass;
            if (keyState.IsKeyDown(Keys.D3)) selectedTileType = Map.Tiles.stone;
            if (keyState.IsKeyDown(Keys.D4)) selectedTileType = Map.Tiles.gold;
            if (keyState.IsKeyDown(Keys.D5)) selectedTileType = Map.Tiles.silver;
        }
        private void CheckMapCollision()
        {
            // Get player bounds in tile coordinates using the collision box
            Rectangle playerBounds = CollisionBox;

            int playerLeft = (int)Math.Floor((double)playerBounds.Left / tileSize);
            int playerRight = (int)Math.Floor((double)playerBounds.Right / tileSize);
            int playerTop = (int)Math.Floor((double)playerBounds.Top / tileSize);
            int playerBottom = (int)Math.Floor((double)playerBounds.Bottom / tileSize);

            // Test wider area for collision
            const int collisionRange = 2;

            // Check surrounding tiles
            IsOnGround = false;

            for (int y = playerTop - collisionRange; y <= playerBottom + collisionRange; y++)
            {
                for (int x = playerLeft - collisionRange; x <= playerRight + collisionRange; x++)
                {
                    // Check if the tile is solid
                    if (map.IsSolidTile(x, y))
                    {
                        // Create tile bounds
                        Rectangle tileBounds = new Rectangle(
                            x * tileSize,
                            y * tileSize,
                            tileSize,
                            tileSize
                        );

                        // Test for intersection
                        if (playerBounds.Intersects(tileBounds))
                        {
                            // Calculate penetration depths
                            float depthX = CalculateXPenetration(playerBounds, tileBounds);
                            float depthY = CalculateYPenetration(playerBounds, tileBounds);

                            // Resolve collision by smallest penetration
                            if (Math.Abs(depthX) < Math.Abs(depthY))
                            {
                                _position.X += depthX;
                                _velocity.X = 0;
                            }
                            else
                            {
                                if (_velocity.Y > 0 && depthY < 0)
                                {
                                    // Player is landing on a tile
                                    _position.Y = tileBounds.Top - (frameHeight * scale) + verticalMargin;
                                    IsOnGround = true;
                                }
                                else if (_velocity.Y < 0 && depthY > 0)
                                {
                                    // we hit the bottom of a tile while jumping
                                    _position.Y = tileBounds.Bottom;
                                    _velocity.Y = 0; // Reset vertical velocity when hitting ceiling
                                }
                                else
                                {
                                    // horizontal collision or other case
                                    _position.Y += depthY;
                                }

                                _velocity.Y = 0;
                            }
                        }
                    }
                }
            }

            // Enhanced ground check - more reliable
            if (!IsOnGround)
            {
                // Check for ground 1 pixel below the player
                Rectangle groundCheckRect = new Rectangle(
                    playerBounds.X,
                    playerBounds.Bottom,
                    playerBounds.Width,
                    1
                );

                int checkLeft = (int)Math.Floor((double)groundCheckRect.Left / tileSize);
                int checkRight = (int)Math.Floor((double)groundCheckRect.Right / tileSize);
                int checkY = (int)Math.Floor((double)groundCheckRect.Y / tileSize);

                // Check if there's ground directly below
                for (int x = checkLeft; x <= checkRight; x++)
                {
                    if (map.IsSolidTile(x, checkY))
                    {
                        IsOnGround = true;
                        break;
                    }
                }
            }
        }


        private float CalculateXPenetration(Rectangle player, Rectangle tile)
        {
            float playerCenter = player.X + player.Width / 2.0f;
            float tileCenter = tile.X + tile.Width / 2.0f;
            float distX = playerCenter - tileCenter;

            // Calculate x-penetration
            if (distX > 0)
                return tile.Right - player.Left; // Push right
            else
                return tile.Left - player.Right; // Push left
        }

        private float CalculateYPenetration(Rectangle player, Rectangle tile)
        {
            float playerCenter = player.Y + player.Height / 2.0f;
            float tileCenter = tile.Y + tile.Height / 2.0f;
            float distY = playerCenter - tileCenter;

            // Calculate y-penetration
            if (distY > 0)
                return tile.Bottom - player.Top; // Push down
            else
                return tile.Top - player.Bottom; // Push up
        }

        private void UpdateAnimation(float deltaTime)
        {
            frameTime += deltaTime;
            if (frameTime < frameDelay) return;
            frameTime = 0f;

            // Animation frame selection
            if (!IsOnGround && isJumping)
            {
                currentFrame = JUMP_FRAME;
            }
            else if (isMoving)
            {
                if (currentFrame < WALK_START_FRAME || currentFrame > WALK_END_FRAME)
                    currentFrame = WALK_START_FRAME;
                else
                    currentFrame = WALK_START_FRAME +
                        ((currentFrame - WALK_START_FRAME + 1)
                         % (WALK_END_FRAME - WALK_START_FRAME + 1));
            }
            else
            {
                currentFrame = IDLE_FRAME;
            }

            // finally clamp to valid range:
            currentFrame = MathHelper.Clamp(currentFrame, 0, framesCount - 1);
        }
        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw player sprite with proper offsets
            Vector2 drawPos = new Vector2(
                _position.X + DrawXOffset,
                _position.Y + DrawYOffset
            );

            spriteBatch.Draw(
                spriteSheet,
                drawPos,
                frames[currentFrame],
                Color.White,
                0f,
                new Vector2(frameWidth / 2f, frameHeight),
                scale,
                spriteEffect,
                0f
            );

            // Draw tile highlight
            MouseState mouse = Mouse.GetState();
            Point tileCoords = map.ScreenToTileCoordinates(
                (int)(mouse.X - DrawXOffset + map.xOffset),
                (int)(mouse.Y - DrawYOffset - map.yOffset)
            );

            if (map.IsTileInRange(Position, tileCoords, TileInteractionRange))
            {
                Rectangle highlightRect = new Rectangle(
                    tileCoords.X * tileSize + (int)DrawXOffset - map.xOffset,
                    tileCoords.Y * tileSize + (int)DrawYOffset + map.yOffset,
                    tileSize,
                    tileSize
                );

                Texture2D pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                pixel.SetData(new[] { highlightColor * highlightAlpha });

                // Draw highlight
                spriteBatch.Draw(pixel, highlightRect, highlightColor * highlightAlpha);
            }
        }

        public Vector2 GetCenter()
        {
            // For camera calculations, we still use the actual position
            // NOT including drawing offsets so gameplay isn't affected
            return new Vector2(
                _position.X + (frameWidth * scale) / 2,
                _position.Y + (frameHeight * scale) / 2
            );
        }
    }
}