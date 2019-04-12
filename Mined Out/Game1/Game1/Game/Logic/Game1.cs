using System;
using System.Collections.Generic;
using System.Linq;
using Game1.Game.Entities;
using Game1.HelperClasses.Comparers;
using Game1.HelperClasses.Entities_Helpers;
using Game1.HelperClasses.Extensions;
using Game1.HelperClasses.Restricters;
using Game1.HelperClasses.Texture_Helpers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Game1.Game.Logic
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        const int fontSize = 20;
        const int pointSize = 30;

        readonly GraphicsDeviceManager graphics;
        readonly int width;
        readonly int height;

        GameState _state;
        KeyboardState previous;

        bool cheatMode;
        int steps;

        Texture2D kitImage;
        Texture2D whiteSquare;
        SpriteBatch sprite;
        SpriteFont font; // Verdana 20

        Player_CT player;
        DefuseKit kits;
        Bomb[] bombs;

        List<Point> path;
        Point endPoint;

        bool HaveWon => player.Position.X == width / 2 && player.Position.Y == 0;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 900,
                PreferredBackBufferHeight = 900
            };

            Content.RootDirectory = "Content";
            TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 12);

            width = graphics.PreferredBackBufferWidth;
            height = graphics.PreferredBackBufferHeight;

            endPoint = new Point(width / 2, 0);

            Window.Title = "Mined Out";
        }

        private void InitializeEntities()
        {
            // Place kits
            kits = EntitiesInitializer.InitKits(width, height, pointSize);

            // Place the player
            player = EntitiesInitializer.InitPlayer(width, height, pointSize, kits.Position);

            // Create the bombs array
            var exceptionsNearNotAllowed = new Point[] {player.Position, kits.Position, endPoint};
            bombs = EntitiesInitializer.InitBombs(width, height, pointSize, exceptionsNearNotAllowed: exceptionsNearNotAllowed).ToArray();
        }

        protected override void Initialize()
        {
            path = new List<Point>(84);
            steps = 0;
            cheatMode = false;
            whiteSquare = TextureCreator.CreateTexture(GraphicsDevice, pointSize, pointSize, t => Color.White);

            InitializeEntities();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            kitImage = Content.Load<Texture2D>("DefuseKit");
            font = Content.Load<SpriteFont>("font");

            sprite = new SpriteBatch(GraphicsDevice);
        }

        protected override void UnloadContent()
        {
            kitImage = null;
            sprite = null;

            whiteSquare?.Dispose();
            whiteSquare = null;

            Content.Unload();

            base.UnloadContent();
        }

        protected void ResetGame()
        {
            Initialize();
        }

        protected void UpdateMainMenu()
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                _state = GameState.Gameplay;
            }
        }

        protected void UpdateEndOfGame()
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                _state = GameState.Gameplay;
                ResetGame();
            }
        }

        protected void UpdateGamePlay()
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.C)) // Easter Egg =D
                cheatMode = true;
            else
                cheatMode = false;

            if (!path.AnyReversed(player.Position))
                path.Add(player.Position);

            if (previous == null)
                previous = Keyboard.GetState();

            if (!InputComparer.AreKeyboardStatesTheSame(previous, Keyboard.GetState()))
            {
                var current = Keyboard.GetState();
                previous = current;

                if (player.Move(current, 
                    DirectionRestricter.ForbiddenDirect(player.Position, 
                        width - pointSize * 2, 
                        pointSize, pointSize, 
                        height - pointSize, new Point(width / 2, pointSize)
                        ), pointSize))
                    steps++;

                if (kits != null && player.Position == kits.Position)
                    player.TakeDefuseKit(ref kits);

                int bombIndex = Array.BinarySearch(bombs, player);

                if (bombIndex >= 0)
                {
                    if (!player.HasDefuseKit)
                    {
                        _state = GameState.EndOfGame; // lost
                    }

                    bombs[bombIndex] = null;
                    player.HasDefuseKit = false;
                }
                if (HaveWon) // won
                    _state = GameState.EndOfGame;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            switch (_state)
            {
                case GameState.MainMenu:
                    UpdateMainMenu();
                    break;
                case GameState.Gameplay:
                    UpdateGamePlay();
                    break;
                case GameState.EndOfGame:
                    UpdateEndOfGame();
                    break;
            }
        }

        protected void DrawMainMenu()
        {
            const string text = "Press enter to start the game";

            var coords = new Vector2(
                graphics.PreferredBackBufferWidth / 2 - text.Length * fontSize / 2.85f,
                graphics.PreferredBackBufferHeight / 2
            );

            GraphicsDevice.Clear(Color.DarkGray);

            sprite.Begin(SpriteSortMode.BackToFront);
            sprite.DrawString(font, text, coords, Color.ForestGreen);
            sprite.End();
        }

        protected void DrawEndOfGame()
        {
            var text = $"{(HaveWon ? "You have won!" : "Unfortunaly, you've lost :C")} Press enter to restart the game";

            var coords = new Vector2(
                graphics.PreferredBackBufferWidth / 2 - text.Length * fontSize / 2.85f,
                graphics.PreferredBackBufferHeight / 2
            );

            GraphicsDevice.Clear(Color.DarkGray);

            sprite.Begin(SpriteSortMode.BackToFront);
            sprite.DrawString(font, text, coords, Color.ForestGreen);
            sprite.End();
        }

        protected void DrawGamePlay()
        {
            var bombsNear = NearbyEntitiesCounter.EntitiesNearToEntity(bombs, player, pointSize);
            var endPoint = new Vector2(width / 2, 0);

            GraphicsDevice.Clear(Color.Black);
            sprite.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            #region draw end point
            {
                sprite.Draw(whiteSquare, endPoint,
                    null, Color.Orange, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            }
            #endregion

            #region draw bombs if cheat mode is turned on
            {
                if (cheatMode)
                    foreach (Bomb bomb in bombs)
                    {
                        if (bomb != null)
                            sprite.Draw(whiteSquare, new Vector2(bomb.Position.X, bomb.Position.Y),
                                null, Color.Red, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
                    }
            }
            #endregion

            #region draw walls
            {
                for (int i = 0; i < width / pointSize; i++)
                {
                    if (i * pointSize != width / 2)
                        sprite.Draw(whiteSquare, new Vector2(i * pointSize, 0), null,
                            Color.Green, 0, Vector2.Zero, 1, SpriteEffects.None, 1);

                    sprite.Draw(whiteSquare, new Vector2(0, i * pointSize),
                        null, Color.Green, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
                    sprite.Draw(whiteSquare, new Vector2(width - pointSize, i * pointSize),
                        null, Color.Green, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
                }
            }
            #endregion

            #region draw the kit if exists
            {
                if (kits != null)
                    sprite.Draw(kitImage, new Vector2(kits.Position.X, kits.Position.Y), Color.White);
            }
            #endregion

            #region draw the amount of bombs nearby
            {
                const float coordsToAddY = -((pointSize / 2) - 13.4f);
                const float coordsToAddX = (pointSize / 2) - 8f;

                var position =
                    new Vector2(player.Position.X, player.Position.Y) + new Vector2(coordsToAddX, coordsToAddY);

                sprite.DrawString(
                    font,
                    bombsNear.ToString(),
                    position,
                    Color.Purple
               );
            }
            #endregion

            #region draw the path the player has gone through
            foreach (Point path in path)
            {
                sprite.Draw(whiteSquare, new Vector2(path.X, path.Y),
                    null, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            }
            #endregion

            #region draw steps, bombs nearby, distance to end
            {
                const string bombsText = "Bombs nearby:";
                const string stepsText = "Steps:";
                const string distanceEndText = "Distance:";

                var rightTop = new Vector2(width - pointSize, 0);
                var leftTop = new Vector2(pointSize, 0);

                var moreLeftRightTop = new Vector2(rightTop.X - (bombsText.Length * 1.0f) * fontSize, rightTop.Y);
                var moreRightLeftTop = new Vector2(leftTop.X + (stepsText.Length * 1.3f) * fontSize, leftTop.Y);

                var distanceX = Math.Abs((player.Position.X - endPoint.X) / 30);
                var distanceY = Math.Abs((player.Position.Y - endPoint.Y) / 30);

                var distance = distanceX + distanceY;

                // steps
                sprite.DrawString(font, $"{stepsText} {steps}", leftTop, Color.CadetBlue);

                // bombs nearby
                sprite.DrawString(font, $"{bombsText} {bombsNear}", moreLeftRightTop, Color.CadetBlue);

                sprite.DrawString(font, $"{distanceEndText} {Math.Round(distance)} ", moreRightLeftTop, Color.CadetBlue);
            }
            #endregion

            sprite.End();
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            switch (_state)
            {
                case GameState.MainMenu:
                    DrawMainMenu();
                    break;
                case GameState.EndOfGame:
                    DrawEndOfGame();
                    break;
                case GameState.Gameplay:
                    DrawGamePlay();
                    break;
            }
        }
    }
}
