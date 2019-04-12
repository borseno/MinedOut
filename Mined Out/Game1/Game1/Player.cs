using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Game1.Entities
{
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
}