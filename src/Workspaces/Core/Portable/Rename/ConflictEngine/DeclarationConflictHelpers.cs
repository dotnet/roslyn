// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine;

internal static class DeclarationConflictHelpers
{
    public static ImmutableArray<Location> GetMembersWithConflictingSignatures(IMethodSymbol renamedMethod, bool trimOptionalParameters, bool distinguishRefKind)
    {
        var potentiallyConflictingMethods =
            renamedMethod.ContainingType.GetMembers(renamedMethod.Name)
                                        .OfType<IMethodSymbol>()
                                        .Where(m => !m.Equals(renamedMethod) && m.Arity == renamedMethod.Arity);

        return GetConflictLocations(renamedMethod, potentiallyConflictingMethods, isMethod: true,
            method => GetAllSignatures(((IMethodSymbol)method).Parameters, trimOptionalParameters, distinguishRefKind));
    }

    public static ImmutableArray<Location> GetMembersWithConflictingSignatures(IPropertySymbol renamedProperty, bool trimOptionalParameters, bool distinguishRefKind)
    {
        var potentiallyConflictingProperties =
            renamedProperty.ContainingType.GetMembers(renamedProperty.Name)
                                        .OfType<IPropertySymbol>()
                                        .Where(m => !m.Equals(renamedProperty) && m.Parameters.Length == renamedProperty.Parameters.Length);

        return GetConflictLocations(renamedProperty, potentiallyConflictingProperties, isMethod: false,
            property => GetAllSignatures(((IPropertySymbol)property).Parameters, trimOptionalParameters, distinguishRefKind));
    }

    private readonly record struct ParameterSignature(ITypeSymbol Type, RefKind RefKind);

    private static ImmutableArray<Location> GetConflictLocations(ISymbol renamedMember,
        IEnumerable<ISymbol> potentiallyConflictingMembers,
        bool isMethod,
        Func<ISymbol, ImmutableArray<ImmutableArray<ParameterSignature>>> getAllSignatures)
    {
        var signatureToConflictingMember = new Dictionary<ImmutableArray<ParameterSignature>, ISymbol>(ConflictingSignatureComparer.Instance);

        foreach (var member in potentiallyConflictingMembers)
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
                // https://github.com/dotnet/roslyn/issues/73772: add other partial property part as conflicting symbol
                if (isMethod && conflictingSymbol is IMethodSymbol conflictingMethod && renamedMember is IMethodSymbol renamedMethod)
                {
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

    private sealed class ConflictingSignatureComparer : IEqualityComparer<ImmutableArray<ParameterSignature>>
    {
        public static readonly ConflictingSignatureComparer Instance = new();

        private ConflictingSignatureComparer() { }

        public bool Equals(ImmutableArray<ParameterSignature> x, ImmutableArray<ParameterSignature> y)
            => x.SequenceEqual(y);

        public int GetHashCode(ImmutableArray<ParameterSignature> obj)
        {
            // This is a very simple GetHashCode implementation. Doing something "fancier" here
            // isn't really worth it, since to compute a proper hash we'd end up walking all the
            // ParameterSignatures anyways.
            return obj.Length;
        }
    }

    private static ImmutableArray<ImmutableArray<ParameterSignature>> GetAllSignatures(ImmutableArray<IParameterSymbol> parameters, bool trimOptionalParameters, bool distinguishRefKind)
    {
        var resultBuilder = ArrayBuilder<ImmutableArray<ParameterSignature>>.GetInstance();

        var signatureBuilder = ArrayBuilder<ParameterSignature>.GetInstance();

        foreach (var parameter in parameters)
        {
            // In VB, a method effectively creates multiple signatures which are produced by
            // chopping off each of the optional parameters on the end, last to first, per 4.1.1 of
            // the spec.
            if (trimOptionalParameters && parameter.IsOptional)
            {
                resultBuilder.Add(signatureBuilder.ToImmutable());
            }

            var refKind = distinguishRefKind ? parameter.RefKind : RefKind.None;
            signatureBuilder.Add(new ParameterSignature(parameter.Type, refKind));
        }

        resultBuilder.Add(signatureBuilder.ToImmutableAndFree());
        return resultBuilder.ToImmutableAndFree();
    }
}
