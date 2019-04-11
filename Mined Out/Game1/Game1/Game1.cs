using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Game1
{
    abstract class Entity : IComparable
    {
        public Point Position { get; set; }

        public Entity(int x, int y)
        {
            Position = new Point(x, y);
        }

        public int CompareTo(object i)
        {
            if (i is Entity entity)
            {
                if (this.Position.X == entity.Position.X)
                    return this.Position.Y - entity.Position.Y;
                else
                    return this.Position.X - entity.Position.X;
            }
            else
            {
#if DEBUG

                Debug.WriteLine(
                    $"Error in {this.GetType().Name}.CompareTo({i.GetType().Name}) {Environment.NewLine}" +
                    $"Invalid cast has been detected");

                throw new InvalidCastException();

#else

                throw new InvalidCastException("Entity is not " + i.GetType().Name);
                
#endif
            }
        }

        public override string ToString()
        {
            return $"{this.GetType()} at {{X = {Position.X} Y = {Position.Y}}}";
        }
    }

    class Bomb : Entity
    {
        public Bomb(int x, int y) : base(x, y) { }
    }

    class Player_CT : Player
    {
        public bool HasDefuseKit { get; set; }

        public Player_CT(int x, int y) : base(x, y)
        {
            HasDefuseKit = false;
        }

        public void TakeDefuseKit(ref DefuseKit kits)
        {
            HasDefuseKit = true;
            kits = null;
        }
    }

    class DefuseKit : Entity
    {
        public DefuseKit(int x, int y) : base(x, y)
        {
        }
    }

    abstract class Player : Entity
    {
        public Player(int x, int y) : base(x, y)
        {
        }

        public bool Move(KeyboardState state, IEnumerable<char> forbidden, int speed)
        {
            Keys[] keys = state.GetPressedKeys();

            foreach (Keys key in keys)
            {
                if (!forbidden.Contains('w') && (key == Keys.W || key == Keys.Up))
                {
                    Position -= new Point(x: 0, y: speed);
                    return true;
                }
                if (!forbidden.Contains('a') && (key == Keys.A || key == Keys.Left))
                {
                    Position -= new Point(x: speed, y: 0);
                    return true;
                }
                if (!forbidden.Contains('s') && (key == Keys.S || key == Keys.Down))
                {
                    Position += new Point(x: 0, y: speed);
                    return true;
                }
                if (!forbidden.Contains('d') && (key == Keys.D || key == Keys.Right))
                {
                    Position += new Point(x: speed, y: 0);
                    return true;
                }
            }
            return false;
        }
    }

    enum GameState
    {
        MainMenu,
        Gameplay,
        EndOfGame
    }

    public class Game1 : Game
    {
        const int fontSize = 20;
        const int pointSize = 30;

        readonly GraphicsDeviceManager graphics;
        readonly Random rnd;
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

            rnd = new Random();

            Window.Title = "Mined Out";
        }

        int BombsNearToLinear(int x, int y)
        {
            int count = 0;

            for (int i = 0; i < bombs.Length; i++)
            {
                if (bombs[i] == null)
                    continue;

                if (Math.Abs(x - bombs[i].Position.X) <= pointSize &&
                    Math.Abs(y - bombs[i].Position.Y) <= pointSize &&
                    (x != bombs[i].Position.X || y != bombs[i].Position.Y))
                    count++;
            }

            return count;
        }

        int BombsNearToBinary(int x, int y)
        {
            int count = 0;

            // 8 possible positions of bombs:
            //         111
            //         101
            //         111
            // where 0 - player, 1 - a bomb.

            // check top 3
            for (int i = 0; i < 3; i++)
            {
                var current = new Point((x - pointSize) + pointSize * i, y - pointSize);

                if (IndexOfBomb(current) >= 0)
                    count++;
            }

            // check bottom 3
            for (int i = 0; i < 3; i++)
            {
                var current = new Point((x - pointSize) + pointSize * i, y + pointSize);

                if (IndexOfBomb(current) >= 0)
                    count++;
            }

            // check right and left sides of the center
            var left = new Point(x - pointSize, y);
            var right = new Point(x + pointSize, y);

            if (IndexOfBomb(left) >= 0)
                count++;

            if (IndexOfBomb(right) >= 0)
                count++;

            return count;
        }

        int BombsNearTo(int x, int y)
        {
            if (bombs.Length < 640)
                return BombsNearToLinear(x, y);
            else
                return BombsNearToBinary(x, y);
        }

        public IEnumerable<char> ForbiddenDirect() // can't move through the walls
        {
            var forbidden = new List<char>(4);
            if (player.Position.X == pointSize)
                forbidden.Add('a');
            if (player.Position.Y == pointSize && player.Position.X != width / 2)
                forbidden.Add('w');
            if (height - pointSize == player.Position.Y)
                forbidden.Add('s');
            if (width - pointSize * 2 == player.Position.X)
                forbidden.Add('d');
            return forbidden;
        }

        private int IndexOfBomb(Point point)
        {
            return Array.BinarySearch(bombs, new Bomb(point.X, point.Y));
        }

        private bool AreKeyboardStatesTheSame(KeyboardState previous, KeyboardState current)
        {
            return
                previous.IsKeyDown(Keys.W) && current.IsKeyDown(Keys.W) ||
                previous.IsKeyDown(Keys.A) && current.IsKeyDown(Keys.A) ||
                previous.IsKeyDown(Keys.S) && current.IsKeyDown(Keys.S) ||
                previous.IsKeyDown(Keys.D) && current.IsKeyDown(Keys.D) ||

                previous.IsKeyDown(Keys.Up) && current.IsKeyDown(Keys.Up) ||
                previous.IsKeyDown(Keys.Down) && current.IsKeyDown(Keys.Down) ||
                previous.IsKeyDown(Keys.Left) && current.IsKeyDown(Keys.Left) ||
                previous.IsKeyDown(Keys.Right) && current.IsKeyDown(Keys.Right);
        }

        private void InitKits()
        {
            kits = new DefuseKit(
                rnd.Next(1, (width - pointSize) / pointSize) * pointSize,
                rnd.Next(1, (height - pointSize * 4) / pointSize) * pointSize
            );
        }

        private void InitPlayer()
        {
            do
            {
                player = new Player_CT(
                        rnd.Next(1, (width - pointSize) / pointSize) * pointSize,
                        height - pointSize
                        );
            } while (player.Position == kits.Position);
        }

        private void InitBombs()
        {
            int min = 100 * 30 / pointSize;
            int max = 200 * 30 / pointSize;

            int length = rnd.Next(min, max);
            bombs = new Bomb[length];

            for (int last = 0; last < length;)
            {
                int x = (rnd.Next(1, (width - pointSize) / pointSize)) * pointSize;
                int y = (rnd.Next(1, (height - pointSize) / pointSize)) * pointSize;
                bool isSpecial = true; // if its coords differ from the other ones'

                for (int i = 0; bombs[i] != null && i < bombs.Length; i++)
                    if (Math.Abs(bombs[i].Position.X - x) < pointSize && Math.Abs(bombs[i].Position.Y - y) < pointSize)
                        isSpecial = false;

                if (isSpecial
                    && (Math.Abs(x - player.Position.X) > pointSize || Math.Abs(y - player.Position.Y) > pointSize)
                    && (Math.Abs(x - kits.Position.X) >= pointSize || Math.Abs(y - kits.Position.Y) >= pointSize)
                    && (!(Math.Abs(x - (width / 2)) <= pointSize && y <= pointSize)))
                {
                    bombs[last] = new Bomb(x, y);
                    last++;
                }
            }

            Array.Sort(bombs);
        }

        private void InitializeEntities()
        {
            // Place kits
            InitKits();

            // Place the player
            InitPlayer();

            // Create the bombs array
            InitBombs();
        }

        public static Texture2D CreateTexture(GraphicsDevice device, int width, int height, Func<int, Color> paint)
        {
            //initialize a texture
            Texture2D texture = new Texture2D(device, width, height);

            //the array holds the color for each pixel in the texture
            Color[] data = new Color[width * height];
            for (int pixel = 0; pixel < data.Length; pixel++)
            {
                //the function applies the color according to the specified pixel
                data[pixel] = paint(pixel);
            }

            //set the color
            texture.SetData(data);

            return texture;
        }

        protected override void Initialize()
        {
            path = new List<Point>(84);
            steps = 0;
            cheatMode = false;
            whiteSquare = CreateTexture(GraphicsDevice, pointSize, pointSize, t => Color.White);

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


            if (!AreKeyboardStatesTheSame(previous, Keyboard.GetState()))
            {
                var current = Keyboard.GetState();
                previous = current;

                if (player.Move(current, ForbiddenDirect(), pointSize))
                    steps++;

                if (kits != null && player.Position == kits.Position)
                    player.TakeDefuseKit(ref kits);

                int bombIndex = IndexOfBomb(player.Position);

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
            var bombsNear = BombsNearTo(player.Position.X, player.Position.Y);
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
