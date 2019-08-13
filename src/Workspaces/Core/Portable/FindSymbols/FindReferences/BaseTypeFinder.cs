// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
            var baseClasses = FindBaseTypesAndInterfaces(symbol.ContainingType)
                .WhereAsArray(t => t.TypeKind == TypeKind.Class);
            var results = ArrayBuilder<SymbolAndProjectId>.GetInstance();

            // Overridded and hidden members to be reviewed for explicit and implicit interface implementations.
            var baseClassesMembers = ArrayBuilder<ISymbol>.GetInstance();

            foreach (var type in baseClasses)
            {
                foreach (var member in type.GetMembers(symbol.Name))
                {
                    var sourceMember = await SymbolFinder.FindSourceDefinitionAsync(
                        SymbolAndProjectId.Create(member, project.Id),
                        solution,
                        cancellationToken).ConfigureAwait(false);

                    if (sourceMember.Symbol != null)
                    {
                        // Add to results overridden members only. Do not add hidden members.
                        if (SymbolFinder.IsOverride(solution, symbol, sourceMember.Symbol, cancellationToken))
                        {
                            results.Add(sourceMember);
                        }

                        // Add both overridden and hidden members.
                        baseClassesMembers.Add(sourceMember.Symbol);
                    }
                }
            }

            // This is called for all: class, struct or interface member.
            results.AddRange(
                await ConvertToSymbolAndProjectIdsAsync(
                    symbol.ExplicitOrImplicitInterfaceImplementations(),
                    project,
                    cancellationToken).ConfigureAwait(false));

            // In case of class, we find overridden and inherited members.
            // Then, find all explicit and implicit interface implementations.
            // Explicit ones may not match by name such as N() Implements I.M().
            foreach (var s in baseClassesMembers)
            {
                results.AddRange(
                    await ConvertToSymbolAndProjectIdsAsync(
                        s.ExplicitOrImplicitInterfaceImplementations(),
                        project,
                        cancellationToken).ConfigureAwait(false));
            }

            // Remove duplicates.
            return results.ToImmutableAndFree().Distinct();
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
