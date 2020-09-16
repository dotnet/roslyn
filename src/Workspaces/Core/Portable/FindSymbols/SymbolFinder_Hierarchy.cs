// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
            return await FindOverridesArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindOverridesAsync"/>
        /// <remarks>
        /// Use this overload to avoid boxing the result into an <see cref="IEnumerable{T}"/>.
        /// </remarks>
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

                        if (await IsOverrideAsync(solution, bestMember, symbol, cancellationToken).ConfigureAwait(false))
                        {
                            results.Add(bestMember);
                        }
                    }
                }
            }

            return results.ToImmutableAndFree();
        }

        internal static async Task<bool> IsOverrideAsync(Solution solution, ISymbol member, ISymbol symbol, CancellationToken cancellationToken)
        {
            for (var current = member; current != null; current = current.OverriddenMember())
            {
                if (await OriginalSymbolsMatchAsync(solution, current.OverriddenMember(), symbol.OriginalDefinition, cancellationToken).ConfigureAwait(false))
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
            return await FindImplementedInterfaceMembersArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindImplementedInterfaceMembersAsync"/>
        /// <remarks>
        /// Use this overload to avoid boxing the result into an <see cref="IEnumerable{T}"/>.
        /// </remarks>
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

                                    var implementations = await type.FindImplementationsForInterfaceMemberAsync(bestMethod, solution, cancellationToken).ConfigureAwait(false);
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

        #region derived classes

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
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects, CancellationToken cancellationToken)
        {
            return FindDerivedClassesAsync(type, solution, transitive: true, projects, cancellationToken);
        }

        /// <summary>
        /// Finds the derived classes of the given type. Implementations of an interface are not considered
        /// "derived", but can be found with <see cref="FindImplementationsAsync(ISymbol, Solution,
        /// IImmutableSet{Project}, CancellationToken)"/>.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="transitive">If the search should stop at immediately derived classes, or should continue past that.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The derived types of the symbol. The symbol passed in is not included in this list.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static async Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type, Solution solution, bool transitive = true, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            return await FindDerivedClassesArrayAsync(type, solution, transitive, projects, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindDerivedClassesArrayAsync(INamedTypeSymbol, Solution, bool, IImmutableSet{Project}, CancellationToken)"/>
        /// <remarks> Use this overload to avoid boxing the result into an <see cref="IEnumerable{T}"/>.</remarks>
        internal static async Task<ImmutableArray<INamedTypeSymbol>> FindDerivedClassesArrayAsync(
            INamedTypeSymbol type, Solution solution, bool transitive, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var types = await DependentTypeFinder.FindDerivedClassesAsync(
                type, solution, projects, transitive, cancellationToken).ConfigureAwait(false);
            return types.WhereAsArray(t => IsAccessible(t));
        }

        #endregion

        #region derived interfaces

        /// <summary>
        /// Finds the derived interfaces of the given interfaces.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="transitive">If the search should stop at immediately derived interfaces, or should continue past that.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
        /// <returns>The derived interfaces of the symbol. The symbol passed in is not included in this list.</returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static async Task<IEnumerable<INamedTypeSymbol>> FindDerivedInterfacesAsync(
            INamedTypeSymbol type, Solution solution, bool transitive = true, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            return await FindDerivedInterfacesArrayAsync(type, solution, transitive, projects, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindDerivedInterfacesAsync(INamedTypeSymbol, Solution, bool, IImmutableSet{Project}, CancellationToken)"/>
        /// <remarks> Use this overload to avoid boxing the result into an <see cref="IEnumerable{T}"/>.</remarks>
        internal static async Task<ImmutableArray<INamedTypeSymbol>> FindDerivedInterfacesArrayAsync(
            INamedTypeSymbol type, Solution solution, bool transitive, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var types = await DependentTypeFinder.FindDerivedInterfacesAsync(
                type, solution, projects, transitive, cancellationToken).ConfigureAwait(false);
            return types.WhereAsArray(t => IsAccessible(t));
        }

        #endregion

        #region interface implementations

        /// <summary>
        /// Finds the accessible <see langword="class"/> or <see langword="struct"/> types that implement the given
        /// interface.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="transitive">If the search should stop at immediately derived interfaces, or should continue past that.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static async Task<IEnumerable<INamedTypeSymbol>> FindImplementationsAsync(
            INamedTypeSymbol type, Solution solution, bool transitive = true, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            return await FindImplementationsArrayAsync(type, solution, transitive, projects, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindImplementationsAsync(INamedTypeSymbol, Solution, bool, IImmutableSet{Project}, CancellationToken)"/>
        /// <remarks> Use this overload to avoid boxing the result into an <see cref="IEnumerable{T}"/>.</remarks>
        internal static async Task<ImmutableArray<INamedTypeSymbol>> FindImplementationsArrayAsync(
            INamedTypeSymbol type, Solution solution, bool transitive, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            var types = await DependentTypeFinder.FindImplementingTypesAsync(
                type, solution, projects, transitive, cancellationToken).ConfigureAwait(false);
            return types.WhereAsArray(t => IsAccessible(t));
        }

        #endregion

        /// <summary>
        /// Finds all the accessible symbols that implement an interface or interface member.  For an <see
        /// cref="INamedTypeSymbol"/> this will be both immediate and transitive implementations.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementationsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            // A symbol can only have implementations if it's an interface or a
            // method/property/event from an interface.
            if (symbol is INamedTypeSymbol namedTypeSymbol)
            {
                return await FindImplementationsAsync(
                    namedTypeSymbol, solution, transitive: true, projects, cancellationToken).ConfigureAwait(false);
            }

            return await FindMemberImplementationsArrayAsync(symbol, solution, projects, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc cref="FindImplementationsAsync(ISymbol, Solution, IImmutableSet{Project}, CancellationToken)"/>
        /// <remarks>
        /// Use this overload to avoid boxing the result into an <see cref="IEnumerable{T}"/>.
        /// </remarks>
        internal static async Task<ImmutableArray<ISymbol>> FindMemberImplementationsArrayAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            if (!symbol.IsImplementableMember())
                return ImmutableArray<ISymbol>.Empty;

            var containingType = symbol.ContainingType.OriginalDefinition;

            // implementations could be found in any class/struct implementations of the containing interface. And, in
            // the case of DIM, they could be found in any derived interface.

            var classAndStructImplementations = await FindImplementationsAsync(containingType, solution, transitive: true, projects, cancellationToken).ConfigureAwait(false);
            var transitiveDerivedInterfaces = await FindDerivedInterfacesAsync(containingType, solution, transitive: true, projects, cancellationToken).ConfigureAwait(false);
            var allTypes = classAndStructImplementations.Concat(transitiveDerivedInterfaces);

            using var _ = ArrayBuilder<ISymbol>.GetInstance(out var results);
            foreach (var t in allTypes)
            {
                var implementations = await t.FindImplementationsForInterfaceMemberAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                foreach (var implementation in implementations)
                {
                    var sourceDef = await FindSourceDefinitionAsync(implementation, solution, cancellationToken).ConfigureAwait(false);
                    var bestDef = sourceDef ?? implementation;
                    if (IsAccessible(bestDef))
                        results.Add(bestDef.OriginalDefinition);
                }
            }

            return results.Distinct(SymbolEquivalenceComparer.Instance).ToImmutableArray();
        }
    }
}
