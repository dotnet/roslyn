using System;
using System.Collections.Concurrent;

namespace Microsoft.CodeAnalysis.Collections
{
    /// <summary>
    /// A mutable typesafe dictionary from a pair of compilation and key (of type <see cref="CompilationStuff.Key{T}"/>)
    /// to a value (of type T). All keys and values associated with a compilation are reachable for GC purposes as a
    /// consequence of this API for only as long as the compilation is reachable. These extension methods make a
    /// compilation act as a property bag but without the <see cref="Compilation"/> itself being mutable.
    /// </summary>
    public static class CompilationStuff
    {
        public class Key<T> { }

        public static bool TryGetStuff<T>(this Compilation compilation, Key<T> key, out T value)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));
            if (key == null) throw new ArgumentNullException(nameof(key));
            object tempValue;
            if (compilation._compilationStuff.TryGetValue(key, out tempValue))
            {
                value = (T)tempValue;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }

        public static bool TrySetStuff<T>(this Compilation compilation, Key<T> key, T value)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));
            if (key == null) throw new ArgumentNullException(nameof(key));
            return compilation._compilationStuff.TryAdd(key, value);
        }

        public static bool TryRemoveStuff<T>(this Compilation compilation, Key<T> key, out T value)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));
            if (key == null) throw new ArgumentNullException(nameof(key));
            object tempValue;
            if (compilation._compilationStuff.TryRemove(key, out tempValue))
            {
                value = (T)tempValue;
                return true;
            }
            else
            {
                value = default(T);
                return false;
            }
        }
    }
}
