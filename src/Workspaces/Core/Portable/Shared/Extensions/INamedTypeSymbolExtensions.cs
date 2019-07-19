// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class INamedTypeSymbolExtensions
    {
        public static IEnumerable<INamedTypeSymbol> GetBaseTypesAndThis(this INamedTypeSymbol namedType)
        {
            var current = namedType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ITypeParameterSymbol> GetAllTypeParameters(this INamedTypeSymbol symbol)
        {
            var stack = GetContainmentStack(symbol);
            return stack.SelectMany(n => n.TypeParameters);
        }

        public static IEnumerable<ITypeSymbol> GetAllTypeArguments(this INamedTypeSymbol symbol)
        {
            var stack = GetContainmentStack(symbol);
            return stack.SelectMany(n => n.TypeArguments);
        }

        private static Stack<INamedTypeSymbol> GetContainmentStack(INamedTypeSymbol symbol)
        {
            var stack = new Stack<INamedTypeSymbol>();
            for (var current = symbol; current != null; current = current.ContainingType)
            {
                stack.Push(current);
            }

            return stack;
        }

        public static bool IsContainedWithin(this INamedTypeSymbol symbol, INamedTypeSymbol outer)
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

        public static ISymbol FindImplementationForAbstractMember(this INamedTypeSymbol type, ISymbol symbol)
        {
            if (symbol.IsAbstract)
            {
                return type.GetBaseTypesAndThis().SelectMany(t => t.GetMembers(symbol.Name))
                                                 .FirstOrDefault(s => symbol.Equals(GetOverriddenMember(s)));
            }

            return null;
        }

        internal static ISymbol GetOverriddenMember(this ISymbol symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol method: return method.OverriddenMethod;
                case IPropertySymbol property: return property.OverriddenProperty;
                case IEventSymbol @event: return @event.OverriddenEvent;
            }

            return null;
        }

        private static bool ImplementationExists(INamedTypeSymbol classOrStructType, ISymbol member)
        {
            return classOrStructType.FindImplementationForInterfaceMember(member) != null;
        }

        private static bool IsImplemented(
            this INamedTypeSymbol classOrStructType,
            ISymbol member,
            Func<INamedTypeSymbol, ISymbol, bool> isValidImplementation,
            CancellationToken cancellationToken)
        {
            if (member.ContainingType.TypeKind == TypeKind.Interface)
            {
                if (member.Kind == SymbolKind.Property)
                {
                    return IsInterfacePropertyImplemented(classOrStructType, (IPropertySymbol)member);
                }
                else
                {
                    return isValidImplementation(classOrStructType, member);
                }
            }

            if (member.IsAbstract)
            {
                if (member.Kind == SymbolKind.Property)
                {
                    return IsAbstractPropertyImplemented(classOrStructType, (IPropertySymbol)member);
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

            static bool IsAccessorImplemented(IMethodSymbol accessor, INamedTypeSymbol classOrStructType)
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

            switch (implementation)
            {
                case IEventSymbol @event: return @event.ExplicitInterfaceImplementations.Length > 0;
                case IMethodSymbol method: return method.ExplicitInterfaceImplementations.Length > 0;
                case IPropertySymbol property: return property.ExplicitInterfaceImplementations.Length > 0;
                default: return false;
            }
        }

        public static ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)> GetAllUnimplementedMembers(
            this INamedTypeSymbol classOrStructType,
            IEnumerable<INamedTypeSymbol> interfacesOrAbstractClasses,
            CancellationToken cancellationToken)
        {
            return classOrStructType.GetAllUnimplementedMembers(
                interfacesOrAbstractClasses,
                IsImplemented,
                ImplementationExists,
                (INamedTypeSymbol type, ISymbol within) =>
                {
                    if (type.TypeKind == TypeKind.Interface)
                    {
                        return type.GetMembers().WhereAsArray(m => m.DeclaredAccessibility == Accessibility.Public &&
                                                                   m.Kind != SymbolKind.NamedType && IsImplementable(m) &&
                                                                   !IsPropertyWithNonPublicImplementableAccessor(m));
                    }

                    return type.GetMembers();
                },
                allowReimplementation: false,
                cancellationToken: cancellationToken);

            // local functions

            static bool IsPropertyWithNonPublicImplementableAccessor(ISymbol member)
            {
                if (member.Kind != SymbolKind.Property)
                {
                    return false;
                }

                var property = (IPropertySymbol)member;

                return IsNonPublicImplementableAccessor(property.GetMethod) || IsNonPublicImplementableAccessor(property.SetMethod);
            }

            static bool IsNonPublicImplementableAccessor(IMethodSymbol accessor)
            {
                return accessor != null && IsImplementable(accessor) && accessor.DeclaredAccessibility != Accessibility.Public;
            }
        }

        private static bool IsImplementable(ISymbol m)
        {
            return m.IsVirtual || m.IsAbstract;
        }

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
                (INamedTypeSymbol type, ISymbol within) =>
                {
                    if (type.TypeKind == TypeKind.Interface)
                    {
                        return type.GetMembers().WhereAsArray(m => m.Kind != SymbolKind.NamedType &&
                                                                   IsImplementable(m) && m.IsAccessibleWithin(within) &&
                                                                   !IsPropertyWithInaccessibleImplementableAccessor(m, within));
                    }

                    return type.GetMembers();
                },
                allowReimplementation: false,
                cancellationToken: cancellationToken);

            // local functions

            static bool IsPropertyWithInaccessibleImplementableAccessor(ISymbol member, ISymbol within)
            {
                if (member.Kind != SymbolKind.Property)
                {
                    return false;
                }

                var property = (IPropertySymbol)member;

                return IsInaccessibleImplementableAccessor(property.GetMethod, within) || IsInaccessibleImplementableAccessor(property.SetMethod, within);
            }

            static bool IsInaccessibleImplementableAccessor(IMethodSymbol accessor, ISymbol within)
            {
                return accessor != null && IsImplementable(accessor) && !accessor.IsAccessibleWithin(within);
            }
        }

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

            if (classOrStructType.TypeKind != TypeKind.Class && classOrStructType.TypeKind != TypeKind.Struct)
            {
                return ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)>.Empty;
            }

            if (!interfacesOrAbstractClasses.Any())
            {
                return ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)>.Empty;
            }

            if (!interfacesOrAbstractClasses.All(i => i.TypeKind == TypeKind.Interface) &&
                !interfacesOrAbstractClasses.All(i => i.IsAbstractClass()))
            {
                return ImmutableArray<(INamedTypeSymbol type, ImmutableArray<ISymbol> members)>.Empty;
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
                : GetAbstractClassesToImplement(classOrStructType, interfacesOrAbstractClasses);
        }

        private static ImmutableArray<INamedTypeSymbol> GetAbstractClassesToImplement(
            INamedTypeSymbol classOrStructType,
            IEnumerable<INamedTypeSymbol> abstractClasses)
        {
            return abstractClasses.SelectMany(a => a.GetBaseTypesAndThis())
                                  .Where(t => t.IsAbstractClass())
                                  .ToImmutableArray();
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
                ? SpecializedCollections.EmptyEnumerable<INamedTypeSymbol>()
                : baseType.AllInterfaces;

            cancellationToken.ThrowIfCancellationRequested();
            interfacesToImplement.RemoveRange(alreadyImplementedInterfaces);
            return interfacesToImplement.ToImmutableArray();
        }

        private static ImmutableArray<ISymbol> GetUnimplementedMembers(
            this INamedTypeSymbol classOrStructType,
            INamedTypeSymbol interfaceType,
            Func<INamedTypeSymbol, ISymbol, Func<INamedTypeSymbol, ISymbol, bool>, CancellationToken, bool> isImplemented,
            Func<INamedTypeSymbol, ISymbol, bool> isValidImplementation,
            Func<INamedTypeSymbol, ISymbol, ImmutableArray<ISymbol>> interfaceMemberGetter,
            CancellationToken cancellationToken)
        {
            var q = from m in interfaceMemberGetter(interfaceType, classOrStructType)
                    where m.Kind != SymbolKind.NamedType
                    where m.Kind != SymbolKind.Method || ((IMethodSymbol)m).MethodKind == MethodKind.Ordinary
                    where m.Kind != SymbolKind.Property || ((IPropertySymbol)m).IsIndexer || ((IPropertySymbol)m).CanBeReferencedByName
                    where m.Kind != SymbolKind.Event || ((IEventSymbol)m).CanBeReferencedByName
                    where !isImplemented(classOrStructType, m, isValidImplementation, cancellationToken)
                    select m;

            return q.ToImmutableArray();
        }

        public static IEnumerable<ISymbol> GetAttributeNamedParameters(
            this INamedTypeSymbol attributeSymbol,
            Compilation compilation,
            ISymbol within)
        {
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
                    if (namedParameter != null)
                    {
                        yield return namedParameter;
                    }
                }
            }
        }

        private static ISymbol IsAttributeNamedParameter(
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
        {
            return type.GetMembers();
        }

        public static INamespaceOrTypeSymbol GenerateRootNamespaceOrType(this INamedTypeSymbol namedType, string[] containers)
        {
            INamespaceOrTypeSymbol currentSymbol = namedType;

            for (var i = containers.Length - 1; i >= 0; i--)
            {
                currentSymbol = CodeGenerationSymbolFactory.CreateNamespaceSymbol(containers[i], members: new[] { currentSymbol });
            }

            return currentSymbol;
        }

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
            var result = new Dictionary<ISymbol, int>();
            var index = 0;

            if (containingType != null &&
                !containingType.IsScriptClass &&
                !containingType.IsImplicitClass &&
                !containingType.IsStatic)
            {
                if (containingType.TypeKind == TypeKind.Class || containingType.TypeKind == TypeKind.Struct)
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
                }
            }

            return result.Keys.OrderBy(s => result[s]).ToImmutableArray();
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
            if (member.IsAbstract || member.IsVirtual || member.IsOverride)
            {
                if (member.IsSealed)
                {
                    return false;
                }

                if (!member.IsAccessibleWithin(containingType))
                {
                    return false;
                }

                switch (member.Kind)
                {
                    case SymbolKind.Event:
                        return true;
                    case SymbolKind.Method:
                        return ((IMethodSymbol)member).MethodKind == MethodKind.Ordinary;
                    case SymbolKind.Property:
                        return !((IPropertySymbol)member).IsWithEvents;
                }
            }

            return false;
        }

        private static void RemoveOverriddenMembers(
            Dictionary<ISymbol, int> result, INamedTypeSymbol containingType, CancellationToken cancellationToken)
        {
            foreach (var member in containingType.GetMembers())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var overriddenMember = member.OverriddenMember();
                if (overriddenMember != null)
                {
                    result.Remove(overriddenMember);
                }
            }
        }

        public static INamedTypeSymbol TryConstruct(this INamedTypeSymbol type, ITypeSymbol[] typeArguments)
        {
            return typeArguments.Length > 0 ? type.ConstructWithNullability(typeArguments) : type;
        }
    }
}
