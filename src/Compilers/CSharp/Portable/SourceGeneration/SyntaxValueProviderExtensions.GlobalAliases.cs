// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.SourceGeneration;
internal static partial class SyntaxValueProviderExtensions
{
    /// <summary>
    /// Simple wrapper class around an immutable array so we can have the value-semantics needed for the incremental
    /// generator to know when a change actually happened and it should run later transform stages.
    /// </summary>
    private class GlobalAliases : IEquatable<GlobalAliases>
    {
        public static readonly GlobalAliases Empty = new(ImmutableArray<(string aliasName, string symbolName)>.Empty);

        public readonly ImmutableArray<(string aliasName, string symbolName)> AliasAndSymbolNames;

        private int _hashCode;

        private GlobalAliases(ImmutableArray<(string aliasName, string symbolName)> aliasAndSymbolNames)
        {
            AliasAndSymbolNames = aliasAndSymbolNames;
        }

        public static GlobalAliases Create(ImmutableArray<(string aliasName, string symbolName)> aliasAndSymbolNames)
        {
            return aliasAndSymbolNames.IsEmpty ? Empty : new GlobalAliases(aliasAndSymbolNames);
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
            {
                var hashCode = 0;
                foreach (var tuple in this.AliasAndSymbolNames)
                    hashCode = Hash.Combine(tuple.GetHashCode(), hashCode);

                _hashCode = hashCode == 0 ? 1 : hashCode;
            }

            return _hashCode;
        }

        public override bool Equals(object? obj)
            => this.Equals(obj as GlobalAliases);

        public bool Equals(GlobalAliases? array)
        {
            if (array is null)
                return false;

            if (ReferenceEquals(this, array))
                return true;

            if (this.AliasAndSymbolNames == array.AliasAndSymbolNames)
                return true;

            if (this.AliasAndSymbolNames.Length != array.AliasAndSymbolNames.Length)
                return false;

            for (int i = 0, n = this.AliasAndSymbolNames.Length; i < n; i++)
            {
                if (this.AliasAndSymbolNames[i] != array.AliasAndSymbolNames[i])
                    return false;
            }

            return true;
        }
    }
}
