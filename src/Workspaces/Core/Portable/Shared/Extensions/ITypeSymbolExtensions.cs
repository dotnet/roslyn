﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ITypeSymbolExtensions
    {
        /// <summary>
        /// Returns the corresponding symbol in this type or a base type that implements
        /// interfaceMember (either implicitly or explicitly), or null if no such symbol exists
        /// (which might be either because this type doesn't implement the container of
        /// interfaceMember, or this type doesn't supply a member that successfully implements
        /// interfaceMember).
        /// </summary>
        public static async Task<ImmutableArray<SymbolAndProjectId>> FindImplementationsForInterfaceMemberAsync(
            this SymbolAndProjectId<ITypeSymbol> typeSymbolAndProjectId,
            SymbolAndProjectId interfaceMemberAndProjectId,
            Solution solution,
            CancellationToken cancellationToken)
        {
            // This method can return multiple results.  Consider the case of:
            //
            // interface IGoo<X> { void Goo(X x); }
            //
            // class C : IGoo<int>, IGoo<string> { void Goo(int x); void Goo(string x); }
            //
            // If you're looking for the implementations of IGoo<X>.Goo then you want to find both
            // results in C.

            var arrBuilder = ArrayBuilder<SymbolAndProjectId>.GetInstance();
            var interfaceMember = interfaceMemberAndProjectId.Symbol;

            // TODO(cyrusn): Implement this using the actual code for
            // TypeSymbol.FindImplementationForInterfaceMember
            var typeSymbol = typeSymbolAndProjectId.Symbol;
            if (typeSymbol == null || interfaceMember == null)
            {
                return arrBuilder.ToImmutableAndFree();
            }

            if (interfaceMember.Kind != SymbolKind.Event &&
                interfaceMember.Kind != SymbolKind.Method &&
                interfaceMember.Kind != SymbolKind.Property)
            {
                return arrBuilder.ToImmutableAndFree();
            }

            // WorkItem(4843)
            //
            // 'typeSymbol' has to at least implement the interface containing the member.  note:
            // this just means that the interface shows up *somewhere* in the inheritance chain of
            // this type.  However, this type may not actually say that it implements it.  For
            // example:
            //
            // interface I { void Goo(); }
            //
            // class B { }
            //
            // class C : B, I { }
            //
            // class D : C { }
            //
            // D does implement I transitively through C.  However, even if D has a "Goo" method, it
            // won't be an implementation of I.Goo.  The implementation of I.Goo must be from a type
            // that actually has I in it's direct interface chain, or a type that's a base type of
            // that.  in this case, that means only classes C or B.
            var interfaceType = interfaceMember.ContainingType;
            if (!typeSymbol.ImplementsIgnoringConstruction(interfaceType))
            {
                return arrBuilder.ToImmutableAndFree();
            }

            // We've ascertained that the type T implements some constructed type of the form I<X>.
            // However, we're not precisely sure which constructions of I<X> are being used.  For
            // example, a type C might implement I<int> and I<string>.  If we're searching for a
            // method from I<X> we might need to find several methods that implement different
            // instantiations of that method.
            var originalInterfaceType = interfaceMember.ContainingType.OriginalDefinition;
            var originalInterfaceMember = interfaceMember.OriginalDefinition;

            var constructedInterfaces = typeSymbol.AllInterfaces.Where(i =>
                SymbolEquivalenceComparer.Instance.Equals(i.OriginalDefinition, originalInterfaceType));

            // Try to get the compilation for the symbol we're searching for,
            // which can help identify matches with the call to SymbolFinder.OriginalSymbolsMatch.
            // OriginalSymbolMatch allows types to be matched across different assemblies
            // if they are considered to be the same type, which provides a more accurate
            // implementations list for interfaces.
            var typeSymbolProject = solution.GetProject(typeSymbolAndProjectId.ProjectId);
            var interfaceMemberProject = solution.GetProject(interfaceMemberAndProjectId.ProjectId);

            var typeSymbolCompilation = await GetCompilationOrNullAsync(typeSymbolProject, cancellationToken).ConfigureAwait(false);
            var interfaceMemberCompilation = await GetCompilationOrNullAsync(interfaceMemberProject, cancellationToken).ConfigureAwait(false);

            foreach (var constructedInterface in constructedInterfaces)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var constructedInterfaceMember = constructedInterface.GetMembers().FirstOrDefault(typeSymbol =>
                    SymbolFinder.OriginalSymbolsMatch(
                        typeSymbol,
                        interfaceMember,
                        solution,
                        typeSymbolCompilation,
                        interfaceMemberCompilation,
                        cancellationToken));

                if (constructedInterfaceMember == null)
                {
                    continue;
                }

                // Now we need to walk the base type chain, but we start at the first type that actually
                // has the interface directly in its interface hierarchy.
                var seenTypeDeclaringInterface = false;
                for (ITypeSymbol? currentType = typeSymbol; currentType != null; currentType = currentType.BaseType)
                {
                    seenTypeDeclaringInterface = seenTypeDeclaringInterface ||
                                                 currentType.GetOriginalInterfacesAndTheirBaseInterfaces().Contains(interfaceType.OriginalDefinition);

                    if (seenTypeDeclaringInterface)
                    {
                        var result = currentType.FindImplementations(constructedInterfaceMember, solution.Workspace);

                        if (result != null)
                        {
                            arrBuilder.Add(typeSymbolAndProjectId.WithSymbol(result));
                            break;
                        }
                    }
                }
            }

            return arrBuilder.ToImmutableAndFree();

            // local functions

            static Task<Compilation?> GetCompilationOrNullAsync(Project? project, CancellationToken cancellationToken)
                => project?.GetCompilationAsync(cancellationToken) ?? SpecializedTasks.Null<Compilation>();
        }


        public static ISymbol? FindImplementations(
            this ITypeSymbol typeSymbol,
            ISymbol constructedInterfaceMember,
            Workspace workspace)
        {
            switch (constructedInterfaceMember)
            {
                case IEventSymbol eventSymbol: return typeSymbol.FindImplementations(eventSymbol, workspace);
                case IMethodSymbol methodSymbol: return typeSymbol.FindImplementations(methodSymbol, workspace);
                case IPropertySymbol propertySymbol: return typeSymbol.FindImplementations(propertySymbol, workspace);
            }

            return null;
        }

        private static ISymbol? FindImplementations<TSymbol>(
            this ITypeSymbol typeSymbol,
            TSymbol constructedInterfaceMember,
            Workspace workspace) where TSymbol : class, ISymbol
        {
            // Check the current type for explicit interface matches.  Otherwise, check
            // the current type and base types for implicit matches.
            var explicitMatches =
                from member in typeSymbol.GetMembers().OfType<TSymbol>()
                from explicitInterfaceMethod in member.ExplicitInterfaceImplementations()
                where SymbolEquivalenceComparer.Instance.Equals(explicitInterfaceMethod, constructedInterfaceMember)
                select member;

            var provider = workspace.Services.GetLanguageServices(typeSymbol.Language);
            var semanticFacts = provider.GetRequiredService<ISemanticFactsService>();

            // Even if a language only supports explicit interface implementation, we
            // can't enforce it for types from metadata. For example, a VB symbol
            // representing System.Xml.XmlReader will say it implements IDisposable, but
            // the XmlReader.Dispose() method will not be an explicit implementation of
            // IDisposable.Dispose()
            if ((!semanticFacts.SupportsImplicitInterfaceImplementation &&
                typeSymbol.Locations.Any(location => location.IsInSource)) ||
                typeSymbol.TypeKind == TypeKind.Interface)
            {
                return explicitMatches.FirstOrDefault();
            }

            var syntaxFacts = provider.GetRequiredService<ISyntaxFactsService>();
            var implicitMatches =
                from baseType in typeSymbol.GetBaseTypesAndThis()
                from member in baseType.GetMembers(constructedInterfaceMember.Name).OfType<TSymbol>()
                where member.DeclaredAccessibility == Accessibility.Public &&
                      !member.IsStatic &&
                      SignatureComparer.Instance.HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(member, constructedInterfaceMember, syntaxFacts.IsCaseSensitive)
                select member;

            return explicitMatches.FirstOrDefault() ?? implicitMatches.FirstOrDefault();
        }

        [return: NotNullIfNotNull(parameterName: "type")]
        public static ITypeSymbol? RemoveUnavailableTypeParameters(
            this ITypeSymbol? type,
            Compilation compilation,
            IEnumerable<ITypeParameterSymbol> availableTypeParameters)
        {
            return type?.RemoveUnavailableTypeParameters(compilation, availableTypeParameters.Select(t => t.Name).ToSet());
        }

        [return: NotNullIfNotNull(parameterName: "type")]
        private static ITypeSymbol? RemoveUnavailableTypeParameters(
            this ITypeSymbol? type,
            Compilation compilation,
            ISet<string> availableTypeParameterNames)
        {
            return type?.Accept(new UnavailableTypeParameterRemover(compilation, availableTypeParameterNames));
        }

        [return: NotNullIfNotNull(parameterName: "type")]
        public static ITypeSymbol? RemoveAnonymousTypes(
            this ITypeSymbol? type,
            Compilation compilation)
        {
            return type?.Accept(new AnonymousTypeRemover(compilation));
        }

        [return: NotNullIfNotNull(parameterName: "type")]
        public static ValueTask<ITypeSymbol?> ReplaceTypeParametersBasedOnTypeConstraintsAsync(
            this ITypeSymbol? type,
            Compilation compilation,
            IEnumerable<ITypeParameterSymbol> availableTypeParameters,
            Solution solution,
            CancellationToken cancellationToken)
        {
#pragma warning disable CA2012 // Use ValueTasks correctly (https://github.com/dotnet/roslyn-analyzers/issues/3384)
            return type?.Accept(new ReplaceTypeParameterBasedOnTypeConstraintVisitor(compilation, availableTypeParameters.Select(t => t.Name).ToSet(), solution, cancellationToken))
#pragma warning restore CA2012 // Use ValueTasks correctly
                ?? new ValueTask<ITypeSymbol?>((ITypeSymbol?)null);
        }

        [return: NotNullIfNotNull(parameterName: "type")]
        public static ITypeSymbol? RemoveUnnamedErrorTypes(
            this ITypeSymbol? type,
            Compilation compilation)
        {
            return type?.Accept(new UnnamedErrorTypeRemover(compilation));
        }

        public static IList<ITypeParameterSymbol> GetReferencedMethodTypeParameters(
            this ITypeSymbol? type, IList<ITypeParameterSymbol>? result = null)
        {
            result ??= new List<ITypeParameterSymbol>();
            type?.Accept(new CollectTypeParameterSymbolsVisitor(result, onlyMethodTypeParameters: true));
            return result;
        }

        public static IList<ITypeParameterSymbol> GetReferencedTypeParameters(
            this ITypeSymbol? type, IList<ITypeParameterSymbol>? result = null)
        {
            result ??= new List<ITypeParameterSymbol>();
            type?.Accept(new CollectTypeParameterSymbolsVisitor(result, onlyMethodTypeParameters: false));
            return result;
        }

        [return: NotNullIfNotNull(parameterName: "type")]
        public static ITypeSymbol? SubstituteTypes<TType1, TType2>(
            this ITypeSymbol? type,
            IDictionary<TType1, TType2> mapping,
            Compilation compilation)
            where TType1 : ITypeSymbol
            where TType2 : ITypeSymbol
        {
            return type.SubstituteTypes(mapping, new CompilationTypeGenerator(compilation));
        }

        [return: NotNullIfNotNull(parameterName: "type")]
        public static ITypeSymbol? SubstituteTypes<TType1, TType2>(
            this ITypeSymbol? type,
            IDictionary<TType1, TType2> mapping,
            ITypeGenerator typeGenerator)
            where TType1 : ITypeSymbol
            where TType2 : ITypeSymbol
        {
            return type?.Accept(new SubstituteTypesVisitor<TType1, TType2>(mapping, typeGenerator));
        }
    }
}
