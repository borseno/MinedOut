using System;
using System.Collections.Generic;
using Game1.Game.Entities;
using Microsoft.Xna.Framework;

namespace Game1.HelperClasses.Comparers
{
    class EntityToPointComparer : IComparer<object>
    {
        public int Compare(object x, object y)
        {
            if (x is Point point && y is Entity entity)
            {
                return Compare(entity, point);
            }
            else if (x is Entity entity1 && y is Point point1)
            {
                return Compare(entity1, point1);
            }
            throw new InvalidCastException($"x was { x.GetType().Name }, y was { y.GetType().Name }");
        }

        private int Compare(Entity entity, Point point)
        {
            if (entity.Position.X == point.X)
                return entity.Position.Y - point.Y;
            else
                return entity.Position.X - point.X;
        }
    }
}
