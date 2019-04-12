using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace Game1.HelperClasses.Extensions
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
