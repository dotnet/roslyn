// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

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

            public static int GetHashCode(GetHashCodeReader reader)
            {
                return Hash.Combine(reader.ReadSymbolKey(),
                                    reader.ReadSymbolKey());
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var reducedFromResolution = reader.ReadSymbolKey();
                var receiverTypeResolution = reader.ReadSymbolKey();

                var q = from m in reducedFromResolution.GetAllSymbols().OfType<IMethodSymbol>()
                        from t in receiverTypeResolution.GetAllSymbols().OfType<ITypeSymbol>()
                        let r = m.ReduceExtensionMethod(t)
                        select r;

                return CreateSymbolInfo(q);
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

            public static int GetHashCode(GetHashCodeReader reader)
            {
                return Hash.Combine(
                    reader.ReadSymbolKey(),
                    reader.ReadSymbolKeyArrayHashCode());
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var constructedFromResolution = reader.ReadSymbolKey();
                var typeArgumentResolutions = reader.ReadSymbolKeyArray();

                Debug.Assert(!typeArgumentResolutions.IsDefault);
                var typeArguments = typeArgumentResolutions.Select(
                    r => GetFirstSymbol<ITypeSymbol>(r)).ToArray();

                if (typeArguments.Any(s_typeIsNull))
                {
                    return default(SymbolKeyResolution);
                }

                var result = constructedFromResolution.GetAllSymbols()
                       .OfType<IMethodSymbol>()
                       .Select(m => m.Construct(typeArguments));

                return CreateSymbolInfo(result);
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
                // happens with cases like Foo<T>(T t);
                Debug.Assert(!visitor.WritingSignature);
                visitor.WritingSignature = true;

                visitor.WriteParameterTypesArray(symbol.OriginalDefinition.Parameters);

                if (symbol.MethodKind == MethodKind.Conversion)
                {
                    visitor.WriteSymbolKey(symbol.ReturnType);
                }
                else
                {
                    visitor.WriteSymbolKey(null);
                }

                // Done writing the signature.  Go back to normal mode.
                Debug.Assert(visitor.WritingSignature);
                visitor.WritingSignature = false;
            }

            public static int GetHashCode(GetHashCodeReader reader)
            {
                return Hash.Combine(reader.ReadString(),
                       Hash.Combine(reader.ReadSymbolKey(),
                       Hash.Combine(reader.ReadInteger(),
                       Hash.Combine(reader.ReadBoolean(),
                       Hash.Combine(reader.ReadRefKindArrayHashCode(),
                       Hash.Combine(reader.ReadSymbolKeyArrayHashCode(),
                                    reader.ReadSymbolKey()))))));
            }

            public static SymbolKeyResolution Resolve(SymbolKeyReader reader)
            {
                var metadataName = reader.ReadString();
                var containingSymbolResolution = reader.ReadSymbolKey();
                var arity = reader.ReadInteger();
                var isPartialMethodImplementationPart = reader.ReadBoolean();
                var parameterRefKinds = reader.ReadRefKindArray();

                // For each method that we look at, we'll have to resolve the parameter list and
                // return type in the context of that method.  i.e. if we have Foo<T>(IList<T> list)
                // then we'll need to have marked that we're on the Foo<T> method so that we know 
                // 'T' in IList<T> resolves to.
                //
                // Because of this, we keep track of where we are in the reader.  Before resolving
                // every parameter list, we'll mark which method we're on and we'll rewind to this
                // point.
                var beforeParametersPosition = reader.Position;

                var result = new List<IMethodSymbol>();

                var namedTypes = containingSymbolResolution.GetAllSymbols().OfType<INamedTypeSymbol>();
                foreach (var namedType in namedTypes)
                {
                    var method = Resolve(reader, metadataName, arity, isPartialMethodImplementationPart,
                        parameterRefKinds, beforeParametersPosition, namedType);

                    // Note: after finding the first method that matches we stop.  That's necessary
                    // as we cache results while searching.  We don't want to override these positive
                    // matches with a negative ones if we were to continue searching.
                    if (method != null)
                    {
                        result.Add(method);
                        break;
                    }
                }

                if (reader.Position == beforeParametersPosition)
                {
                    // We didn't find any candidates.  We still need to stream through this
                    // method signature so the reader is in a proper position.
                    var parameterTypeResolutions = reader.ReadSymbolKeyArray();
                    var returnType = GetFirstSymbol<ITypeSymbol>(reader.ReadSymbolKey());
                }

                return CreateSymbolInfo(result);
            }

            private static IMethodSymbol Resolve(
                SymbolKeyReader reader, string metadataName, int arity, bool isPartialMethodImplementationPart,
                ImmutableArray<RefKind> parameterRefKinds, int beforeParametersPosition,
                INamedTypeSymbol namedType)
            {
                foreach (var method in namedType.GetMembers().OfType<IMethodSymbol>())
                {
                    var result = Resolve(reader, metadataName, arity, isPartialMethodImplementationPart,
                        parameterRefKinds, beforeParametersPosition, method);

                    if (result != null)
                    {
                        return result; 
                    }
                }

                return null;
            }

            private static IMethodSymbol Resolve(
                SymbolKeyReader reader, string metadataName, int arity, bool isPartialMethodImplementationPart,
                ImmutableArray<RefKind> parameterRefKinds, int beforeParametersPosition,
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
                    // in the reader that we're on this specific method.

                    // Restore our position to right before the list of parameters.
                    // Also set the current method so that we can properly resolve
                    // method type parameter ordinals.
                    reader.Position = beforeParametersPosition;

                    Debug.Assert(reader.CurrentMethod == null);
                    reader.CurrentMethod = method;

                    var result = Resolve(reader, isPartialMethodImplementationPart, method);

                    Debug.Assert(reader.CurrentMethod == method);
                    reader.CurrentMethod = null;

                    if (result != null)
                    {
                        return result;
                    }
                }

                return null;
            }

            private static IMethodSymbol Resolve(
                SymbolKeyReader reader, bool isPartialMethodImplementationPart,
                IMethodSymbol method)
            {
                var originalParameterTypeResolutions = reader.ReadSymbolKeyArray();
                var returnType = GetFirstSymbol<ITypeSymbol>(reader.ReadSymbolKey());

                var originalParameterTypes = originalParameterTypeResolutions.Select(
                    r => GetFirstSymbol<ITypeSymbol>(r)).ToArray();

                if (!originalParameterTypes.Any(s_typeIsNull))
                {
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
                }

                return null;
            }
        }
    }
}