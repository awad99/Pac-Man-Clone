using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace pac_man.Game
{
    public class ClassicMaze
    {
        public const int Width = 28;
        public const int Height = 36;
        public const int TileSize = 8; // Each tile is 8x8 pixels in virtual resolution

        // Template map layout
        private static readonly string[] MapLayout = {
            "                            ", // 0 (Blank for UI)
            "                            ", // 1 (Blank for UI)
            "                            ", // 2 (Blank for UI)
            "############################", // 3
            "#............##............#", // 4
            "#.####.#####.##.#####.####.#", // 5
            "#o####.#####.##.#####.####o#", // 6
            "#.####.#####.##.#####.####.#", // 7
            "#..........................#", // 8
            "#.####.##.########.##.####.#", // 9
            "#.####.##.########.##.####.#", // 10
            "#......##....##....##......#", // 11
            "######.##### ## #####.######", // 12
            "     #.##### ## #####.#     ", // 13
            "     #.##          ##.#     ", // 14
            "     #.## ggg==ggg ##.#     ", // 15 (Ghost House Top)
            "######.## g      g ##.######", // 16
            "      .   g  x x g   .      ", // 17 (Tunnel Row)
            "######.## g      g ##.######", // 18
            "     #.## gggggggg ##.#     ", // 19 (Ghost House Bottom)
            "     #.##          ##.#     ", // 20
            "     #.## ######## ##.#     ", // 21
            "######.## ######## ##.######", // 22
            "#............##............#", // 23
            "#.####.#####.##.#####.####.#", // 24
            "#.####.#####.##.#####.####.#", // 25
            "#o..##................##..o#", // 26
            "###.##.##.########.##.##.###", // 27
            "###.##.##.########.##.##.###", // 28
            "#......##....##....##......#", // 29
            "#.##########.##.##########.#", // 30
            "#.##########.##.##########.#", // 31
            "#..........................#", // 32
            "############################", // 33
            "                            ", // 34 (Blank for UI)
            "                            "  // 35 (Blank for UI)
        };

        private char[,] _grid;
        private Texture2D _pixel;
        public int TotalDots { get; private set; }

        public ClassicMaze(GraphicsDevice gd)
        {
            _pixel = new Texture2D(gd, 1, 1);
            _pixel.SetData(new[] { Color.White });

            _grid = new char[Width, Height];
            Reset();
        }

        public void Reset()
        {
            TotalDots = 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char c = MapLayout[y][x];
                    _grid[x, y] = c;
                    if (c == '.' || c == 'o')
                    {
                        TotalDots++;
                    }
                }
            }
        }

        public char GetTile(int x, int y)
        {
            // Wrap X for tunnel
            if (y == 17)
            {
                x = (x % Width + Width) % Width;
            }

            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return '#'; // solid wall out of bounds

            return _grid[x, y];
        }

        public void SetTile(int x, int y, char c)
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                _grid[x, y] = c;
            }
        }

        public bool IsWall(int x, int y)
        {
            char t = GetTile(x, y);
            return t == '#' || t == 'g';
        }

        public bool IsPacmanBlocked(int x, int y)
        {
            char t = GetTile(x, y);
            // Pac-man blocked by walls, ghost house walls, and ghost house gate
            return t == '#' || t == 'g' || t == '=';
        }

        public bool IsGhostBlocked(int x, int y, bool isEatenOrExiting)
        {
            char t = GetTile(x, y);
            if (t == '#') return true;
            if (t == 'g') return true;
            if (t == '=')
            {
                // Ghosts can only pass through the door if eaten or exiting
                return !isEatenOrExiting;
            }
            return false;
        }

        public bool EatDot(int x, int y, out bool isEnergizer)
        {
            isEnergizer = false;
            char t = GetTile(x, y);
            if (t == '.')
            {
                SetTile(x, y, ' ');
                TotalDots--;
                return true;
            }
            if (t == 'o')
            {
                SetTile(x, y, ' ');
                TotalDots--;
                isEnergizer = true;
                return true;
            }
            return false;
        }

        public void Draw(SpriteBatch spriteBatch, int frameCounter)
        {
            Color wallColor = new Color(33, 33, 255); // Classic neon blue
            Color ghostHouseWallColor = new Color(33, 33, 255); 
            Color gateColor = new Color(255, 182, 193); // Pink gate
            Color dotColor = new Color(255, 183, 174);  // Peach dot

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char t = _grid[x, y];
                    Rectangle rect = new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize);

                    if (t == '#')
                    {
                        // Draw beautiful retro double outlines
                        // Draw a solid background so lines look clean
                        // We check 4 directions to draw lines on borders
                        bool up = IsWall(x, y - 1);
                        bool down = IsWall(x, y + 1);
                        bool left = IsWall(x - 1, y);
                        bool right = IsWall(x + 1, y);

                        // Draw lines on boundaries that face path
                        if (!up)    DrawHorizontalLine(spriteBatch, x * TileSize, y * TileSize, TileSize, wallColor, 1);
                        if (!down)  DrawHorizontalLine(spriteBatch, x * TileSize, (y + 1) * TileSize - 1, TileSize, wallColor, 1);
                        if (!left)  DrawVerticalLine(spriteBatch, x * TileSize, y * TileSize, TileSize, wallColor, 1);
                        if (!right) DrawVerticalLine(spriteBatch, (x + 1) * TileSize - 1, y * TileSize, TileSize, wallColor, 1);
                    }
                    else if (t == 'g')
                    {
                        // Ghost house solid blue/indigo boundaries
                        bool up = GetTile(x, y - 1) != 'g' && GetTile(x, y - 1) != '=';
                        bool down = GetTile(x, y + 1) != 'g';
                        bool left = GetTile(x - 1, y) != 'g';
                        bool right = GetTile(x + 1, y) != 'g';

                        if (up)    DrawHorizontalLine(spriteBatch, x * TileSize, y * TileSize, TileSize, ghostHouseWallColor, 1);
                        if (down)  DrawHorizontalLine(spriteBatch, x * TileSize, (y + 1) * TileSize - 1, TileSize, ghostHouseWallColor, 1);
                        if (left)  DrawVerticalLine(spriteBatch, x * TileSize, y * TileSize, TileSize, ghostHouseWallColor, 1);
                        if (right) DrawVerticalLine(spriteBatch, (x + 1) * TileSize - 1, y * TileSize, TileSize, ghostHouseWallColor, 1);
                    }
                    else if (t == '=')
                    {
                        // Ghost house gate
                        DrawHorizontalLine(spriteBatch, x * TileSize, y * TileSize + TileSize / 2 - 1, TileSize, gateColor, 2);
                    }
                    else if (t == '.')
                    {
                        // Normal pac-dot (2x2 square in center)
                        spriteBatch.Draw(TextureGenerator.PelletTexture, 
                            new Rectangle(x * TileSize + 3, y * TileSize + 3, 2, 2), 
                            dotColor);
                    }
                    else if (t == 'o')
                    {
                        // Energizer (8x8 circle centered, blinking every 10 frames)
                        if ((frameCounter / 15) % 2 == 0)
                        {
                            spriteBatch.Draw(TextureGenerator.EnergizerTexture, 
                                new Rectangle(x * TileSize, y * TileSize, 8, 8), 
                                dotColor);
                        }
                    }
                }
            }
        }

        // Level completed animation: maze walls flash white
        public void DrawFlash(SpriteBatch spriteBatch, bool flashWhite)
        {
            Color wallColor = flashWhite ? Color.White : new Color(33, 33, 255);

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    char t = _grid[x, y];
                    if (t == '#' || t == 'g')
                    {
                        bool up = IsWall(x, y - 1);
                        bool down = IsWall(x, y + 1);
                        bool left = IsWall(x - 1, y);
                        bool right = IsWall(x + 1, y);

                        if (!up)    DrawHorizontalLine(spriteBatch, x * TileSize, y * TileSize, TileSize, wallColor, 1);
                        if (!down)  DrawHorizontalLine(spriteBatch, x * TileSize, (y + 1) * TileSize - 1, TileSize, wallColor, 1);
                        if (!left)  DrawVerticalLine(spriteBatch, x * TileSize, y * TileSize, TileSize, wallColor, 1);
                        if (!right) DrawVerticalLine(spriteBatch, (x + 1) * TileSize - 1, y * TileSize, TileSize, wallColor, 1);
                    }
                    else if (t == '=')
                    {
                        DrawHorizontalLine(spriteBatch, x * TileSize, y * TileSize + TileSize / 2 - 1, TileSize, wallColor, 2);
                    }
                }
            }
        }

        private void DrawHorizontalLine(SpriteBatch sb, int x, int y, int length, Color color, int thickness)
        {
            sb.Draw(_pixel, new Rectangle(x, y, length, thickness), color);
        }

        private void DrawVerticalLine(SpriteBatch sb, int x, int y, int length, Color color, int thickness)
        {
            sb.Draw(_pixel, new Rectangle(x, y, thickness, length), color);
        }
    }
}
