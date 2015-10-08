using System;

namespace Microsoft.CodeAnalysis.Collections
{
    /// <summary>
    /// A mutable typesafe dictionary from a pair of compilation and key (of type <see cref="CompilationContext.Key{T}"/>)
    /// to a value (of type T). All keys and values associated with a compilation are reachable for GC purposes as a
    /// consequence of this API for only as long as the compilation is reachable.
    /// </summary>
    public class CompilationContext
    {
        private static CompilationContext _instance = new CompilationContext();
        public static CompilationContext Instance => _instance;
        private CompilationContext() { }

        public sealed class Key<T> { }

        public bool TryGetValue<T>(Compilation compilation, Key<T> key, out T value)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));
            if (key == null) throw new ArgumentNullException(nameof(key));
            object tempValue;
            if (compilation._compilationContext.TryGetValue(key, out tempValue))
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

        public bool TryAdd<T>(Compilation compilation, Key<T> key, T value)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));
            if (key == null) throw new ArgumentNullException(nameof(key));
            return compilation._compilationContext.TryAdd(key, value);
        }

        public bool TryRemove<T>(Compilation compilation, Key<T> key, out T value)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));
            if (key == null) throw new ArgumentNullException(nameof(key));
            object tempValue;
            if (compilation._compilationContext.TryRemove(key, out tempValue))
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

        public T GetOrAdd<T>(Compilation compilation, Key<T> key, T value)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));
            if (key == null) throw new ArgumentNullException(nameof(key));
            return (T)compilation._compilationContext.GetOrAdd(key, value);
        }
    }
}
