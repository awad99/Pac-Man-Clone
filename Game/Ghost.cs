using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace pac_man.Game
{
    public enum GhostState
    {
        InsideHouse,
        ExitingHouse,
        Scatter,
        Chase,
        Frightened,
        Eaten
    }

    public class Ghost
    {
        public int Index; // 0 = Blinky (Red), 1 = Pinky (Pink), 2 = Inky (Cyan), 3 = Clyde (Orange)
        public string Name;
        public Color BodyColor;

        public Vector2 Position;
        public Direction CurrentDir;
        public Direction NextDir;

        public int TileX => (int)Math.Round(Position.X / ClassicMaze.TileSize);
        public int TileY => (int)Math.Round(Position.Y / ClassicMaze.TileSize);

        public GhostState State;
        public GhostState PreviousState; // To restore after fright mode
        
        public int TargetX;
        public int TargetY;

        public float Speed;
        public int DotCounter; // Dots eaten tracking for release
        
        // Inside house bounce properties
        private float _bounceTimer;
        private float _bounceOffset;

        // Animation
        private float _animTimer;
        private int _animFrame;

        // Frightened state properties
        public float FrightTimer;
        private static Random _rand = new Random();

        // Spawn positions inside house
        private readonly Vector2[] _homePositions = {
            new Vector2(13.5f * ClassicMaze.TileSize, 14f * ClassicMaze.TileSize), // Blinky starts outside
            new Vector2(13.5f * ClassicMaze.TileSize, 17f * ClassicMaze.TileSize), // Pinky (middle)
            new Vector2(11.5f * ClassicMaze.TileSize, 17f * ClassicMaze.TileSize), // Inky (left)
            new Vector2(15.5f * ClassicMaze.TileSize, 17f * ClassicMaze.TileSize)  // Clyde (right)
        };

        // Scatter target corners
        private readonly Point[] _scatterTargets = {
            new Point(27, -3),  // Blinky: Top-Right
            new Point(2, -3),   // Pinky: Top-Left
            new Point(27, 32),  // Inky: Bottom-Right
            new Point(0, 32)    // Clyde: Bottom-Left
        };

        public Ghost(int index)
        {
            Index = index;
            Reset();
        }

        public void Reset()
        {
            Position = _homePositions[Index];
            
            if (Index == 0)
            {
                State = GhostState.Scatter;
                CurrentDir = Direction.Left;
                NextDir = Direction.Left;
            }
            else
            {
                State = GhostState.InsideHouse;
                CurrentDir = Direction.Up;
                NextDir = Direction.Up;
            }

            PreviousState = GhostState.Scatter;
            _bounceTimer = (float)(Index * 0.5); // Desynchronize bounces
            _bounceOffset = 0f;
            _animTimer = 0f;
            _animFrame = 0;
            FrightTimer = 0f;
            Speed = 1.0f;
            DotCounter = 0;
        }

        // Force immediate 180 turn (triggered when switching modes like Chase <-> Scatter)
        public void ForceTurn180()
        {
            if (State == GhostState.InsideHouse || State == GhostState.ExitingHouse || State == GhostState.Eaten)
                return;

            Direction opposite = GetOpposite(CurrentDir);
            CurrentDir = opposite;
            NextDir = opposite;
        }

        private Direction GetOpposite(Direction dir)
        {
            if (dir == Direction.Up) return Direction.Down;
            if (dir == Direction.Down) return Direction.Up;
            if (dir == Direction.Left) return Direction.Right;
            if (dir == Direction.Right) return Direction.Left;
            return Direction.None;
        }

        public void Update(GameTime gameTime, ClassicMaze maze, Pacman pacman, Ghost blinky, int dotsEaten, int level)
        {
            // 1. Update animations
            _animTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_animTimer > 0.15f)
            {
                _animTimer = 0f;
                _animFrame = 1 - _animFrame; // Alternate 0 and 1
            }

            // 2. State speed modifiers
            float baseSpeed = 1.0f;
            if (level > 4) baseSpeed = 1.1f;

            if (State == GhostState.InsideHouse)
            {
                Speed = 0.5f;
                UpdateInsideHouse(gameTime);
                return;
            }
            else if (State == GhostState.ExitingHouse)
            {
                Speed = 0.6f;
                UpdateExitingHouse(maze);
                return;
            }
            else if (State == GhostState.Frightened)
            {
                Speed = 0.6f;
                FrightTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (FrightTimer <= 0)
                {
                    State = PreviousState;
                    ForceTurn180();
                }
            }
            else if (State == GhostState.Eaten)
            {
                Speed = 4.0f; // Rapid return eyes!
                TargetX = 13;
                TargetY = 14; // Target the tile right above ghost house door
                if (TileX == 13 && TileY == 14)
                {
                    // Reached door! Re-enter house to regenerate
                    State = GhostState.InsideHouse;
                    DotCounter = 0;
                    Position = _homePositions[Index];
                    CurrentDir = Direction.Up;
                    NextDir = Direction.Up;
                    return;
                }
            }
            else // Chase or Scatter
            {
                // Blinky speed-up (Cruise Elroy mechanic)
                if (Index == 0 && maze.TotalDots < 20)
                {
                    Speed = baseSpeed + 0.2f;
                }
                else
                {
                    Speed = baseSpeed;
                }

                // Choose targets based on state
                if (State == GhostState.Scatter)
                {
                    TargetX = _scatterTargets[Index].X;
                    TargetY = _scatterTargets[Index].Y;
                }
                else // Chase mode! Unique original arcade targets
                {
                    CalculateChaseTarget(pacman, blinky);
                }
            }

            // 3. Normal pathfinding movement (grid alignment and steering)
            int targetTileX = TileX;
            int targetTileY = TileY;

            if (CurrentDir == Direction.Up)    targetTileY = (int)Math.Floor(Position.Y / ClassicMaze.TileSize);
            if (CurrentDir == Direction.Down)  targetTileY = (int)Math.Ceiling(Position.Y / ClassicMaze.TileSize);
            if (CurrentDir == Direction.Left)  targetTileX = (int)Math.Floor(Position.X / ClassicMaze.TileSize);
            if (CurrentDir == Direction.Right) targetTileX = (int)Math.Ceiling(Position.X / ClassicMaze.TileSize);

            float cx = targetTileX * ClassicMaze.TileSize;
            float cy = targetTileY * ClassicMaze.TileSize;

            float snapThreshold = Speed;
            if (Speed > 2.0f)
            {
                snapThreshold = Speed - 1.0f; // Prevent early snapping at halfway points for high-speed states (like Eaten)
            }

            if (Vector2.Distance(Position, new Vector2(cx, cy)) <= snapThreshold)
            {
                // Align with tile center
                Position = new Vector2(cx, cy);

                // Set direction to pre-calculated next direction
                CurrentDir = NextDir;

                // Handle tunnel wrap around
                if (Position.X < -4)
                {
                    Position.X = (ClassicMaze.Width - 1) * ClassicMaze.TileSize + 4;
                }
                else if (Position.X > (ClassicMaze.Width - 1) * ClassicMaze.TileSize + 4)
                {
                    Position.X = -4;
                }

                // Pre-calculate the NEXT direction for the upcoming tile
                NextDir = ChooseNextDirection(maze);
            }

            // Move
            Vector2 velocity = Vector2.Zero;
            if (CurrentDir == Direction.Up) velocity.Y = -Speed;
            if (CurrentDir == Direction.Down) velocity.Y = Speed;
            if (CurrentDir == Direction.Left) velocity.X = -Speed;
            if (CurrentDir == Direction.Right) velocity.X = Speed;

            Position += velocity;
        }

        private void UpdateInsideHouse(GameTime gameTime)
        {
            // Bounces up and down in its slot
            _bounceTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * 4f;
            _bounceOffset = (float)Math.Sin(_bounceTimer) * 4f;

            float homeX = _homePositions[Index].X;
            float homeY = _homePositions[Index].Y + _bounceOffset;
            Position = new Vector2(homeX, homeY);
        }

        private void UpdateExitingHouse(ClassicMaze maze)
        {
            // Move horizontally to center (13.5 * TileSize), then vertically up to (14 * TileSize)
            float targetCX = 13.5f * ClassicMaze.TileSize;
            float targetCY = 14f * ClassicMaze.TileSize;

            if (Math.Abs(Position.X - targetCX) > 0.5f)
            {
                // Move towards center column
                if (Position.X < targetCX) Position.X += Speed;
                else Position.X -= Speed;
            }
            else
            {
                Position.X = targetCX;
                if (Position.Y > targetCY)
                {
                    Position.Y -= Speed; // Move up through the door
                }
                else
                {
                    // Finished exiting!
                    Position.Y = targetCY;
                    State = GhostState.Scatter;
                    CurrentDir = Direction.Left;
                    NextDir = Direction.Left;
                }
            }
        }

        private void CalculateChaseTarget(Pacman pacman, Ghost blinky)
        {
            switch (Index)
            {
                case 0: // Blinky: directly target Pac-Man
                    TargetX = pacman.TileX;
                    TargetY = pacman.TileY;
                    break;

                case 1: // Pinky: 4 tiles ahead of Pac-Man (Up offset includes Left offset due to original bug!)
                    TargetX = pacman.TileX;
                    TargetY = pacman.TileY;
                    OffsetTileAhead(pacman.CurrentDir, 4, ref TargetX, ref TargetY);
                    break;

                case 2: // Inky: 2 tiles ahead of Pac-Man, offset from Blinky, doubled!
                    int pivotX = pacman.TileX;
                    int pivotY = pacman.TileY;
                    OffsetTileAhead(pacman.CurrentDir, 2, ref pivotX, ref pivotY);

                    // Vector from Blinky to Pivot
                    int dx = pivotX - blinky.TileX;
                    int dy = pivotY - blinky.TileY;

                    // Double the vector
                    TargetX = blinky.TileX + dx * 2;
                    TargetY = blinky.TileY + dy * 2;
                    break;

                case 3: // Clyde: Targets Pac-Man if distance > 8 tiles, else defaults to his Scatter corner
                    double dist = Math.Sqrt(Math.Pow(TileX - pacman.TileX, 2) + Math.Pow(TileY - pacman.TileY, 2));
                    if (dist > 8.0)
                    {
                        TargetX = pacman.TileX;
                        TargetY = pacman.TileY;
                    }
                    else
                    {
                        TargetX = _scatterTargets[3].X;
                        TargetY = _scatterTargets[3].Y;
                    }
                    break;
            }
        }

        private void OffsetTileAhead(Direction dir, int amount, ref int tx, ref int ty)
        {
            if (dir == Direction.Up)
            {
                ty -= amount;
                tx -= amount; // Original arcade bug: facing Up also offsets Left by same amount!
            }
            if (dir == Direction.Down)  ty += amount;
            if (dir == Direction.Left)  tx -= amount;
            if (dir == Direction.Right) tx += amount;
        }

        private Direction ChooseNextDirection(ClassicMaze maze)
        {
            // Evaluate neighbors of the tile we are entering
            int upcomingX = TileX;
            int upcomingY = TileY;
            Pacman.GetNextTileCoords(TileX, TileY, CurrentDir, out upcomingX, out upcomingY);

            // Possible choices: Up, Left, Down, Right (Tie-breaker order of original arcade)
            Direction[] candidates = { Direction.Up, Direction.Left, Direction.Down, Direction.Right };
            Direction bestDir = Direction.None;
            double minDist = double.MaxValue;

            // In frightened mode, we choose a random valid direction instead of shortest path
            if (State == GhostState.Frightened)
            {
                var validDirs = new List<Direction>();
                foreach (var d in candidates)
                {
                    if (d == GetOpposite(CurrentDir)) continue; // Can't turn 180 degrees

                    int nx, ny;
                    Pacman.GetNextTileCoords(upcomingX, upcomingY, d, out nx, out ny);
                    if (!maze.IsGhostBlocked(nx, ny, false))
                    {
                        validDirs.Add(d);
                    }
                }
                if (validDirs.Count > 0)
                {
                    return validDirs[_rand.Next(validDirs.Count)];
                }
                return GetOpposite(CurrentDir); // Fallback
            }

            // Normal Target-seeking AI
            foreach (var d in candidates)
            {
                if (d == GetOpposite(CurrentDir)) continue; // Can't turn back

                int nx, ny;
                Pacman.GetNextTileCoords(upcomingX, upcomingY, d, out nx, out ny);

                // Special check for forbidden upward turns (only in Chase or Scatter)
                if (d == Direction.Up && (State == GhostState.Chase || State == GhostState.Scatter))
                {
                    if ((upcomingX == 12 && upcomingY == 14) ||
                        (upcomingX == 15 && upcomingY == 14) ||
                        (upcomingX == 12 && upcomingY == 26) ||
                        (upcomingX == 15 && upcomingY == 26))
                    {
                        continue;
                    }
                }

                bool isEatenOrExiting = (State == GhostState.Eaten || State == GhostState.ExitingHouse);
                if (!maze.IsGhostBlocked(nx, ny, isEatenOrExiting))
                {
                    // Calculate Euclidean distance squared from candidate tile to Target
                    double distSq = Math.Pow(nx - TargetX, 2) + Math.Pow(ny - TargetY, 2);
                    if (distSq < minDist)
                    {
                        minDist = distSq;
                        bestDir = d;
                    }
                }
            }

            if (bestDir != Direction.None)
                return bestDir;

            return GetOpposite(CurrentDir); // Fallback if trapped
        }

        public void Draw(SpriteBatch spriteBatch, bool frightBlinkWhite)
        {
            Texture2D tex = null;

            if (State == GhostState.Eaten)
            {
                // Draw eyes only
                int dIdx = GetDirectionIndex(CurrentDir);
                tex = TextureGenerator.EatenGhostTextures[dIdx];
            }
            else if (State == GhostState.Frightened)
            {
                // Draw blue/white frightened ghost
                int fFrame = frightBlinkWhite ? 1 : 0;
                tex = TextureGenerator.FrightenedGhostTextures[fFrame];
            }
            else
            {
                // Draw normal ghost sprite with direction and walk frame
                int dIdx = GetDirectionIndex(CurrentDir);
                tex = TextureGenerator.GhostTextures[Index][dIdx][_animFrame];
            }

            Vector2 drawPos = new Vector2(Position.X - 4, Position.Y - 4);
            spriteBatch.Draw(tex, drawPos, Color.White);
        }

        private int GetDirectionIndex(Direction dir)
        {
            if (dir == Direction.Up) return 0;
            if (dir == Direction.Down) return 1;
            if (dir == Direction.Left) return 2;
            return 3; // Right
        }
    }
}
