using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Game1.Game.Entities
{
    abstract class Entity : IComparable
    {
        public Point Position { get; set; }

        public Entity(int x, int y)
        {
            Position = new Point(x, y);
        }

        public Entity(Point point)
        {
            Position = point;
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
}