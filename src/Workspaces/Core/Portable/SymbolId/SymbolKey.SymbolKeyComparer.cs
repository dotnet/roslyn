// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class SymbolKeyComparer : IEqualityComparer<SymbolKey>
        {
            private readonly ComparisonOptions _options;

            private SymbolKeyComparer(ComparisonOptions options)
            {
                _options = options;
            }

            public bool Equals(SymbolKey x, SymbolKey y)
            {
                if (ReferenceEquals(x, y))
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                return x.Equals(y, _options);
            }

            public int GetHashCode(SymbolKey obj)
            {
                if (obj == null)
                {
                    return 0;
                }

                return obj.GetHashCode(_options);
            }

            public static IEqualityComparer<SymbolKey> GetComparer(bool ignoreCase, bool ignoreAssemblyKey, bool compareMethodTypeParametersByName)
            {
                return GetComparer(new ComparisonOptions(ignoreCase, ignoreAssemblyKey, compareMethodTypeParametersByName));
            }

            private static readonly SymbolKeyComparer[] s_cachedComparers = new SymbolKeyComparer[8];

            private static SymbolKeyComparer EnsureInitialized(ref SymbolKeyComparer location, ComparisonOptions options)
            {
                // This doesn't need to be interlocked since comparers store no state
                return location ?? (location = new SymbolKeyComparer(options));
            }

            public static IEqualityComparer<SymbolKey> GetComparer(ComparisonOptions options)
            {
                return EnsureInitialized(ref s_cachedComparers[options.FlagsValue], options);
            }
        }
    }
}
