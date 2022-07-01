using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoToys
{
    static class Extension
    {
        public static T Assign<T>(this T item, out T t)
        {
            t = item;
            return item;
        }
        public static T Edit<T>(this T item, Action<T> t)
        {
            t.Invoke(item);
            return item;
        }
    }
}
