// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
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
            var results = ArrayBuilder<SymbolAndProjectId>.GetInstance();
            var interfaceImplementations = ArrayBuilder<ISymbol>.GetInstance();

            // This is called for all: class, struct or interface member.
            interfaceImplementations.AddRange(symbol.ExplicitOrImplicitInterfaceImplementations());

            // The type scenario. Iterate over all base classes to find overridden and hidden (new/Shadows) methods.
            foreach (var type in FindBaseTypes(symbol.ContainingType))
            {
                foreach (var member in type.GetMembers(symbol.Name))
                {
                    // Add to results overridden members only. Do not add hidden members.
                    if (SymbolFinder.IsOverride(solution, symbol, member, cancellationToken))
                    {
                        var sourceMember = await SymbolFinder.FindSourceDefinitionAsync(
                            SymbolAndProjectId.Create(member, project.Id),
                            solution,
                            cancellationToken).ConfigureAwait(false);

                        if (sourceMember.Symbol != null)
                        {
                            results.Add(sourceMember);
                        }
                        else
                        {
                            if (member.Locations.Any(l => l.IsInMetadata))
                            {
                                results.Add(new SymbolAndProjectId(member, null));
                            }
                        }

                        // We should add implementations only for overridden members but not for hidden ones.
                        // In the following example:
                        // interface I { void M(); }
                        // class A : I { public void M(); }
                        // class B : A { public new void M(); }
                        // we should not find anything for B.M() because it does not implement the interface:
                        // I i = new B(); i.M(); 
                        // will call the method from A.
                        // However, if we change the code to 
                        // class B : A, I { public new void M(); }
                        // then
                        // I i = new B(); i.M(); 
                        // will call the method from B. We should find the base for B.M in this case.
                        // And if we change 'new' to 'override' in the original code and add 'virtual' where needed, 
                        // we should find I.M as a base for B.M(). And the next line helps with this scenario.
                        interfaceImplementations.AddRange(member.ExplicitOrImplicitInterfaceImplementations());
                    }
                }
            }

            // Remove duplicates from interface implementations before adding their projects.
            results.AddRange(
                await ConvertToSymbolAndProjectIdsAsync(
                    interfaceImplementations.ToImmutableAndFree().Distinct(),
                    project,
                    cancellationToken).ConfigureAwait(false));

            return results.ToImmutableAndFree().Distinct();
        }

        private static ImmutableArray<INamedTypeSymbol> FindBaseTypesAndInterfaces(INamedTypeSymbol type)
            => FindBaseTypes(type).AddRange(type.AllInterfaces);

        private static ImmutableArray<INamedTypeSymbol> FindBaseTypes(INamedTypeSymbol type)
        {
            var typesBuilder = ArrayBuilder<INamedTypeSymbol>.GetInstance();

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
