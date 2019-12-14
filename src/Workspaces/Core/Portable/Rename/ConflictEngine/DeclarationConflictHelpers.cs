// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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

            return GetConflictLocations(renamedMethod, potentiallyConfictingMethods, isMethod: true,
                (method) => GetAllSignatures((method as IMethodSymbol).Parameters, trimOptionalParameters));
        }

        public static ImmutableArray<Location> GetMembersWithConflictingSignatures(IPropertySymbol renamedProperty, bool trimOptionalParameters)
        {
            var potentiallyConfictingProperties =
                renamedProperty.ContainingType.GetMembers(renamedProperty.Name)
                                            .OfType<IPropertySymbol>()
                                            .Where(m => !m.Equals(renamedProperty) && m.Parameters.Length == renamedProperty.Parameters.Length);

            return GetConflictLocations(renamedProperty, potentiallyConfictingProperties, isMethod: false,
                (property) => GetAllSignatures((property as IPropertySymbol).Parameters, trimOptionalParameters));
        }

        private static ImmutableArray<Location> GetConflictLocations(ISymbol renamedMember,
            IEnumerable<ISymbol> potentiallyConfictingMembers,
            bool isMethod,
            Func<ISymbol, ImmutableArray<ImmutableArray<ITypeSymbol>>> getAllSignatures)
        {
            var signatureToConflictingMember = new Dictionary<ImmutableArray<ITypeSymbol>, ISymbol>(ConflictingSignatureComparer.Instance);

            foreach (var member in potentiallyConfictingMembers)
            {
                foreach (var signature in getAllSignatures(member))
                {
                    signatureToConflictingMember[signature] = member;
                }
            }

            var builder = ArrayBuilder<Location>.GetInstance();

            foreach (var signature in getAllSignatures(renamedMember))
            {
                if (signatureToConflictingMember.TryGetValue(signature, out var conflictingSymbol))
                {
                    if (isMethod)
                    {
                        var conflictingMethod = conflictingSymbol as IMethodSymbol;
                        var renamedMethod = renamedMember as IMethodSymbol;
                        if (!(conflictingMethod.PartialDefinitionPart != null && Equals(conflictingMethod.PartialDefinitionPart, renamedMethod)) &&
                            !(conflictingMethod.PartialImplementationPart != null && Equals(conflictingMethod.PartialImplementationPart, renamedMethod)))
                        {
                            builder.AddRange(conflictingSymbol.Locations);
                        }
                    }
                    else
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

        private static ImmutableArray<ImmutableArray<ITypeSymbol>> GetAllSignatures(ImmutableArray<IParameterSymbol> parameters, bool trimOptionalParameters)
        {
            var resultBuilder = ArrayBuilder<ImmutableArray<ITypeSymbol>>.GetInstance();

            var signatureBuilder = ArrayBuilder<ITypeSymbol>.GetInstance();

            foreach (var parameter in parameters)
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
