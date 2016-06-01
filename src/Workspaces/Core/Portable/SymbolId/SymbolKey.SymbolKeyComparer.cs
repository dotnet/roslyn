﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    internal partial class SymbolKey
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
                if (x == y)
                {
                    return true;
                }

                if (x == null || y == null)
                {
                    return false;
                }

                var comparer = _options.IgnoreCase
                    ? StringComparer.OrdinalIgnoreCase
                    : StringComparer.Ordinal;

                if (!_options.IgnoreAssemblyKey)
                {
                    // Easiest case.  We can directly compare the raw contents of the keys.
                    return comparer.Equals(x._symbolKeyData, y._symbolKeyData);
                }
                else
                {
                    // This is harder.  To compare these we need to remove the entries related to 
                    // assemblies.
                    var data1 = x.GetSymbolKeyDataWithoutAssemblies();
                    var data2 = y.GetSymbolKeyDataWithoutAssemblies();

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
            {
                if (obj == null)
                {
                    return 0;
                }

                var comparer = _options.IgnoreCase
                    ? StringComparer.OrdinalIgnoreCase
                    : StringComparer.Ordinal;

                if (!_options.IgnoreAssemblyKey)
                {
                    // Easiest case.  We can directly compare the raw contents of the keys.
                    return comparer.GetHashCode(obj._symbolKeyData);
                }
                else
                {
                    // This is harder.  To compare these we need to remove the entries related to 
                    // assemblies.
                    var data1 = obj.GetSymbolKeyDataWithoutAssemblies();

                    return comparer.GetHashCode(data1);
                }

            }

            public static IEqualityComparer<SymbolKey> GetComparer(bool ignoreCase, bool ignoreAssemblyKey)
            {
                return GetComparer(new ComparisonOptions(ignoreCase, ignoreAssemblyKey));
            }

            private static readonly SymbolKeyComparer[] s_cachedComparers = new SymbolKeyComparer[4];

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