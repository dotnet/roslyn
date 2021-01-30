// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class DelegateSymbolKey
        {
            public static void Create(INamedTypeSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
                visitor.WriteInteger(symbol.Arity);
                visitor.WriteBoolean(symbol.IsUnboundGenericType);

                // If we're a constructed in some fashion, then write out the type args we were constructed with.
                if (!symbol.Equals(symbol.ConstructedFrom) && !symbol.IsUnboundGenericType)
                {
                    visitor.WriteSymbolKeyArray(symbol.TypeArguments);
                }
                else
                {
                    visitor.WriteSymbolKeyArray(ImmutableArray<ITypeSymbol>.Empty);
                }

                // Now, write out the parameter types for the delegate's original invoke method.  Ensure we push
                // ourselves into the visitor context so that any type parameters that reference us get written out as
                // an ordinal and do not cause circularities.
                visitor.PushDelegate(symbol);

                visitor.WriteRefKindArray(symbol.DelegateInvokeMethod!.Parameters);
                visitor.WriteParameterTypesArray(symbol.DelegateInvokeMethod!.Parameters);

                visitor.PopDelegate(symbol);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var metadataName = reader.ReadString()!;
                var containingSymbolResolution = reader.ReadSymbolKey(out var containingSymbolFailureReason);
                var arity = reader.ReadInteger();
                var isUnboundGenericType = reader.ReadBoolean();
                using var typeArguments = reader.ReadSymbolKeyArray<ITypeSymbol>(out var typeArgumentsFailureReason);
                using var refKinds = reader.ReadRefKindArray();

                if (containingSymbolFailureReason != null)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(containingSymbolFailureReason)} failed -> {containingSymbolFailureReason})";
                    return default;
                }

                if (typeArgumentsFailureReason != null)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(typeArguments)} failed -> {typeArgumentsFailureReason})";
                    return default;
                }

                if (typeArguments.IsDefault)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(typeArguments)} failed)";
                    return default;
                }

                if (refKinds.IsDefault)
                {
                    failureReason = $"({nameof(NamedTypeSymbolKey)} {nameof(refKinds)} failed)";
                    return default;
                }

                var typeArgumentArray = typeArguments.Count == 0
                    ? Array.Empty<ITypeSymbol>()
                    : typeArguments.Builder.ToArray();

                using var result = PooledArrayBuilder<INamedTypeSymbol>.GetInstance();
                using var _1 = ArrayBuilder<INamedTypeSymbol>.GetInstance(out var candidateDelegates);

                // Get all the candidate delegates.  These will be delegates with the  right name/arity, and whose
                // signature has the right number of parameters and refkinds.  We'll validate that the signature types
                // actually match after this.
                AddDelegatesInContainer(containingSymbolResolution, metadataName, arity, isUnboundGenericType, typeArgumentArray, refKinds, candidateDelegates);

                // For each delegate type that we look at, we'll have to resolve the parameter list of its 'Invoke'
                // method in the context of that delegate type.  i.e. if we have `delegate void Goo<T>(IList<T> list);
                // then we'll need to have marked that we're on the Goo<T> delegate type so that we know 'T' in IList<T>
                // resolves to.
                //
                // Because of this, we keep track of where we are in the reader.  Before resolving every parameter list,
                // we'll mark which method we're on and we'll rewind to this point.
                var beforeParametersPosition = reader.Position;

                foreach (var candidateDelegate in candidateDelegates)
                {
                    // Restore our position to right before the list of parameters.  Also, push this candidate into our
                    // delegate-resolution-stack so that we can properly resolve type parameter ordinals.
                    reader.Position = beforeParametersPosition;
                    reader.PushDelegate(candidateDelegate);

                    // Now try to read in the parameter types and compare them to the parameter types of our delegate.
                    using var originalParameterTypes = reader.ReadSymbolKeyArray<ITypeSymbol>(out _);

                    var match = reader.ParameterTypesMatch(candidateDelegate.DelegateInvokeMethod!.Parameters, originalParameterTypes)
                        ? candidateDelegate
                        : null;

                    reader.PopDelegate(candidateDelegate);

                    // Break on the first result we find.
                    if (match != null)
                    {
                        result.AddIfNotNull(match);
                        break;
                    }
                }

                if (reader.Position == beforeParametersPosition)
                {
                    // We didn't find any candidates.  We still need to stream through this signature so the reader is
                    // in a proper position.

                    // Push an null-delegate to our stack so that any delegate-type-parameters can at least be read (if
                    // not resolved) properly.
                    reader.PushDelegate(delegateType: null);

                    // read out the values.  We don't actually need to use them, but we have to effectively read past
                    // them in the string.

                    using var _2 = reader.ReadSymbolKeyArray<ITypeSymbol>(out _);

                    reader.PopDelegate(delegateType: null);
                }

                return CreateResolution(result, $"({nameof(NamedTypeSymbolKey)} failed)", out failureReason);
            }

            private static void AddDelegatesInContainer(
                SymbolKeyResolution containingSymbolResolution,
                string metadataName,
                int arity,
                bool isUnboundGenericType,
                ITypeSymbol[] typeArguments,
                PooledArrayBuilder<RefKind> refKinds,
                ArrayBuilder<INamedTypeSymbol> delegatesInContainer)
            {
                foreach (var nsOrType in containingSymbolResolution.OfType<INamespaceOrTypeSymbol>())
                {
                    foreach (var type in nsOrType.GetTypeMembers(GetName(metadataName), arity))
                    {
                        if (type?.TypeKind != TypeKind.Delegate)
                            continue;

                        // If the delegate doesn't have the right parameter-count/ref-kinds, ignore it.
                        if (!ParameterRefKindsMatch(type.DelegateInvokeMethod!.Parameters, refKinds))
                            continue;

                        // Instantiate the delegate as appropriate.
                        var currentType = typeArguments.Length > 0 ? type.Construct(typeArguments) : type;
                        currentType = isUnboundGenericType ? currentType.ConstructUnboundGenericType() : currentType;

                        delegatesInContainer.Add(currentType);
                    }
                }
            }
        }
    }
}
