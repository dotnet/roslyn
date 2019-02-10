using System;
using System.Collections.Immutable;

namespace Analyzer.Utilities.FlowAnalysis.Analysis.PropertySetAnalysis
{
    /// <summary>
    /// So we can use <see cref="ImmutableArray{T}"/> without paying the cost of allocating a bunch of underlying arrays.
    /// </summary>
    internal static class PropertySetImmutableArrayCache
    {
        public static ImmutableArray<PropertySetAbstractValueKind> Get(PropertySetAbstractValueKind k1)
        {
            return ImmutableArray.Create(k1);
        }

        public static ImmutableArray<PropertySetAbstractValueKind> Get(PropertySetAbstractValueKind k1, PropertySetAbstractValueKind k2)
        {
            return ImmutableArray.Create(k1, k2);
        }

        public static ImmutableArray<PropertySetAbstractValueKind> Get(PropertySetAbstractValueKind k1, PropertySetAbstractValueKind k2, PropertySetAbstractValueKind k3)
        {
            return ImmutableArray.Create(k1, k2, k3);
        }

        public static ImmutableArray<PropertySetAbstractValueKind> Get(PropertySetAbstractValueKind k1, PropertySetAbstractValueKind k2, PropertySetAbstractValueKind k3, PropertySetAbstractValueKind k4)
        {
            return ImmutableArray.Create(k1, k2, k3, k4);
        }

        public static ImmutableArray<PropertySetAbstractValueKind> Get(params PropertySetAbstractValueKind[] kinds)
        {
            return ImmutableArray.Create(kinds);
        }

        public static ImmutableArray<PropertySetAbstractValueKind> ReplaceAt(this ImmutableArray<PropertySetAbstractValueKind> kinds, int index, PropertySetAbstractValueKind kind)
        {
            if (index < 0 || index >= kinds.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (kinds[index] == kind)
            {
                return kinds;
            }
            else
            {
                return kinds.SetItem(index, kind);
            }
        }
    }
}
