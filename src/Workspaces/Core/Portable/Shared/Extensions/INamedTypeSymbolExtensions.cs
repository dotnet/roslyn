// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.FindSymbols;
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

        public static Task<IEnumerable<INamedTypeSymbol>> FindDerivedClassesAsync(
            this INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return DependentTypeFinder.FindDerivedClassesAsync(type, solution, projects, cancellationToken);
        }

        public static Task<IEnumerable<INamedTypeSymbol>> FindImplementingTypesAsync(
            this INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            CancellationToken cancellationToken)
        {
            return DependentTypeFinder.FindImplementingTypesAsync(type, solution, projects, cancellationToken);
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

        private static ISymbol GetOverriddenMember(ISymbol symbol)
        {
            return symbol.TypeSwitch(
                (IMethodSymbol method) => (ISymbol)method.OverriddenMethod,
                (IPropertySymbol property) => property.OverriddenProperty,
                (IEventSymbol @event) => @event.OverriddenEvent);
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
                    return IsInterfacePropertyImplemented(classOrStructType, (IPropertySymbol)member, cancellationToken);
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
                    return IsAbstractPropertyImplemented(classOrStructType, (IPropertySymbol)member, cancellationToken);
                }
                else
                {
                    return classOrStructType.FindImplementationForAbstractMember(member) != null;
                }
            }

            return true;
        }

        private static bool IsInterfacePropertyImplemented(INamedTypeSymbol classOrStructType, IPropertySymbol propertySymbol, CancellationToken cancellationToken)
        {
            // A property is only fully implemented if both it's setter and getter is implemented.
            if (propertySymbol.GetMethod != null)
            {
                if (classOrStructType.FindImplementationForInterfaceMember(propertySymbol.GetMethod) == null)
                {
                    return false;
                }
            }

            if (propertySymbol.SetMethod != null)
            {
                if (classOrStructType.FindImplementationForInterfaceMember(propertySymbol.SetMethod) == null)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAbstractPropertyImplemented(INamedTypeSymbol classOrStructType, IPropertySymbol propertySymbol, CancellationToken cancellationToken)
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
            return implementation.TypeSwitch(
                (IEventSymbol @event) => @event.ExplicitInterfaceImplementations.Length > 0,
                (IMethodSymbol method) => method.ExplicitInterfaceImplementations.Length > 0,
                (IPropertySymbol property) => property.ExplicitInterfaceImplementations.Length > 0);
        }

        public static IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> GetAllUnimplementedMembers(
            this INamedTypeSymbol classOrStructType,
            IEnumerable<INamedTypeSymbol> interfacesOrAbstractClasses,
            CancellationToken cancellationToken)
        {
            return classOrStructType.GetAllUnimplementedMembers(
                interfacesOrAbstractClasses,
                IsImplemented,
                ImplementationExists,
                GetMembers,
                allowReimplementation: false,
                cancellationToken: cancellationToken);
        }

        public static IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> GetAllUnimplementedMembersInThis(
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
                    return implementation != null && implementation.ContainingType == classOrStructType;
                },
                GetMembers,
                allowReimplementation: true,
                cancellationToken: cancellationToken);
        }

        public static IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> GetAllUnimplementedMembersInThis(
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
                    return implementation != null && implementation.ContainingType == classOrStructType;
                },
                interfaceMemberGetter,
                allowReimplementation: true,
                cancellationToken: cancellationToken);
        }

        public static IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> GetAllUnimplementedMembers(
            this INamedTypeSymbol classOrStructType,
            IEnumerable<INamedTypeSymbol> interfacesOrAbstractClasses,
            Func<INamedTypeSymbol, ISymbol, ImmutableArray<ISymbol>> interfaceMemberGetter,
            CancellationToken cancellationToken)
        {
            return classOrStructType.GetAllUnimplementedMembers(
                interfacesOrAbstractClasses,
                IsImplemented,
                ImplementationExists,
                interfaceMemberGetter,
                allowReimplementation: false,
                cancellationToken: cancellationToken);
        }

        public static IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> GetAllUnimplementedExplicitMembers(
            this INamedTypeSymbol classOrStructType,
            IEnumerable<INamedTypeSymbol> interfaces,
            CancellationToken cancellationToken)
        {
            return classOrStructType.GetAllUnimplementedMembers(
                interfaces,
                IsExplicitlyImplemented,
                ImplementationExists,
                GetMembers,
                allowReimplementation: false,
                cancellationToken: cancellationToken);
        }

        public static IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> GetAllUnimplementedExplicitMembers(
            this INamedTypeSymbol classOrStructType,
            IEnumerable<INamedTypeSymbol> interfaces,
            Func<INamedTypeSymbol, ISymbol, ImmutableArray<ISymbol>> interfaceMemberGetter,
            CancellationToken cancellationToken)
        {
            return classOrStructType.GetAllUnimplementedMembers(
                interfaces,
                IsExplicitlyImplemented,
                ImplementationExists,
                interfaceMemberGetter,
                allowReimplementation: false,
                cancellationToken: cancellationToken);
        }

        private static IList<Tuple<INamedTypeSymbol, IList<ISymbol>>> GetAllUnimplementedMembers(
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
                return SpecializedCollections.EmptyList<Tuple<INamedTypeSymbol, IList<ISymbol>>>();
            }

            if (!interfacesOrAbstractClasses.Any())
            {
                return SpecializedCollections.EmptyList<Tuple<INamedTypeSymbol, IList<ISymbol>>>();
            }

            if (!interfacesOrAbstractClasses.All(i => i.TypeKind == TypeKind.Interface) &&
                !interfacesOrAbstractClasses.All(i => i.IsAbstractClass()))
            {
                return SpecializedCollections.EmptyList<Tuple<INamedTypeSymbol, IList<ISymbol>>>();
            }

            var typesToImplement = GetTypesToImplement(classOrStructType, interfacesOrAbstractClasses, allowReimplementation, cancellationToken);
            return typesToImplement.Select(s => Tuple.Create(s, GetUnimplementedMembers(classOrStructType, s, isImplemented, isValidImplementation, interfaceMemberGetter, cancellationToken)))
                                        .Where(t => t.Item2.Count > 0)
                                        .ToList();
        }

        private static IList<INamedTypeSymbol> GetTypesToImplement(
            INamedTypeSymbol classOrStructType,
            IEnumerable<INamedTypeSymbol> interfacesOrAbstractClasses,
            bool allowReimplementation,
            CancellationToken cancellationToken)
        {
            return interfacesOrAbstractClasses.First().TypeKind == TypeKind.Interface
                ? GetInterfacesToImplement(classOrStructType, interfacesOrAbstractClasses, allowReimplementation, cancellationToken)
                : GetAbstractClassesToImplement(classOrStructType, interfacesOrAbstractClasses, cancellationToken);
        }

        private static IList<INamedTypeSymbol> GetAbstractClassesToImplement(
            INamedTypeSymbol classOrStructType,
            IEnumerable<INamedTypeSymbol> abstractClasses,
            CancellationToken cancellationToken)
        {
            return abstractClasses.SelectMany(a => a.GetBaseTypesAndThis())
                                  .Where(t => t.IsAbstractClass())
                                  .ToList();
        }

        private static IList<INamedTypeSymbol> GetInterfacesToImplement(
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
            return interfacesToImplement;
        }

        private static IList<ISymbol> GetUnimplementedMembers(
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

            return q.ToList();
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
            for (int i = containers.Length - 1; i >= 0; i--)
            {
                currentSymbol = CodeGenerationSymbolFactory.CreateNamespaceSymbol(containers[i], members: new[] { currentSymbol });
            }

            return currentSymbol;
        }
    }
}
