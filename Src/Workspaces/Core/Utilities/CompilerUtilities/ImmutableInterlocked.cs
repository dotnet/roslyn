using System;
using System.Collections.Immutable;
using System.Threading;

namespace Roslyn.Utilities
{
    /// <summary>
    /// These will eventually be replaced by the BCL versions and moved into the System.Collections.Immutable namespace
    /// </summary>
    internal static class ImmutableInterlocked
    {
        public static TValue GetOrAdd<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, TValue value)
        {
            var priorCollection = Volatile.Read(ref location);
            TValue oldValue;
            while (true)
            {
                if (priorCollection == null)
                {
                    throw new ArgumentNullException("location");
                }

                if (priorCollection.TryGetValue(key, out oldValue))
                {
                    break;
                }

                var updatedCollection = priorCollection.Add(key, value);
                var interlockedResult = Interlocked.CompareExchange(ref location, updatedCollection, priorCollection);
                if (priorCollection == interlockedResult)
                {
                    return value;
                }

                priorCollection = interlockedResult;
            }

            return oldValue;
        }

        public static TValue GetOrAdd<TKey, TValue>(ref ImmutableDictionary<TKey, TValue> location, TKey key, Func<TKey, TValue> valueFactory)
        {
            if (valueFactory == null)
            {
                throw new ArgumentNullException("valueFactory");
            }

            var map = Volatile.Read(ref location);
            if (location == null)
            {
                throw new ArgumentNullException("location");
            }

            TValue value;
            if (map.TryGetValue(key, out value))
            {
                return value;
            }

            value = valueFactory(key);
            return GetOrAdd(ref location, key, value);
        }

        /// <summary>
        /// Lookup a value in the map or, if it's not present, create a new value and add it.
        /// </summary>
        /// <remarks>
        /// No locks are taken in this method. As a result, the factory method may be called more
        /// than once for the same key in the presence of thread races. This method ensures that,
        /// in that situation, only one value will successfully be added and the value(s) returned
        /// from any losing thread's factory will be discarded.
        /// </remarks>
        /// <typeparam name="TKey">Type of the key in the map</typeparam>
        /// <typeparam name="TValue">Type of the values in the map</typeparam>
        /// <typeparam name="TArg">Type of the <paramref name="factoryArgument"/> argument</typeparam>
        /// <param name="location">Reference to a field containing the map to use or update. The value may be a null, which is treated the same as an empty map.</param>
        /// <param name="key">The key of the value to look up.</param>
        /// <param name="valueFactory">A factory object that can create the new value.</param>
        /// <param name="factoryArgument">A user-defined argument to be passed to the factory object in addition to the key.</param>
        /// <returns>The value retrieved from the dictionary or from <paramref name="valueFactory"/> if it was not present.</returns>
        public static TValue GetOrAdd<TKey, TValue, TArg>(ref ImmutableDictionary<TKey, TValue> location, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            if (valueFactory == null)
            {
                throw new ArgumentNullException("valueFactory");
            }

            var map = Volatile.Read(ref location);
            if (location == null)
            {
                throw new ArgumentNullException("location");
            }

            TValue existingValue;
            if (map.TryGetValue(key, out existingValue))
            {
                return existingValue;
            }

            TValue newValue = valueFactory(key, factoryArgument);
            return GetOrAdd(ref location, key, newValue);
        }
    }
}