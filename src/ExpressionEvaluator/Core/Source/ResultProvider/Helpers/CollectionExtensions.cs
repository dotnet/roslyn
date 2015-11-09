using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class CollectionExtensions
    {
        // Normally, this functionality would be available through System.Linq.  But since we target the .NET 2.0
        // framework, we can't use it, so we roll our own ToArray() method instead.
        public static T[] ToArray<T>(this ICollection<T> collection)
        {
            T[] array = new T[collection.Count];

            int i = 0;
            foreach(T item in collection)
            {
                array[i++] = item;
            }

            return array;
        }
    }
}
