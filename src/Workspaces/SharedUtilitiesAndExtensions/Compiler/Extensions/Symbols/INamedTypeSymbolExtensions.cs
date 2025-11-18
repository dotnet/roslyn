// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
#pragma warning disable RS1024 // Use 'SymbolEqualityComparer' when comparing symbols (https://github.com/dotnet/roslyn/issues/78583)

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class INamedTypeSymbolExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(this INamedTypeSymbol? namedType)
    {
        var current = namedType;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }

    public static IEnumerable<INamedTypeSymbol> GetContainingTypesAndThis(this INamedTypeSymbol? namedType)
    {
        var current = namedType;
        while (current != null)
        {
            yield return current;
            current = current.ContainingType;
        }
    }

    public static ImmutableArray<ITypeParameterSymbol> GetAllTypeParameters(this INamedTypeSymbol? symbol)
    {
        var stack = GetContainmentStack(symbol);
        return stack.SelectManyAsArray(n => n.TypeParameters);
    }

    public static ImmutableArray<ITypeSymbol> GetAllTypeArguments(this INamedTypeSymbol? symbol)
    {
        var stack = GetContainmentStack(symbol);
        return stack.SelectManyAsArray(n => n.TypeArguments);
    }

    private static Stack<INamedTypeSymbol> GetContainmentStack(INamedTypeSymbol? symbol)
    {
        var stack = new Stack<INamedTypeSymbol>();
        for (var current = symbol; current != null; current = current.ContainingType)
        {
            stack.Push(current);
        }

        return stack;
    }

    public static bool IsContainedWithin([NotNullWhen(returnValue: true)] this INamedTypeSymbol? symbol, INamedTypeSymbol outer)
    {
        // TODO(cyrusn): Should we be using OriginalSymbol here?
        for (var current = symbol; current != null; current = current.ContainingType)
        {
            if (current.Equals(outer))
            {
                return true;
            }
        }

        return false;
    }

    public static ISymbol? FindImplementationForAbstractMember(this INamedTypeSymbol? type, ISymbol symbol)
    {
        if (symbol.IsAbstract)
        {
            return type.GetBaseTypesAndThis().SelectMany(t => t.GetMembers(symbol.Name))
                                             .FirstOrDefault(s => symbol.Equals(s.GetOverriddenMember()));
        }

        return null;
    }

    private static bool ImplementationExists(INamedTypeSymbol classOrStructType, ISymbol member)
        => classOrStructType.FindImplementationForInterfaceMember(member) != null;

    private static bool IsImplemented(
        this INamedTypeSymbol classOrStructType,
        ISymbol member,
        Func<INamedTypeSymbol, ISymbol, bool> isValidImplementation,
        CancellationToken cancellationToken)
    {
        if (member.ContainingType.TypeKind == TypeKind.Interface)
        {
            if (member is IPropertySymbol property)
            {
                return IsInterfacePropertyImplemented(classOrStructType, property);
            }
            else
            {
                return isValidImplementation(classOrStructType, member);
            }
        }

        if (member.IsAbstract)
        {
            if (member is IPropertySymbol property)
            {
                return IsAbstractPropertyImplemented(classOrStructType, property);
            }
            else
            {
                return classOrStructType.FindImplementationForAbstractMember(member) != null;
            }
        }

        return true;
    }

    private static bool IsInterfacePropertyImplemented(INamedTypeSymbol classOrStructType, IPropertySymbol propertySymbol)
    {
        // A property is only fully implemented if both it's setter and getter is implemented.

        return IsAccessorImplemented(propertySymbol.GetMethod, classOrStructType) && IsAccessorImplemented(propertySymbol.SetMethod, classOrStructType);

        // local functions

        static bool IsAccessorImplemented(IMethodSymbol? accessor, INamedTypeSymbol classOrStructType)
        {
            return accessor == null || !IsImplementable(accessor) || classOrStructType.FindImplementationForInterfaceMember(accessor) != null;
        }
    }

    private static bool IsAbstractPropertyImplemented(INamedTypeSymbol classOrStructType, IPropertySymbol propertySymbol)
    {
        // A property is only fully implemented if both it's setter and getter is implemented.
        if (propertySymbol.GetMethod != null)
        {
            if (classOrStructType.FindImplementationForAbstractMember(propertySymbol.GetMethod) == null)
            {
                return false;
            }
        }

        if (propertySymbol.SetMethod != null)
        {
            if (classOrStructType.FindImplementationForAbstractMember(propertySymbol.SetMethod) == null)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsExplicitlyImplemented(
        this INamedTypeSymbol classOrStructType,
        ISymbol member,
        Func<INamedTypeSymbol, ISymbol, bool> isValid,
        CancellationToken cancellationToken)
    {
        var implementation = classOrStructType.FindImplementationForInterfaceMember(member);

        if (implementation?.ContainingType.TypeKind == TypeKind.Interface)
        {
            // Treat all implementations in interfaces as explicit, even the original declaration with implementation.
            // There are no implicit interface implementations in derived interfaces and it feels reasonable to treat
            // original declaration with implementation as an explicit implementation as well, the implementation is
            // explicitly provided after all. All implementations in interfaces will be treated uniformly.
            return true;
        }

        return implementation switch
        {
            IEventSymbol @event => @event.ExplicitInterfaceImplementations.Length > 0,
            IMethodSymbol method => method.ExplicitInterfaceImplementations.Length > 0,
            IPropertySymbol property => property.ExplicitInterfaceImplementations.Length > 0,
            _ => false,
        };
    }

    public static ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> GetAllUnimplementedMembers(
        this INamedTypeSymbol classOrStructType,
        IEnumerable<INamedTypeSymbol> interfaces,
        bool includeMembersRequiringExplicitImplementation,
        CancellationToken cancellationToken)
    {
        return classOrStructType.GetAllUnimplementedMembers(
            interfaces,
            IsImplemented,
            ImplementationExists,
            includeMembersRequiringExplicitImplementation
                ? GetExplicitlyImplementableMembers
                : GetImplicitlyImplementableMembers,
            allowReimplementation: false,
            cancellationToken: cancellationToken);

        // local functions

        static ImmutableArray<ISymbol> GetImplicitlyImplementableMembers(INamedTypeSymbol type, ISymbol within)
        {
            if (type.TypeKind == TypeKind.Interface)
            {
                return type.GetMembers().WhereAsArray(
                    m => m.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected &&
                    m.Kind != SymbolKind.NamedType &&
                    IsImplementable(m) &&
                    !IsPropertyWithNonPublicImplementableAccessor(m) &&
                    IsImplicitlyImplementable(m, within));
            }

            return type.GetMembers();
        }

        static bool IsPropertyWithNonPublicImplementableAccessor(ISymbol member)
        {
            if (member.Kind != SymbolKind.Property)
            {
                return false;
            }

            var property = (IPropertySymbol)member;

            return IsNonPublicImplementableAccessor(property.GetMethod) || IsNonPublicImplementableAccessor(property.SetMethod);
        }

        static bool IsNonPublicImplementableAccessor(IMethodSymbol? accessor)
        {
            return accessor != null && IsImplementable(accessor) && accessor.DeclaredAccessibility != Accessibility.Public;
        }

        static bool IsImplicitlyImplementable(ISymbol member, ISymbol within)
        {
            if (member is IMethodSymbol { IsStatic: true, IsAbstract: true, MethodKind: MethodKind.UserDefinedOperator } method)
            {
                // For example, the following is not implementable implicitly.
                // interface I { static abstract int operator -(I x); }
                // But the following is implementable:
                // interface I<T> where T : I<T> { static abstract int operator -(T x); }

                // See https://github.com/dotnet/csharplang/blob/main/spec/classes.md#unary-operators.
                return method.Parameters.Any(static (p, within) => p.Type.Equals(within, SymbolEqualityComparer.Default), within);
            }

            return true;
        }
    }

    private static bool IsImplementable(ISymbol m)
        => m.IsVirtual || m.IsAbstract;

    public static ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> GetAllUnimplementedMembersInThis(
        this INamedTypeSymbol classOrStructType,
        IEnumerable<INamedTypeSymbol> interfacesOrAbstractClasses,
        CancellationToken cancellationToken)
    {
        return classOrStructType.GetAllUnimplementedMembers(
            interfacesOrAbstractClasses,
            IsImplemented,
            (t, m) =>
            {
                var implementation = classOrStructType.FindImplementationForInterfaceMember(m);
                return implementation != null && Equals(implementation.ContainingType, classOrStructType);
            },
            GetMembers,
            allowReimplementation: true,
            cancellationToken: cancellationToken);
    }

    public static ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> GetAllUnimplementedMembersInThis(
        this INamedTypeSymbol classOrStructType,
        IEnumerable<INamedTypeSymbol> interfacesOrAbstractClasses,
        Func<INamedTypeSymbol, ISymbol, ImmutableArray<ISymbol>> interfaceMemberGetter,
        CancellationToken cancellationToken)
    {
        return classOrStructType.GetAllUnimplementedMembers(
            interfacesOrAbstractClasses,
            IsImplemented,
            (t, m) =>
            {
                var implementation = classOrStructType.FindImplementationForInterfaceMember(m);
                return implementation != null && Equals(implementation.ContainingType, classOrStructType);
            },
            interfaceMemberGetter,
            allowReimplementation: true,
            cancellationToken: cancellationToken);
    }

    public static ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> GetAllUnimplementedExplicitMembers(
        this INamedTypeSymbol classOrStructType,
        IEnumerable<INamedTypeSymbol> interfaces,
        CancellationToken cancellationToken)
    {
        return classOrStructType.GetAllUnimplementedMembers(
            interfaces,
            IsExplicitlyImplemented,
            ImplementationExists,
            GetExplicitlyImplementableMembers,
            allowReimplementation: false,
            cancellationToken: cancellationToken);
    }

    private static ImmutableArray<ISymbol> GetExplicitlyImplementableMembers(INamedTypeSymbol type, ISymbol within)
    {
        if (type.TypeKind == TypeKind.Interface)
        {
            return type.GetMembers().WhereAsArray(m => m.Kind != SymbolKind.NamedType &&
                                                       IsImplementable(m) && m.IsAccessibleWithin(within) &&
                                                       !IsPropertyWithInaccessibleImplementableAccessor(m, within));
        }

        return type.GetMembers();
    }

    private static bool IsPropertyWithInaccessibleImplementableAccessor(ISymbol member, ISymbol within)
    {
        if (member.Kind != SymbolKind.Property)
        {
            return false;
        }

        var property = (IPropertySymbol)member;

        return IsInaccessibleImplementableAccessor(property.GetMethod, within) || IsInaccessibleImplementableAccessor(property.SetMethod, within);
    }

    private static bool IsInaccessibleImplementableAccessor(IMethodSymbol? accessor, ISymbol within)
        => accessor != null && IsImplementable(accessor) && !accessor.IsAccessibleWithin(within);

    private static ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> GetAllUnimplementedMembers(
        this INamedTypeSymbol classOrStructType,
        IEnumerable<INamedTypeSymbol> interfacesOrAbstractClasses,
        Func<INamedTypeSymbol, ISymbol, Func<INamedTypeSymbol, ISymbol, bool>, CancellationToken, bool> isImplemented,
        Func<INamedTypeSymbol, ISymbol, bool> isValidImplementation,
        Func<INamedTypeSymbol, ISymbol, ImmutableArray<ISymbol>> interfaceMemberGetter,
        bool allowReimplementation,
        CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(classOrStructType);
        Contract.ThrowIfNull(interfacesOrAbstractClasses);
        Contract.ThrowIfNull(isImplemented);

        if (classOrStructType.TypeKind is not TypeKind.Class and not TypeKind.Struct)
        {
            return [];
        }

        if (!interfacesOrAbstractClasses.Any())
        {
            return [];
        }

        if (!interfacesOrAbstractClasses.All(i => i.TypeKind == TypeKind.Interface) &&
            !interfacesOrAbstractClasses.All(i => i.IsAbstractClass()))
        {
            return [];
        }

        var typesToImplement = GetTypesToImplement(classOrStructType, interfacesOrAbstractClasses, allowReimplementation, cancellationToken);
        return typesToImplement.SelectAsArray(s => (s, members: GetUnimplementedMembers(classOrStructType, s, isImplemented, isValidImplementation, interfaceMemberGetter, cancellationToken)))
                               .WhereAsArray(t => t.members.Length > 0);
    }

    private static ImmutableArray<INamedTypeSymbol> GetTypesToImplement(
        INamedTypeSymbol classOrStructType,
        IEnumerable<INamedTypeSymbol> interfacesOrAbstractClasses,
        bool allowReimplementation,
        CancellationToken cancellationToken)
    {
        return interfacesOrAbstractClasses.First().TypeKind == TypeKind.Interface
            ? GetInterfacesToImplement(classOrStructType, interfacesOrAbstractClasses, allowReimplementation, cancellationToken)
            : GetAbstractClassesToImplement(interfacesOrAbstractClasses);
    }

    private static ImmutableArray<INamedTypeSymbol> GetAbstractClassesToImplement(
        IEnumerable<INamedTypeSymbol> abstractClasses)
    {
        return [.. abstractClasses.SelectMany(a => a.GetBaseTypesAndThis()).Where(t => t.IsAbstractClass())];
    }

    private static ImmutableArray<INamedTypeSymbol> GetInterfacesToImplement(
        INamedTypeSymbol classOrStructType,
        IEnumerable<INamedTypeSymbol> interfaces,
        bool allowReimplementation,
        CancellationToken cancellationToken)
    {
        // We need to not only implement the specified interface, but also everything it
        // inherits from.
        cancellationToken.ThrowIfCancellationRequested();
        var interfacesToImplement = new List<INamedTypeSymbol>(
            interfaces.SelectMany(i => i.GetAllInterfacesIncludingThis()).Distinct());

        // However, there's no need to re-implement any interfaces that our base types already
        // implement.  By definition they must contain all the necessary methods.
        var baseType = classOrStructType.BaseType;
        var alreadyImplementedInterfaces = baseType == null || allowReimplementation
            ? []
            : baseType.AllInterfaces;

        cancellationToken.ThrowIfCancellationRequested();
        interfacesToImplement.RemoveRange(alreadyImplementedInterfaces);
        return [.. interfacesToImplement];
    }

    private static ImmutableArray<ISymbol> GetUnimplementedMembers(
        this INamedTypeSymbol classOrStructType,
        INamedTypeSymbol interfaceType,
        Func<INamedTypeSymbol, ISymbol, Func<INamedTypeSymbol, ISymbol, bool>, CancellationToken, bool> isImplemented,
        Func<INamedTypeSymbol, ISymbol, bool> isValidImplementation,
        Func<INamedTypeSymbol, ISymbol, ImmutableArray<ISymbol>> interfaceMemberGetter,
        CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var results);

        foreach (var member in interfaceMemberGetter(interfaceType, classOrStructType))
        {
            switch (member)
            {
                case IPropertySymbol property:
                    if (property.IsIndexer || property.CanBeReferencedByName)
                        AddIfNotImplemented(property);

                    break;

                case IEventSymbol ev:
                    if (ev.CanBeReferencedByName)
                        AddIfNotImplemented(ev);

                    break;

                case IMethodSymbol method:
                    if (method is { MethodKind: MethodKind.UserDefinedOperator or MethodKind.Conversion } ||
                        method is { MethodKind: MethodKind.Ordinary, CanBeReferencedByName: true })
                    {
                        AddIfNotImplemented(method);
                    }

                    break;
            }
        }

        return results.ToImmutableAndClear();

        void AddIfNotImplemented(ISymbol member)
        {
            if (!isImplemented(classOrStructType, member, isValidImplementation, cancellationToken))
                results.Add(member);
        }
    }

    public static IEnumerable<ISymbol> GetAttributeNamedParameters(
        this INamedTypeSymbol attributeSymbol,
        Compilation compilation,
        ISymbol within)
    {
        using var _ = PooledHashSet<string>.GetInstance(out var seenNames);

        var systemAttributeType = compilation.AttributeType();

        foreach (var type in attributeSymbol.GetBaseTypesAndThis())
        {
            if (type.Equals(systemAttributeType))
            {
                break;
            }

            foreach (var member in type.GetMembers())
            {
                var namedParameter = IsAttributeNamedParameter(member, within ?? compilation.Assembly);
                if (namedParameter != null && seenNames.Add(namedParameter.Name))
                {
                    yield return namedParameter;
                }
            }
        }
    }

    private static ISymbol? IsAttributeNamedParameter(
        ISymbol symbol,
        ISymbol within)
    {
        if (!symbol.CanBeReferencedByName ||
            !symbol.IsAccessibleWithin(within))
        {
            return null;
        }

        switch (symbol.Kind)
        {
            case SymbolKind.Field:
                var fieldSymbol = (IFieldSymbol)symbol;
                if (!fieldSymbol.IsConst &&
                    !fieldSymbol.IsReadOnly &&
                    !fieldSymbol.IsStatic)
                {
                    return fieldSymbol;
                }

                break;

            case SymbolKind.Property:
                var propertySymbol = (IPropertySymbol)symbol;
                if (!propertySymbol.IsReadOnly &&
                    !propertySymbol.IsWriteOnly &&
                    !propertySymbol.IsStatic &&
                    propertySymbol.GetMethod != null &&
                    propertySymbol.SetMethod != null &&
                    propertySymbol.GetMethod.IsAccessibleWithin(within) &&
                    propertySymbol.SetMethod.IsAccessibleWithin(within))
                {
                    return propertySymbol;
                }

                break;
        }

        return null;
    }

    private static ImmutableArray<ISymbol> GetMembers(INamedTypeSymbol type, ISymbol within)
        => type.GetMembers();

    /// <summary>
    /// Gets the set of members in the inheritance chain of <paramref name="containingType"/> that
    /// are overridable.  The members will be returned in furthest-base type to closest-base
    /// type order.  i.e. the overridable members of <see cref="System.Object"/> will be at the start
    /// of the list, and the members of the direct parent type of <paramref name="containingType"/> 
    /// will be at the end of the list.
    /// 
    /// If a member has already been overridden (in <paramref name="containingType"/> or any base type) 
    /// it will not be included in the list.
    /// </summary>
    public static ImmutableArray<ISymbol> GetOverridableMembers(
        this INamedTypeSymbol containingType, CancellationToken cancellationToken)
    {
        // Keep track of the symbols we've seen and what order we saw them in.  The 
        // order allows us to produce the symbols in the end from the furthest base-type
        // to the closest base-type
        using var _ = PooledDictionary<ISymbol, int>.GetInstance(out var result);
        var index = 0;

        if (containingType is
            {
                IsScriptClass: false,
                IsImplicitClass: false,
                IsStatic: false,
                TypeKind: TypeKind.Class or TypeKind.Struct
            })
        {
            var baseTypes = containingType.GetBaseTypes().Reverse();
            foreach (var type in baseTypes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Prefer overrides in derived classes
                RemoveOverriddenMembers(result, type, cancellationToken);

                // Retain overridable methods
                AddOverridableMembers(result, containingType, type, ref index, cancellationToken);
            }

            // Don't suggest already overridden members
            RemoveOverriddenMembers(result, containingType, cancellationToken);

            // Don't suggest members that can't be overridden (because they would collide with an existing member).
            RemoveNonOverriddableMembers(result, containingType, cancellationToken);
        }

        return [.. result.Keys.OrderBy(s => result[s])];

        static void RemoveOverriddenMembers(
            Dictionary<ISymbol, int> result, INamedTypeSymbol containingType, CancellationToken cancellationToken)
        {
            foreach (var member in containingType.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                // An implicitly declared override is still something the user can provide their own explicit
                // override for.  This is true for all implicit overrides *except* for the one for `bool
                // object.Equals(object)`. This override is not one the user is allowed to provide their own
                // override for as it must have a very particular implementation to ensure proper record equality
                // semantics.
                if (!member.IsImplicitlyDeclared || IsEqualsObjectOverride(member))
                {
                    var overriddenMember = member.GetOverriddenMember();
                    if (overriddenMember != null)
                        result.Remove(overriddenMember);
                }
            }
        }

        static void RemoveNonOverriddableMembers(
            Dictionary<ISymbol, int> result, INamedTypeSymbol containingType, CancellationToken cancellationToken)
        {
            var caseSensitive = containingType.Language != LanguageNames.VisualBasic;
            var comparer = caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            foreach (var member in containingType.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (member.IsImplicitlyDeclared)
                    continue;

                var matches = result.Where(kvp =>
                    comparer.Equals(member.Name, kvp.Key.Name) &&
                    SignatureComparer.Instance.HaveSameSignature(member, kvp.Key, caseSensitive));

                // realize the matches since we're mutating the collection we're querying.
                foreach (var match in matches.ToImmutableArray())
                    result.Remove(match.Key);
            }
        }
    }

    private static void AddOverridableMembers(
        Dictionary<ISymbol, int> result, INamedTypeSymbol containingType,
        INamedTypeSymbol type, ref int index, CancellationToken cancellationToken)
    {
        foreach (var member in type.GetMembers())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsOverridable(member, containingType))
            {
                result[member] = index++;
            }
        }
    }

    private static bool IsOverridable(ISymbol member, INamedTypeSymbol containingType)
    {
        if (!member.IsAbstract && !member.IsVirtual && !member.IsOverride)
            return false;

        if (member.IsSealed)
            return false;

        if (!member.IsAccessibleWithin(containingType))
            return false;

        return member switch
        {
            IEventSymbol => true,
            IMethodSymbol { MethodKind: MethodKind.Ordinary, CanBeReferencedByName: true } => true,
            IMethodSymbol { MethodKind: MethodKind.UserDefinedOperator } => true,
            IPropertySymbol { IsWithEvents: false } => true,
            _ => false,
        };
    }

    private static bool IsEqualsObjectOverride(ISymbol? member)
    {
        if (member == null)
            return false;

        if (IsEqualsObject(member))
            return true;

        return IsEqualsObjectOverride(member.GetOverriddenMember());
    }

    private static bool IsEqualsObject(ISymbol member)
    {
        return member is IMethodSymbol
        {
            Name: nameof(Equals),
            IsStatic: false,
            ContainingType.SpecialType: SpecialType.System_Object,
            Parameters.Length: 1,
        };
    }

    public static INamedTypeSymbol TryConstruct(this INamedTypeSymbol type, ITypeSymbol[] typeArguments)
        => typeArguments.Length > 0 ? type.Construct(typeArguments) : type;

    public static bool IsCollectionBuilderAttribute([NotNullWhen(true)] this INamedTypeSymbol? type)
        => type is
        {
            Name: "CollectionBuilderAttribute",
            ContainingNamespace:
            {
                Name: nameof(System.Runtime.CompilerServices),
                ContainingNamespace:
                {
                    Name: nameof(System.Runtime),
                    ContainingNamespace:
                    {
                        Name: nameof(System),
                        ContainingNamespace.IsGlobalNamespace: true,
                    }
                }
            }
        };
}
