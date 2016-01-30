// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServices;
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
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Method can only have overrides if its a virtual, abstract or override and is not
            // sealed.
            if (symbol.IsOverridable())
            {
                // To find the overrides, we need to walk down the type hierarchy and check all
                // derived types.  TODO(cyrusn): This seems extremely costly.  Is there any way to
                // speed this up?
                var containingType = symbol.ContainingType.OriginalDefinition;
                var derivedTypes = await FindDerivedClassesAsync(containingType, solution, projects, cancellationToken).ConfigureAwait(false);

                List<ISymbol> results = null;
                foreach (var type in derivedTypes)
                {
                    foreach (var m in type.GetMembers(symbol.Name))
                    {
                        var member = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false) ?? m;

                        if (member != null &&
                            member.IsOverride &&
                            member.OverriddenMember() != null &&
                           SymbolEquivalenceComparer.Instance.Equals(member.OverriddenMember().OriginalDefinition, symbol.OriginalDefinition))
                        {
                            results = results ?? new List<ISymbol>();
                            results.Add(member);
                        }
                    }
                }

                if (results != null)
                {
                    return results;
                }
            }

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }

        /// <summary>
        /// Find symbols for declarations that implement members of the specified interface symbol
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementedInterfaceMembersAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
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
                    //  interface IFoo { void Foo(); }
                    //
                    //  class Base { public void Foo(); }
                    //
                    //  class Derived : Base, IFoo { }
                    //
                    // In this case, Base.Foo *does* implement IFoo.Foo in the context of the type
                    // Derived.
                    var containingType = symbol.ContainingType.OriginalDefinition;
                    var derivedClasses = await SymbolFinder.FindDerivedClassesAsync(containingType, solution, projects, cancellationToken).ConfigureAwait(false);
                    var allTypes = derivedClasses.Concat(containingType);

                    List<ISymbol> results = null;

                    foreach (var type in allTypes)
                    {
                        foreach (var interfaceType in type.AllInterfaces)
                        {
                            if (interfaceType.MemberNames.Contains(symbol.Name))
                            {
                                foreach (var m in interfaceType.GetMembers(symbol.Name))
                                {
                                    var interfaceMethod = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false) ?? m;

                                    foreach (var implementation in type.FindImplementationsForInterfaceMember(interfaceMethod, solution.Workspace, cancellationToken))
                                    {
                                        if (implementation != null && SymbolEquivalenceComparer.Instance.Equals(implementation.OriginalDefinition, symbol.OriginalDefinition))
                                        {
                                            results = results ?? new List<ISymbol>();
                                            results.Add(interfaceMethod);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (results != null)
                    {
                        return results.Distinct(SymbolEquivalenceComparer.Instance);
                    }
                }
            }

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }

        /// <summary>
        /// Finds the derived classes of the given type. Implementations of an interface are not considered "derived", but can be found
        /// with <see cref="FindImplementationsAsync"/>.
        /// </summary>
        /// <param name="type">The symbol to find derived types of.</param>
        /// <param name="solution">The solution to search in.</param>
        /// <param name="projects">The projects to search. Can be null to search the entire solution.</param>
        /// <param name="cancellationToken"></param>
        /// <returns>The derived types of the symbol. The symbol passed in is not included in this list.</returns>
        public static Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            INamedTypeSymbol type, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            return DependentTypeFinder.FindDerivedClassesAsync(type, solution, projects, cancellationToken);
        }

        /// <summary>
        /// Finds the symbols that implement an interface or interface member.
        /// </summary>
        public static async Task<IEnumerable<ISymbol>> FindImplementationsAsync(
            ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // A symbol can only have implementations if it's an interface or a
            // method/property/event from an interface.
            if (symbol is INamedTypeSymbol)
            {
                var namedTypeSymbol = (INamedTypeSymbol)symbol;
                var implementingTypes = await DependentTypeFinder.FindImplementingTypesAsync(namedTypeSymbol, solution, projects, cancellationToken).ConfigureAwait(false);
                return implementingTypes.Where(IsAccessible);
            }
            else if (symbol.IsImplementableMember())
            {
                var containingType = symbol.ContainingType.OriginalDefinition;
                var allTypes = await DependentTypeFinder.FindImplementingTypesAsync(containingType, solution, projects, cancellationToken).ConfigureAwait(false);

                List<ISymbol> results = null;
                foreach (var t in allTypes)
                {
                    foreach (var m in t.FindImplementationsForInterfaceMember(symbol, solution.Workspace, cancellationToken))
                    {
                        var s = await FindSourceDefinitionAsync(m, solution, cancellationToken).ConfigureAwait(false) ?? m;
                        if (IsAccessible(s))
                        {
                            results = results ?? new List<ISymbol>();
                            results.Add(s.OriginalDefinition);
                        }
                    }
                }

                if (results != null)
                {
                    return results.Distinct(SymbolEquivalenceComparer.Instance);
                }
            }

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }

        private static bool IsAccessible(ISymbol s)
        {
            if (s.Locations.Any(l => l.IsInMetadata))
            {
                var accessibility = s.DeclaredAccessibility;
                return accessibility == Accessibility.Public ||
                    accessibility == Accessibility.Protected ||
                    accessibility == Accessibility.ProtectedOrInternal;
            }

            return true;
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken = default(CancellationToken))
        {
            return FindCallersAsync(symbol, solution, documents: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Finds all the callers of a specified symbol.
        /// </summary>
        public static async Task<IEnumerable<SymbolCallerInfo>> FindCallersAsync(ISymbol symbol, Solution solution, IImmutableSet<Document> documents, CancellationToken cancellationToken = default(CancellationToken))
        {
            symbol = symbol.OriginalDefinition;
            var foundSymbol = await SymbolFinder.FindSourceDefinitionAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
            symbol = foundSymbol ?? symbol;

            var callReferences = await FindCallReferencesAsync(solution, symbol, documents, cancellationToken).ConfigureAwait(false);

            var directReferences = callReferences.Where(
                r => SymbolEquivalenceComparer.Instance.Equals(symbol, r.Definition)).FirstOrDefault();

            var indirectReferences = callReferences.Where(r => r != directReferences).ToList();

            List<SymbolCallerInfo> results = null;

            if (directReferences != null)
            {
                foreach (var kvp in await directReferences.Locations.FindReferencingSymbolsAsync(cancellationToken).ConfigureAwait(false))
                {
                    results = results ?? new List<SymbolCallerInfo>();
                    results.Add(new SymbolCallerInfo(kvp.Key, symbol, kvp.Value, isDirect: true));
                }
            }

            {
                var indirectLocations = indirectReferences.SelectMany(r => r.Locations);
                foreach (var kvp in await indirectLocations.FindReferencingSymbolsAsync(cancellationToken).ConfigureAwait(false))
                {
                    results = results ?? new List<SymbolCallerInfo>();
                    results.Add(new SymbolCallerInfo(kvp.Key, symbol, kvp.Value, isDirect: false));
                }
            }

            return results ?? SpecializedCollections.EmptyEnumerable<SymbolCallerInfo>();
        }

        private static Task<IEnumerable<ReferencedSymbol>> FindCallReferencesAsync(
            Solution solution,
            ISymbol symbol,
            IImmutableSet<Document> documents,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (symbol != null)
            {
                if (symbol.Kind == SymbolKind.Event ||
                    symbol.Kind == SymbolKind.Method ||
                    symbol.Kind == SymbolKind.Property)
                {
                    return SymbolFinder.FindReferencesAsync(symbol, solution, documents, cancellationToken);
                }
            }

            return SpecializedTasks.EmptyEnumerable<ReferencedSymbol>();
        }
    }
}
