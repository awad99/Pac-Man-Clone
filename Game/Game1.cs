using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
namespace pac_man.Game
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Visual Resolution and Scaling (Arcade size 224x288)
        public const int VirtualWidth = 224;
        public const int VirtualHeight = 288;
        private RenderTarget2D _renderTarget;

        // Game Entities
        private ClassicMaze _maze;
        private Pacman _pacman;
        private Ghost[] _ghosts; // 0 = Blinky, 1 = Pinky, 2 = Inky, 3 = Clyde

        // Game States and Timers
        private GameState _state;
        private float _stateTimer;
        private float _gameplayTimer; // Toggles Scatter/Chase
        private int _frameCounter;

        // Score Tracking
        private int _score;
        private int _highScore;
        private string _highScoreFile = "highscore.txt";

        // Level & Fruit Spawn
        private int _level;
        private bool _fruitActive;
        private float _fruitTimer;
        private Point _fruitTile = new Point(13, 20); // Just below ghost house
        private int _dotsEatenThisLevel;
        private bool _fruitEatenTextActive;
        private float _fruitEatenTextTimer;
        private int _fruitPoints;

        // Frightened State Details
        private int _ghostEatenValue; // 200 -> 400 -> 800 -> 1600
        private Point _eatenGhostPosition;
        private string _eatenGhostScoreText;
        private float _eatenFreezeTimer;

        // Audio Looping Instances
        private SoundEffectInstance _sirenInstance;
        private SoundEffectInstance _frightSirenInstance;
        private float _wakaTimer; // Limits waka play rate

        // Ghost house release variables
        private bool _globalDotCounterActive;
        private int _globalDotCounter;
        private float _ghostReleaseTimer;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Set retro aspect ratio and large window size
            _graphics.PreferredBackBufferWidth = 672; // 224 * 3
            _graphics.PreferredBackBufferHeight = 864; // 288 * 3
            _graphics.ApplyChanges();

            Window.Title = "Pac-Man Retro Arcade";
            Exiting += (s, e) => SaveHighScore();
        }

        protected override void Initialize()
        {
            _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);

            // Initialize retro drawing systems
            TextRenderer.Initialize(GraphicsDevice);

            _maze = new ClassicMaze(GraphicsDevice);
            _pacman = new Pacman();

            _ghosts = new Ghost[4];
            for (int i = 0; i < 4; i++)
            {
                _ghosts[i] = new Ghost(i);
            }

            _state = GameState.StartScreen;
            _level = 1;
            _score = 0;
            LoadHighScore();



            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // Generate sprites in RAM
            TextureGenerator.GenerateAll(GraphicsDevice);

            // Synthesize retro wave audio in RAM
            SoundSynth.Initialize();
        }

        private void LoadHighScore()
        {
            _highScore = 10000; // Retro Default
            try
            {
                if (File.Exists(_highScoreFile))
                {
                    string content = File.ReadAllText(_highScoreFile);
                    if (int.TryParse(content, out int val))
                    {
                        _highScore = val;
                    }
                }
            }
            catch
            {
                // Silence
            }
        }

        private void SaveHighScore()
        {
            try
            {
                File.WriteAllText(_highScoreFile, _highScore.ToString());
            }
            catch
            {
                // Silence
            }
        }

        private void StartNewGame()
        {
            _score = 0;
            _level = 1;
            _pacman.Lives = 3;
            StartLevel();
        }

        private void StartLevel()
        {
            _maze.Reset();
            _dotsEatenThisLevel = 0;
            _fruitActive = false;
            _fruitTimer = 0f;
            _fruitEatenTextActive = false;
            _gameplayTimer = 0f;

            ResetPositions();

            _globalDotCounterActive = false;
            _globalDotCounter = 0;
            _ghostReleaseTimer = 0f;
            for (int i = 0; i < 4; i++)
            {
                _ghosts[i].DotCounter = 0;
            }

            _state = GameState.ReadyWait;
            _stateTimer = 4.2f; // Freeze for length of starting theme

            PlaySound(SoundSynth.StartTheme);
        }

        private void ResetPositions()
        {
            _pacman.Reset();
            for (int i = 0; i < 4; i++)
            {
                _ghosts[i].Reset();
            }

            StopLoopingSounds();

            _globalDotCounterActive = true;
            _globalDotCounter = 0;
            _ghostReleaseTimer = 0f;
        }

        private int GetIndividualDotLimit(int ghostIndex)
        {
            if (ghostIndex == 1) return 0; // Pinky
            if (ghostIndex == 2) return (_level == 1) ? 30 : 0; // Inky
            if (ghostIndex == 3) return (_level == 1) ? 60 : (_level == 2 ? 50 : 0); // Clyde
            return 0; // Blinky
        }

        private void PlaySound(SoundEffect effect)
        {
            if (effect == null) return;
            try
            {
                effect.Play();
            }
            catch
            {
                // Missing audio driver/device
            }
        }

        private void StopLoopingSounds()
        {
            try
            {
                if (_sirenInstance != null && _sirenInstance.State == SoundState.Playing)
                    _sirenInstance.Stop();
                if (_frightSirenInstance != null && _frightSirenInstance.State == SoundState.Playing)
                    _frightSirenInstance.Stop();
            }
            catch { }
        }

        protected override void Update(GameTime gameTime)
        {
            _frameCounter++;

            // Global Keyboard Esc Exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                SaveHighScore();
                Exit();
            }

            switch (_state)
            {
                case GameState.StartScreen:
                    if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                    {
                        StartNewGame();
                    }
                    break;

                case GameState.ReadyWait:
                    _stateTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (_stateTimer <= 0)
                    {
                        _state = GameState.Playing;
                    }
                    break;

                case GameState.Playing:
                    UpdateGameplay(gameTime);
                    break;

                case GameState.FrightenedFreeze:
                    // Ghost eaten score banner freeze
                    _eatenFreezeTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (_eatenFreezeTimer <= 0)
                    {
                        _state = GameState.Playing;
                    }
                    break;

                case GameState.PacmanDeath:
                    _stateTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (_stateTimer <= 0)
                    {
                        _pacman.Lives--;
                        try
                        {
                            File.AppendAllText("ai_results.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Death. Lives left: {_pacman.Lives}, Score: {_score}, Level: {_level}\n");
                        }
                        catch { }

                        if (_pacman.Lives > 0)
                        {
                            ResetPositions();
                            _state = GameState.ReadyWait;
                            _stateTimer = 2.0f; // Brief freeze before restart
                        }
                        else
                        {
                            try
                            {
                                File.AppendAllText("ai_results.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - GAME OVER. Final Score: {_score}, Level: {_level}\n");
                            }
                            catch { }

                            _state = GameState.GameOver;
                            _stateTimer = 3.0f;
                            SaveHighScore();
                        }
                    }
                    break;

                case GameState.LevelComplete:
                    _stateTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (_stateTimer <= 0)
                    {
                        _level++;
                        StartLevel();
                    }
                    break;

                case GameState.GameOver:
                    _stateTimer -= (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if (_stateTimer <= 0)
                    {
                        _state = GameState.StartScreen;
                    }
                    break;
            }



            base.Update(gameTime);
        }

        private void UpdateGameplay(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _gameplayTimer += dt;

            // 1. Core input handle for steering Pacman
            _pacman.HandleInput();

            // 2. Play Background Siren
            UpdateSirenAudio();

            // 3. Determine Ghost Modes based on Level Timer
            // Classic sequence: Scatter 7s, Chase 20s, Scatter 7s, Chase 20s, Scatter 5s, Chase 20s, Scatter 5s, Chase permanent
            GhostState ghostTargetMode = GhostState.Chase;
            if (_gameplayTimer < 7f) ghostTargetMode = GhostState.Scatter;
            else if (_gameplayTimer < 27f) ghostTargetMode = GhostState.Chase;
            else if (_gameplayTimer < 34f) ghostTargetMode = GhostState.Scatter;
            else if (_gameplayTimer < 54f) ghostTargetMode = GhostState.Chase;
            else if (_gameplayTimer < 59f) ghostTargetMode = GhostState.Scatter;
            else if (_gameplayTimer < 79f) ghostTargetMode = GhostState.Chase;
            else if (_gameplayTimer < 84f) ghostTargetMode = GhostState.Scatter;

            // Propagate mode to ghosts (if not Frightened or Eaten)
            bool isFrightMode = false;
            for (int i = 0; i < 4; i++)
            {
                if (_ghosts[i].State == GhostState.Frightened)
                {
                    isFrightMode = true;
                }
                else if (_ghosts[i].State == GhostState.Scatter || _ghosts[i].State == GhostState.Chase)
                {
                    if (_ghosts[i].State != ghostTargetMode)
                    {
                        _ghosts[i].State = ghostTargetMode;
                        _ghosts[i].PreviousState = ghostTargetMode;
                        _ghosts[i].ForceTurn180();
                    }
                }
            }

            // 4. Ghost house release logic (dot counters and failsafe timer)
            // Increment failsafe timer
            _ghostReleaseTimer += dt;

            // Immediate release for Blinky (0) if he's inside the house
            if (_ghosts[0].State == GhostState.InsideHouse)
            {
                _ghosts[0].State = GhostState.ExitingHouse;
            }

            // If global counter is inactive, release active ghost if their individual limit is 0
            if (!_globalDotCounterActive)
            {
                int activeGhostIdx = -1;
                if (_ghosts[1].State == GhostState.InsideHouse) activeGhostIdx = 1;
                else if (_ghosts[2].State == GhostState.InsideHouse) activeGhostIdx = 2;
                else if (_ghosts[3].State == GhostState.InsideHouse) activeGhostIdx = 3;

                if (activeGhostIdx != -1 && GetIndividualDotLimit(activeGhostIdx) == 0)
                {
                    _ghosts[activeGhostIdx].State = GhostState.ExitingHouse;
                }
            }

            // Failsafe release timer check
            float releaseLimit = (_level >= 5) ? 3.0f : 4.0f;
            if (_ghostReleaseTimer >= releaseLimit)
            {
                _ghostReleaseTimer = 0f;
                int ghostToRelease = -1;
                if (_ghosts[1].State == GhostState.InsideHouse) ghostToRelease = 1;
                else if (_ghosts[2].State == GhostState.InsideHouse) ghostToRelease = 2;
                else if (_ghosts[3].State == GhostState.InsideHouse) ghostToRelease = 3;

                if (ghostToRelease != -1)
                {
                    _ghosts[ghostToRelease].State = GhostState.ExitingHouse;
                    if (ghostToRelease == 3)
                    {
                        _globalDotCounterActive = false;
                    }
                }
            }

            // 5. Update Pac-man position
            bool wasEating = false;
            bool gotDot = _maze.EatDot(_pacman.TileX, _pacman.TileY, out bool isEnergizer);
            if (gotDot)
            {
                wasEating = true;
                _dotsEatenThisLevel++;

                // Eating a dot resets the failsafe timer
                _ghostReleaseTimer = 0f;

                // Handle dot counter release triggers
                if (_globalDotCounterActive)
                {
                    _globalDotCounter++;
                    if (_ghosts[1].State == GhostState.InsideHouse && _globalDotCounter >= 7)
                    {
                        _ghosts[1].State = GhostState.ExitingHouse;
                    }
                    if (_ghosts[2].State == GhostState.InsideHouse && _globalDotCounter >= 17)
                    {
                        _ghosts[2].State = GhostState.ExitingHouse;
                    }
                    if (_ghosts[3].State == GhostState.InsideHouse && _globalDotCounter >= 32)
                    {
                        _ghosts[3].State = GhostState.ExitingHouse;
                        _globalDotCounterActive = false;
                    }
                    else if (_globalDotCounter >= 32)
                    {
                        _globalDotCounterActive = false;
                    }
                }
                else
                {
                    int activeGhostIdx = -1;
                    if (_ghosts[1].State == GhostState.InsideHouse) activeGhostIdx = 1;
                    else if (_ghosts[2].State == GhostState.InsideHouse) activeGhostIdx = 2;
                    else if (_ghosts[3].State == GhostState.InsideHouse) activeGhostIdx = 3;

                    if (activeGhostIdx != -1)
                    {
                        _ghosts[activeGhostIdx].DotCounter++;
                        if (_ghosts[activeGhostIdx].DotCounter >= GetIndividualDotLimit(activeGhostIdx))
                        {
                            _ghosts[activeGhostIdx].State = GhostState.ExitingHouse;
                        }
                    }
                }
                
                int pointVal = isEnergizer ? 50 : 10;
                AddScore(pointVal);

                // Play waka audio
                _wakaTimer += dt;
                if (_wakaTimer > 0.12f)
                {
                    _wakaTimer = 0f;
                    PlaySound(SoundSynth.Waka);
                }

                // Trigger Fruit spawn at 70 and 170 dots
                if (_dotsEatenThisLevel == 70 || _dotsEatenThisLevel == 170)
                {
                    _fruitActive = true;
                    _fruitTimer = 9.0f; // stays active for 9 seconds
                }

                // Handle Energizer frightened trigger
                if (isEnergizer)
                {
                    float fDuration = Math.Max(2.0f, 7.0f - _level * 0.5f);
                    _ghostEatenValue = 200; // reset eat multiplier

                    for (int i = 0; i < 4; i++)
                    {
                        if (_ghosts[i].State == GhostState.Chase || _ghosts[i].State == GhostState.Scatter || _ghosts[i].State == GhostState.Frightened)
                        {
                            _ghosts[i].PreviousState = _ghosts[i].State == GhostState.Frightened ? _ghosts[i].PreviousState : _ghosts[i].State;
                            _ghosts[i].State = GhostState.Frightened;
                            _ghosts[i].FrightTimer = fDuration;
                            _ghosts[i].ForceTurn180();
                        }
                    }
                }

                // Level Completed Check
                if (_maze.TotalDots == 0)
                {
                    try
                    {
                        File.AppendAllText("ai_results.log", $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Level Complete. Completed Level: {_level}, Score: {_score}\n");
                    }
                    catch { }

                    _state = GameState.LevelComplete;
                    _stateTimer = 2.5f; // Wall flashing delay
                    StopLoopingSounds();
                    return;
                }
            }

            _pacman.Update(gameTime, _maze, wasEating, isFrightMode);

            // 6. Update Fruits timer
            if (_fruitActive)
            {
                _fruitTimer -= dt;
                if (_fruitTimer <= 0)
                {
                    _fruitActive = false;
                }

                // Check collision with Fruit
                if (_pacman.TileX == _fruitTile.X && _pacman.TileY == _fruitTile.Y)
                {
                    _fruitActive = false;
                    PlaySound(SoundSynth.FruitEat);
                    
                    // Fruits score based on level
                    _fruitPoints = 100;
                    if (_level == 2) _fruitPoints = 300;
                    else if (_level == 3) _fruitPoints = 500;
                    else if (_level >= 4) _fruitPoints = 700;

                    AddScore(_fruitPoints);

                    _fruitEatenTextActive = true;
                    _fruitEatenTextTimer = 2.0f; // Show eaten score text
                }
            }

            if (_fruitEatenTextActive)
            {
                _fruitEatenTextTimer -= dt;
                if (_fruitEatenTextTimer <= 0)
                {
                    _fruitEatenTextActive = false;
                }
            }

            // 7. Update Ghosts Positions & Check Collisions
            for (int i = 0; i < 4; i++)
            {
                _ghosts[i].Update(gameTime, _maze, _pacman, _ghosts[0], _dotsEatenThisLevel, _level);

                // Collision Check
                if (_pacman.TileX == _ghosts[i].TileX && _pacman.TileY == _ghosts[i].TileY)
                {
                    if (_ghosts[i].State == GhostState.Frightened)
                    {
                        // Eat ghost!
                        PlaySound(SoundSynth.GhostEat);
                        _ghosts[i].State = GhostState.Eaten;
                        
                        // Snap immediately to nearest tile center to ensure perfect grid alignment at high speed
                        _ghosts[i].Position = new Vector2(
                            _ghosts[i].TileX * ClassicMaze.TileSize,
                            _ghosts[i].TileY * ClassicMaze.TileSize
                        );
                        
                        AddScore(_ghostEatenValue);

                        // Freeze gameplay briefly and display ghost eaten points
                        _state = GameState.FrightenedFreeze;
                        _eatenFreezeTimer = 0.5f;
                        _eatenGhostPosition = new Point(_ghosts[i].TileX * ClassicMaze.TileSize, _ghosts[i].TileY * ClassicMaze.TileSize);
                        _eatenGhostScoreText = _ghostEatenValue.ToString();

                        // Multiply value for next ghost eaten
                        _ghostEatenValue *= 2;
                    }
                    else if (_ghosts[i].State == GhostState.Chase || _ghosts[i].State == GhostState.Scatter)
                    {
                        // Pac-man Dies!
                        _state = GameState.PacmanDeath;
                        _stateTimer = 2.6f;
                        _pacman.IsDead = true;
                        StopLoopingSounds();
                        PlaySound(SoundSynth.Death);


                        return;
                    }
                }
            }
        }

        private void UpdateSirenAudio()
        {
            try
            {
                bool frightActive = false;
                for (int i = 0; i < 4; i++)
                {
                    if (_ghosts[i].State == GhostState.Frightened)
                        frightActive = true;
                }

                if (frightActive)
                {
                    // Play Frightened looping Siren
                    if (_sirenInstance != null && _sirenInstance.State == SoundState.Playing)
                        _sirenInstance.Stop();

                    if (_frightSirenInstance == null)
                    {
                        _frightSirenInstance = SoundSynth.FrightenedSiren.CreateInstance();
                        _frightSirenInstance.IsLooped = true;
                    }
                    if (_frightSirenInstance.State != SoundState.Playing)
                        _frightSirenInstance.Play();
                }
                else
                {
                    // Play normal Siren
                    if (_frightSirenInstance != null && _frightSirenInstance.State == SoundState.Playing)
                        _frightSirenInstance.Stop();

                    if (_sirenInstance == null)
                    {
                        _sirenInstance = SoundSynth.Siren.CreateInstance();
                        _sirenInstance.IsLooped = true;
                    }
                    if (_sirenInstance.State != SoundState.Playing)
                        _sirenInstance.Play();
                }
            }
            catch
            {
                // Audio device missing
            }
        }

        private void AddScore(int points)
        {
            int oldScore = _score;
            _score += points;
            
            // Check High Score
            if (_score > _highScore)
            {
                _highScore = _score;
            }

            // Extra Life at 10,000 points!
            if (oldScore < 10000 && _score >= 10000)
            {
                _pacman.Lives++;
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // First: Draw to the 224x288 Virtual Render Target
            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin();

            switch (_state)
            {
                case GameState.StartScreen:
                    DrawStartScreen();
                    break;

                case GameState.ReadyWait:
                    DrawGameplayBoard();
                    TextRenderer.DrawText(_spriteBatch, "READY!", 11 * ClassicMaze.TileSize - 4, 20 * ClassicMaze.TileSize, Color.Yellow, 1);
                    break;

                case GameState.Playing:
                    DrawGameplayBoard();
                    break;

                case GameState.FrightenedFreeze:
                    // Maze/Pacman stay drawn, but frozen. Draw the eaten score value on top
                    DrawGameplayBoard(drawGhosts: false); // draw board, pacman but hide other moving ghosts
                    TextRenderer.DrawText(_spriteBatch, _eatenGhostScoreText, _eatenGhostPosition.X - 4, _eatenGhostPosition.Y + 2, Color.Cyan, 1);
                    break;

                case GameState.PacmanDeath:
                    DrawGameplayBoard(drawPacman: false, drawGhosts: false);
                    if (_stateTimer > 1.6f)
                    {
                        // 1. Freeze phase: draw normal Pacman facing his last direction
                        int dirIndex = 3;
                        if (_pacman.CurrentDir == Direction.Up)    dirIndex = 0;
                        if (_pacman.CurrentDir == Direction.Down)  dirIndex = 1;
                        if (_pacman.CurrentDir == Direction.Left)  dirIndex = 2;
                        if (_pacman.CurrentDir == Direction.Right) dirIndex = 3;

                        Texture2D tex = TextureGenerator.PacmanTextures[dirIndex][0];
                        _spriteBatch.Draw(tex, new Vector2(_pacman.Position.X - 4, _pacman.Position.Y - 4), Color.White);
                    }
                    else if (_stateTimer > 0.4f)
                    {
                        // 2. Dissolve/Death Animation phase
                        float progress = (1.6f - _stateTimer) / 1.2f;
                        int frame = (int)(progress * 11);
                        frame = Math.Clamp(frame, 0, 10);
                        Texture2D deadTex = TextureGenerator.PacmanDeathTextures[frame];
                        _spriteBatch.Draw(deadTex, new Vector2(_pacman.Position.X - 4, _pacman.Position.Y - 4), Color.White);
                    }
                    // 3. Invisible phase: do not draw Pac-Man
                    break;

                case GameState.LevelComplete:
                    // Flash the maze walls white and blue
                    bool flashWhite = ((int)(_stateTimer * 4) % 2 == 0);
                    _maze.DrawFlash(_spriteBatch, flashWhite);
                    _pacman.Draw(_spriteBatch);
                    break;

                case GameState.GameOver:
                    DrawGameplayBoard(drawPacman: false);
                    TextRenderer.DrawText(_spriteBatch, "GAME OVER", 9 * ClassicMaze.TileSize, 20 * ClassicMaze.TileSize, Color.Red, 1);
                    break;
            }

            _spriteBatch.End();

            // Second: Draw Render Target to actual Window back-buffer (scaling pixel-perfect)
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);

            // Calculate scaled drawing rectangle maintaining aspect ratio
            int windowWidth = GraphicsDevice.Viewport.Width;
            int windowHeight = GraphicsDevice.Viewport.Height;
            
            float scaleX = (float)windowWidth / VirtualWidth;
            float scaleY = (float)windowHeight / VirtualHeight;
            float scale = Math.Min(scaleX, scaleY);

            int drawWidth = (int)(VirtualWidth * scale);
            int drawHeight = (int)(VirtualHeight * scale);
            int drawX = (windowWidth - drawWidth) / 2;
            int drawY = (windowHeight - drawHeight) / 2;

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, null, null);
            _spriteBatch.Draw(_renderTarget, new Rectangle(drawX, drawY, drawWidth, drawHeight), Color.White);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawStartScreen()
        {
            // Title PAC-MAN
            TextRenderer.DrawText(_spriteBatch, "PAC-MAN", 3 * ClassicMaze.TileSize, 6 * ClassicMaze.TileSize, Color.Yellow, 3);

            // Blinking start command
            if ((_frameCounter / 30) % 2 == 0)
            {
                TextRenderer.DrawText(_spriteBatch, "PRESS ENTER TO PLAY", 3 * ClassicMaze.TileSize + 10, 14 * ClassicMaze.TileSize, Color.Cyan, 1);
            }

            // High Score Banner
            TextRenderer.DrawText(_spriteBatch, "HIGH SCORE", 7 * ClassicMaze.TileSize, 20 * ClassicMaze.TileSize, Color.White, 1);
            string hsString = _highScore.ToString().PadLeft(6, '0');
            TextRenderer.DrawText(_spriteBatch, hsString, 9 * ClassicMaze.TileSize, 22 * ClassicMaze.TileSize, Color.Yellow, 1);

         
        }

        private void DrawGameplayBoard(bool drawPacman = true, bool drawGhosts = true)
        {
            // 1. Draw top UI scores
            TextRenderer.DrawText(_spriteBatch, "1UP", 3 * ClassicMaze.TileSize, 0, Color.White, 1);
            string scString = _score.ToString().PadLeft(5, '0');
            TextRenderer.DrawText(_spriteBatch, scString, 3 * ClassicMaze.TileSize, 8, Color.White, 1);

            TextRenderer.DrawText(_spriteBatch, "HIGH SCORE", 10 * ClassicMaze.TileSize, 0, Color.White, 1);
            string hsString = _highScore.ToString().PadLeft(6, '0');
            TextRenderer.DrawText(_spriteBatch, hsString, 12 * ClassicMaze.TileSize, 8, Color.White, 1);

            // 2. Draw Maze
            _maze.Draw(_spriteBatch, _frameCounter);

            // 3. Draw Fruit if active
            if (_fruitActive)
            {
                _spriteBatch.Draw(TextureGenerator.CherryTexture, 
                    new Vector2(_fruitTile.X * ClassicMaze.TileSize - 4, _fruitTile.Y * ClassicMaze.TileSize - 4), 
                    Color.White);
            }

            // Draw fruit eaten score points if recently consumed
            if (_fruitEatenTextActive)
            {
                TextRenderer.DrawText(_spriteBatch, _fruitPoints.ToString(), 
                    _fruitTile.X * ClassicMaze.TileSize - 4, _fruitTile.Y * ClassicMaze.TileSize + 2, Color.Magenta, 1);
            }

            // 4. Draw Pacman
            if (drawPacman)
            {
                _pacman.Draw(_spriteBatch);
            }

            // 5. Draw Ghosts
            if (drawGhosts)
            {
                bool frightBlink = false;
                for (int i = 0; i < 4; i++)
                {
                    if (_ghosts[i].State == GhostState.Frightened && _ghosts[i].FrightTimer < 2.0f)
                    {
                        // Flash white/blue every 8 frames as fright timer is ending
                        frightBlink = ((int)(_ghosts[i].FrightTimer * 10) % 2 == 0);
                    }
                    _ghosts[i].Draw(_spriteBatch, frightBlink);
                }
            }

            // 6. Draw lives icons at bottom (row 34)
            for (int l = 0; l < _pacman.Lives; l++)
            {
                // Draw small yellow circle facing left (looks like classic life counter!)
                _spriteBatch.Draw(TextureGenerator.PacmanTextures[2][1], 
                    new Vector2((2 + l * 2) * ClassicMaze.TileSize, 34 * ClassicMaze.TileSize), 
                    Color.White);
            }

            // 7. Draw fruits eaten icons at bottom right
            _spriteBatch.Draw(TextureGenerator.CherryTexture, 
                new Vector2(24 * ClassicMaze.TileSize, 34 * ClassicMaze.TileSize), 
                Color.White);


        }

        private bool IsOpposite(Direction d1, Direction d2)
        {
            if (d1 == Direction.Up && d2 == Direction.Down) return true;
            if (d1 == Direction.Down && d2 == Direction.Up) return true;
            if (d1 == Direction.Left && d2 == Direction.Right) return true;
            if (d1 == Direction.Right && d2 == Direction.Left) return true;
            return false;
        }
    }
}
