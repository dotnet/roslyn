// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        /// <summary>
        /// Find symbols for members that override the specified member symbol.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindOverridesAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            if (solution.GetOriginatingProjectId(symbol) == null)
                throw new ArgumentException(WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution, nameof(symbol));

            return await FindOverridesArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<ImmutableArray<ISymbol>> FindOverridesArrayAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var results = ArrayBuilder<ISymbol>.GetInstance();

            symbol = symbol?.OriginalDefinition;
            if (symbol.IsOverridable())
            {
                // To find the overrides, we need to walk down the type hierarchy and check all
                // derived types.
                var containingType = symbol.ContainingType;
                var derivedTypes = await FindDerivedClassesAsync(
                    containingType, solution, projects, cancellationToken).ConfigureAwait(false);

                foreach (var type in derivedTypes)
                {
                    foreach (var m in type.GetMembers(symbol.Name))
                    {
                        var sourceMember = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false);
                        var bestMember = sourceMember ?? m;

                        if (IsOverride(solution, bestMember, symbol, cancellationToken))
                        {
                            results.Add(bestMember);
                        }
                    }
                }
            }

            return results.ToImmutableAndFree();
        }

        internal static bool IsOverride(
            Solution solution, ISymbol member, ISymbol symbol, CancellationToken cancellationToken)
        {
            for (var current = member; current != null; current = current.OverriddenMember())
            {
                if (OriginalSymbolsMatch(current.OverriddenMember(), symbol.OriginalDefinition, solution, cancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Find symbols for declarations that implement members of the specified interface symbol
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementedInterfaceMembersAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            if (solution.GetOriginatingProjectId(symbol) == null)
                throw new ArgumentException(WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution, nameof(symbol));

            return await FindImplementedInterfaceMembersArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<ImmutableArray<ISymbol>> FindImplementedInterfaceMembersArrayAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            // Member can only implement interface members if it is an explicit member, or if it is
            // public and non static.
            if (symbol != null)
            {
                var explicitImplementations = symbol.ExplicitInterfaceImplementations();
                if (explicitImplementations.Length > 0)
                {
                    return explicitImplementations;
                }
                else if (
                    symbol.DeclaredAccessibility == Accessibility.Public && !symbol.IsStatic &&
                    (symbol.ContainingType.TypeKind == TypeKind.Class || symbol.ContainingType.TypeKind == TypeKind.Struct))
                {
                    // Interface implementation is a tricky thing.  A method may implement an interface
                    // method, even if its containing type doesn't state that it implements the
                    // interface.  For example:
                    //
                    //  interface IGoo { void Goo(); }
                    //
                    //  class Base { public void Goo(); }
                    //
                    //  class Derived : Base, IGoo { }
                    //
                    // In this case, Base.Goo *does* implement IGoo.Goo in the context of the type
                    // Derived.
                    var containingType = symbol.ContainingType.OriginalDefinition;
                    var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(
                        containingType, solution, projects, cancellationToken).ConfigureAwait(false);
                    var allTypes = derivedClasses.Concat(containingType);

                    using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);

                    foreach (var type in allTypes)
                    {
                        foreach (var interfaceType in type.AllInterfaces)
                        {
                            // We don't want to look inside this type if we can avoid it. So first
                            // make sure that the interface even contains a symbol with the same
                            // name as the symbol we're looking for.
                            var nameToLookFor = symbol.IsPropertyAccessor()
                                ? ((IMethodSymbol)symbol).AssociatedSymbol.Name
                                : symbol.Name;
                            if (interfaceType.MemberNames.Contains(nameToLookFor))
                            {
                                foreach (var m in interfaceType.GetMembers(symbol.Name))
                                {
                                    var sourceMethod = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false);
                                    var bestMethod = sourceMethod ?? m;

                                    var implementations = await type.FindImplementationsForInterfaceMemberAsync(
                                        bestMethod, solution, cancellationToken).ConfigureAwait(false);
                                    foreach (var implementation in implementations)
                                    {
                                        if (implementation != null &&
                                            SymbolEquivalenceComparer.Instance.Equals(implementation.OriginalDefinition, symbol.OriginalDefinition))
                                        {
                                            builder.Add(bestMethod);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    return builder.Distinct(SymbolEquivalenceComparer.Instance).ToImmutableArray();
                }
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        /// <summary>
        /// Finds all the derived classes of the given type. Implementations of an interface are not considered
        /// "derived", but can be found with <see cref="FindImplementationsAsync(ISymbol, Solution,
        /// IImmutableSet{Project}, CancellationToken)"/>.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The derived types of the symbol. The symbol passed in is not included in this list.</returns>
        public static async Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (solution.GetOriginatingProjectId(type) == null)
                throw new ArgumentException(WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution, nameof(type));

            return await DependentTypeFinder.FindTransitivelyDerivedClassesAsync(
                type, solution, projects, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds the immediate derived classes of the given type. Implementations of an interface are not considered
        /// "derived", but can be found with <see cref="FindImplementationsAsync(ISymbol, Solution,
        /// IImmutableSet{Project}, CancellationToken)"/>.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The derived types of the symbol. The symbol passed in is not included in this list.</returns>
        public static async Task<IEnumerable<INamedTypeSymbol>> FindImmediatelyDerivedClassesAsync(
            INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (solution.GetOriginatingProjectId(type) == null)
                throw new ArgumentException(WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution, nameof(type));

            return await DependentTypeFinder.FindImmediatelyDerivedClassesAsync(
                type, solution, projects, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Finds the symbols that implement an interface or interface member.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementationsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            if (solution.GetOriginatingProjectId(symbol) == null)
                throw new ArgumentException(WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution, nameof(symbol));

            return await FindImplementationsArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);
        }

        internal static async Task<ImmutableArray<ISymbol>> FindImplementationsArrayAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            // A symbol can only have implementations if it's an interface or a
            // method/property/event from an interface.
            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                var implementingTypes = await DependentTypeFinder.FindTransitivelyImplementingStructuresAndClassesAsync(
                    namedTypeSymbol, solution, projects, cancellationToken).ConfigureAwait(false);
                return ImmutableArray<ISymbol>.CastUp(implementingTypes).WhereAsArray(IsAccessible);
            }
            else if (symbol.IsImplementableMember())
            {
                var containingType = symbol.ContainingType.OriginalDefinition;
                var allTypes = await DependentTypeFinder.FindTransitivelyImplementingStructuresClassesAndInterfacesAsync(
                    containingType, solution, projects, cancellationToken).ConfigureAwait(false);

                ImmutableArray<ISymbol>.Builder results = null;
                foreach (var t in allTypes)
                {
                    var implementations = await t.FindImplementationsForInterfaceMemberAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                    foreach (var implementation in implementations)
                    {
                        var sourceDef = await FindSourceDefinitionAsync(implementation, solution, cancellationToken).ConfigureAwait(false);
                        var bestDef = sourceDef ?? implementation;
                        if (IsAccessible(bestDef))
                        {
                            results ??= ImmutableArray.CreateBuilder<ISymbol>();
                            results.Add(bestDef.OriginalDefinition);
                        }
                    }
                }

                if (results != null)
                {
                    return results.Distinct(SymbolEquivalenceComparer.Instance).ToImmutableArray();
                }
            }

            return ImmutableArray<ISymbol>.Empty;
        }
    }
}
