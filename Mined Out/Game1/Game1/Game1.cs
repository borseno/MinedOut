using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Game1
{
    static class Numbers
    {
        static private Texture2D[] Textures;

        static public Texture2D GetTexture(int number)
        {
            return Textures[number];
        }

        static public void SetTextures(params Texture2D[] texture2s)
        {
            Textures = texture2s;
        }
    }

    class Point
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

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
            System.Diagnostics.Debug.WriteLine("TakeDefuseKit method, HasDefuseKit = " + HasDefuseKit);
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
                if (!forbidden.Contains('w') && key == Keys.W)
                {
                    Position.Y -= speed;
                    return true;
                }
                if (!forbidden.Contains('a') && key == Keys.A)
                {
                    Position.X -= speed;
                    return true;
                }
                if (!forbidden.Contains('s') && key == Keys.S)
                {
                    Position.Y += speed;
                    return true;
                }
                if (!forbidden.Contains('d') && key == Keys.D)
                {
                    Position.X += speed;
                    return true;
                }
            }
            return false;
        }
    }

    public class Game1 : Game
    {
        enum GameState
        {
            MainMenu,
            Gameplay,
            EndOfGame
        }
        GameState _state;
        bool CheatMode = false;

        GraphicsDeviceManager graphics;

        Texture2D whitesprite;
        Texture2D KitImage;
        Texture2D Menu;

        SpriteBatch sprite;
        //SpriteFont font; // Verdana 20

        readonly int PointSize = 30;
        readonly int Width;
        readonly int Height;

        Player_CT player;
        DefuseKit Kits;
        Bomb[] Bombs;
        LinkedList<Point> Path;
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
            (previous.IsKeyDown(Keys.W) == current.IsKeyDown(Keys.W) && current.IsKeyDown(Keys.W) != false)
                   || (previous.IsKeyDown(Keys.A) == current.IsKeyDown(Keys.A) && current.IsKeyDown(Keys.A) != false)
                   || (previous.IsKeyDown(Keys.S) == current.IsKeyDown(Keys.S) && current.IsKeyDown(Keys.S) != false)
                   || (previous.IsKeyDown(Keys.D) == current.IsKeyDown(Keys.D) && current.IsKeyDown(Keys.D) != false);
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

        protected override void Initialize()
        {
            Window.Title = "Mined Out";

            InitializeEntities();

            Path = new LinkedList<Point>();
            Steps = 0;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            whitesprite = Content.Load<Texture2D>("white1x1");
            KitImage = Content.Load<Texture2D>("DefuseKit");
            Menu = Content.Load<Texture2D>("PressEnterToContinue");
            sprite = new SpriteBatch(GraphicsDevice);

            {
                Texture2D[] numbers = new Texture2D[9];

                for (int i = 0; i < 9; i++)
                {
                    numbers[i] = Content.Load<Texture2D>($@"numbers\{i}");
                }
                Numbers.SetTextures(numbers);
            }

        }


        protected override void UnloadContent()
        {

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
                CheatMode = !CheatMode;

            Point point = new Point(player.Position.X, player.Position.Y);
            if (!Path.Contains(point))
                Path.AddLast(point);


            if (previous == null)
            {
                previous = Keyboard.GetState();
                SameKeyboardStates = false;
            }
            if (!(SameKeyboardStates = AreKeyboardStatesTheSame(previous, Keyboard.GetState()) ))
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
            GraphicsDevice.Clear(Color.DeepSkyBlue);

            sprite.Begin(SpriteSortMode.BackToFront);
            sprite.Draw(Menu, new Vector2(0, 0), Color.White);
            sprite.End();
        }

        protected void DrawEndOfGame(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DeepSkyBlue);

            sprite.Begin(SpriteSortMode.BackToFront);
            sprite.Draw(Menu, new Vector2(0, 0), Color.White);
            sprite.End();
        }

        protected void DrawGamePlay(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.DeepSkyBlue);
            sprite.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);

            sprite.Draw(whitesprite, new Vector2(Width / 2, 0),
                null, Color.Orange, 0, Vector2.Zero, PointSize, SpriteEffects.None, 1);

            if (CheatMode)
                foreach (Bomb bomb in Bombs)
                {
                    if (bomb != null)
                        sprite.Draw(whitesprite, new Vector2(bomb.Position.X, bomb.Position.Y),
                            null, Color.Red, 0, Vector2.Zero, PointSize, SpriteEffects.None, 1);
                }

            for (int i = 0; i < Width / PointSize; i++)
            {
                if (i * PointSize != Width / 2)
                    sprite.Draw(whitesprite, new Vector2(i * PointSize, 0), null,
                        Color.Green, 0, Vector2.Zero, PointSize, SpriteEffects.None, 1);

                sprite.Draw(whitesprite, new Vector2(0, i * PointSize),
                    null, Color.Green, 0, Vector2.Zero, PointSize, SpriteEffects.None, 1);
                sprite.Draw(whitesprite, new Vector2(Width - PointSize, i * PointSize),
                    null, Color.Green, 0, Vector2.Zero, PointSize, SpriteEffects.None, 1);
            }

            if (Kits != null)
                sprite.Draw(KitImage, new Vector2(Kits.Position.X, Kits.Position.Y), Color.White);

            sprite.Draw(Numbers.GetTexture(BombsNearTo(player.Position.X, player.Position.Y)),
                new Vector2(player.Position.X, player.Position.Y), Color.Purple);

            foreach (Point path in Path)
            {
                sprite.Draw(whitesprite, new Vector2(path.X, path.Y),
                    null, Color.Black, 0, Vector2.Zero, PointSize, SpriteEffects.None, 1);
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
