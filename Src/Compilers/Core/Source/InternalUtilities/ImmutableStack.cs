namespace System.Collections.Immutable
{
    internal static class ImmutableStack
    {
        public static ImmutableStack<T> Create<T>()
        {
            return ImmutableStack<T>.PrivateEmpty_DONOTUSE();
        }
    }
}