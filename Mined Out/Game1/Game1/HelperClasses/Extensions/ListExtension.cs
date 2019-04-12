using System.Collections.Generic;

namespace Game1.HelperClasses.Extensions
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
