// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ITypeSymbolExtensions
{
    /// <summary>
    /// Returns the corresponding symbol in this type or a base type that implements
    /// interfaceMember (either implicitly or explicitly), or null if no such symbol exists
    /// (which might be either because this type doesn't implement the container of
    /// interfaceMember, or this type doesn't supply a member that successfully implements
    /// interfaceMember).
    /// </summary>
    public static ImmutableArray<ISymbol> FindImplementationsForInterfaceMember(
        this ITypeSymbol typeSymbol,
        ISymbol interfaceMember,
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

        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var builder);

        // TODO(cyrusn): Implement this using the actual code for
        // TypeSymbol.FindImplementationForInterfaceMember
        if (typeSymbol == null || interfaceMember == null)
            return [];

        if (interfaceMember.Kind is not SymbolKind.Event and
            not SymbolKind.Method and
            not SymbolKind.Property)
        {
            return [];
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
            return [];

        // We've ascertained that the type T implements some constructed type of the form I<X>.
        // However, we're not precisely sure which constructions of I<X> are being used.  For
        // example, a type C might implement I<int> and I<string>.  If we're searching for a
        // method from I<X> we might need to find several methods that implement different
        // instantiations of that method.
        var originalInterfaceType = interfaceMember.ContainingType.OriginalDefinition;
        var originalInterfaceMember = interfaceMember.OriginalDefinition;

        var constructedInterfaces = typeSymbol.AllInterfaces.Where(i =>
            SymbolEquivalenceComparer.Instance.Equals(i.OriginalDefinition, originalInterfaceType));

        foreach (var constructedInterface in constructedInterfaces)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // OriginalSymbolMatch allows types to be matched across different assemblies if they are considered to
            // be the same type, which provides a more accurate implementations list for interfaces.
            var constructedInterfaceMember =
                constructedInterface.GetMembers(interfaceMember.Name).FirstOrDefault(
                    typeSymbol => SymbolFinder.OriginalSymbolsMatch(solution, typeSymbol, interfaceMember));

            if (constructedInterfaceMember == null)
                continue;

            // Now we need to walk the base type chain, but we start at the first type that actually
            // has the interface directly in its interface hierarchy.
            var seenTypeDeclaringInterface = false;
            for (var currentType = typeSymbol; currentType != null; currentType = currentType.BaseType)
            {
                seenTypeDeclaringInterface = seenTypeDeclaringInterface ||
                                             currentType.GetOriginalInterfacesAndTheirBaseInterfaces().Contains(interfaceType.OriginalDefinition);

                if (seenTypeDeclaringInterface)
                {
                    var result = currentType.FindImplementations(constructedInterfaceMember, solution.Services);

                    if (result != null)
                    {
                        builder.Add(result);
                        break;
                    }
                }
            }
        }

        return builder.ToImmutableAndClear();
    }

    public static ISymbol? FindImplementations(this ITypeSymbol typeSymbol, ISymbol constructedInterfaceMember, SolutionServices services)
        => constructedInterfaceMember switch
        {
            IEventSymbol eventSymbol => typeSymbol.FindImplementations(eventSymbol, services),
            IMethodSymbol methodSymbol => typeSymbol.FindImplementations(methodSymbol, services),
            IPropertySymbol propertySymbol => typeSymbol.FindImplementations(propertySymbol, services),
            _ => null,
        };

    private static ISymbol? FindImplementations<TSymbol>(
        this ITypeSymbol typeSymbol,
        TSymbol constructedInterfaceMember,
        SolutionServices services) where TSymbol : class, ISymbol
    {
        // Check the current type for explicit interface matches.  Otherwise, check
        // the current type and base types for implicit matches.
        var explicitMatches =
            from member in typeSymbol.GetMembers().OfType<TSymbol>()
            from explicitInterfaceMethod in member.ExplicitInterfaceImplementations()
            where SymbolEquivalenceComparer.Instance.Equals(explicitInterfaceMethod, constructedInterfaceMember)
            select member;

        var provider = services.GetLanguageServices(typeSymbol.Language);
        var semanticFacts = provider.GetRequiredService<ISemanticFactsService>();

        // Even if a language only supports explicit interface implementation, we
        // can't enforce it for types from metadata. For example, a VB symbol
        // representing System.Xml.XmlReader will say it implements IDisposable, but
        // the XmlReader.Dispose() method will not be an explicit implementation of
        // IDisposable.Dispose()
        if ((!semanticFacts.SupportsImplicitInterfaceImplementation &&
            typeSymbol.Locations.Any(static location => location.IsInSource)) ||
            typeSymbol.TypeKind == TypeKind.Interface)
        {
            return explicitMatches.FirstOrDefault();
        }

        var syntaxFacts = provider.GetRequiredService<ISyntaxFactsService>();
        var implicitMatches =
            from baseType in typeSymbol.GetBaseTypesAndThis()
            from member in baseType.GetMembers(constructedInterfaceMember.Name).OfType<TSymbol>()
            where member.DeclaredAccessibility == Accessibility.Public &&
                  SignatureComparer.Instance.HaveSameSignatureAndConstraintsAndReturnTypeAndAccessors(member, constructedInterfaceMember, syntaxFacts.IsCaseSensitive)
            select member;

        return explicitMatches.FirstOrDefault() ?? implicitMatches.FirstOrDefault();
    }

    [return: NotNullIfNotNull(parameterName: nameof(type))]
    public static ITypeSymbol? RemoveAnonymousTypes(
        this ITypeSymbol? type,
        Compilation compilation)
    {
        return type?.Accept(new AnonymousTypeRemover(compilation));
    }

    [return: NotNullIfNotNull(parameterName: nameof(type))]
    public static ITypeSymbol? RemoveUnnamedErrorTypes(
        this ITypeSymbol? type,
        Compilation compilation)
    {
        return type?.Accept(new UnnamedErrorTypeRemover(compilation));
    }

    public static bool CanBeEnumerated(this ITypeSymbol type)
    {
        // Type itself is IEnumerable/IEnumerable<SomeType>
        if (type.OriginalDefinition is { SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T or SpecialType.System_Collections_IEnumerable })
        {
            return true;
        }

        return type.AllInterfaces.Any(s => s.SpecialType is SpecialType.System_Collections_Generic_IEnumerable_T or SpecialType.System_Collections_IEnumerable);
    }

    public static bool CanBeAsynchronouslyEnumerated(this ITypeSymbol type, Compilation compilation)
    {
        var asyncEnumerableType = compilation.IAsyncEnumerableOfTType();

        if (asyncEnumerableType is null)
        {
            return false;
        }

        // Type itself is an IAsyncEnumerable<SomeType>
        if (type.TypeKind == TypeKind.Interface &&
            type.OriginalDefinition.Equals(asyncEnumerableType, SymbolEqualityComparer.Default))
        {
            return true;
        }

        foreach (var @interface in type.AllInterfaces)
        {
            if (@interface.OriginalDefinition.Equals(asyncEnumerableType, SymbolEqualityComparer.Default))
            {
                return true;
            }
        }

        return false;
    }
}
