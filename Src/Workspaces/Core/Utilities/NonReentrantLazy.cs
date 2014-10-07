using System;

namespace Roslyn.Utilities
{
    internal static class NonReentrantLazy
    {
        public static NonReentrantLazy<T> Create<T>(T value)
        {
            return new NonReentrantLazy<T>(value);
        }

        public static NonReentrantLazy<T> Create<T>(Func<T> valueFactory)
        {
            return new NonReentrantLazy<T>(valueFactory);
        }
    }
}
