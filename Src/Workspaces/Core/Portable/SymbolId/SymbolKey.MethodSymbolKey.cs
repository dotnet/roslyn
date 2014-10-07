// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal abstract partial class SymbolKey
    {
        private class MethodSymbolKey : AbstractSymbolKey<MethodSymbolKey>
        {
            private readonly SymbolKey containerKey;
            private readonly string metadataName;
            private readonly int arity;
            private readonly SymbolKey[] typeArgumentKeysOpt;
            private readonly RefKind[] refKinds;
            private readonly SymbolKey[] originalParameterTypeKeys;
            private readonly bool isPartialMethodImplementationPart;
            private readonly SymbolKey returnType;

            internal MethodSymbolKey(IMethodSymbol symbol, Visitor visitor)
            {
                // First thing we do when creating the symbol ID for a method is create symbol IDs
                // for it's type parameters.  However, we pass ourselves to that type parameter so
                // that it does not recurse back to us.
                foreach (var typeParameter in symbol.TypeParameters)
                {
                    var symbolId = new MethodTypeParameterSymbol(this, typeParameter);
                    visitor.SymbolCache.Add(typeParameter, symbolId);
                }

                this.isPartialMethodImplementationPart = symbol.PartialDefinitionPart != null;

                this.containerKey = GetOrCreate(symbol.ContainingSymbol, visitor);
                this.metadataName = symbol.MetadataName;
                this.arity = symbol.Arity;
                this.refKinds = symbol.Parameters.Select(p => p.RefKind).ToArray();
                this.originalParameterTypeKeys = symbol.OriginalDefinition.Parameters.Select(p => GetOrCreate(p.Type, visitor)).ToArray();

                if (!symbol.Equals(symbol.ConstructedFrom))
                {
                    this.typeArgumentKeysOpt = symbol.TypeArguments.Select(t => GetOrCreate(t, visitor)).ToArray();
                }

                // If this is conversion operator, we must also compare the return type. Otherwise, we'll return
                // an ambiguous result in the case that there are multiple conversions from the same type to different
                // types.
                this.returnType = symbol.MethodKind == MethodKind.Conversion
                    ? GetOrCreate(symbol.ReturnType, visitor)
                    : null;
            }

            public override SymbolKeyResolution Resolve(Compilation compilation, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var container = containerKey.Resolve(compilation, ignoreAssemblyKey, cancellationToken);

                var methods = GetAllSymbols<INamedTypeSymbol>(container).SelectMany(t => Resolve(compilation, t, ignoreAssemblyKey, cancellationToken));
                return CreateSymbolInfo(methods);
            }

            private IEnumerable<IMethodSymbol> Resolve(Compilation compilation, INamedTypeSymbol container, bool ignoreAssemblyKey, CancellationToken cancellationToken)
            {
                var comparisonOptions = new ComparisonOptions(compilation.IsCaseSensitive, ignoreAssemblyKey, compareMethodTypeParametersByName: true);
                ITypeSymbol[] typeArguments = null;

                if (typeArgumentKeysOpt != null)
                {
                    typeArguments = typeArgumentKeysOpt.Select(a => a.Resolve(compilation).Symbol as ITypeSymbol).ToArray();

                    if (typeArguments.Any(a => a == null))
                    {
                        yield break;
                    }
                }

                foreach (var method in container.GetMembers().OfType<IMethodSymbol>())
                {
                    // Quick checks first
                    if (method.MetadataName != this.metadataName || method.Arity != this.arity || method.Parameters.Length != this.originalParameterTypeKeys.Length)
                    {
                        continue;
                    }

                    // Is this a conversion operator? If so, we must also compare the return type.
                    if (this.returnType != null)
                    {
                        if (!this.returnType.Equals(SymbolKey.Create(method.ReturnType, compilation, cancellationToken), comparisonOptions))
                        {
                            continue;
                        }
                    }

                    if (!ParametersMatch(comparisonOptions, compilation, method.OriginalDefinition.Parameters, refKinds, originalParameterTypeKeys, cancellationToken))
                    {
                        continue;
                    }

                    // It matches, so let's return it, but we might have to do some construction first
                    var methodToReturn = method;

                    if (isPartialMethodImplementationPart)
                    {
                        methodToReturn = methodToReturn.PartialImplementationPart ?? methodToReturn;
                    }

                    if (typeArguments != null)
                    {
                        methodToReturn = methodToReturn.Construct(typeArguments);
                    }

                    yield return methodToReturn;
                }
            }

            internal override bool Equals(MethodSymbolKey other, ComparisonOptions options)
            {
                var comparer = SymbolKeyComparer.GetComparer(options.IgnoreCase, options.IgnoreAssemblyKey, compareMethodTypeParametersByName: true);
                return
                    other.arity == this.arity &&
                    other.refKinds.Length == this.refKinds.Length &&
                    other.isPartialMethodImplementationPart == this.isPartialMethodImplementationPart &&
                    Equals(options.IgnoreCase, other.metadataName, this.metadataName) &&
                    other.containerKey.Equals(this.containerKey, options) &&
                    other.refKinds.SequenceEqual(this.refKinds) &&
                    other.originalParameterTypeKeys.SequenceEqual(this.originalParameterTypeKeys, comparer) &&
                    SequenceEquals(other.typeArgumentKeysOpt, this.typeArgumentKeysOpt, comparer);
            }

            internal override int GetHashCode(ComparisonOptions options)
            {
                // TODO: Consider hashing the parameters as well
                return
                    Hash.Combine(this.arity,
                    Hash.Combine(this.refKinds.Length,
                    Hash.Combine(this.isPartialMethodImplementationPart,
                    Hash.Combine(GetHashCode(options.IgnoreCase, this.metadataName),
                                 this.containerKey.GetHashCode(options)))));
            }
        }
    }
}