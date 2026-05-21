using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace pac_man.Game
{
    public static class TextureGenerator
    {
        // Pac-Man textures: [Direction][Frame]
        // Directions: 0 = Up, 1 = Down, 2 = Left, 3 = Right
        // Frames: 0 = Closed, 1 = Partial, 2 = Open
        public static Texture2D[][] PacmanTextures;

        // Ghost textures: [GhostColor][Direction][Frame]
        // GhostColor: 0 = Red (Blinky), 1 = Pink (Pinky), 2 = Cyan (Inky), 3 = Orange (Clyde)
        // Directions: 0 = Up, 1 = Down, 2 = Left, 3 = Right
        // Frames: 0 = Walk1, 1 = Walk2
        public static Texture2D[][][] GhostTextures;

        // Frightened ghost textures: [Frame]
        // Frame 0 = Blue, Frame 1 = White/Red (Blinking)
        public static Texture2D[] FrightenedGhostTextures;

        // Eaten ghost eyes textures: [Direction]
        // Directions: 0 = Up, 1 = Down, 2 = Left, 3 = Right
        public static Texture2D[] EatenGhostTextures;

        // Fruit textures
        public static Texture2D CherryTexture;

        // Pellet textures
        public static Texture2D PelletTexture;
        public static Texture2D EnergizerTexture;

        public static void GenerateAll(GraphicsDevice graphicsDevice)
        {
            // 1. Generate Pellets
            PelletTexture = GeneratePellet(graphicsDevice, 3, new Color(255, 183, 174)); // peach color dot
            EnergizerTexture = GenerateEnergizer(graphicsDevice, 8, new Color(255, 183, 174));

            // Load spritesheet from Content
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content", "spritesheet.png");
            if (!System.IO.File.Exists(path))
            {
                path = System.IO.Path.Combine("Content", "spritesheet.png");
            }

            Texture2D spritesheet;
            using (var stream = System.IO.File.OpenRead(path))
            {
                spritesheet = Texture2D.FromStream(graphicsDevice, stream);
            }

            // Helper function to extract a 16x16 sub-texture
            Texture2D Extract(int x, int y)
            {
                return ExtractSprite(graphicsDevice, spritesheet, x, y, 16, 16);
            }

            // 2. Load Pac-Man
            // Directions: 0 = Up, 1 = Down, 2 = Left, 3 = Right
            // Frames: 0 = Closed, 1 = Partial, 2 = Open
            PacmanTextures = new Texture2D[4][];
            for (int dir = 0; dir < 4; dir++)
            {
                PacmanTextures[dir] = new Texture2D[3];
                // Frame 0 (Closed) is a shared sprite at Row 0, Col 2 (x=42, y=2)
                PacmanTextures[dir][0] = Extract(42, 2);
            }

            // Up (dir 0)
            PacmanTextures[0][1] = Extract(2, 42);   // Row 2, Col 0
            PacmanTextures[0][2] = Extract(22, 42);  // Row 2, Col 1

            // Down (dir 1)
            PacmanTextures[1][1] = Extract(2, 62);   // Row 3, Col 0
            PacmanTextures[1][2] = Extract(22, 62);  // Row 3, Col 1

            // Left (dir 2)
            PacmanTextures[2][1] = Extract(2, 2);    // Row 0, Col 0 (Left facing)
            PacmanTextures[2][2] = Extract(22, 2);   // Row 0, Col 1

            // Right (dir 3)
            PacmanTextures[3][1] = Extract(2, 22);   // Row 1, Col 0 (Right facing)
            PacmanTextures[3][2] = Extract(22, 22);  // Row 1, Col 1

            // 3. Load Ghosts
            // GhostColor: 0 = Red (Blinky), 1 = Pink (Pinky), 2 = Cyan (Inky), 3 = Orange (Clyde)
            // Directions: 0 = Up, 1 = Down, 2 = Left, 3 = Right
            // Frames: 0 = Walk1, 1 = Walk2
            GhostTextures = new Texture2D[4][][];
            for (int g = 0; g < 4; g++)
            {
                GhostTextures[g] = new Texture2D[4][];
                int rowY = (4 + g) * 20 + 2; // Rows 4, 5, 6, 7

                // Up (0): Cols 0 and 1
                GhostTextures[g][0] = new Texture2D[2];
                GhostTextures[g][0][0] = Extract(2, rowY);
                GhostTextures[g][0][1] = Extract(22, rowY);

                // Down (1): Cols 2 and 3
                GhostTextures[g][1] = new Texture2D[2];
                GhostTextures[g][1][0] = Extract(42, rowY);
                GhostTextures[g][1][1] = Extract(62, rowY);

                // Left (2): Cols 4 and 5
                GhostTextures[g][2] = new Texture2D[2];
                GhostTextures[g][2][0] = Extract(82, rowY);
                GhostTextures[g][2][1] = Extract(102, rowY);

                // Right (3): Cols 6 and 7
                GhostTextures[g][3] = new Texture2D[2];
                GhostTextures[g][3][0] = Extract(122, rowY);
                GhostTextures[g][3][1] = Extract(142, rowY);
            }

            // 4. Load Frightened Ghost
            // Frame 0 = Blue, Frame 1 = White (Blinking)
            FrightenedGhostTextures = new Texture2D[2];
            FrightenedGhostTextures[0] = Extract(2, 8 * 20 + 2);   // Row 8, Col 0 (Blue Walk1)
            FrightenedGhostTextures[1] = Extract(42, 8 * 20 + 2);  // Row 8, Col 2 (White Walk1)

            // 5. Load Eaten Ghost Eyes
            // Directions: 0 = Up, 1 = Down, 2 = Left, 3 = Right
            EatenGhostTextures = new Texture2D[4];
            EatenGhostTextures[0] = Extract(2, 10 * 20 + 2);   // Up: Row 10, Col 0
            EatenGhostTextures[1] = Extract(22, 10 * 20 + 2);  // Down: Row 10, Col 1
            EatenGhostTextures[2] = Extract(42, 10 * 20 + 2);  // Left: Row 10, Col 2
            EatenGhostTextures[3] = Extract(62, 10 * 20 + 2);  // Right: Row 10, Col 3

            // 6. Load Cherry
            CherryTexture = ExtractSprite(graphicsDevice, spritesheet, 171, 182, 16, 16);
        }

        private static Texture2D ExtractSprite(GraphicsDevice gd, Texture2D source, int x, int y, int width, int height)
        {
            Texture2D cropped = new Texture2D(gd, width, height);
            Color[] data = new Color[width * height];
            source.GetData(0, new Rectangle(x, y, width, height), data, 0, data.Length);
            cropped.SetData(data);
            return cropped;
        }

        private static Texture2D GeneratePellet(GraphicsDevice gd, int size, Color color)
        {
            Texture2D tex = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];
            for (int i = 0; i < data.Length; i++) data[i] = color;
            tex.SetData(data);
            return tex;
        }

        private static Texture2D GenerateEnergizer(GraphicsDevice gd, int size, Color color)
        {
            Texture2D tex = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];
            float cx = size / 2.0f - 0.5f;
            float cy = size / 2.0f - 0.5f;
            float r = size / 2.0f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    if (dx * dx + dy * dy <= r * r)
                        data[y * size + x] = color;
                    else
                        data[y * size + x] = Color.Transparent;
                }
            }
            tex.SetData(data);
            return tex;
        }

    }
}
