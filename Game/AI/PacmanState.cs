using System;
using Microsoft.Xna.Framework;

namespace pac_man.Game.AI
{
    public struct GhostInfo
    {
        public int TileX;
        public int TileY;
        public Direction Direction;
        public GhostState State;
        public float FrightTimer;
    }

    public class PacmanState
    {
        public int PacmanTileX { get; set; }
        public int PacmanTileY { get; set; }
        public Direction PacmanDir { get; set; }
        public bool IsPacmanDead { get; set; }
        public int Lives { get; set; }

        public GhostInfo[] Ghosts { get; set; } = new GhostInfo[4];

        public int Level { get; set; }
        public int Score { get; set; }
        public int DotsEaten { get; set; }
        public int TotalDots { get; set; }

        public bool IsFruitActive { get; set; }
        public Point FruitTile { get; set; }

        // Maze reference for wall checks
        public ClassicMaze Maze { get; set; }
    }
}
