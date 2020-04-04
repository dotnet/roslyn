// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        internal class SymbolKeyComparer : IEqualityComparer<SymbolKey>, IEqualityComparer<ISymbol>
        {
            private readonly ComparisonOptions _options;

            private SymbolKeyComparer(ComparisonOptions options)
                => _options = options;

            public bool Equals(SymbolKey x, SymbolKey y)
                => Equals(x._symbolKeyData, y._symbolKeyData);

            public bool Equals(ISymbol x, ISymbol y)
                => Equals(CreateString(x), CreateString(y));

            private bool Equals(string xData, string yData)
            {
                var comparer = _options.IgnoreCase
                    ? StringComparer.OrdinalIgnoreCase
                    : StringComparer.Ordinal;

                if (!_options.IgnoreAssemblyKey)
                {
                    // Easiest case.  We can directly compare the raw contents of the keys.
                    return comparer.Equals(xData, yData);
                }
                else
                {
                    // This is harder.  To compare these we need to remove the entries related to 
                    // assemblies.
                    var data1 = RemoveAssemblyKeys(xData);
                    var data2 = RemoveAssemblyKeys(yData);

                    return comparer.Equals(data1, data2);
                }
            }

            private string RemoveAssemblyKeys(string data)
            {
                var reader = new RemoveAssemblySymbolKeysReader();
                reader.Initialize(data);
                return reader.RemoveAssemblySymbolKeys();
            }

            public int GetHashCode(SymbolKey obj)
                => GetHashCode(obj._symbolKeyData);

            public int GetHashCode(ISymbol obj)
                => GetHashCode(CreateString(obj));

            private int GetHashCode(string data)
            {
                var comparer = _options.IgnoreCase
                    ? StringComparer.OrdinalIgnoreCase
                    : StringComparer.Ordinal;

                if (!_options.IgnoreAssemblyKey)
                {
                    // Easiest case.  We can directly hash the raw contents of the keys.
                    return comparer.GetHashCode(data);
                }
                else
                {
                    // This is harder.  To compare these we need to remove the entries related to 
                    // assemblies.
                    var data1 = RemoveAssemblyKeys(data);

                    return comparer.GetHashCode(data1);
                }
            }

            public static SymbolKeyComparer GetComparer(bool ignoreCase, bool ignoreAssemblyKey)
                => GetComparer(new ComparisonOptions(ignoreCase, ignoreAssemblyKey));

            private static readonly SymbolKeyComparer[] s_cachedComparers = new SymbolKeyComparer[4];

            private static SymbolKeyComparer EnsureInitialized(ref SymbolKeyComparer location, ComparisonOptions options)
            {
                // This doesn't need to be interlocked since comparers store no state
                return location ??= new SymbolKeyComparer(options);
            }

            private static SymbolKeyComparer GetComparer(ComparisonOptions options)
                => EnsureInitialized(ref s_cachedComparers[options.FlagsValue], options);
        }
    }
}
