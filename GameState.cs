namespace pac_man
{
    public enum GameState
    {
        StartScreen,
        ReadyWait,
        Playing,
        FrightenedFreeze,
        PacmanDeath,
        LevelComplete,
        GameOver
    }

    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
}
