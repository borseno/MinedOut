using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Game1.HelperClasses.Restricters
{
    static class DirectionRestricter
    {
        public static IEnumerable<char> ForbiddenDirect(Point position, int eastX, int westX, int northY, int southY, params Point[] exceptions) // can't move through the walls
        {
            foreach (var i in exceptions)
                if (position == i)
                    return Enumerable.Empty<char>();

            var forbidden = new List<char>(4);

            if (position.X == westX)
                forbidden.Add('a');
            if (position.Y == northY)
                forbidden.Add('w');
            if (position.Y == southY)
                forbidden.Add('s');
            if (position.X == eastX)
                forbidden.Add('d');

            return forbidden;
        }

    }
}
