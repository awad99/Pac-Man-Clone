using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace pac_man
{
    public class Pacman
    {
        public Vector2 Position; // Float position in pixels. 8 pixels = 1 tile
        public Direction CurrentDir;
        public Direction QueuedDir;

        public int TileX => (int)Math.Round(Position.X / ClassicMaze.TileSize);
        public int TileY => (int)Math.Round(Position.Y / ClassicMaze.TileSize);

        public int Lives;
        public bool IsDead;
        public float Speed;
        
        // Animation states
        private float _animTimer;
        public int AnimFrame; // 0 = Closed, 1 = Partial, 2 = Open
        private bool _animGrowing = true;

        // Start position constants
        private readonly Vector2 _startPosition = new Vector2(13.5f * ClassicMaze.TileSize, 26f * ClassicMaze.TileSize); // Tile 13.5 (centered), 26

        public Pacman()
        {
            Lives = 3;
            Reset();
        }

        public void Reset()
        {
            Position = _startPosition;
            CurrentDir = Direction.Left; // Starts moving Left
            QueuedDir = Direction.None;
            IsDead = false;
            Speed = 1.2f;
            AnimFrame = 0;
            _animTimer = 0f;
            _animGrowing = true;
        }

        public void HandleInput()
        {
            var kState = Keyboard.GetState();

            Direction newDir = Direction.None;
            if (kState.IsKeyDown(Keys.Up))    newDir = Direction.Up;
            if (kState.IsKeyDown(Keys.Down))  newDir = Direction.Down;
            if (kState.IsKeyDown(Keys.Left))  newDir = Direction.Left;
            if (kState.IsKeyDown(Keys.Right)) newDir = Direction.Right;

            if (newDir != Direction.None)
            {
                // If pressing opposite direction, turn immediately!
                if (IsOpposite(newDir, CurrentDir))
                {
                    CurrentDir = newDir;
                    QueuedDir = Direction.None;
                }
                else
                {
                    QueuedDir = newDir;
                }
            }
        }

        private bool IsOpposite(Direction d1, Direction d2)
        {
            if (d1 == Direction.Up && d2 == Direction.Down) return true;
            if (d1 == Direction.Down && d2 == Direction.Up) return true;
            if (d1 == Direction.Left && d2 == Direction.Right) return true;
            if (d1 == Direction.Right && d2 == Direction.Left) return true;
            return false;
        }

        public void Update(GameTime gameTime, ClassicMaze maze, bool isEatingDot, bool frightMode)
        {
            if (IsDead) return;

            // Define speed based on states
            if (frightMode)
                Speed = 1.4f; // Moves faster during fright mode
            else if (isEatingDot)
                Speed = 0.9f; // Slowed down slightly when eating a dot
            else
                Speed = 1.2f; // Normal speed

            // 1. Check if we can turn into the queued direction
            if (QueuedDir != Direction.None)
            {
                // We can turn if we are closely aligned with a tile center, 
                // and the target tile in the queued direction is open.
                float cx = TileX * ClassicMaze.TileSize;
                float cy = TileY * ClassicMaze.TileSize;

                // Check distance to current tile center
                if (Vector2.Distance(Position, new Vector2(cx, cy)) <= Speed)
                {
                    int nextX = TileX;
                    int nextY = TileY;
                    GetNextTileCoords(TileX, TileY, QueuedDir, out nextX, out nextY);

                    if (!maze.IsPacmanBlocked(nextX, nextY))
                    {
                        // Snap to center and turn!
                        Position = new Vector2(cx, cy);
                        CurrentDir = QueuedDir;
                        QueuedDir = Direction.None;
                    }
                }
            }

            // 2. Move in CurrentDir
            if (CurrentDir != Direction.None)
            {
                int targetX = TileX;
                int targetY = TileY;
                GetNextTileCoords(TileX, TileY, CurrentDir, out targetX, out targetY);

                bool blocked = maze.IsPacmanBlocked(targetX, targetY);

                if (blocked)
                {
                    // If blocked, we can only move up to the center of the current tile
                    float cx = TileX * ClassicMaze.TileSize;
                    float cy = TileY * ClassicMaze.TileSize;

                    // Vector from position to center
                    Vector2 toCenter = new Vector2(cx, cy) - Position;
                    float dist = toCenter.Length();

                    if (dist <= Speed)
                    {
                        // Snap to center and stop!
                        Position = new Vector2(cx, cy);
                    }
                    else
                    {
                        // Move towards center
                        toCenter.Normalize();
                        Position += toCenter * Speed;
                    }

                    // Stopped, no waka animation
                    AnimFrame = 0;
                }
                else
                {
                    // Move freely in current direction
                    Vector2 velocity = Vector2.Zero;
                    if (CurrentDir == Direction.Up)    velocity.Y = -Speed;
                    if (CurrentDir == Direction.Down)  velocity.Y = Speed;
                    if (CurrentDir == Direction.Left)  velocity.X = -Speed;
                    if (CurrentDir == Direction.Right) velocity.X = Speed;

                    Position += velocity;

                    // Handle tunnel wrap around
                    if (Position.X < -4)
                    {
                        Position.X = (ClassicMaze.Width - 1) * ClassicMaze.TileSize + 4;
                    }
                    else if (Position.X > (ClassicMaze.Width - 1) * ClassicMaze.TileSize + 4)
                    {
                        Position.X = -4;
                    }

                    // Animate mouth
                    _animTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (_animTimer > 0.05f) // Adjust speed of mouth waka
                    {
                        _animTimer = 0f;
                        if (_animGrowing)
                        {
                            AnimFrame++;
                            if (AnimFrame >= 2)
                            {
                                AnimFrame = 2;
                                _animGrowing = false;
                            }
                        }
                        else
                        {
                            AnimFrame--;
                            if (AnimFrame <= 0)
                            {
                                AnimFrame = 0;
                                _animGrowing = true;
                            }
                        }
                    }
                }
            }
        }

        public static void GetNextTileCoords(int x, int y, Direction dir, out int nx, out int ny)
        {
            nx = x;
            ny = y;
            if (dir == Direction.Up)    ny = y - 1;
            if (dir == Direction.Down)  ny = y + 1;
            if (dir == Direction.Left)  nx = x - 1;
            if (dir == Direction.Right) nx = x + 1;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (IsDead) return;

            // Map Direction enum to TextureGenerator arrays
            // Directions: 0 = Up, 1 = Down, 2 = Left, 3 = Right
            int dirIndex = 3; // default Right
            if (CurrentDir == Direction.Up)    dirIndex = 0;
            if (CurrentDir == Direction.Down)  dirIndex = 1;
            if (CurrentDir == Direction.Left)  dirIndex = 2;
            if (CurrentDir == Direction.Right) dirIndex = 3;

            Texture2D tex = TextureGenerator.PacmanTextures[dirIndex][AnimFrame];

            // Render Pac-Man. Position is top-left of his tile. Pac-Man sprite is 16x16.
            // Center Pac-Man on the 8x8 tile by shifting top-left by -4, -4.
            Vector2 drawPos = new Vector2(Position.X - 4, Position.Y - 4);
            spriteBatch.Draw(tex, drawPos, Color.White);
        }
    }
}
