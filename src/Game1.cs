using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Project1;
using SimpleTerrariaClone;
using Lidgren.Network;
using System.Net;
using static SimpleTerrariaClone.Map;

namespace SimpleTerrariaClone
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;
        private Camera2D camera;
        private Map map;
        private Texture2D dirt, grass, stone, gold, silver, treeTexture;
        private Texture2D playerSpritesheet;
        private Texture2D logoTexture, backgroundTexture;
        private Texture2D hammerItem, dirtItem, goldItem, stoneItem;
        private Texture2D slotTexture; // Texture for inventory slot
        private Player player;
        private Logo logo;
        private SpriteFont font;
        private GameState currentState = GameState.MainMenu;
        private HUD hud; // HUD object

        private string errorMessage = string.Empty;

        // Networking
        private Server _gameServer;
        private NetClient _networkClient;
        private bool _isHost = false;
        private bool _isConnected = false;
        private int _localPlayerId = -1;
        private Dictionary<int, Player> _remotePlayers = new Dictionary<int, Player>();

        // Game states
        private enum GameState
        {
            MainMenu,
            Connecting,
            Playing
        }

        // World settings
        private const int WorldHeight = 150;
        private const int TileSize = 16;
        private float groundY = WorldHeight * TileSize;
        private int xStart = 10 / TileSize;
        private int yStart = 10 / TileSize;
        private int xEnd = 900 / TileSize;
        private int yEnd = 500 / TileSize;

        //private Map map;
        private Tileset Tiles;

        // Camera control settings
        private bool freeCameraMode = false;
        private KeyboardState previousKeyboardState;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.PreferredBackBufferWidth = 800;
            graphics.PreferredBackBufferHeight = 600;
            Window.Title = "Terraria Clone";

            // Enable fixed time step for consistent physics
            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromSeconds(1 / 60.0); // 60fps target
        }

        private void ConnectToServer(string address)
        {
            var config = new NetPeerConfiguration(Server.AppIdentifier);
            _networkClient = new NetClient(config);
            _networkClient.Start();
            _networkClient.Connect(address, Server.Port);
            currentState = GameState.Connecting;
        }

        protected override void Initialize()
        {
            camera = new Camera2D(GraphicsDevice.Viewport)
            {
                // center vertically on the middle of the "infinite" strip
                Position = new Vector2(0, WorldHeight * TileSize * 0.5f),
                Zoom = 1f
            };
            // snap to integer pixels
            camera.Position = new Vector2(
                (float)Math.Round(camera.Position.X),
                (float)Math.Round(camera.Position.Y)
            );

            previousKeyboardState = Keyboard.GetState();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // 1) Load all your textures first
            dirt = Content.Load<Texture2D>("Tiles_0");
            grass = Content.Load<Texture2D>("Tiles_2");
            stone = Content.Load<Texture2D>("Tiles_1 (1)");
            gold = Content.Load<Texture2D>("Tiles_8");
            silver = Content.Load<Texture2D>("Tiles_9");
            treeTexture = Content.Load<Texture2D>("Trees");

            // Load player spritesheet
            playerSpritesheet = Content.Load<Texture2D>("Player");

            // Load UI elements
            logoTexture = Content.Load<Texture2D>("Logo");
            backgroundTexture = Content.Load<Texture2D>("Background");
            font = Content.Load<SpriteFont>("Font"); // Make sure to add a font to your Content

            hammerItem = Content.Load<Texture2D>("Items/Item_1");
            dirtItem = Content.Load<Texture2D>("Items/Item_2");
            stoneItem = Content.Load<Texture2D>("Items/Item_3");
            goldItem = Content.Load<Texture2D>("Items/Item_13");

            // Create a simple grey texture for inventory slots
            slotTexture = new Texture2D(GraphicsDevice, 1, 1);
            slotTexture.SetData(new[] { Color.DarkGray });

            Tiles = new Tileset(dirt, grass, stone, gold, silver, TileSize);

            // 3) Now you can safely pass both into Map
            map = new Map(Tiles, TileSize, treeTexture);

            // Generate initial world
            map.EnsureWorldGenerated(camera.Position);

            // Create player after map is generated
            player = new Player(playerSpritesheet, map, TileSize);

            // Create logo screen
            logo = new Logo(logoTexture, backgroundTexture, font);

            // Create HUD with 5 inventory slots
            hud = new HUD(slotTexture, 5);

            // Set hammer in the first slot
            hud.SetItem(0, hammerItem);
        }

      
        private void StartNetworking(bool asHost, string serverAddress = "localhost")
        {
            // Decide if we're hosting or just joining
            _isHost = asHost;

            if (_isHost)
            {
                // Create and start the server
                _gameServer = new Server();
                _gameServer.Start();
                ConnectToServer("localhost");
            }
            else
            {
                // Just connect to an existing server
                ConnectToServer(serverAddress);
            }
        }

        private void ProcessNetworkMessages()
        {
            if (_networkClient == null)
                return;

            NetIncomingMessage msg;
            while ((msg = _networkClient.ReadMessage()) != null)
            {
                switch (msg.MessageType)
                {
                    case NetIncomingMessageType.Data:
                        HandleNetworkData(msg);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        HandleConnectionStatus(msg);
                        break;
                }
                _networkClient.Recycle(msg);
            }

            // If server is running, update it too
            if (_isHost && _gameServer != null)
            {
                _gameServer.Update();
            }
        }

        private void HandleConnectionStatus(NetIncomingMessage msg)
        {
            var status = (NetConnectionStatus)msg.ReadByte();

            if (status == NetConnectionStatus.Connected)
            {
                // Connected successfully to server
                _isConnected = true;

                // Read player ID if available
                if (msg.LengthBytes - msg.PositionInBytes >= 4)
                {
                    _localPlayerId = msg.ReadInt32();
                    player.NetworkId = _localPlayerId;
                }

                currentState = GameState.Playing;  // Switch to playing state
            }
            else if (status == NetConnectionStatus.Disconnected)
            {
                _isConnected = false;
                // Could transition back to menu or show disconnect message
            }
        }

        private void StartHost()
        {
            try
            {
                if (_gameServer != null)
                {
                    _gameServer.Stop();
                    System.Threading.Thread.Sleep(1000); // Wait 1 second
                }
                _gameServer = new Server();
                _gameServer.Start();
                ConnectToServer("localhost");
            }
            catch (Exception ex)
            {
                // Show error message to player
                errorMessage = $"Failed to start server: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\n{ex.InnerException.Message}";
                }
            }
        }
        private void HandleNetworkData(NetIncomingMessage msg)
        {
            var messageType = (NetworkMessageType)msg.ReadByte();
            switch (messageType)
            {
                case NetworkMessageType.PlayerState:
                    UpdateRemotePlayer(msg);
                    break;

                case NetworkMessageType.TileChange:
                    UpdateWorldTile(msg);
                    break;

                case NetworkMessageType.PlayerJoined:
                    HandlePlayerJoined(msg);
                    break;

                case NetworkMessageType.PlayerLeft:
                    HandlePlayerLeft(msg);
                    break;
            }
        }


        private void HandlePlayerJoined(NetIncomingMessage msg)
        {
            int playerId = msg.ReadInt32();

            // Only create remote player if it's not us
            if (playerId != _localPlayerId && !_remotePlayers.ContainsKey(playerId))
            {
                var remotePlayer = new Player(playerSpritesheet, map, TileSize);
                remotePlayer.NetworkId = playerId;
                _remotePlayers.Add(playerId, remotePlayer);
            }
        }

        private void HandlePlayerLeft(NetIncomingMessage msg)
        {
            int playerId = msg.ReadInt32();
            if (_remotePlayers.ContainsKey(playerId))
            {
                _remotePlayers.Remove(playerId);
            }
        }

        private void UpdateRemotePlayer(NetIncomingMessage msg)
        {
            var playerId = msg.ReadInt32();

            // Don't update ourselves
            if (playerId == _localPlayerId)
                return;

            if (!_remotePlayers.TryGetValue(playerId, out var remotePlayer))
            {
                remotePlayer = new Player(playerSpritesheet, map, TileSize);
                remotePlayer.NetworkId = playerId;
                _remotePlayers.Add(playerId, remotePlayer);
            }

            remotePlayer.Position = new Vector2(msg.ReadFloat(), msg.ReadFloat());
            remotePlayer.SetAnimationFrame(msg.ReadInt32());
        }

        private void UpdateWorldTile(NetIncomingMessage msg)
        {
            var x = msg.ReadInt32();
            var y = msg.ReadInt32();
            var tileType = (Tiles)msg.ReadByte();

            // Apply the tile change to your local world
            map.SetTile(x, y, (int)tileType);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState currentKeyboardState = Keyboard.GetState();

            // exit
            if (currentKeyboardState.IsKeyDown(Keys.Escape))
                Exit();

            switch (currentState)
            {
                case GameState.MainMenu:
                    // Update logo/menu screen
                    logo.Update(gameTime);

                    // Host game (H key)
                    if (currentKeyboardState.IsKeyDown(Keys.H) && previousKeyboardState.IsKeyUp(Keys.H))
                    {
                        StartNetworking(true);
                    }
                    // Join game (J key)
                    else if (currentKeyboardState.IsKeyDown(Keys.J) && previousKeyboardState.IsKeyUp(Keys.J))
                    {
                        StartNetworking(false, "192.168.0.48"); // Change this to your server IP
                    }
                    // Single player (regular click or enter)
                    else if (!logo.IsActive)
                    {
                        currentState = GameState.Playing;
                    }
                    break;

                case GameState.Connecting:
                    // Process networking while waiting for connection
                    ProcessNetworkMessages();
                    break;

                case GameState.Playing:
                    // Process network updates
                    ProcessNetworkMessages();

                    // Update game logic
                    UpdateGame(gameTime, currentKeyboardState);

                    // Send player state update if connected
                    if (_isConnected && _localPlayerId > 0)
                    {
                        SendPlayerUpdate();
                    }
                    break;
            }

            // Store current keyboard state
            previousKeyboardState = currentKeyboardState;

            base.Update(gameTime);
        }

        private void UpdateGame(GameTime gameTime, KeyboardState currentKeyboardState)
        {
            // Toggle camera mode with Tab key
            if (currentKeyboardState.IsKeyDown(Keys.Tab) && previousKeyboardState.IsKeyUp(Keys.Tab))
                freeCameraMode = !freeCameraMode;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Update player first
            player.Update(gameTime);

            // Update remote players if needed
            foreach (var remotePair in _remotePlayers.Values)
            {
                remotePair.Update(gameTime);
            }

            // Camera behavior depends on mode
            if (freeCameraMode)
            {
                // Free camera movement
                float speed = 300f;
                Vector2 pos = camera.Position;

                if (currentKeyboardState.IsKeyDown(Keys.Left)) pos.X -= speed * dt;
                if (currentKeyboardState.IsKeyDown(Keys.Right)) pos.X += speed * dt;
                if (currentKeyboardState.IsKeyDown(Keys.Up)) pos.Y -= speed * dt;
                if (currentKeyboardState.IsKeyDown(Keys.Down)) pos.Y += speed * dt;

                // snap to pixel grid
                camera.Position = new Vector2(
                    (float)Math.Round(pos.X),
                    (float)Math.Round(pos.Y)
                );
            }
            else
            {
                // Follow player
                // Get player center position
                Vector2 playerCenter = player.GetCenter();

                // Set camera target position (centered on player)
                camera.FollowTarget(playerCenter);
            }

            // Update camera with smooth following
            camera.Update(gameTime);

            // Ensure world is generated in the current view
            map.EnsureWorldGenerated(camera.Position);

            // Handle inventory selection with number keys
            for (int i = 0; i < 5; i++)
            {
                Keys key = Keys.D1 + i;
                if (currentKeyboardState.IsKeyDown(key) && previousKeyboardState.IsKeyUp(key))
                {
                    hud.SelectSlot(i);
                }
            }

            // Check if player modifies a tile and needs to broadcast it
            if (_isConnected && player.HasModifiedTile())
            {
                var tileChange = player.GetLastTileChange();
                SendTileChange(tileChange.X, tileChange.Y, (Tiles)tileChange.TileType);
            }
        }

        private void SendTileChange(int x, int y, Tiles tileType)
        {
            if (_networkClient == null || !_isConnected)
                return;

            var msg = _networkClient.CreateMessage();
            msg.Write((byte)NetworkMessageType.TileChange);
            msg.Write(x);
            msg.Write(y);
            msg.Write((byte)tileType);

            _networkClient.SendMessage(msg, NetDeliveryMethod.ReliableOrdered);
        }

        private void SendPlayerUpdate()
        {
            if (_networkClient == null || !_isConnected)
                return;

            var msg = _networkClient.CreateMessage();
            msg.Write((byte)NetworkMessageType.PlayerState);
            msg.Write(_localPlayerId);
            msg.Write(player.Position.X);
            msg.Write(player.Position.Y);
            msg.Write(player.CurrentAnimationFrame);

            _networkClient.SendMessage(msg, NetDeliveryMethod.UnreliableSequenced);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                spriteBatch.Begin();
                Vector2 errorSize = font.MeasureString(errorMessage);
                spriteBatch.DrawString(font, errorMessage,
                    new Vector2(
                        GraphicsDevice.Viewport.Width / 2 - errorSize.X / 2,
                        GraphicsDevice.Viewport.Height - 100
                    ),
                    Color.Red);
                spriteBatch.End();
            }

            switch (currentState)
            {
                case GameState.MainMenu:
                    // Draw the logo/menu screen
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
                    logo.Draw(spriteBatch);

                    // Add networking prompt
                    string menuText = "Press H to host or J to join a game";
                    Vector2 textSize = font.MeasureString(menuText);
                    spriteBatch.DrawString(font, menuText,
                                          new Vector2(GraphicsDevice.Viewport.Width / 2 - textSize.X / 2,
                                                     GraphicsDevice.Viewport.Height - 50),
                                          Color.White);

                    spriteBatch.End();
                    break;

                case GameState.Connecting:
                    // Draw connecting screen
                    spriteBatch.Begin();
                    string connectingText = "Connecting to server...";
                    Vector2 connectSize = font.MeasureString(connectingText);
                    spriteBatch.DrawString(font, connectingText,
                                          new Vector2(GraphicsDevice.Viewport.Width / 2 - connectSize.X / 2,
                                                     GraphicsDevice.Viewport.Height / 2),
                                          Color.White);
                    spriteBatch.End();
                    break;

                case GameState.Playing:
                    DrawGame(gameTime);
                    break;
            }

            base.Draw(gameTime);
        }

        private void DrawGame(GameTime gameTime)
        {
            // Draw the game world with camera transformation
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null,
                null,
                camera.Transform);

            // Draw base dirt and terrain
            map.gearteath(spriteBatch, 0, 1, 2, TileSize, camera.Position);

            // Draw mountains with caves, ores, and trees
            map.DrawMountain(spriteBatch, 0, 1, 2, 3, 4, TileSize, camera.Position);

            // Draw remote players
            foreach (var remotePlayer in _remotePlayers.Values)
            {
                remotePlayer.Draw(spriteBatch);
            }

            // Draw local player
            player.Draw(spriteBatch);

            spriteBatch.End();

            // Draw HUD without camera transformation (fixed to screen)
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            hud.Draw(spriteBatch);

            // If connected, show player ID
            if (_isConnected)
            {
                string networkInfo = $"Connected as Player {_localPlayerId}";
                if (_isHost)
                    networkInfo += " (Host)";
                networkInfo += $" - {_remotePlayers.Count} other player(s) online";

                spriteBatch.DrawString(font, networkInfo, new Vector2(10, 10), Color.White);
            }

            spriteBatch.End();
        }
    }
}