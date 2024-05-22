// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private class SymbolKeyComparer : IEqualityComparer<SymbolKey>
    {
        private readonly ComparisonOptions _options;

        private SymbolKeyComparer(ComparisonOptions options)
            => _options = options;

        public bool Equals(SymbolKey x, SymbolKey y)
        {
            if (!_options.IgnoreAssemblyKey)
            {
                // Easiest case.  We can directly compare the raw contents of the keys.
                return x.Equals(y, _options.IgnoreCase);
            }
            else
            {
                // This is harder.  To compare these we need to remove the entries related to assemblies.
                //
                // Note: this will remove the language-string as well, so we don't have to worry about that here.
                var data1 = RemoveAssemblyKeys(x._symbolKeyData);
                var data2 = RemoveAssemblyKeys(y._symbolKeyData);

                var comparer = _options.IgnoreCase
                    ? StringComparer.OrdinalIgnoreCase
                    : StringComparer.Ordinal;

                return comparer.Equals(data1, data2);
            }
        }

        private static string RemoveAssemblyKeys(string data)
        {
            var reader = new RemoveAssemblySymbolKeysReader();
            reader.Initialize(data);
            return reader.RemoveAssemblySymbolKeys();
        }

        public int GetHashCode(SymbolKey obj)
            => obj.GetHashCode();

        public static IEqualityComparer<SymbolKey> GetComparer(bool ignoreCase, bool ignoreAssemblyKey)
            => GetComparer(new ComparisonOptions(ignoreCase, ignoreAssemblyKey));

        private static readonly SymbolKeyComparer[] s_cachedComparers = new SymbolKeyComparer[4];

        private static SymbolKeyComparer EnsureInitialized(ref SymbolKeyComparer location, ComparisonOptions options)
        {
            // This doesn't need to be interlocked since comparers store no state
            return location ??= new SymbolKeyComparer(options);
        }

        public static IEqualityComparer<SymbolKey> GetComparer(ComparisonOptions options)
            => EnsureInitialized(ref s_cachedComparers[options.FlagsValue], options);
    }
}
