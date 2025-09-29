// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis;

internal partial struct SymbolKey
{
    private sealed class ReducedExtensionMethodSymbolKey : AbstractSymbolKey<IMethodSymbol>
    {
        public static readonly ReducedExtensionMethodSymbolKey Instance = new();

        public sealed override void Create(IMethodSymbol symbol, SymbolKeyWriter visitor)
        {
            Debug.Assert(symbol.Equals(symbol.ConstructedFrom));

            visitor.WriteSymbolKey(symbol.ReducedFrom);
            visitor.WriteSymbolKey(symbol.ReceiverType);
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IMethodSymbol? contextualMethod, out string? failureReason)
        {
            var reducedFromResolution = reader.ReadSymbolKey(contextualMethod?.ReducedFrom, out var reducedFromFailureReason);
            var receiverTypeResolution = reader.ReadSymbolKey(contextualMethod?.ReceiverType, out var receiverTypeFailureReason);

            if (reducedFromFailureReason != null)
            {
                failureReason = $"({nameof(ReducedExtensionMethodSymbolKey)} {nameof(reducedFromResolution)} failed -> {reducedFromFailureReason})";
                return default;
            }

            if (receiverTypeFailureReason != null)
            {
                failureReason = $"({nameof(ReducedExtensionMethodSymbolKey)} {nameof(receiverTypeResolution)} failed -> {receiverTypeFailureReason})";
                return default;
            }

            using var result = PooledArrayBuilder<IMethodSymbol>.GetInstance();
            foreach (var reducedFrom in reducedFromResolution.OfType<IMethodSymbol>())
            {
                foreach (var receiverType in receiverTypeResolution.OfType<ITypeSymbol>())
                    result.AddIfNotNull(reducedFrom.ReduceExtensionMethod(receiverType));
            }

            return CreateResolution(result, $"({nameof(ReducedExtensionMethodSymbolKey)} failed)", out failureReason);
        }
    }
}

internal partial struct SymbolKey
{
    private sealed class ConstructedMethodSymbolKey : AbstractSymbolKey<IMethodSymbol>
    {
        public static readonly ConstructedMethodSymbolKey Instance = new();

        public sealed override void Create(IMethodSymbol symbol, SymbolKeyWriter visitor)
        {
            visitor.WriteSymbolKey(symbol.ConstructedFrom);
            visitor.WriteSymbolKeyArray(symbol.TypeArguments);
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IMethodSymbol? contextualMethod, out string? failureReason)
        {
            var constructedFrom = reader.ReadSymbolKey(contextualMethod?.ConstructedFrom, out var constructedFromFailureReason);

            using var typeArguments = reader.ReadSymbolKeyArray<IMethodSymbol, ITypeSymbol>(
                contextualMethod,
                getContextualSymbol: static (contextualMethod, i) => SafeGet(contextualMethod.TypeArguments, i),
                out var typeArgumentsFailureReason);

            if (constructedFromFailureReason != null)
            {
                failureReason = $"({nameof(ConstructedMethodSymbolKey)} {nameof(constructedFrom)} failed -> {constructedFromFailureReason})";
                return default;
            }

            if (typeArgumentsFailureReason != null)
            {
                failureReason = $"({nameof(ConstructedMethodSymbolKey)} {nameof(typeArguments)} failed -> {typeArgumentsFailureReason})";
                return default;
            }

            if (constructedFrom.SymbolCount == 0)
            {
                failureReason = $"({nameof(ConstructedMethodSymbolKey)} {nameof(typeArguments)} failed -> 'constructedFrom.SymbolCount == 0')";
                return default;
            }

            if (typeArguments.IsDefault)
            {
                failureReason = $"({nameof(ConstructedMethodSymbolKey)} {nameof(typeArguments)} failed -> 'typeArguments.IsDefault')";
                return default;
            }

            var typeArgumentArray = typeArguments.Builder.ToArray();

            using var result = PooledArrayBuilder<IMethodSymbol>.GetInstance();
            foreach (var method in constructedFrom.OfType<IMethodSymbol>())
            {
                if (!method.Equals(method.ConstructedFrom))
                    continue;

                if (method.Arity == 0)
                    continue;

                if (method.TypeParameters.Length != typeArgumentArray.Length)
                    continue;

                result.AddIfNotNull(method.Construct(typeArgumentArray));
            }

            return CreateResolution(result, $"({nameof(ConstructedMethodSymbolKey)} could not successfully construct)", out failureReason);
        }
    }
}

internal partial struct SymbolKey
{
    private sealed class MethodSymbolKey : AbstractSymbolKey<IMethodSymbol>
    {
        public static readonly MethodSymbolKey Instance = new();

        public sealed override void Create(IMethodSymbol symbol, SymbolKeyWriter visitor)
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

            if (symbol.MethodKind == MethodKind.Conversion)
            {
                visitor.WriteSymbolKey(symbol.ReturnType);
            }
            else
            {
                visitor.WriteSymbolKey(null);
            }

            visitor.WriteParameterTypesArray(symbol.OriginalDefinition.Parameters);

            // Done writing the signature of this method.  Remove it from the set of methods
            // we're writing signatures for.
            visitor.PopMethod(symbol);
        }

        protected sealed override SymbolKeyResolution Resolve(
            SymbolKeyReader reader, IMethodSymbol? contextualSymbol, out string? failureReason)
        {
            var metadataName = reader.ReadRequiredString();

            var containingType = reader.ReadSymbolKey(contextualSymbol?.ContainingSymbol, out var containingTypeFailureReason);
            var arity = reader.ReadInteger();
            var isPartialImplementationPart = reader.ReadBoolean();
            using var parameterRefKinds = reader.ReadRefKindArray();

            // For each method that we look at, we'll have to resolve the parameter list and
            // return type in the context of that method.  i.e. if we have Goo<T>(IList<T> list)
            // then we'll need to have marked that we're on the Goo<T> method so that we know 
            // 'T' in IList<T> resolves to.
            //
            // Because of this, we keep track of where we are in the reader.  Before resolving
            // every parameter list, we'll mark which method we're on and we'll rewind to this
            // point.
            var beforeReturnTypeAndParameters = reader.Position;

            using var methods = GetMembersOfNamedType<IMethodSymbol>(containingType, metadataName: null);
            IMethodSymbol? method = null;
            foreach (var candidate in methods)
            {
                if (candidate.Arity != arity ||
                    candidate.MetadataName != metadataName ||
                    !ParameterRefKindsMatch(candidate.Parameters, parameterRefKinds))
                {
                    continue;
                }

                // Method looks like a potential match.  It has the right arity, name and refkinds match.  We now
                // need to do the more complicated work of checking the parameters (and possibly the return type).
                // This is more complicated because those symbols might refer to method type parameters.  In order
                // for resolution to work on those type parameters, we have to keep track in the reader that we're
                // resolving this method. 
                using (reader.PushMethod(candidate))
                {
                    method = Resolve(reader, isPartialImplementationPart, candidate);
                }

                // Note: after finding the first method that matches we stop.  That's necessary as we cache results
                // while searching.  We don't want to override these positive matches with a negative ones if we
                // were to continue searching.
                if (method != null)
                    break;

                // reset ourselves so we can check the return-type/parameters against the next candidate.
                reader.Position = beforeReturnTypeAndParameters;
            }

            if (reader.Position == beforeReturnTypeAndParameters)
            {
                // We didn't find a match.  Read through the stream one final time so we're at the correct location
                // after this MethodSymbolKey.

                // Push an null-method to our stack so that any method-type-parameters can at least be read (if not
                // resolved) properly.
                using var _1 = reader.PushMethod(method: null);

                // Read the return type.
                _ = reader.ReadSymbolKey(contextualSymbol: null, out _);

                // then the parameter types.
                _ = reader.ReadSymbolKeyArray<IMethodSymbol, ITypeSymbol>(
                    contextualSymbol: null, getContextualSymbol: null, failureReason: out _);
            }

            if (containingTypeFailureReason != null)
            {
                failureReason = $"({nameof(MethodSymbolKey)} {nameof(containingType)} failed -> {containingTypeFailureReason})";
                return default;
            }

            if (method == null)
            {
                failureReason = $"({nameof(MethodSymbolKey)} '{metadataName}' not found)";
                return default;
            }

            failureReason = null;
            return new SymbolKeyResolution(method);
        }

        private static IMethodSymbol? Resolve(
            SymbolKeyReader reader, bool isPartialImplementationPart, IMethodSymbol method)
        {
            var returnType = (ITypeSymbol?)reader.ReadSymbolKey(contextualSymbol: method.ReturnType, out _).GetAnySymbol();
            if (returnType != null &&
                !reader.Comparer.Equals(returnType, method.ReturnType))
            {
                // ok to early exit here, the topmost caller will reset the position in the stream accordingly.
                return null;
            }

            if (reader.ParameterTypesMatch(
                    method,
                    getContextualType: static (method, i) => SafeGet(method.Parameters, i)?.Type,
                    method.OriginalDefinition.Parameters))
            {
                if (isPartialImplementationPart)
                    method = method.PartialImplementationPart ?? method;

                Debug.Assert(method != null);
                return method;
            }

            return null;
        }
    }
}
