using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace pac_man.Game.AI
{
    public class CognitiveEngine
    {
        // Spatial Graph for Causal World Model
        public class SpatialGraph
        {
            public int Width = ClassicMaze.Width;
            public int Height = ClassicMaze.Height;
            private Dictionary<Point, List<Point>> _adjacency = new Dictionary<Point, List<Point>>();

            public SpatialGraph(ClassicMaze maze)
            {
                BuildGraph(maze);
            }

            private void BuildGraph(ClassicMaze maze)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (maze.IsPacmanBlocked(x, y)) continue;

                        Point current = new Point(x, y);
                        var neighbors = new List<Point>();

                        Point[] dirs = {
                            new Point(x, y - 1), // Up
                            new Point(x, y + 1), // Down
                            new Point(x - 1, y), // Left
                            new Point(x + 1, y)  // Right
                        };

                        foreach (var next in dirs)
                        {
                            int nx = next.X;
                            int ny = next.Y;

                            // Wrap tunnel
                            if (ny == 17)
                            {
                                if (nx < 0) nx = Width - 1;
                                if (nx >= Width) nx = 0;
                             }

                            if (nx >= 0 && nx < Width && ny >= 0 && ny < Height)
                            {
                                if (!maze.IsPacmanBlocked(nx, ny))
                                {
                                    neighbors.Add(new Point(nx, ny));
                                }
                            }
                        }

                        _adjacency[current] = neighbors;
                    }
                }
            }

            public List<Point> GetNeighbors(Point p)
            {
                if (_adjacency.TryGetValue(p, out var list))
                    return list;
                return new List<Point>();
            }

            // Computes distance to all reachable tiles in the maze from start tile
            public int[,] ComputeDistanceGrid(Point start, out Dictionary<Point, Point> parentMap)
            {
                int[,] distances = new int[Width, Height];
                parentMap = new Dictionary<Point, Point>();

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        distances[x, y] = 9999;
                    }
                }

                var queue = new Queue<Point>();
                queue.Enqueue(start);
                distances[start.X, start.Y] = 0;

                while (queue.Count > 0)
                {
                    Point current = queue.Dequeue();
                    int dist = distances[current.X, current.Y];

                    foreach (var neighbor in GetNeighbors(current))
                    {
                        if (distances[neighbor.X, neighbor.Y] == 9999)
                        {
                            distances[neighbor.X, neighbor.Y] = dist + 1;
                            parentMap[neighbor] = current;
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                return distances;
            }

            // Computes distance to all reachable tiles in the maze from start tile, respecting ghost movement direction (no 180s on first step)
            public int[,] ComputeGhostDistanceGrid(Point start, Direction currentDir, out Dictionary<Point, Point> parentMap)
            {
                int[,] distances = new int[Width, Height];
                parentMap = new Dictionary<Point, Point>();

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        distances[x, y] = 9999;
                    }
                }

                var queue = new Queue<Point>();
                distances[start.X, start.Y] = 0;

                Direction opposite = GetOppositeDirection(currentDir);
                var neighbors = GetNeighbors(start);

                foreach (var neighbor in neighbors)
                {
                    Direction dir = GetDirectionFromPoint(start, neighbor);
                    // Skip the 180-degree turn direction on the first step
                    if (dir == opposite && currentDir != Direction.None)
                    {
                        continue;
                    }

                    distances[neighbor.X, neighbor.Y] = 1;
                    parentMap[neighbor] = start;
                    queue.Enqueue(neighbor);
                }

                // If no moves were queued (fallback)
                if (queue.Count == 0)
                {
                    foreach (var neighbor in neighbors)
                    {
                        distances[neighbor.X, neighbor.Y] = 1;
                        parentMap[neighbor] = start;
                        queue.Enqueue(neighbor);
                    }
                }

                while (queue.Count > 0)
                {
                    Point current = queue.Dequeue();
                    int dist = distances[current.X, current.Y];

                    foreach (var neighbor in GetNeighbors(current))
                    {
                        if (distances[neighbor.X, neighbor.Y] == 9999)
                        {
                            distances[neighbor.X, neighbor.Y] = dist + 1;
                            parentMap[neighbor] = current;
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                return distances;
            }

            private Direction GetOppositeDirection(Direction dir)
            {
                if (dir == Direction.Up) return Direction.Down;
                if (dir == Direction.Down) return Direction.Up;
                if (dir == Direction.Left) return Direction.Right;
                if (dir == Direction.Right) return Direction.Left;
                return Direction.None;
            }


            public Direction GetDirectionFromPoint(Point from, Point to)
            {
                int dx = to.X - from.X;
                int dy = to.Y - from.Y;

                // Account for wrap-around tunnel at row 17
                if (from.Y == 17 && to.Y == 17)
                {
                    if (from.X == 0 && to.X == Width - 1) return Direction.Left;
                    if (from.X == Width - 1 && to.X == 0) return Direction.Right;
                }

                if (dx == 1) return Direction.Right;
                if (dx == -1) return Direction.Left;
                if (dy == 1) return Direction.Down;
                if (dy == -1) return Direction.Up;

                return Direction.None;
            }
        }

        // Structural Intents
        public enum Intent
        {
            Survival,
            Progress,
            Hunting
        }

        // --- Fields ---
        private SpatialGraph _graph;
        private Dictionary<ulong, float[]> _episodicMemory = new Dictionary<ulong, float[]>();
        private List<(ulong stateKey, Direction action)> _episodeHistory = new List<(ulong stateKey, Direction action)>();
        private List<Point> _positionHistory = new List<Point>();
        
        // Loop breaking state
        private Direction _forcedDirection = Direction.None;
        private int _forcedDirectionSteps = 0;
        private Point _lastTile = new Point(-1, -1);
        private Point _targetPellet = new Point(-1, -1);

        public int MemoryCount => _episodicMemory.Count;

        // Tuning parameters
        private const int DangerRadius = 6;
        private const float Gamma = 0.85f; // Credit assignment discount factor (applied per tile step)
        private const float DeathPenalty = -10000f;

        public CognitiveEngine(ClassicMaze maze)
        {
            _graph = new SpatialGraph(maze);
        }

        public void ResetEpisode()
        {
            _episodeHistory.Clear();
            _positionHistory.Clear();
            _forcedDirection = Direction.None;
            _forcedDirectionSteps = 0;
            _lastTile = new Point(-1, -1);
            _targetPellet = new Point(-1, -1);
        }

        // Learn immediately from death
        public void LearnFromDeath()
        {
            if (_episodeHistory.Count == 0) return;

            float penalty = DeathPenalty;
            // Iterate backward through tile steps (credit assignment is tile-based now)
            for (int i = _episodeHistory.Count - 1; i >= 0; i--)
            {
                var step = _episodeHistory[i];
                int actionIdx = GetActionIndex(step.action);

                if (actionIdx != -1)
                {
                    if (!_episodicMemory.ContainsKey(step.stateKey))
                    {
                        _episodicMemory[step.stateKey] = new float[4];
                    }

                    // Propagate the worst-case consequence
                    _episodicMemory[step.stateKey][actionIdx] = Math.Min(_episodicMemory[step.stateKey][actionIdx], penalty);
                }

                penalty *= Gamma;
                if (Math.Abs(penalty) < 10.0f) break; // Optimization threshold
            }

            _episodeHistory.Clear();
        }

        public Direction Think(PacmanState state)
        {
            // Sanitize input state coordinates to handle tunnel wrap-around safely
            int pacX = state.PacmanTileX;
            int pacY = state.PacmanTileY;
            WrapTileCoords(ref pacX, ref pacY);
            state.PacmanTileX = pacX;
            state.PacmanTileY = pacY;

            for (int i = 0; i < 4; i++)
            {
                var g = state.Ghosts[i];
                int gx = g.TileX;
                int gy = g.TileY;
                WrapTileCoords(ref gx, ref gy);
                g.TileX = gx;
                g.TileY = gy;
                state.Ghosts[i] = g;
            }

            Point pacPoint = new Point(state.PacmanTileX, state.PacmanTileY);
            bool isNewTile = (pacPoint != _lastTile);

            // Track position history to detect oscillations
            if (isNewTile)
            {
                _lastTile = pacPoint;
                _positionHistory.Add(pacPoint);
                if (_positionHistory.Count > 10)
                {
                    _positionHistory.RemoveAt(0);
                }

                if (_forcedDirectionSteps > 0)
                {
                    _forcedDirectionSteps--;
                    if (_forcedDirectionSteps == 0)
                    {
                        _forcedDirection = Direction.None;
                    }
                }
            }

            // 1. Build Causal distance grid from Pac-Man
            int[,] pacDistances = _graph.ComputeDistanceGrid(pacPoint, out var parentMap);

            // 2. Identify Intent & Ghost States
            Intent currentIntent = Intent.Progress;
            bool hostileGhostNear = false;
            
            // Check if any ghost is frightened (Hunting) or close and hostile (Survival)
            for (int i = 0; i < 4; i++)
            {
                GhostInfo g = state.Ghosts[i];
                if (g.State == GhostState.InsideHouse || g.State == GhostState.ExitingHouse || g.State == GhostState.Eaten)
                    continue;

                int dist = pacDistances[g.TileX, g.TileY];
                if (g.State == GhostState.Frightened)
                {
                    // Target ghost only if fright time is sufficient or we are close
                    if (g.FrightTimer > 1.5f || dist < 3)
                    {
                        if (dist < 15)
                        {
                            currentIntent = Intent.Hunting;
                        }
                    }
                }
                else // Hostile ghost
                {
                    if (dist <= DangerRadius)
                    {
                        hostileGhostNear = true;
                    }
                }
            }

            if (hostileGhostNear)
            {
                currentIntent = Intent.Survival;
            }

            // 3. Loop Breaking Logic
            if (_forcedDirection != Direction.None && _forcedDirectionSteps > 0)
            {
                Point nextP = GetNextPoint(pacPoint, _forcedDirection);
                if (!state.Maze.IsPacmanBlocked(nextP.X, nextP.Y))
                {
                    return _forcedDirection;
                }
                else
                {
                    _forcedDirection = Direction.None;
                    _forcedDirectionSteps = 0;
                }
            }

            if (DetectLoop())
            {
                var validDirs = GetWalkableDirections(pacPoint, state.Maze);
                if (validDirs.Count > 0)
                {
                    // Choose a direction that is different from current to break stagnation
                    Random rand = new Random();
                    _forcedDirection = validDirs[rand.Next(validDirs.Count)];
                    _forcedDirectionSteps = 2; // Maintain override for 2 tile entries

                    // Penalize loop action in episodic memory
                    ulong currentKey = CompressState(state, pacDistances, currentIntent);
                    int actionIdx = GetActionIndex(state.PacmanDir);
                    if (actionIdx != -1)
                    {
                        if (!_episodicMemory.ContainsKey(currentKey))
                            _episodicMemory[currentKey] = new float[4];
                        _episodicMemory[currentKey][actionIdx] -= 200f; // loop penalty
                    }

                    return _forcedDirection;
                }
            }

            // 4. Determine Strategic Target Direction (System 2)
            Direction targetDir = Direction.None;
            if (currentIntent == Intent.Hunting)
            {
                targetDir = RunSystem2Hunting(state, pacPoint, pacDistances, parentMap);
            }
            else
            {
                targetDir = RunSystem2Progress(state, pacPoint, pacDistances, parentMap);
            }

            // 5. Unified Decision Engine: Evaluate all walkable directions
            var walkable = GetWalkableDirections(pacPoint, state.Maze);
            if (walkable.Count == 0) return Direction.None;

            ulong stateKey = CompressState(state, pacDistances, currentIntent);
            float[] qValues = _episodicMemory.TryGetValue(stateKey, out var vals) ? vals : new float[4];

            // Compute exact distance grids for active hostile ghosts using the direction-constrained BFS
            var ghostDistGrids = new List<int[,]>();
            for (int i = 0; i < 4; i++)
            {
                GhostInfo g = state.Ghosts[i];
                if (g.State == GhostState.InsideHouse || g.State == GhostState.ExitingHouse || g.State == GhostState.Eaten || g.State == GhostState.Frightened)
                    continue;

                Point gPoint = new Point(g.TileX, g.TileY);
                ghostDistGrids.Add(_graph.ComputeGhostDistanceGrid(gPoint, g.Direction, out _));
            }

            Direction chosenDir = Direction.None;
            float bestScore = -9999999f;

            foreach (Direction dir in walkable)
            {
                Point neighbor = GetNextPoint(pacPoint, dir);

                // Compute safety score based on ghost distances
                int minGhostDist = 9999;
                foreach (var grid in ghostDistGrids)
                {
                    int dist = grid[neighbor.X, neighbor.Y];
                    if (dist < minGhostDist)
                    {
                        minGhostDist = dist;
                    }
                }

                // Safety penalty
                float safetyScore = 0f;
                if (minGhostDist <= 1) safetyScore = -25000f;      // Immediate collision path
                else if (minGhostDist == 2) safetyScore = -10000f; // Critical danger
                else if (minGhostDist == 3) safetyScore = -4000f;  // High danger
                else if (minGhostDist == 4) safetyScore = -1500f;  // Mid danger
                else if (minGhostDist == 5) safetyScore = -600f;   // Warning
                else if (minGhostDist == 6) safetyScore = -200f;   // Mild warning
                else if (minGhostDist == 7) safetyScore = -50f;    // Safe warning
                else safetyScore = 0f;

                // Dead End penalty (only active if ghost is nearby)
                float deadEndPenalty = 0f;
                var neighborWalkable = GetWalkableDirections(neighbor, state.Maze);
                bool isDeadEnd = true;
                foreach (var ndir in neighborWalkable)
                {
                    if (GetNextPoint(neighbor, ndir) != pacPoint)
                    {
                        isDeadEnd = false;
                        break;
                    }
                }
                if (isDeadEnd && minGhostDist < 7)
                {
                    deadEndPenalty = -6000f; // Prevent getting trapped
                }

                // Target alignment bonus
                float targetBonus = 0f;
                if (dir == targetDir && targetDir != Direction.None)
                {
                    targetBonus = 2000f;
                }

                // 180-degree turn penalty (momentum) to prevent shivering
                float turnPenalty = 0f;
                if (IsOpposite(dir, state.PacmanDir))
                {
                    turnPenalty = -800f;
                }

                // Episodic memory value
                int actionIdx = GetActionIndex(dir);
                float qValue = actionIdx != -1 ? qValues[actionIdx] : 0f;

                // Combine scores
                float totalScore = safetyScore + deadEndPenalty + targetBonus + turnPenalty + qValue;

                if (totalScore > bestScore)
                {
                    bestScore = totalScore;
                    chosenDir = dir;
                }
            }

            // Fallback
            if (chosenDir == Direction.None && walkable.Count > 0)
            {
                chosenDir = walkable[0];
            }

            // 6. Record experience ONLY when entering a new tile
            if (chosenDir != Direction.None && (isNewTile || _episodeHistory.Count == 0))
            {
                _episodeHistory.Add((stateKey, chosenDir));
                if (_episodeHistory.Count > 100)
                {
                    _episodeHistory.RemoveAt(0);
                }
            }

            // Log AI decision process for troubleshooting
            if (isNewTile)
            {
                try
                {
                    string logMsg = $"[{DateTime.Now:HH:mm:ss.fff}] Pacman at ({pacPoint.X}, {pacPoint.Y}) dir={state.PacmanDir} intent={currentIntent} targetDir={targetDir}\n";
                    for (int i = 0; i < 4; i++)
                    {
                        var g = state.Ghosts[i];
                        logMsg += $"  Ghost {i} ({g.State}): ({g.TileX}, {g.TileY}) dir={g.Direction}\n";
                    }
                    logMsg += $"  Walkable scores:\n";
                    foreach (Direction dir in walkable)
                    {
                        Point neighbor = GetNextPoint(pacPoint, dir);
                        int minGhostDist = 9999;
                        foreach (var grid in ghostDistGrids)
                        {
                            int dist = grid[neighbor.X, neighbor.Y];
                            if (dist < minGhostDist) minGhostDist = dist;
                        }
                        float safetyScore = 0f;
                        if (minGhostDist <= 1) safetyScore = -25000f;
                        else if (minGhostDist == 2) safetyScore = -10000f;
                        else if (minGhostDist == 3) safetyScore = -4000f;
                        else if (minGhostDist == 4) safetyScore = -1500f;
                        else if (minGhostDist == 5) safetyScore = -600f;
                        else if (minGhostDist == 6) safetyScore = -200f;
                        else if (minGhostDist == 7) safetyScore = -50f;

                        float deadEndPenalty = 0f;
                        var neighborWalkable = GetWalkableDirections(neighbor, state.Maze);
                        bool isDeadEnd = true;
                        foreach (var ndir in neighborWalkable)
                        {
                            if (GetNextPoint(neighbor, ndir) != pacPoint) { isDeadEnd = false; break; }
                        }
                        if (isDeadEnd && minGhostDist < 7) deadEndPenalty = -6000f;

                        float targetBonus = (dir == targetDir && targetDir != Direction.None) ? 2000f : 0f;
                        float turnPenalty = IsOpposite(dir, state.PacmanDir) ? -800f : 0f;
                        int actionIdx = GetActionIndex(dir);
                        float qValue = actionIdx != -1 ? qValues[actionIdx] : 0f;
                        float totalScore = safetyScore + deadEndPenalty + targetBonus + turnPenalty + qValue;

                        logMsg += $"    {dir}: total={totalScore} (safety={safetyScore}, dead={deadEndPenalty}, target={targetBonus}, turn={turnPenalty}, q={qValue}, minDist={minGhostDist})\n";
                    }
                    logMsg += $"  Chosen: {chosenDir}\n\n";
                    System.IO.File.AppendAllText("ai_choices.log", logMsg);
                }
                catch {}
            }

            return chosenDir;
        }

        private Direction RunSystem2Hunting(PacmanState state, Point pacPoint, int[,] pacDistances, Dictionary<Point, Point> parentMap)
        {
            Point nearestGhostTile = new Point(-1, -1);
            int minGhostDist = int.MaxValue;

            for (int i = 0; i < 4; i++)
            {
                GhostInfo g = state.Ghosts[i];
                if (g.State == GhostState.Frightened)
                {
                    int dist = pacDistances[g.TileX, g.TileY];
                    if (g.FrightTimer > 1.5f || dist < 3)
                    {
                        if (dist < minGhostDist)
                        {
                            minGhostDist = dist;
                            nearestGhostTile = new Point(g.TileX, g.TileY);
                        }
                    }
                }
            }

            if (nearestGhostTile.X == -1)
            {
                return RunSystem2Progress(state, pacPoint, pacDistances, parentMap);
            }

            // Reconstruct path
            Point curr = nearestGhostTile;
            Point prev = curr;
            while (curr != pacPoint)
            {
                prev = curr;
                if (!parentMap.TryGetValue(curr, out curr))
                {
                    break;
                }
            }

            return _graph.GetDirectionFromPoint(pacPoint, prev);
        }

        private Direction RunSystem2Progress(PacmanState state, Point pacPoint, int[,] pacDistances, Dictionary<Point, Point> parentMap)
        {
            // Hysteresis/Commitment check:
            // Check if current target pellet is still valid
            bool targetValid = false;
            if (_targetPellet.X != -1)
            {
                char tile = state.Maze.GetTile(_targetPellet.X, _targetPellet.Y);
                if ((tile == '.' || tile == 'o' || (state.IsFruitActive && state.FruitTile.X == _targetPellet.X && state.FruitTile.Y == _targetPellet.Y)) && pacDistances[_targetPellet.X, _targetPellet.Y] < 9999)
                {
                    targetValid = true;
                }
            }

            // Find closest target (pellet, energizer, or active fruit)
            Point closestTarget = new Point(-1, -1);
            int minTargetDist = int.MaxValue;

            for (int y = 0; y < ClassicMaze.Height; y++)
            {
                for (int x = 0; x < ClassicMaze.Width; x++)
                {
                    char tile = state.Maze.GetTile(x, y);
                    if (tile == '.' || tile == 'o' || (state.IsFruitActive && state.FruitTile.X == x && state.FruitTile.Y == y))
                    {
                        int dist = pacDistances[x, y];
                        if (dist < minTargetDist)
                        {
                            minTargetDist = dist;
                            closestTarget = new Point(x, y);
                        }
                    }
                }
            }

            // Only switch targets if the new one is significantly closer (prevents shivering/oscillations)
            if (targetValid && closestTarget.X != -1)
            {
                int currentTargetDist = pacDistances[_targetPellet.X, _targetPellet.Y];
                if (minTargetDist < currentTargetDist - 4)
                {
                    _targetPellet = closestTarget;
                }
            }
            else
            {
                _targetPellet = closestTarget;
            }

            try
            {
                string log = $"    [RunSystem2Progress] pacPoint=({pacPoint.X},{pacPoint.Y}) closestTarget=({closestTarget.X},{closestTarget.Y}) minTargetDist={minTargetDist} _targetPellet=({_targetPellet.X},{_targetPellet.Y}) targetValid={targetValid}\n";
                System.IO.File.AppendAllText("ai_choices.log", log);
            }
            catch {}

            if (_targetPellet.X == -1) return Direction.None;

            // Reconstruct path
            Point curr = _targetPellet;
            Point prev = curr;
            while (curr != pacPoint)
            {
                prev = curr;
                if (!parentMap.TryGetValue(curr, out curr))
                {
                    break;
                }
            }

            return _graph.GetDirectionFromPoint(pacPoint, prev);
        }

        // --- Helpers ---

        private bool DetectLoop()
        {
            if (_positionHistory.Count < 8) return false;

            // Count unique positions
            var unique = new HashSet<Point>(_positionHistory);
            if (unique.Count <= 2)
            {
                return true;
            }

            Point current = _positionHistory[_positionHistory.Count - 1];
            int count = 0;
            foreach (var p in _positionHistory)
            {
                if (p == current) count++;
            }

            return count > 3;
        }

        private ulong CompressState(PacmanState state, int[,] pacDistances, Intent intent)
        {
            ulong signature = 0;

            // 1. Local walls (bits 0-3)
            Point pacPoint = new Point(state.PacmanTileX, state.PacmanTileY);
            uint localWalls = 0;
            if (state.Maze.IsPacmanBlocked(pacPoint.X, pacPoint.Y - 1)) localWalls |= (1u << 0); // Up
            if (state.Maze.IsPacmanBlocked(pacPoint.X, pacPoint.Y + 1)) localWalls |= (1u << 1); // Down
            if (state.Maze.IsPacmanBlocked(pacPoint.X - 1, pacPoint.Y)) localWalls |= (1u << 2); // Left
            if (state.Maze.IsPacmanBlocked(pacPoint.X + 1, pacPoint.Y)) localWalls |= (1u << 3); // Right
            signature |= (ulong)localWalls;

            // 2. Intent target direction from BFS pathing (bits 4-6) - encoded for all intents to aid generalization
            Direction targetDir = Direction.None;
            _graph.ComputeDistanceGrid(pacPoint, out var parents);
            if (intent == Intent.Progress || intent == Intent.Survival)
            {
                targetDir = RunSystem2Progress(state, pacPoint, pacDistances, parents);
            }
            else if (intent == Intent.Hunting)
            {
                targetDir = RunSystem2Hunting(state, pacPoint, pacDistances, parents);
            }
            signature |= ((ulong)targetDir << 4);

            // 3. Closest hostile or active ghost (generalizes across all ghosts, bits 7-15)
            int closestGhostIdx = -1;
            int minGhostDist = 9999;
            bool secondGhostNear = false;

            for (int i = 0; i < 4; i++)
            {
                GhostInfo g = state.Ghosts[i];
                if (g.State == GhostState.InsideHouse || g.State == GhostState.ExitingHouse || g.State == GhostState.Eaten)
                    continue;

                int dist = pacDistances[g.TileX, g.TileY];
                if (dist < minGhostDist)
                {
                    if (minGhostDist < 6)
                    {
                        secondGhostNear = true;
                    }
                    minGhostDist = dist;
                    closestGhostIdx = i;
                }
                else if (dist < 6)
                {
                    secondGhostNear = true;
                }
            }

            int sector = 0;
            int distCat = 0;
            int gState = 0;

            if (closestGhostIdx != -1)
            {
                GhostInfo g = state.Ghosts[closestGhostIdx];

                // Distance Category (2 bits)
                if (minGhostDist < 2) distCat = 3;      // Critical
                else if (minGhostDist < 4) distCat = 2; // Danger
                else if (minGhostDist < 8) distCat = 1; // Warning
                else distCat = 0;                       // Safe

                // Relative sector/quadrant (4 bits)
                int dx = g.TileX - state.PacmanTileX;
                int dy = g.TileY - state.PacmanTileY;

                // Adjust tunnel wrap
                if (state.PacmanTileY == 17 && g.TileY == 17)
                {
                    if (dx > ClassicMaze.Width / 2) dx -= ClassicMaze.Width;
                    else if (dx < -ClassicMaze.Width / 2) dx += ClassicMaze.Width;
                }

                if (dx == 0 && dy == 0) sector = 0;
                else if (dy < 0 && dx == 0) sector = 1;  // North
                else if (dy < 0 && dx > 0)  sector = 2;  // North-East
                else if (dy == 0 && dx > 0) sector = 3;  // East
                else if (dy > 0 && dx > 0)  sector = 4;  // South-East
                else if (dy > 0 && dx == 0) sector = 5;  // South
                else if (dy > 0 && dx < 0)  sector = 6;  // South-West
                else if (dy == 0 && dx < 0) sector = 7;  // West
                else if (dy < 0 && dx < 0)  sector = 8;  // North-West

                // State (2 bits)
                if (g.State == GhostState.Frightened) gState = 1;
                else if (g.State == GhostState.Eaten) gState = 2;
                else gState = 0; // Hostile
            }

            signature |= ((ulong)sector << 7);
            signature |= ((ulong)distCat << 11);
            signature |= ((ulong)gState << 13);
            if (secondGhostNear) signature |= (1uL << 15);

            return signature;
        }

        private List<Direction> GetWalkableDirections(Point p, ClassicMaze maze)
        {
            var list = new List<Direction>();
            if (!maze.IsPacmanBlocked(p.X, p.Y - 1)) list.Add(Direction.Up);
            if (!maze.IsPacmanBlocked(p.X, p.Y + 1)) list.Add(Direction.Down);
            // Left (with wrap check)
            int lx = p.X - 1;
            if (p.Y == 17 && lx < 0) lx = ClassicMaze.Width - 1;
            if (!maze.IsPacmanBlocked(lx, p.Y)) list.Add(Direction.Left);
            // Right (with wrap check)
            int rx = p.X + 1;
            if (p.Y == 17 && rx >= ClassicMaze.Width) rx = 0;
            if (!maze.IsPacmanBlocked(rx, p.Y)) list.Add(Direction.Right);

            return list;
        }

        private Point GetNextPoint(Point p, Direction dir)
        {
            int nx = p.X;
            int ny = p.Y;
            if (dir == Direction.Up) ny--;
            if (dir == Direction.Down) ny++;
            if (dir == Direction.Left) nx--;
            if (dir == Direction.Right) nx++;

            // Wrap tunnel
            if (ny == 17)
            {
                if (nx < 0) nx = ClassicMaze.Width - 1;
                if (nx >= ClassicMaze.Width) nx = 0;
            }

            return new Point(nx, ny);
        }

        private int GetActionIndex(Direction dir)
        {
            if (dir == Direction.Up) return 0;
            if (dir == Direction.Down) return 1;
            if (dir == Direction.Left) return 2;
            if (dir == Direction.Right) return 3;
            return -1;
        }

        private bool IsOpposite(Direction d1, Direction d2)
        {
            if (d1 == Direction.Up && d2 == Direction.Down) return true;
            if (d1 == Direction.Down && d2 == Direction.Up) return true;
            if (d1 == Direction.Left && d2 == Direction.Right) return true;
            if (d1 == Direction.Right && d2 == Direction.Left) return true;
            return false;
        }

        private void WrapTileCoords(ref int x, ref int y)
        {
            if (y == 17)
            {
                if (x < 0) x = ClassicMaze.Width - 1;
                if (x >= ClassicMaze.Width) x = 0;
            }
            else
            {
                if (x < 0) x = 0;
                if (x >= ClassicMaze.Width) x = ClassicMaze.Width - 1;
            }
            if (y < 0) y = 0;
            if (y >= ClassicMaze.Height) y = ClassicMaze.Height - 1;
        }
    }
}
