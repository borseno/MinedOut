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
        public Entity()
        { }

        public int CompareTo(object i)
        {
            if (i is Entity entity)
            {
                if (this.Position.X == entity.Position.X)
                    return this.Position.Y - entity.Position.Y;
                else
                    return this.Position.X - entity.Position.X;
            }
            throw new InvalidCastException();
        }

        public override string ToString()
        {
            return $"{this.GetType()} on coords {{X = {Position.X} Y = {Position.Y} }}";
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

        public Player_CT() : base()
        {
            HasDefuseKit = false;
        }

        public void TakeDefuseKit(ref DefuseKit Kits)
        {
            HasDefuseKit = true;
            Kits = null;
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
        public Player() : base()
        {
        }
        public Player(int x, int y) : base(x, y)
        {

        }

        public bool Move(LinkedList<char> forbidden, int speed)
        {
            Keys[] keys = Keyboard.GetState().GetPressedKeys();

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
        const int PointSize = 30;

        GameState _state;
        bool CheatMode = false;

        GraphicsDeviceManager graphics;

        Texture2D KitImage;
        Texture2D whiteSquare;

        SpriteBatch sprite;
        SpriteFont font; // Verdana 20

        readonly int Width;
        readonly int Height;

        Player_CT player;
        DefuseKit Kits;
        Bomb[] Bombs;
        List<Point> Path;
        KeyboardState previous;

        bool SameKeyboardStates { get; set; }
        bool HaveWon { get { return player.Position.X == Width / 2 && player.Position.Y == 0; } }
        int Steps { get; set; }

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = 900,
                PreferredBackBufferHeight = 900
            };

            Content.RootDirectory = "Content";
            TargetElapsedTime = new System.TimeSpan(0, 0, 0, 0, 16);

            Width = graphics.PreferredBackBufferWidth;
            Height = graphics.PreferredBackBufferHeight;
        }

        int BombsNearTo(int x, int y)
        {
            int Count = 0;
            for (int i = 0; i < Bombs.Length; i++)
            {
                if (Bombs[i] == null)
                    continue;
                if (Math.Abs(x - Bombs[i].Position.X) <= PointSize && Math.Abs(y - Bombs[i].Position.Y)
                    <= PointSize && (x != Bombs[i].Position.X || y != Bombs[i].Position.Y))
                    Count++;
            }
            return Count;
        }

        public LinkedList<char> ForbiddenDirect() // can't move through the walls
        {
            LinkedList<char> forbidden = new LinkedList<char>();
            if (player.Position.X == PointSize)
                forbidden.AddLast('a');
            if (player.Position.Y == PointSize && player.Position.X != Width / 2)
                forbidden.AddLast('w');
            if (Height - PointSize == player.Position.Y)
                forbidden.AddLast('s');
            if (Width - PointSize * 2 == player.Position.X)
                forbidden.AddLast('d');
            return forbidden;
        }

        private int BombUnder(Player_CT player)
        {
            return Array.BinarySearch(Bombs, new Bomb(player.Position.X, player.Position.Y));
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

        private void InitializeEntities()
        {
            Random rnd = new Random();

            // Place kits
            Kits = new DefuseKit(rnd.Next(1, (Width - PointSize) / PointSize) * PointSize,
                rnd.Next(1, (Height - PointSize * 4) / PointSize) * PointSize);

            // Place the player
            player = new Player_CT();
            do
            {
                player = new Player_CT(rnd.Next(1, (Width - PointSize) / PointSize) * PointSize,
                    Height - PointSize);
            } while (player.Position.X == Kits.Position.X && Kits.Position.Y == player.Position.Y);

            // Create the bombs array
            byte Length = (byte)rnd.Next(100, 200);
            Bombs = new Bomb[Length];

            for (int Last = 0; Last < Length;)
            {
                int X = (rnd.Next(1, (Width - PointSize) / PointSize)) * PointSize;
                int Y = (rnd.Next(1, (Height - PointSize) / PointSize)) * PointSize;
                bool isSpecial = true; // if its coords differ from the other ones'

                for (int i = 0; Bombs[i] != null && i < Bombs.Length; i++)
                    if (Math.Abs(Bombs[i].Position.X - X) < PointSize && Math.Abs(Bombs[i].Position.Y - Y) < PointSize)
                        isSpecial = false;

                if (isSpecial
                    && (Math.Abs(X - player.Position.X) > PointSize || Math.Abs(Y - player.Position.Y) > PointSize)
                    && (Math.Abs(X - Kits.Position.X) >= PointSize || Math.Abs(Y - Kits.Position.Y) >= PointSize)
                    && (!(Math.Abs(X - (Width / 2)) <= PointSize && Y <= PointSize)))
                {
                    Bombs[Last] = new Bomb(X, Y);
                    Last++;
                }
            }
            Array.Sort(Bombs);
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
            Window.Title = "Mined Out";

            InitializeEntities();

            Path = new List<Point>();
            Steps = 0;

            const int sideSize = 30;

            whiteSquare = CreateTexture(GraphicsDevice, sideSize, sideSize, t => Color.White);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            KitImage = Content.Load<Texture2D>("DefuseKit");
            sprite = new SpriteBatch(GraphicsDevice);

            font = Content.Load<SpriteFont>("font");
        }


        protected override void UnloadContent()
        {
            KitImage = null;
            sprite = null;

            Content.Unload();

            base.UnloadContent();
        }

        protected void ResetGame()
        {
            Initialize();
        }

        protected void UpdateMainMenu(GameTime gametime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                _state = GameState.Gameplay;
            }
        }

        protected void UpdateEndOfGame(GameTime gametime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();
            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
            {
                _state = GameState.Gameplay;
                ResetGame();
            }
        }

        protected void UpdateGamePlay(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (Keyboard.GetState().IsKeyDown(Keys.C)) // Easter Egg =D
                CheatMode = true;
            else
                CheatMode = false;

            Point point = new Point(player.Position.X, player.Position.Y);

            if (!Path.AnyReversed(point))
                Path.Add(point);

            Debug.WriteLine(Path.Count);

            if (previous == null)
            {
                previous = Keyboard.GetState();
                SameKeyboardStates = false;
            }

            if (!(SameKeyboardStates = AreKeyboardStatesTheSame(previous, Keyboard.GetState())))
            {
                previous = Keyboard.GetState();
            }

            if (!SameKeyboardStates)
            {
                if (player.Move(ForbiddenDirect(), PointSize))
                    Steps++;

                if (Kits != null && player.Position.X == Kits.Position.X && player.Position.Y == Kits.Position.Y)
                {
                    player.TakeDefuseKit(ref Kits);
                }

                int BombIndex = BombUnder(player);

                if (BombIndex >= 0)
                {
                    if (!player.HasDefuseKit)
                    {
                        _state = GameState.EndOfGame; // lost
                    }

                    Bombs[BombIndex] = null;
                    player.HasDefuseKit = false;
                }
                if (HaveWon)
                    _state = GameState.EndOfGame;
            }
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            switch (_state)
            {
                case GameState.MainMenu:
                    UpdateMainMenu(gameTime);
                    break;
                case GameState.Gameplay:
                    UpdateGamePlay(gameTime);
                    break;
                case GameState.EndOfGame:
                    UpdateEndOfGame(gameTime);
                    break;
            }
        }

        protected void DrawMainMenu(GameTime gameTime)
        {
            const string text = "Press enter to start the game";

            var coords = new Vector2(
                graphics.PreferredBackBufferWidth / 2 - text.Length * fontSize / 2.85f,
                graphics.PreferredBackBufferHeight / 2
            );

            GraphicsDevice.Clear(Color.ForestGreen);

            sprite.Begin(SpriteSortMode.BackToFront);
            sprite.DrawString(font, text, coords, Color.DarkGray);
            sprite.End();
        }

        protected void DrawEndOfGame(GameTime gameTime)
        {
            const string text = "Press enter to restart the game";

            var coords = new Vector2(
                graphics.PreferredBackBufferWidth / 2 - text.Length * fontSize / 2.85f,
                graphics.PreferredBackBufferHeight / 2
            );

            GraphicsDevice.Clear(Color.ForestGreen);

            sprite.Begin(SpriteSortMode.BackToFront);
            sprite.DrawString(font, text, coords, Color.DarkGray);
            sprite.End();
        }

        protected void DrawGamePlay(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DeepSkyBlue);
            sprite.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            sprite.Draw(whiteSquare, new Vector2(Width / 2, 0),
                null, Color.Orange, 0, Vector2.Zero, 1, SpriteEffects.None, 1); // draw player square

            if (CheatMode)
                foreach (Bomb bomb in Bombs)
                {
                    if (bomb != null)
                        sprite.Draw(whiteSquare, new Vector2(bomb.Position.X, bomb.Position.Y),
                            null, Color.Red, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
                }

            // draw walls
            for (int i = 0; i < Width / PointSize; i++)
            {
                if (i * PointSize != Width / 2)
                    sprite.Draw(whiteSquare, new Vector2(i * PointSize, 0), null,
                        Color.Green, 0, Vector2.Zero, 1, SpriteEffects.None, 1);

                sprite.Draw(whiteSquare, new Vector2(0, i * PointSize),
                    null, Color.Green, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
                sprite.Draw(whiteSquare, new Vector2(Width - PointSize, i * PointSize),
                    null, Color.Green, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            }

            // draw kit image
            if (Kits != null)
                sprite.Draw(KitImage, new Vector2(Kits.Position.X, Kits.Position.Y), Color.White);

            // draw amount of bombs nearby
            {
                const float coordsToAddY = -((PointSize / 2) - 13.4f);
                const float coordsToAddX = (PointSize / 2) - 8f;

                var position = 
                    new Vector2(player.Position.X, player.Position.Y) + new Vector2(coordsToAddX, coordsToAddY);

                sprite.DrawString(
                    font, 
                    BombsNearTo(player.Position.X, player.Position.Y).ToString(),
                    position, 
                    Color.Purple
               );
            }

            foreach (Point path in Path)
            {
                sprite.Draw(whiteSquare, new Vector2(path.X, path.Y),
                    null, Color.Black, 0, Vector2.Zero, 1, SpriteEffects.None, 1);
            }

            sprite.End();
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            switch (_state)
            {
                case GameState.MainMenu:
                    DrawMainMenu(gameTime);
                    break;
                case GameState.EndOfGame:
                    DrawEndOfGame(gameTime);
                    break;
                case GameState.Gameplay:
                    DrawGamePlay(gameTime);
                    break;
            }
        }
    }
}
