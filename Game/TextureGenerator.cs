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

            // 2. Generate Pac-Man
            PacmanTextures = new Texture2D[4][];
            for (int dir = 0; dir < 4; dir++)
            {
                PacmanTextures[dir] = new Texture2D[3];
                for (int frame = 0; frame < 3; frame++)
                {
                    PacmanTextures[dir][frame] = GeneratePacmanSprite(graphicsDevice, dir, frame);
                }
            }

            // 3. Generate Ghosts
            GhostTextures = new Texture2D[4][][];
            Color[] ghostColors = { Color.Red, new Color(255, 182, 193), Color.Cyan, new Color(255, 165, 0) };
            for (int g = 0; g < 4; g++)
            {
                GhostTextures[g] = new Texture2D[4][];
                for (int dir = 0; dir < 4; dir++)
                {
                    GhostTextures[g][dir] = new Texture2D[2];
                    for (int frame = 0; frame < 2; frame++)
                    {
                        GhostTextures[g][dir][frame] = GenerateGhostSprite(graphicsDevice, ghostColors[g], dir, frame);
                    }
                }
            }

            // 4. Generate Frightened Ghost
            FrightenedGhostTextures = new Texture2D[2];
            FrightenedGhostTextures[0] = GenerateFrightenedGhostSprite(graphicsDevice, new Color(33, 33, 255), Color.White, 0); // Blue with white eyes/wiggle
            FrightenedGhostTextures[1] = GenerateFrightenedGhostSprite(graphicsDevice, Color.White, Color.Red, 1); // White with red eyes/wiggle (blinking)

            // 5. Generate Eaten Ghost Eyes
            EatenGhostTextures = new Texture2D[4];
            for (int dir = 0; dir < 4; dir++)
            {
                EatenGhostTextures[dir] = GenerateEatenGhostSprite(graphicsDevice, dir);
            }

            // 6. Generate Cherry
            CherryTexture = GenerateCherrySprite(graphicsDevice);
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

        private static Texture2D GeneratePacmanSprite(GraphicsDevice gd, int dir, int frame)
        {
            int size = 16;
            Texture2D tex = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];
            float cx = 7.5f;
            float cy = 7.5f;
            float r = 7.0f;

            // Mouth angle width in radians
            // Frame 0 = closed, Frame 1 = partial, Frame 2 = fully open
            float mouthAngleWidth = 0f;
            if (frame == 1) mouthAngleWidth = (float)Math.PI / 4.0f; // 45 degrees total
            if (frame == 2) mouthAngleWidth = (float)Math.PI / 2.0f; // 90 degrees total

            // Target directions: 0 = Up, 1 = Down, 2 = Left, 3 = Right
            float targetAngle = 0f;
            if (dir == 0) targetAngle = -(float)Math.PI / 2f; // -90 deg
            if (dir == 1) targetAngle = (float)Math.PI / 2f;  // 90 deg
            if (dir == 2) targetAngle = (float)Math.PI;       // 180 deg
            if (dir == 3) targetAngle = 0f;                   // 0 deg

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float distSq = dx * dx + dy * dy;

                    if (distSq <= r * r)
                    {
                        // Check if inside the mouth slice
                        if (frame > 0)
                        {
                            float angle = (float)Math.Atan2(dy, dx);
                            float diff = angle - targetAngle;

                            // Normalize diff to -PI to PI
                            while (diff < -Math.PI) diff += 2 * (float)Math.PI;
                            while (diff > Math.PI) diff -= 2 * (float)Math.PI;

                            if (Math.Abs(diff) < mouthAngleWidth / 2.0f)
                            {
                                data[y * size + x] = Color.Transparent;
                                continue;
                            }
                        }
                        data[y * size + x] = Color.Yellow;
                    }
                    else
                    {
                        data[y * size + x] = Color.Transparent;
                    }
                }
            }
            tex.SetData(data);
            return tex;
        }

        private static Texture2D GenerateGhostSprite(GraphicsDevice gd, Color bodyColor, int dir, int frame)
        {
            int size = 16;
            Texture2D tex = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Default transparent
                    Color c = Color.Transparent;

                    float dx = x - 7.5f;
                    float dy = y - 7.5f;

                    // 1. Ghost Head (semi-circle on top)
                    if (y <= 7 && dx * dx + dy * dy <= 7.2f * 7.2f)
                    {
                        c = bodyColor;
                    }
                    // 2. Ghost Body (rectangle in middle)
                    else if (y > 7 && y < 13 && x >= 1 && x <= 14)
                    {
                        c = bodyColor;
                    }
                    // 3. Ghost Feet (skirt wave)
                    else if (y >= 13 && y < 16 && x >= 1 && x <= 14)
                    {
                        // Frame 0 wave vs Frame 1 wave
                        bool drawSolid = false;
                        int relativeX = x - 1; // 0 to 13
                        if (frame == 0)
                        {
                            // Wave pattern 1: peaks at columns 1, 4, 7, 10, 13
                            int rem = relativeX % 4;
                            if (y == 13) drawSolid = true;
                            if (y == 14 && rem != 2) drawSolid = true;
                            if (y == 15 && (rem == 0 || rem == 3)) drawSolid = true;
                        }
                        else
                        {
                            // Wave pattern 2: shifted peaks
                            int rem = (relativeX + 2) % 4;
                            if (y == 13) drawSolid = true;
                            if (y == 14 && rem != 2) drawSolid = true;
                            if (y == 15 && (rem == 0 || rem == 3)) drawSolid = true;
                        }

                        if (drawSolid) c = bodyColor;
                    }

                    // 4. Ghost Eyes (white eyeballs and blue pupils)
                    // Offset based on direction (0 = Up, 1 = Down, 2 = Left, 3 = Right)
                    int eyeOffsetY = 0;
                    int eyeOffsetX = 0;
                    if (dir == 0) { eyeOffsetY = -2; eyeOffsetX = 0; }
                    if (dir == 1) { eyeOffsetY = 1; eyeOffsetX = 0; }
                    if (dir == 2) { eyeOffsetY = 0; eyeOffsetX = -1; }
                    if (dir == 3) { eyeOffsetY = 0; eyeOffsetX = 1; }

                    // Eyeball 1: left (columns 3-5, rows 4-6 approx)
                    // Eyeball 2: right (columns 9-11, rows 4-6 approx)
                    bool isLeftEyeball = (x >= 3 + eyeOffsetX && x <= 5 + eyeOffsetX && y >= 4 + eyeOffsetY && y <= 6 + eyeOffsetY);
                    bool isRightEyeball = (x >= 9 + eyeOffsetX && x <= 11 + eyeOffsetX && y >= 4 + eyeOffsetY && y <= 6 + eyeOffsetY);

                    // Cut corners of eyeball to make them rounded
                    if (isLeftEyeball)
                    {
                        if ((x == 3 + eyeOffsetX || x == 5 + eyeOffsetX) && (y == 4 + eyeOffsetY || y == 6 + eyeOffsetY))
                            isLeftEyeball = false;
                    }
                    if (isRightEyeball)
                    {
                        if ((x == 9 + eyeOffsetX || x == 11 + eyeOffsetX) && (y == 4 + eyeOffsetY || y == 6 + eyeOffsetY))
                            isRightEyeball = false;
                    }

                    if (isLeftEyeball || isRightEyeball)
                    {
                        c = Color.White;
                    }

                    // Pupils (blue, 1x1 or 1x2 pixel offset in look direction)
                    int pupilOffsetX = eyeOffsetX * 2;
                    int pupilOffsetY = eyeOffsetY * 2;
                    if (dir == 0) pupilOffsetY = -1; // tweak
                    if (dir == 1) pupilOffsetY = 1;

                    bool isLeftPupil = (x == 4 + eyeOffsetX + pupilOffsetX && y == 5 + eyeOffsetY + pupilOffsetY);
                    bool isRightPupil = (x == 10 + eyeOffsetX + pupilOffsetX && y == 5 + eyeOffsetY + pupilOffsetY);

                    if (isLeftPupil || isRightPupil)
                    {
                        c = Color.Blue;
                    }

                    data[y * size + x] = c;
                }
            }
            tex.SetData(data);
            return tex;
        }

        private static Texture2D GenerateFrightenedGhostSprite(GraphicsDevice gd, Color bodyColor, Color faceColor, int frame)
        {
            int size = 16;
            Texture2D tex = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = Color.Transparent;
                    float dx = x - 7.5f;
                    float dy = y - 7.5f;

                    // Head, body, feet
                    if (y <= 7 && dx * dx + dy * dy <= 7.2f * 7.2f)
                    {
                        c = bodyColor;
                    }
                    else if (y > 7 && y < 13 && x >= 1 && x <= 14)
                    {
                        c = bodyColor;
                    }
                    else if (y >= 13 && y < 16 && x >= 1 && x <= 14)
                    {
                        bool drawSolid = false;
                        int relativeX = x - 1;
                        int rem = (relativeX + frame) % 4; // wiggle with frame
                        if (y == 13) drawSolid = true;
                        if (y == 14 && rem != 2) drawSolid = true;
                        if (y == 15 && (rem == 0 || rem == 3)) drawSolid = true;

                        if (drawSolid) c = bodyColor;
                    }

                    // Face: Frightened face (wiggly mouth and small square eyes)
                    // Eyes at columns 4,5 and 9,10, row 5
                    bool isEye = ((x == 4 || x == 5 || x == 9 || x == 10) && y == 5);
                    if (isEye) c = faceColor;

                    // Wiggly mouth: row 8 to 10
                    // Red/Orange wiggle
                    bool isMouth = false;
                    if (y == 9 && (x == 3 || x == 5 || x == 7 || x == 9 || x == 11)) isMouth = true;
                    if (y == 10 && (x == 4 || x == 6 || x == 8 || x == 10)) isMouth = true;

                    if (isMouth) c = faceColor;

                    data[y * size + x] = c;
                }
            }
            tex.SetData(data);
            return tex;
        }

        private static Texture2D GenerateEatenGhostSprite(GraphicsDevice gd, int dir)
        {
            int size = 16;
            Texture2D tex = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = Color.Transparent;

                    int eyeOffsetY = 0;
                    int eyeOffsetX = 0;
                    if (dir == 0) { eyeOffsetY = -2; eyeOffsetX = 0; }
                    if (dir == 1) { eyeOffsetY = 1; eyeOffsetX = 0; }
                    if (dir == 2) { eyeOffsetY = 0; eyeOffsetX = -1; }
                    if (dir == 3) { eyeOffsetY = 0; eyeOffsetX = 1; }

                    bool isLeftEyeball = (x >= 3 + eyeOffsetX && x <= 5 + eyeOffsetX && y >= 4 + eyeOffsetY && y <= 6 + eyeOffsetY);
                    bool isRightEyeball = (x >= 9 + eyeOffsetX && x <= 11 + eyeOffsetX && y >= 4 + eyeOffsetY && y <= 6 + eyeOffsetY);

                    if (isLeftEyeball)
                    {
                        if ((x == 3 + eyeOffsetX || x == 5 + eyeOffsetX) && (y == 4 + eyeOffsetY || y == 6 + eyeOffsetY))
                            isLeftEyeball = false;
                    }
                    if (isRightEyeball)
                    {
                        if ((x == 9 + eyeOffsetX || x == 11 + eyeOffsetX) && (y == 4 + eyeOffsetY || y == 6 + eyeOffsetY))
                            isRightEyeball = false;
                    }

                    if (isLeftEyeball || isRightEyeball)
                    {
                        c = Color.White;
                    }

                    int pupilOffsetX = eyeOffsetX * 2;
                    int pupilOffsetY = eyeOffsetY * 2;
                    if (dir == 0) pupilOffsetY = -1;
                    if (dir == 1) pupilOffsetY = 1;

                    bool isLeftPupil = (x == 4 + eyeOffsetX + pupilOffsetX && y == 5 + eyeOffsetY + pupilOffsetY);
                    bool isRightPupil = (x == 10 + eyeOffsetX + pupilOffsetX && y == 5 + eyeOffsetY + pupilOffsetY);

                    if (isLeftPupil || isRightPupil)
                    {
                        c = Color.Blue;
                    }

                    data[y * size + x] = c;
                }
            }
            tex.SetData(data);
            return tex;
        }

        private static Texture2D GenerateCherrySprite(GraphicsDevice gd)
        {
            int size = 16;
            Texture2D tex = new Texture2D(gd, size, size);
            Color[] data = new Color[size * size];

            // Define cherry layout using programmatic pixel values
            // R = Red, G = Green, B = Brown/Stem, T = Transparent
            string[] grid = new string[]
            {
                "TTTTTTTTTTTTTTTT",
                "TTTTTTTTTTGGTTTT",
                "TTTTTTTTTGGTTTTT",
                "TTTTTTTTGGTGGTTT",
                "TTTTTTTGGTTGGTTT",
                "TTTTTTGGTTTGTTTT",
                "TTTTTGGTTTGGTTTT",
                "TTTTGGTTTTGTTTTT",
                "TTTGGTTTGGGGTTTT",
                "TTGGTTTGGGGGGTTT",
                "TTRRTTTGRRRRGTTT",
                "RRRRRTTGRRRRRRTT",
                "RRRRRTTGRRRRRRTT",
                "TRRRTTTTGRRRRGTT",
                "TTTTTTTTTGGGGTTT",
                "TTTTTTTTTTTTTTTT"
            };

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    char pixelChar = grid[y][x];
                    Color c = Color.Transparent;
                    if (pixelChar == 'R') c = Color.Red;
                    else if (pixelChar == 'G') c = Color.Green;
                    else if (pixelChar == 'B') c = new Color(139, 69, 19); // brown

                    data[y * size + x] = c;
                }
            }

            tex.SetData(data);
            return tex;
        }
    }
}
