using System;
using Game1.Game.Entities;
using Game1.HelperClasses.Comparers;
using Microsoft.Xna.Framework;

namespace Game1.HelperClasses.Entities_Helpers
{
    static class NearbyEntitiesCounter
    {
        private static int EntitiesNearToLinear(Entity[] entities, Entity entity, int pointSize)
        {
            var x = entity.Position.X;
            var y = entity.Position.Y;

            int count = 0;

            for (int i = 0; i < entities.Length; i++)
            {
                if (entities[i] == null)
                    continue;

                if (Math.Abs(x - entities[i].Position.X) <= pointSize &&
                    Math.Abs(y - entities[i].Position.Y) <= pointSize &&
                    (x != entities[i].Position.X || y != entities[i].Position.Y))
                    count++;
            }

            return count;
        }

        private static int EntitiesNearToBinary(Entity[] entities, Entity entity, int pointSize)
        {
            var x = entity.Position.X;
            var y = entity.Position.Y;

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

                if (Array.BinarySearch(entities, current, new EntityToPointComparer()) >= 0)
                    count++;
            }

            // check bottom 3
            for (int i = 0; i < 3; i++)
            {
                var current = new Point((x - pointSize) + pointSize * i, y + pointSize);

                if (Array.BinarySearch(entities, current, new EntityToPointComparer()) >= 0)
                    count++;
            }

            // check right and left sides of the center
            var left = new Point(x - pointSize, y);
            var right = new Point(x + pointSize, y);

            if (Array.BinarySearch(entities, left, new EntityToPointComparer()) >= 0)
                count++;

            if (Array.BinarySearch(entities, right, new EntityToPointComparer()) >= 0)
                count++;

            return count;
        }

        public static int EntitiesNearToEntity(Entity[] entities, Entity ent, int pointSize)
        {
            if (entities.Length < 640)
                return EntitiesNearToLinear(entities, ent, pointSize);
            else
                return EntitiesNearToBinary(entities, ent, pointSize);
        }
    }
}
