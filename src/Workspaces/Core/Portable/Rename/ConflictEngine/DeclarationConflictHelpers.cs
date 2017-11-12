// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    internal static class DeclarationConflictHelpers
    {
        public static ImmutableArray<Location> GetMembersWithConflictingSignatures(IMethodSymbol renamedMethod, bool trimOptionalParameters)
        {
            var potentiallyConfictingMethods =
                renamedMethod.ContainingType.GetMembers(renamedMethod.Name)
                                            .OfType<IMethodSymbol>()
                                            .Where(m => !m.Equals(renamedMethod) && m.Arity == renamedMethod.Arity);

            var signatureToConflictingMember = new Dictionary<ImmutableArray<ITypeSymbol>, IMethodSymbol>(ConflictingSignatureComparer.Instance);

            foreach (var method in potentiallyConfictingMethods)
            {
                foreach (var signature in GetAllSignatures(method, trimOptionalParameters))
                {
                    signatureToConflictingMember[signature] = method;
                }
            }

            var builder = ArrayBuilder<Location>.GetInstance();

            foreach (var signature in GetAllSignatures(renamedMethod, trimOptionalParameters))
            {
                if (signatureToConflictingMember.TryGetValue(signature, out var conflictingSymbol))
                {
                    if (!(conflictingSymbol.PartialDefinitionPart != null && conflictingSymbol.PartialDefinitionPart == renamedMethod) &&
                        !(conflictingSymbol.PartialImplementationPart != null && conflictingSymbol.PartialImplementationPart == renamedMethod))
                    {
                        builder.AddRange(conflictingSymbol.Locations);
                    }
                }
            }

            return builder.ToImmutableAndFree();
        }

        private sealed class ConflictingSignatureComparer : IEqualityComparer<ImmutableArray<ITypeSymbol>>
        {
            public static readonly ConflictingSignatureComparer Instance = new ConflictingSignatureComparer();

            private ConflictingSignatureComparer() { }

            public bool Equals(ImmutableArray<ITypeSymbol> x, ImmutableArray<ITypeSymbol> y)
            {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(ImmutableArray<ITypeSymbol> obj)
            {
                // This is a very simple GetHashCode implementation. Doing something "fancier" here  
                // isn't really worth it, since to compute a proper hash we'd end up walking all the  
                // ITypeSymbols anyways.  
                return obj.Length;
            }
        }

        private static ImmutableArray<ImmutableArray<ITypeSymbol>> GetAllSignatures(IMethodSymbol method, bool trimOptionalParameters)
        {
            var resultBuilder = ArrayBuilder<ImmutableArray<ITypeSymbol>>.GetInstance();

            var signatureBuilder = ArrayBuilder<ITypeSymbol>.GetInstance();

            if (method.MethodKind == MethodKind.Conversion)
            {
                signatureBuilder.Add(method.ReturnType);
            }

            foreach (var parameter in method.Parameters)
            {
                // In VB, a method effectively creates multiple signatures which are produced by
                // chopping off each of the optional parameters on the end, last to first, per 4.1.1 of
                // the spec.
                if (trimOptionalParameters && parameter.IsOptional)
                {
                    resultBuilder.Add(signatureBuilder.ToImmutable());
                }

                signatureBuilder.Add(parameter.Type);
            }

            resultBuilder.Add(signatureBuilder.ToImmutableAndFree());
            return resultBuilder.ToImmutableAndFree();
        }
    }
}
