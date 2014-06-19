using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Roslyn.Compilers
{
    /// <summary>
    /// Utility methods associated with ConsList.
    /// </summary>
    public static class ConsList
    {
        public static ConsList<T> Empty<T>()
        {
            return ConsList<T>.Empty;
        }

        public static ConsList<T> Singleton<T>(T t)
        {
            return ConsList<T>.Empty.Prepend(t);
        }
    }
}
