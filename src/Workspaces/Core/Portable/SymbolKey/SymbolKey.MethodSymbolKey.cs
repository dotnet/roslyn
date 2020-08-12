// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;

namespace Microsoft.CodeAnalysis
{
    internal partial struct SymbolKey
    {
        private static class ReducedExtensionMethodSymbolKey
        {
            public static void Create(IMethodSymbol symbol, SymbolKeyWriter visitor)
            {
                Debug.Assert(symbol.Equals(symbol.ConstructedFrom));

                visitor.WriteSymbolKey(symbol.ReducedFrom);
                visitor.WriteSymbolKey(symbol.ReceiverType);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var reducedFromResolution = reader.ReadSymbolKey(out var reducedFromFailureReason);
                if (reducedFromFailureReason != null)
                {
                    failureReason = $"({nameof(ReducedExtensionMethodSymbolKey)} {nameof(reducedFromResolution)} failed -> {reducedFromFailureReason})";
                    return default;
                }

                var receiverTypeResolution = reader.ReadSymbolKey(out var receiverTypeFailureReason);
                if (receiverTypeFailureReason != null)
                {
                    failureReason = $"({nameof(ReducedExtensionMethodSymbolKey)} {nameof(receiverTypeResolution)} failed -> {receiverTypeFailureReason})";
                    return default;
                }

                using var result = PooledArrayBuilder<IMethodSymbol>.GetInstance();
                foreach (var reducedFrom in reducedFromResolution.OfType<IMethodSymbol>())
                {
                    foreach (var receiverType in receiverTypeResolution.OfType<ITypeSymbol>())
                    {
                        result.AddIfNotNull(reducedFrom.ReduceExtensionMethod(receiverType));
                    }
                }

                return CreateResolution(result, $"({nameof(ReducedExtensionMethodSymbolKey)} failed)", out failureReason);
            }
        }
    }

    internal partial struct SymbolKey
    {
        private static class ConstructedMethodSymbolKey
        {
            public static void Create(IMethodSymbol symbol, SymbolKeyWriter visitor)
            {
                visitor.WriteSymbolKey(symbol.ConstructedFrom);
                visitor.WriteSymbolKeyArray(symbol.TypeArguments);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var constructedFrom = reader.ReadSymbolKey(out var constructedFromFailureReason);
                if (constructedFromFailureReason != null)
                {
                    failureReason = $"({nameof(ConstructedMethodSymbolKey)} {nameof(constructedFrom)} failed -> {constructedFromFailureReason})";
                    return default;
                }

                using var typeArguments = reader.ReadSymbolKeyArray<ITypeSymbol>(out var typeArgumentsFailureReason);
                if (typeArgumentsFailureReason != null)
                {
                    failureReason = $"({nameof(ConstructedMethodSymbolKey)} {nameof(typeArguments)} failed -> {typeArgumentsFailureReason})";
                    return default;
                }

                if (constructedFrom.SymbolCount == 0 || typeArguments.IsDefault)
                {
                    failureReason = $"({nameof(ConstructedMethodSymbolKey)} {nameof(typeArguments)} failed -> 'constructedFrom.SymbolCount == 0 || typeArguments.IsDefault')";
                    return default;
                }

                var typeArgumentArray = typeArguments.Builder.ToArray();

                using var result = PooledArrayBuilder<IMethodSymbol>.GetInstance();
                foreach (var method in constructedFrom.OfType<IMethodSymbol>())
                {
                    if (method.TypeParameters.Length == typeArgumentArray.Length)
                    {
                        result.AddIfNotNull(method.Construct(typeArgumentArray));
                    }
                }

                return CreateResolution(result, $"({nameof(ConstructedMethodSymbolKey)} could not successfully construct)", out failureReason);
            }
        }
    }

    internal partial struct SymbolKey
    {
        private static class MethodSymbolKey
        {
            public static void Create(IMethodSymbol symbol, SymbolKeyWriter visitor)
            {
                Debug.Assert(symbol.Equals(symbol.ConstructedFrom));

                visitor.WriteString(symbol.MetadataName);
                visitor.WriteSymbolKey(symbol.ContainingSymbol);
                visitor.WriteInteger(symbol.Arity);
                visitor.WriteBoolean(symbol.PartialDefinitionPart != null);
                visitor.WriteRefKindArray(symbol.Parameters);

                // Mark that we're writing out the signature of a method.  This way if we hit a 
                // method type parameter in our parameter-list or return type, we won't recurse
                // into it, but will instead only write out the type parameter ordinal.  This
                // happens with cases like Goo<T>(T t);
                visitor.PushMethod(symbol);

                visitor.WriteParameterTypesArray(symbol.OriginalDefinition.Parameters);

                if (symbol.MethodKind == MethodKind.Conversion)
                {
                    visitor.WriteSymbolKey(symbol.ReturnType);
                }
                else
                {
                    visitor.WriteSymbolKey(null);
                }

                // Done writing the signature of this method.  Remove it from the set of methods
                // we're writing signatures for.
                visitor.PopMethod(symbol);
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader, out string? failureReason)
            {
                var metadataName = reader.ReadString()!;

                var containingType = reader.ReadSymbolKey(out var containingTypeFailureReason);
                var arity = reader.ReadInteger();
                var isPartialMethodImplementationPart = reader.ReadBoolean();
                using var parameterRefKinds = reader.ReadRefKindArray();

                // For each method that we look at, we'll have to resolve the parameter list and
                // return type in the context of that method.  i.e. if we have Goo<T>(IList<T> list)
                // then we'll need to have marked that we're on the Goo<T> method so that we know 
                // 'T' in IList<T> resolves to.
                //
                // Because of this, we keep track of where we are in the reader.  Before resolving
                // every parameter list, we'll mark which method we're on and we'll rewind to this
                // point.
                var beforeParametersPosition = reader.Position;

                using var methods = GetMembersOfNamedType<IMethodSymbol>(containingType, metadataName: null);
                using var result = PooledArrayBuilder<IMethodSymbol>.GetInstance();

                foreach (var candidate in methods)
                {
                    var method = Resolve(reader, metadataName, arity, isPartialMethodImplementationPart,
                        parameterRefKinds, beforeParametersPosition, candidate);

                    // Note: after finding the first method that matches we stop.  That's necessary
                    // as we cache results while searching.  We don't want to override these positive
                    // matches with a negative ones if we were to continue searching.
                    if (method != null)
                    {
                        result.AddIfNotNull(method);
                        break;
                    }
                }

                if (reader.Position == beforeParametersPosition)
                {
                    // We didn't find any candidates.  We still need to stream through this
                    // method signature so the reader is in a proper position.

                    // Push an null-method to our stack so that any method-type-parameters
                    // can at least be read (if not resolved) properly.
                    reader.PushMethod(method: null);

                    // read out the values.  We don't actually need to use them, but we have
                    // to effectively read past them in the string.

                    using (reader.ReadSymbolKeyArray<ITypeSymbol>(out _))
                    {
                        _ = reader.ReadSymbolKey(out _);
                    }

                    reader.PopMethod(method: null);
                }

                if (containingTypeFailureReason != null)
                {
                    failureReason = $"({nameof(MethodSymbolKey)} {nameof(containingType)} failed -> {containingTypeFailureReason})";
                    return default;
                }

                return CreateResolution(result, $"({nameof(MethodSymbolKey)} '{metadataName}' not found)", out failureReason);
            }

            private static IMethodSymbol? Resolve(
                SymbolKeyReader reader, string metadataName, int arity, bool isPartialMethodImplementationPart,
                PooledArrayBuilder<RefKind> parameterRefKinds, int beforeParametersPosition,
                IMethodSymbol method)
            {
                if (method.Arity == arity &&
                    method.MetadataName == metadataName &&
                    ParameterRefKindsMatch(method.Parameters, parameterRefKinds))
                {
                    // Method looks like a potential match.  It has the right arity, name and 
                    // refkinds match.  We now need to do the more complicated work of checking
                    // the parameters (and possibly the return type).  This is more complicated 
                    // because those symbols might refer to method type parameters.  In order
                    // for resolution to work on those type parameters, we have to keep track
                    // in the reader that we're resolving this method. 

                    // Restore our position to right before the list of parameters.  Also, push
                    // this method into our method-resolution-stack so that we can properly resolve
                    // method type parameter ordinals.
                    reader.Position = beforeParametersPosition;
                    reader.PushMethod(method);

                    var result = Resolve(reader, isPartialMethodImplementationPart, method);

                    reader.PopMethod(method);

                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }

            private static IMethodSymbol? Resolve(
                SymbolKeyReader reader, bool isPartialMethodImplementationPart, IMethodSymbol method)
            {
                using var originalParameterTypes = reader.ReadSymbolKeyArray<ITypeSymbol>(out _);
                var returnType = (ITypeSymbol?)reader.ReadSymbolKey(out _).GetAnySymbol();

                if (reader.ParameterTypesMatch(method.OriginalDefinition.Parameters, originalParameterTypes))
                {
                    if (returnType == null ||
                        reader.Comparer.Equals(returnType, method.ReturnType))
                    {
                        if (isPartialMethodImplementationPart)
                        {
                            method = method.PartialImplementationPart ?? method;
                        }

                        Debug.Assert(method != null);
                        return method;
                    }
                }

                return null;
            }
        }
    }
}
