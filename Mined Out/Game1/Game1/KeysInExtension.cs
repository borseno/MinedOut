using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace Game1
{
    static class KeysInExtension
    {
        public static bool In(this Keys key, IEnumerable<Keys> keys)
        {
            foreach (var i in keys)
                if (i == key)
                    return true;
            return false;
        }
    }
}
