using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game1
{
    static class ListExtension
    {
        public static bool AnyReversed<T>(this List<T> list, T elem)
        {
            for (int i = list.Count - 1; i >= 0; --i)
            {
                if (list[i].Equals(elem))
                {
                    return true;
                }
            }   
            return false;
        }
    }
}
