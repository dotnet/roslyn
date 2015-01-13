// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class PropertySymbolKey : AbstractSymbolKey<PropertySymbolKey>
        {
            private readonly SymbolKey containerKey;
            private readonly string metadataName;
            private readonly RefKind[] refKinds;
            private readonly SymbolKey[] originalParameterTypeKeys;
            private readonly bool isIndexer;

            internal PropertySymbolKey(IPropertySymbol symbol, Visitor visitor)
            {
                this.containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                this.metadataName = symbol.MetadataName;
                this.isIndexer = symbol.IsIndexer;
                this.refKinds = symbol.Parameters.Select(p => p.RefKind).ToArray();
                this.originalParameterTypeKeys = symbol.OriginalDefinition.Parameters.Select(p => GetOrCreate(p.Type, visitor)).ToArray();
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);
                var namedTypes = GetAllSymbols<INamedTypeSymbol>(container);
                var properties = isIndexer
                    ? namedTypes.SelectMany(t => t.GetMembers()).OfType<IPropertySymbol>().Where(p => p.IsIndexer)
                    : namedTypes.SelectMany(t => t.GetMembers(this.metadataName)).OfType<IPropertySymbol>();
                properties = properties.Where(p => p.Parameters.Length == refKinds.Length);

                var comparisonOptions = new ComparisonOptions(compilation.IsCaseSensitive, ignoreAssemblyKey, compareMethodTypeParametersByName: true);
                var matchingProperties = properties.Where(p =>
                    ParametersMatch(comparisonOptions, compilation, p.OriginalDefinition.Parameters, refKinds, originalParameterTypeKeys, cancellationToken));

                return CreateSymbolInfo(matchingProperties);
            }

            internal override bool Equals(PropertySymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options.IgnoreCase, options.IgnoreAssemblyKey, compareMethodTypeParametersByName: true);
                return
                    other.isIndexer == this.isIndexer &&
                    other.refKinds.Length == this.refKinds.Length &&
                    Equals(options.IgnoreCase, other.metadataName, this.metadataName) &&
                    other.containerKey.Equals(this.containerKey, options) &&
                    other.refKinds.SequenceEqual(this.refKinds) &&
                    SequenceEquals(other.originalParameterTypeKeys, this.originalParameterTypeKeys, comparer);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // TODO(cyrusn): Consider hashing the parameters as well
                return
                    Hash.Combine(this.isIndexer,
                    Hash.Combine(this.refKinds.Length,
                    Hash.Combine(GetHashCode(options.IgnoreCase, this.metadataName),
                                 this.containerKey.GetHashCode(options))));
            }
        }
    }
}