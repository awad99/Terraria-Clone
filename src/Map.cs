using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Project1;
using static SimpleTerrariaClone.Map;

namespace SimpleTerrariaClone
{
    public class Chunk
    {
        private Tiles[,] tiles;
        private int width;
        private int height;

        public Chunk(int width, int height)
        {
            this.width = width;
            this.height = height;
            tiles = new Tiles[width, height];
        }

        public Tiles GetTile(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
                return tiles[x, y];
            return Map.Tiles.None;
        }

        public void SetTile(int x, int y, Map.Tiles tileType)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
                tiles[x, y] = tileType;
        }
    }

    public class Map
    {
        Tileset Tileset;
        private Texture2D _treeTexture;
        private int WorldHeight { get; set; }
        private int TileSize = 10;
        public int yOffset = 1200, xOffset = 500;
        private const int StoneStartDepth = 15;
        private const int StoneVariation = 5;
        private const double TreeFrequency = 0.05;
        private const float TreeScale = 0.3f;

        private HashSet<Point> treePositions = new HashSet<Point>();
        private Dictionary<int, MountainGroup> worldData = new Dictionary<int, MountainGroup>();
        private Dictionary<int, StoneData> stoneData = new Dictionary<int, StoneData>();
        private HashSet<Point> cavePoints = new HashSet<Point>();
        private int viewRange = 80;

        private int minWorldY = 0;
        private const int baseY = 50;
        public const int surfaceLevel = 50;
        private const int ChunkSize = 16; // Added ChunkSize constant
        private Dictionary<int, Chunk> chunks = new Dictionary<int, Chunk>(); // Added chunks dictionary

        // Cave generation parameters
        private const double CaveFrequency = 0.15;
        private const int MaxCaveSize = 20;
        private const int MinCaveSize = 5;
        private const double OreChanceInCave = 0.35;
        private const double GoldToSilverRatio = 0.7;

        Random random = new Random();


        public enum Tiles
        {
            None = 0,
            dirt = 1,
            grass = 2,
            stone = 3,
            gold = 4,
            silver = 5
        }

        private class MountainGroup
        {
            public int X;
            public int Height;
            public int PeakHeight;
            public bool HasGrass;
            public bool IsSurface;
            public HashSet<int> GrassYPositions = new HashSet<int>(); // Store Y positions where grass should appear
        }

        private class StoneData
        {
            public int X;
            public int StartDepth;
            public HashSet<Point> StoneVariations = new HashSet<Point>();
            public HashSet<Point> GoldVariations = new HashSet<Point>();
            public HashSet<Point> SilverVariations = new HashSet<Point>();
            public HashSet<Point> CavePoints = new HashSet<Point>(); // New: caves within this column
        }

        public Map(Tileset tileset, int tileSize, Texture2D treeTexture)
        {
            this.Tileset = tileset;
            this._treeTexture = treeTexture;
            TileSize = tileSize;
            WorldHeight = 120;
            EnsureWorldGenerated(Vector2.Zero);
        }

        public void gearteath(SpriteBatch spriteBatch, int indextile, int indexgress, int indexstone, int TileSize, Vector2 cameraPosition)
        {
            int centerX = (int)(cameraPosition.X / TileSize);
            int minX = centerX - viewRange;
            int maxX = centerX + viewRange;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = 0; y <= WorldHeight; y++)
                {
                    Vector2 position = new Vector2(
                        x * TileSize - xOffset,
                        y * TileSize + yOffset
                    );

                    if (y == 0)
                    {
                        Tileset.DrawTile(
                            spriteBatch,
                            indexgress,
                            position,
                            true,
                            false
                        );
                    }
                    else if (ShouldBeStone(x, y))
                    {
                        Tileset.DrawTile(
                            spriteBatch,
                            indexstone,
                            position,
                            false,
                            true
                        );
                    }
                    else
                    {
                        Tileset.DrawTile(
                           spriteBatch,
                           indextile,
                           position,
                           false,
                           false
                       );
                    }
                }
            }
        }

        public bool IsSolidTile(int x, int y)
        {
            // figure out which chunk x lives in
            int chunkX = x / ChunkSize;
            int localX = x % ChunkSize;
            if (localX < 0) { localX += ChunkSize; chunkX--; }

            // **new**: guarantee we have a chunk there
            EnsureChunkExists(chunkX);

            // now do your old bounds checks…
            if (y < 0 || y >= WorldHeight)
                return false;

            Chunk chunk = chunks[chunkX];
            Tiles tileType = chunk.GetTile(localX, y);
            return tileType != Tiles.None;
        }

        public Point ScreenToTileCoordinates(int screenX, int screenY)
        {
            // Constants that match the drawing offsets in Player and Map
            const float drawYOffset = 460f;   // Player's DrawYOffset
            const float drawXOffset = -480f;  // Player's DrawXOffset

            float worldX = screenX - Player.DrawXOffset + xOffset;
            float worldY = screenY - Player.DrawYOffset - yOffset;

            int tileX = (int)Math.Floor(worldX / TileSize);
            int tileY = (int)Math.Floor(worldY / TileSize);

            return new Point(tileX, tileY);
        }

        public bool IsTileInRange(Vector2 playerPosition, Point tileCoords, float range)
        {
            Vector2 playerCenter = new Vector2(
                playerPosition.X + Player.FrameWidth / 2,
                playerPosition.Y + Player.FrameHeight / 2
            );

            Vector2 tileCenter = new Vector2(
                tileCoords.X * TileSize + TileSize / 2,
                tileCoords.Y * TileSize + TileSize / 2
            );

            return Vector2.Distance(playerCenter, tileCenter) <= range;
        }

        public void DrawTileHighlight(SpriteBatch spriteBatch, Point tileCoords, Color highlightColor, float alpha)
        {
            const float drawYOffset = 460f;   // Player's DrawYOffset
            const float drawXOffset = -480f;  // Player's DrawXOffset

            // Create a highlight rectangle
            Rectangle tileRect = new Rectangle(
                (int)(tileCoords.X * TileSize - xOffset + drawXOffset),
                (int)(tileCoords.Y * TileSize + yOffset + drawYOffset),
                TileSize,
                TileSize
            );

            // Create a one-pixel white texture for highlighting
            Texture2D pixelTexture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            // Draw outline
            int outlineThickness = 2;
            Color outlineColor = highlightColor * alpha;

            // Top
            spriteBatch.Draw(pixelTexture, new Rectangle(tileRect.X, tileRect.Y, tileRect.Width, outlineThickness), outlineColor);
            // Bottom
            spriteBatch.Draw(pixelTexture, new Rectangle(tileRect.X, tileRect.Y + tileRect.Height - outlineThickness, tileRect.Width, outlineThickness), outlineColor);
            // Left
            spriteBatch.Draw(pixelTexture, new Rectangle(tileRect.X, tileRect.Y, outlineThickness, tileRect.Height), outlineColor);
            // Right
            spriteBatch.Draw(pixelTexture, new Rectangle(tileRect.X + tileRect.Width - outlineThickness, tileRect.Y, outlineThickness, tileRect.Height), outlineColor);
        }

        public Tiles GetTileType(int x, int y)
        {
            // Check if coordinates are out of bounds
            if (y < 0 || y >= WorldHeight)
                return Tiles.None;

            // Get chunk coordinates
            int chunkX = x / ChunkSize;
            int localX = x % ChunkSize;
            if (localX < 0) { localX += ChunkSize; chunkX--; }

            // Ensure chunk exists
            EnsureChunkExists(chunkX);

            // Return the tile type
            return chunks[chunkX].GetTile(localX, y);
        }

        public void RemoveTile(int x, int y)
        {
            // Don't allow removing tiles that are out of bounds
            if (y < 0 || y >= WorldHeight)
                return;

            // Get chunk coordinates
            int chunkX = x / ChunkSize;
            int localX = x % ChunkSize;
            if (localX < 0) { localX += ChunkSize; chunkX--; }

            // Make sure the chunk exists
            EnsureChunkExists(chunkX);

            // Only remove if there's actually a tile there
            Chunk chunk = chunks[chunkX];
            if (chunk.GetTile(localX, y) != Tiles.None)
            {
                // Set tile to None (empty)
                chunk.SetTile(localX, y, Tiles.None);

                // If this was in worldData or stoneData, update those too
                if (worldData.ContainsKey(x))
                {
                    // If we're removing grass, update grass positions
                    int yPos = surfaceLevel - worldData[x].Height;
                    if (y == yPos && worldData[x].GrassYPositions.Contains(y))
                    {
                        worldData[x].GrassYPositions.Remove(y);
                    }
                }

                if (stoneData.ContainsKey(x))
                {
                    // Remove from stone variations if it was there
                    stoneData[x].StoneVariations.Remove(new Point(x, y));

                    // Remove from ore variations if it was there
                    stoneData[x].GoldVariations.Remove(new Point(x, y));
                    stoneData[x].SilverVariations.Remove(new Point(x, y));

                    // Add to cave points if it was stone before
                    if (y > surfaceLevel + stoneData[x].StartDepth)
                    {
                        stoneData[x].CavePoints.Add(new Point(x, y));
                    }
                }
            }
        }

        // Place a tile at the specified world position
        public void PlaceTile(int x, int y, Tiles tileType)
        {
            // Don't allow placing tiles that are out of bounds
            if (y < 0 || y >= WorldHeight)
                return;

            // Get chunk coordinates
            int chunkX = x / ChunkSize;
            int localX = x % ChunkSize;
            if (localX < 0) { localX += ChunkSize; chunkX--; }

            // Make sure the chunk exists
            EnsureChunkExists(chunkX);

            // Check if the tile is currently empty
            Chunk chunk = chunks[chunkX];
            if (chunk.GetTile(localX, y) == Tiles.None)
            {
                // Place the new tile
                chunk.SetTile(localX, y, tileType);

                // Update worldData or stoneData based on tile type
                if (tileType == Tiles.grass)
                {
                    if (!worldData.ContainsKey(x))
                    {
                        worldData[x] = new MountainGroup
                        {
                            X = x,
                            Height = surfaceLevel - y,
                            PeakHeight = surfaceLevel - y,
                            HasGrass = true,
                            IsSurface = y == surfaceLevel,
                            GrassYPositions = new HashSet<int>()
                        };
                    }
                    worldData[x].GrassYPositions.Add(y);
                }
                else if (tileType == Tiles.stone)
                {
                    EnsureStoneGenerated(x);
                    stoneData[x].StoneVariations.Add(new Point(x, y));
                    stoneData[x].CavePoints.Remove(new Point(x, y));
                }
                else if (tileType == Tiles.gold)
                {
                    EnsureStoneGenerated(x);
                    stoneData[x].GoldVariations.Add(new Point(x, y));
                    stoneData[x].CavePoints.Remove(new Point(x, y));
                }
                else if (tileType == Tiles.silver)
                {
                    EnsureStoneGenerated(x);
                    stoneData[x].SilverVariations.Add(new Point(x, y));
                    stoneData[x].CavePoints.Remove(new Point(x, y));
                }
            }
        }

        // Helper method to ensure a chunk exists
        private void EnsureChunkExists(int chunkX)
        {
            if (!chunks.ContainsKey(chunkX))
            {
                chunks[chunkX] = new Chunk(ChunkSize, WorldHeight);
                // Initialize the chunk based on existing world data
                for (int x = 0; x < ChunkSize; x++)
                {
                    int worldX = chunkX * ChunkSize + x;
                    for (int y = 0; y < WorldHeight; y++)
                    {
                        // Determine tile type based on existing logic
                        Tiles tileType = DetermineTileType(worldX, y);
                        chunks[chunkX].SetTile(x, y, tileType);
                    }
                }
            }
        }

        // Helper method to determine tile type based on existing world data
        private Tiles DetermineTileType(int x, int y)
        {
            // Check if this is within a cave
            EnsureStoneGenerated(x);
            if (stoneData.TryGetValue(x, out StoneData data) && data.CavePoints.Contains(new Point(x, y)))
                return Tiles.None;

            // Check if this is an ore point
            bool isGold;
            if (IsOrePoint(x, y, out isGold))
                return isGold ? Tiles.gold : Tiles.silver;

            // Check if this is stone
            if (ShouldBeStone(x, y))
                return Tiles.stone;

            // Check mountain/surface
            if (worldData.TryGetValue(x, out MountainGroup mountain))
            {
                int yStart = Math.Max(surfaceLevel - mountain.Height, minWorldY);
                if (y < surfaceLevel)
                {
                    if (y >= yStart)
                    {
                        if (mountain.GrassYPositions.Contains(y))
                            return Tiles.grass;
                        return Tiles.dirt;
                    }
                    return Tiles.None; // Above mountain
                }
                else
                {
                    // Below surface
                    return Tiles.dirt;
                }
            }

            return y < surfaceLevel ? Tiles.None : (y == surfaceLevel ? Tiles.grass : Tiles.dirt);
        }

        private bool ShouldBeStone(int x, int y)
        {
            EnsureStoneGenerated(x);

            if (!stoneData.TryGetValue(x, out StoneData data))
                return false;

            // Check if this is a cave point
            if (data.CavePoints.Contains(new Point(x, y)))
                return false;

            if (y > surfaceLevel + data.StartDepth)
                return true;

            foreach (Point variation in data.StoneVariations)
            {
                if (y == variation.Y && x == variation.X)
                    return true;
            }

            return false;
        }

        private bool IsOrePoint(int x, int y, out bool isGold)
        {
            isGold = false;
            EnsureStoneGenerated(x);

            if (!stoneData.TryGetValue(x, out StoneData data))
                return false;

            if (data.GoldVariations.Contains(new Point(x, y)))
            {
                isGold = true;
                return true;
            }

            if (data.SilverVariations.Contains(new Point(x, y)))
            {
                isGold = false;
                return true;
            }

            return false;
        }

        private void EnsureStoneGenerated(int x)
        {
            if (!stoneData.ContainsKey(x))
            {
                StoneData data = new StoneData
                {
                    X = x,
                    StartDepth = StoneStartDepth + random.Next(-StoneVariation, StoneVariation + 1),
                    StoneVariations = new HashSet<Point>(),
                    GoldVariations = new HashSet<Point>(),
                    SilverVariations = new HashSet<Point>(),
                    CavePoints = new HashSet<Point>()
                };

                // Generate stone variations
                if (random.NextDouble() < 0.3)
                {
                    int formationHeight = random.Next(1, 5);
                    int baseDepth = data.StartDepth - formationHeight;

                    for (int h = 0; h < formationHeight; h++)
                    {
                        int yVar = random.Next(-1, 2);
                        Point p = new Point(x, surfaceLevel + baseDepth + h + yVar);
                        double chance = random.NextDouble();
                        if (chance < 0.05)
                            data.GoldVariations.Add(p);
                        else if (chance < 0.1)
                            data.SilverVariations.Add(p);
                        else
                            data.StoneVariations.Add(p);
                    }

                    if (random.NextDouble() < 0.5)
                    {
                        int sideX = x + (random.Next(2) * 2 - 1);
                        int sideHeight = random.Next(1, formationHeight);
                        for (int h = 0; h < sideHeight; h++)
                        {
                            Point p = new Point(sideX, surfaceLevel + baseDepth + formationHeight - sideHeight + h);
                            EnsureStoneGenerated(sideX); // Critical fix
                            double chance = random.NextDouble();
                            if (chance < 0.05)
                                stoneData[sideX].GoldVariations.Add(p);
                            else if (chance < 0.1)
                                stoneData[sideX].SilverVariations.Add(p);
                            else
                                stoneData[sideX].StoneVariations.Add(p);
                        }
                    }
                }

                stoneData[x] = data;
            }
        }

        private void GenerateCaves(int startX, int endX)
        {
            // Generate caves at random positions in the stone layer
            for (int x = startX; x <= endX; x += random.Next(5, 15))
            {
                if (random.NextDouble() < CaveFrequency)
                {
                    // Cave parameters
                    int caveSize = random.Next(MinCaveSize, MaxCaveSize + 1);
                    int centerY = surfaceLevel + StoneStartDepth + random.Next(5, WorldHeight - 30);
                    int centerX = x + random.Next(-5, 6);

                    // Generate a roughly circular cave
                    GenerateCaveShape(centerX, centerY, caveSize);
                }
            }
        }

        public void ApplyNetworkTileChange(int x, int y, Tiles tileType)
        {
            if (tileType == Tiles.None)
                RemoveTile(x, y);
            else
                PlaceTile(x, y, tileType);
        }

        // Modified tile interaction methods
        public void SetTile(int x, int y, int tileType)
        {
            if (tileType == (int)Tiles.None)
                RemoveTile(x, y);
            else
                PlaceTile(x, y, (Tiles)tileType);
        }
        private void GenerateCaveShape(int centerX, int centerY, int caveSize)
        {
            // Generate a roughly circular or oval cave
            double radiusX = caveSize * (0.5 + random.NextDouble() * 0.5);
            double radiusY = caveSize * (0.5 + random.NextDouble() * 0.5);

            // Generate the cave points
            for (int y = (int)(centerY - radiusY); y <= centerY + radiusY; y++)
            {
                for (int x = (int)(centerX - radiusX); x <= centerX + radiusX; x++)
                {
                    // Calculate distance from center (oval formula)
                    double distanceSquared = Math.Pow((x - centerX) / radiusX, 2) +
                                           Math.Pow((y - centerY) / radiusY, 2);

                    // Add noise to cave edge
                    double noiseFactor = 0.2 + random.NextDouble() * 0.4;

                    // If within the cave radius plus noise
                    if (distanceSquared <= 1 + noiseFactor)
                    {
                        Point cavePoint = new Point(x, y);

                        // Add this point to the cave
                        EnsureStoneGenerated(x);
                        if (stoneData.ContainsKey(x))
                        {
                            stoneData[x].CavePoints.Add(cavePoint);

                            // Chance to add ore in the cave walls
                            if (random.NextDouble() < OreChanceInCave)
                            {
                                // Check nearby points to place ore on cave walls
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    for (int dy = -1; dy <= 1; dy++)
                                    {
                                        if (dx == 0 && dy == 0) continue; // Skip the cave point itself

                                        Point wallPoint = new Point(x + dx, y + dy);

                                        // If point is not a cave, it's a wall
                                        EnsureStoneGenerated(wallPoint.X);
                                        if (stoneData.ContainsKey(wallPoint.X) &&
                                            !stoneData[wallPoint.X].CavePoints.Contains(wallPoint))
                                        {
                                            // Determine ore type (gold is more common)
                                            if (random.NextDouble() < GoldToSilverRatio)
                                            {
                                                stoneData[wallPoint.X].GoldVariations.Add(wallPoint);
                                                stoneData[wallPoint.X].StoneVariations.Remove(wallPoint);
                                            }
                                            else
                                            {
                                                stoneData[wallPoint.X].SilverVariations.Add(wallPoint);
                                                stoneData[wallPoint.X].StoneVariations.Remove(wallPoint);
                                            }

                                            break; // Only add one ore per cave wall section
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GenerateMountainChunk(int startX, int endX)
        {
            const int minLinesPerGroup = 10;
            const int maxLinesPerGroup = 30;
            const int deltaStep = 2;

            for (int x = startX; x <= endX; x++)
            {
                if (!worldData.ContainsKey(x))
                {
                    worldData[x] = new MountainGroup
                    {
                        X = x,
                        Height = 0,
                        PeakHeight = 0,
                        HasGrass = true,
                        IsSurface = true,
                        GrassYPositions = new HashSet<int>()
                    };
                }
                EnsureStoneGenerated(x);
            }

            int X = startX;
            while (X <= endX)
            {
                if (random.NextDouble() < 0.75)
                {
                    int linesPerGroup = random.Next(minLinesPerGroup, maxLinesPerGroup + 1);
                    int maxDelta = linesPerGroup / 2;
                    int spaceSize = random.Next(3, 10);

                    int peakHeight = random.Next(12, 30);
                    int maxPossibleHeight = baseY - minWorldY + 1;
                    peakHeight = Math.Min(peakHeight, maxPossibleHeight);

                    int baseHeight = peakHeight - (maxDelta * deltaStep);

                    // Track the last height to determine edges
                    int lastHeight = 0;
                    if (X > startX && worldData.ContainsKey(X - 1))
                    {
                        lastHeight = worldData[X - 1].Height;
                    }

                    for (int i = 0; i < linesPerGroup && X <= endX; i++, X++)
                    {
                        int delta = (i <= linesPerGroup / 2) ? i : (linesPerGroup - 1 - i);
                        int height = baseHeight + delta * deltaStep;
                        height = Math.Min(height, maxPossibleHeight);

                        worldData[X] = new MountainGroup
                        {
                            X = X,
                            Height = height,
                            PeakHeight = peakHeight,
                            HasGrass = true,
                            IsSurface = false,
                            GrassYPositions = new HashSet<int>()
                        };

                        // Add grass at the top position
                        int yStart = Math.Max(surfaceLevel - height, minWorldY);
                        worldData[X].GrassYPositions.Add(yStart);

                        // Check if this is an edge by comparing with the previous column
                        if (Math.Abs(height - lastHeight) >= 2)
                        {
                            // This is an edge - add grass on the sides if slope is steep enough
                            int minY = Math.Min(surfaceLevel - height, surfaceLevel - lastHeight);
                            int maxY = Math.Max(surfaceLevel - height, surfaceLevel - lastHeight);

                            // Add grass to some positions on the edge
                            for (int y = minY + 1; y <= maxY - 1; y++)
                            {
                                if (random.NextDouble() < 0.7) // 70% chance for each position to have grass
                                {
                                    worldData[X].GrassYPositions.Add(y);
                                }
                            }
                        }

                        lastHeight = height;
                        EnsureStoneGenerated(X);
                    }

                    X += spaceSize;
                }
                else
                {
                    X += random.Next(5, 15);
                }
            }

            for (int x = startX; x <= endX; x++)
            {
                if (random.NextDouble() < 0.2)
                {
                    int veinHeight = random.Next(3, 8);
                    int veinWidth = random.Next(2, 5);
                    int veinStartX = x;
                    int stoneStartY = surfaceLevel + StoneStartDepth - veinHeight;

                    for (int vx = veinStartX; vx < veinStartX + veinWidth && vx <= endX; vx++)
                    {
                        EnsureStoneGenerated(vx);
                        for (int vy = 0; vy < veinHeight; vy++)
                        {
                            int yVariation = random.Next(-1, 2);
                            stoneData[vx].StoneVariations.Add(new Point(vx, stoneStartY + vy + yVariation));
                        }
                    }

                    x += veinWidth + random.Next(5, 15);
                }
            }

            // Generate caves after terrain is established
            GenerateCaves(startX, endX);

            // Generate chunks for this region
            for (int x = startX; x <= endX; x++)
            {
                int chunkX = x / ChunkSize;
                EnsureChunkExists(chunkX);
            }
        }

        public void EnsureWorldGenerated(Vector2 cameraPosition)
        {
            int centerX = (int)(cameraPosition.X / TileSize);
            int generateRange = viewRange + 20;
            int minX = centerX - generateRange;
            int maxX = centerX + generateRange;

            // Generate terrain in chunks
            for (int x = minX; x <= maxX; x += 50)
            {
                int chunkStart = x;
                int chunkEnd = x + 50;

                bool needsGeneration = false;
                for (int checkX = chunkStart; checkX <= chunkEnd; checkX++)
                {
                    if (!worldData.ContainsKey(checkX) || !stoneData.ContainsKey(checkX))
                    {
                        needsGeneration = true;
                        break;
                    }
                }

                if (needsGeneration)
                {
                    GenerateMountainChunk(chunkStart, chunkEnd);
                    GenerateTrees(chunkStart, chunkEnd);
                }
            }
        }

        public void DrawMountain(SpriteBatch spriteBatch, int tileIndex, int grassTileIndex,
                           int stoneTileIndex, int goldTileIndex, int silverTileIndex,
                           int TileSize, Vector2 cameraPosition)
        {
            EnsureWorldGenerated(cameraPosition);

            int centerX = (int)(cameraPosition.X / TileSize);
            int minX = centerX - viewRange;
            int maxX = centerX + viewRange;

            int additionalYOffset = 10;

            for (int x = minX; x <= maxX; x++)
            {
                bool hasMountain = worldData.TryGetValue(x, out MountainGroup group) && !group.IsSurface;
                EnsureStoneGenerated(x);
                StoneData stone = stoneData[x];

                if (!hasMountain)
                {
                    Tileset.DrawTile(
                        spriteBatch,
                        grassTileIndex,
                        new Vector2(
                            x * TileSize - xOffset,
                            surfaceLevel * TileSize + 390 + additionalYOffset
                        ),
                        true
                    );
                }

                for (int y = surfaceLevel + 1; y <= surfaceLevel + WorldHeight; y++)
                {
                    Vector2 position = new Vector2(
                        x * TileSize - xOffset,
                        y * TileSize + 390 + additionalYOffset
                    );

                    Point currentPos = new Point(x, y);

                    // Skip drawing if this is a cave point
                    if (stone.CavePoints.Contains(currentPos))
                        continue;

                    // Check for ore points
                    if (stone.GoldVariations.Contains(currentPos))
                    {
                        Tileset.DrawTile(spriteBatch, goldTileIndex, position, false, false, true, false);
                        continue;
                    }
                    if (stone.SilverVariations.Contains(currentPos))
                    {
                        Tileset.DrawTile(spriteBatch, silverTileIndex, position, false, false, false, true);
                        continue;
                    }

                    bool isStone = y > surfaceLevel + stone.StartDepth || stone.StoneVariations.Contains(currentPos);
                    if (isStone)
                    {
                        Tileset.DrawTile(spriteBatch, stoneTileIndex, position, false, true, false, false);
                    }
                    else
                    {
                        // Only draw dirt if it's not stone
                        Tileset.DrawTile(spriteBatch, tileIndex, position);
                    }
                }
            }

            // Draw mountains and apply grass to edges
            for (int x = minX; x <= maxX; x++)
            {
                if (worldData.TryGetValue(x, out MountainGroup mountain) && !mountain.IsSurface)
                {
                    int yStart = Math.Max(surfaceLevel - mountain.Height, minWorldY);
                    for (int y = yStart; y < surfaceLevel; y++)
                    {
                        Vector2 position = new Vector2(
                            x * TileSize - xOffset,
                            y * TileSize + 390 + additionalYOffset
                        );

                        // Check if this Y position should have grass
                        if (mountain.GrassYPositions.Contains(y))
                        {
                            // Use index 0 for grass on mountain edges instead of grassTileIndex
                            if (y > yStart) // This is an edge, not the top
                                Tileset.DrawTile(spriteBatch, 0, position, true);
                            else // This is the top surface - use regular grass tile
                                Tileset.DrawTile(spriteBatch, grassTileIndex, position, true);
                        }
                        else
                        {
                            Tileset.DrawTile(spriteBatch, tileIndex, position);
                        }
                    }
                }
            }

            // Draw trees directly in the DrawMountain method
            DrawTrees(spriteBatch, cameraPosition);
        }

        private void GenerateTrees(int startX, int endX)
        {
            for (int x = startX; x <= endX; x++)
            {
                // Only place on flat surfaces (with some additional checks)
                if (worldData.TryGetValue(x, out MountainGroup group))
                {
                    // Check if this is a flat surface or the top of a mountain
                    bool canPlaceTree = group.IsSurface ||
                                       (x > 0 && worldData.TryGetValue(x - 1, out var leftGroup) &&
                                        leftGroup.Height == group.Height);

                    if (canPlaceTree && random.NextDouble() < TreeFrequency)
                    {
                        // Calculate proper Y position - either surface level or mountain top
                        int y = group.IsSurface ? surfaceLevel : (surfaceLevel - group.Height);

                        // Add to tree positions HashSet to ensure we don't place duplicates
                        Point treePos = new Point(x, y);
                        if (!treePositions.Contains(treePos))
                        {
                            treePositions.Add(treePos);
                        }
                    }
                }
            }
        }

        public void DrawTrees(SpriteBatch spriteBatch, Vector2 cameraPosition)
        {
            int centerX = (int)(cameraPosition.X / TileSize);
            int minX = centerX - viewRange;
            int maxX = centerX + viewRange;
            int additionalYOffset = 10;

            // Draw trees that are in view
            foreach (var pos in treePositions)
            {
                if (pos.X < minX || pos.X > maxX) continue;

                int worldY = pos.Y;

                // Calculate the scaled dimensions
                float scaledWidth = _treeTexture.Width * TreeScale;
                float scaledHeight = _treeTexture.Height * TreeScale;

                // Calculate proper position accounting for centering the scaled tree texture
                float drawX = pos.X * TileSize - xOffset - (scaledWidth - TileSize) / 2f;
                float drawY = worldY * TileSize + 390 + additionalYOffset - scaledHeight + TileSize;

                // Draw the tree with scaling
                spriteBatch.Draw(
                    _treeTexture,
                    new Vector2(drawX, drawY),
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    TreeScale,  // Apply the scaling factor
                    SpriteEffects.None,
                    0f
                );
            }
        }
    }
}