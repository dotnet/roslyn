namespace Microsoft.CodeAnalysis
{
    internal static class ReadOnlyInterlocked
    {
        internal static ReadOnlyArray<T> Exchange<T>(ref ReadOnlyArray<T> location, ReadOnlyArray<T> value)
        {
            return ReadOnlyArray<T>.InterlockedExchange(ref location, value);
        }

        internal static ReadOnlyArray<T> CompareExchange<T>(ref ReadOnlyArray<T> location, ReadOnlyArray<T> value, ReadOnlyArray<T> comparand)
        {
            return ReadOnlyArray<T>.InterlockedCompareExchange(ref location, value, comparand);
        }

        internal static bool CompareExchangeIfNull<T>(ref ReadOnlyArray<T> location, ReadOnlyArray<T> value)
        {
            return ReadOnlyArray<T>.InterlockedCompareExchangeIfNull(ref location, value);
        }
    }
}