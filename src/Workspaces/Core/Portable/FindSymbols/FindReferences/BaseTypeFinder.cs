﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.FindReferences
{
    internal static partial class BaseTypeFinder
    {
        public static async Task<ImmutableArray<SymbolAndProjectId>> FindBaseTypesAndInterfacesAsync(
            INamedTypeSymbol type, Project project, CancellationToken cancellationToken)
        {
            var typesAndInterfaces = FindBaseTypesAndInterfaces(type);
            return await ConvertToSymbolAndProjectIdsAsync(typesAndInterfaces.CastArray<ISymbol>(), project, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<ImmutableArray<SymbolAndProjectId>> FindOverriddenAndImplementedMembersAsync(
            ISymbol symbol, Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var baseClassesAndInterfaces = FindBaseTypesAndInterfaces(symbol.ContainingType);
            var results = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            // These are implicit interface implementations not matching by name such as void I.M();
            results.AddRange(
                await ConvertToSymbolAndProjectIdsAsync(
                    symbol.ExplicitInterfaceImplementations(),
                    project,
                    cancellationToken).ConfigureAwait(false));

            foreach (var type in baseClassesAndInterfaces)
            {
                foreach (var member in type.GetMembers(symbol.Name))
                {
                    var sourceMember = await SymbolFinder.FindSourceDefinitionAsync(
                        SymbolAndProjectId.Create(member, project.Id),
                        solution,
                        cancellationToken).ConfigureAwait(false);

                    if (sourceMember.Symbol != null)
                    {
                        // These are explicit interface implementations matching by name.
                        if (type?.TypeKind == TypeKind.Interface)
                        {
                            if (symbol.ContainingType?.TypeKind == TypeKind.Class || symbol.ContainingType?.TypeKind == TypeKind.Struct)
                            {
                                var implementation = symbol.ContainingType.FindImplementations(sourceMember.Symbol, solution.Workspace);

                                if (implementation != null &&
                                    SymbolEquivalenceComparer.Instance.Equals(implementation.OriginalDefinition, symbol.OriginalDefinition))
                                {
                                    results.Add(sourceMember);
                                }
                            }
                        }
                        else if (SymbolFinder.IsOverride(solution, symbol, sourceMember.Symbol, cancellationToken))
                        {
                            results.Add(sourceMember);
                        }
                    }
                }
            }

            return results.ToImmutableAndFree();
        }

        private static ImmutableArray<INamedTypeSymbol> FindBaseTypesAndInterfaces(INamedTypeSymbol type)
        {
            var typesBuilder = ArrayBuilder<INamedTypeSymbol>.GetInstance();
            typesBuilder.AddRange(type.AllInterfaces);

            var currentType = type.BaseType;
            while (currentType != null)
            {
                typesBuilder.Add(currentType);
                currentType = currentType.BaseType;
            }

            return typesBuilder.ToImmutableAndFree();
        }

        private static async Task<ImmutableArray<SymbolAndProjectId>> ConvertToSymbolAndProjectIdsAsync(
            ImmutableArray<ISymbol> implementations, Project project, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<SymbolAndProjectId>.GetInstance();
            foreach (var implementation in implementations)
            {
                var sourceDefinition = await SymbolFinder.FindSourceDefinitionAsync(
                    SymbolAndProjectId.Create(implementation, project.Id), project.Solution, cancellationToken).ConfigureAwait(false);
                if (sourceDefinition.Symbol != null)
                {
                    result.Add(sourceDefinition);
                }
            }

            return result.ToImmutableAndFree();
        }
    }
}
