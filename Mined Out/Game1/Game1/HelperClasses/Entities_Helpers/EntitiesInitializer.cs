using System;
using System.Collections.Generic;
using System.Linq;
using Game1.Game.Entities;
using Microsoft.Xna.Framework;

namespace Game1.HelperClasses.Entities_Helpers
{
    static class EntitiesInitializer
    {
        private static readonly Random rnd = new Random();

        public static Player_CT InitPlayer(int width, int height, int pointSize, params Point[] toIgnore)
        {
            Player_CT player;

            do
            {
                player = new Player_CT(
                    rnd.Next(1, (width - pointSize) / pointSize) * pointSize,
                    height - pointSize
                );
            } while (toIgnore?.Contains(player.Position) ?? false);

            return player;
        }

        public static DefuseKit InitKits(int width, int height, int pointSize, params Point[] toIgnore)
        {
            DefuseKit kits;

            do
            {
                kits = new DefuseKit(
                    rnd.Next(1, (width - pointSize) / pointSize) * pointSize,
                    rnd.Next(1, (height - pointSize * 4) / pointSize) * pointSize
                );
            } while (toIgnore?.Contains(kits.Position) ?? false);

            return kits;
        }

        public static IEnumerable<Bomb> InitBombs(int width, int height, int pointSize,
    IEnumerable<Point> exceptionsNearNotAllowed = null, IEnumerable<Point> exceptionsNearAllowed = null)
        {
            if (exceptionsNearNotAllowed == null)
                exceptionsNearNotAllowed = Enumerable.Empty<Point>();

            if (exceptionsNearAllowed == null)
                exceptionsNearAllowed = Enumerable.Empty<Point>();

            int min = 100 * 30 / pointSize;
            int max = 200 * 30 / pointSize;

            int length = rnd.Next(min, max);

            var bombs = new Bomb[length];

            for (int last = 0; last < length;)
            {
                int x = (rnd.Next(1, (width - pointSize) / pointSize)) * pointSize;
                int y = (rnd.Next(1, (height - pointSize) / pointSize)) * pointSize;
                bool isAllowed = true; // if its coords differ from the other bombs and exceptions

                for (int i = 0; bombs[i] != null && i < bombs.Length; i++)
                    if (Math.Abs(bombs[i].Position.X - x) < pointSize && Math.Abs(bombs[i].Position.Y - y) < pointSize)
                        isAllowed = false;

                if (isAllowed)
                {
                    foreach (var i in exceptionsNearNotAllowed)
                    {
                        var condition = Math.Abs(i.X - x) > pointSize || Math.Abs(i.Y - y) > pointSize;

                        if (!condition)
                        {
                            isAllowed = false;
                            break;
                        }
                    }
                }

                if (isAllowed)
                {
                    foreach (var i in exceptionsNearAllowed)
                    {
                        var condition = Math.Abs(i.X - x) >= pointSize || Math.Abs(i.Y - y) >= pointSize;

                        if (!condition)
                        {
                            isAllowed = false;
                            break;
                        }
                    }
                }

                if (isAllowed)
                {
                    bombs[last] = new Bomb(x, y);
                    last++;
                }
            }

            Array.Sort(bombs);

            return bombs;
        }
    }
}
