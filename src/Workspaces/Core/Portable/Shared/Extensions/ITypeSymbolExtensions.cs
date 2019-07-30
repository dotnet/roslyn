// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private const string DefaultParameterName = "p";
        private const string DefaultBuiltInParameterName = "v";

        public static bool CanAddNullCheck([NotNullWhen(returnValue: true)] this ITypeSymbol? type)
            => type != null && (type.IsReferenceType || type.IsNullable());

        public static IList<INamedTypeSymbol> GetAllInterfacesIncludingThis(this ITypeSymbol type)
        {
            var allInterfaces = type.AllInterfaces;
            if (type is INamedTypeSymbol namedType && namedType.TypeKind == TypeKind.Interface && !allInterfaces.Contains(namedType))
            {
                var result = new List<INamedTypeSymbol>(allInterfaces.Length + 1);
                result.Add(namedType);
                result.AddRange(allInterfaces);
                return result;
            }

            return allInterfaces;
        }

        public static bool IsAbstractClass([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            return symbol?.TypeKind == TypeKind.Class && symbol.IsAbstract;
        }

        public static bool IsSystemVoid([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            return symbol?.SpecialType == SpecialType.System_Void;
        }

        public static bool IsNullable([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
            => symbol?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

        public static bool IsModuleType([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            return symbol?.TypeKind == TypeKind.Module;
        }

        public static bool IsInterfaceType([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            return symbol?.TypeKind == TypeKind.Interface;
        }

        public static bool IsDelegateType([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            return symbol?.TypeKind == TypeKind.Delegate;
        }

        public static bool IsStructType([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            return symbol?.TypeKind == TypeKind.Struct;
        }

        public static bool IsAnonymousType([NotNullWhen(returnValue: true)] this INamedTypeSymbol? symbol)
        {
            return symbol?.IsAnonymousType == true;
        }

        [return: NotNullIfNotNull(parameterName: "symbol")]
        public static ITypeSymbol? RemoveNullableIfPresent([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            if (symbol.IsNullable())
            {
                return symbol.GetTypeArguments().Single();
            }

            return symbol;
        }

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
                for (var currentType = typeSymbol; currentType != null; currentType = currentType.BaseType)
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

            static Task<Compilation> GetCompilationOrNullAsync(Project? project, CancellationToken cancellationToken)
                => project?.GetCompilationAsync(cancellationToken) ?? SpecializedTasks.Default<Compilation>();
        }


        private static HashSet<INamedTypeSymbol> GetOriginalInterfacesAndTheirBaseInterfaces(
            this ITypeSymbol type,
            HashSet<INamedTypeSymbol>? symbols = null)
        {
            symbols ??= new HashSet<INamedTypeSymbol>(SymbolEquivalenceComparer.Instance);

            foreach (var interfaceType in type.Interfaces)
            {
                symbols.Add(interfaceType.OriginalDefinition);
                symbols.AddRange(interfaceType.AllInterfaces.Select(i => i.OriginalDefinition));
            }

            return symbols;
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

        private static ISymbol FindImplementations<TSymbol>(
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

        public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol? type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type)
        {
            var current = type.BaseType;
            while (current != null)
            {
                yield return current;
                current = current.BaseType;
            }
        }

        public static IEnumerable<ITypeSymbol> GetContainingTypesAndThis(this ITypeSymbol? type)
        {
            var current = type;
            while (current != null)
            {
                yield return current;
                current = current.ContainingType;
            }
        }

        public static IEnumerable<INamedTypeSymbol> GetContainingTypes(this ITypeSymbol type)
        {
            var current = type.ContainingType;
            while (current != null)
            {
                yield return current;
                current = current.ContainingType;
            }
        }

        // Determine if "type" inherits from "baseType", ignoring constructed types, optionally including interfaces,
        // dealing only with original types.
        public static bool InheritsFromOrEquals(
            this ITypeSymbol type, ITypeSymbol baseType, bool includeInterfaces)
        {
            if (!includeInterfaces)
            {
                return InheritsFromOrEquals(type, baseType);
            }

            return type.GetBaseTypesAndThis().Concat(type.AllInterfaces).Contains(t => SymbolEquivalenceComparer.Instance.Equals(t, baseType));
        }

        // Determine if "type" inherits from "baseType", ignoring constructed types and interfaces, dealing
        // only with original types.
        public static bool InheritsFromOrEquals(
            this ITypeSymbol type, ITypeSymbol baseType)
        {
            return type.GetBaseTypesAndThis().Contains(t => SymbolEquivalenceComparer.Instance.Equals(t, baseType));
        }

        // Determine if "type" inherits from or implements "baseType", ignoring constructed types, and dealing
        // only with original types.
        public static bool InheritsFromOrImplementsOrEqualsIgnoringConstruction(
            this ITypeSymbol type, ITypeSymbol baseType)
        {
            var originalBaseType = baseType.OriginalDefinition;
            type = type.OriginalDefinition;

            if (SymbolEquivalenceComparer.Instance.Equals(type, originalBaseType))
            {
                return true;
            }

            IEnumerable<ITypeSymbol> baseTypes = (baseType.TypeKind == TypeKind.Interface) ? type.AllInterfaces : type.GetBaseTypes();
            return baseTypes.Contains(t => SymbolEquivalenceComparer.Instance.Equals(t.OriginalDefinition, originalBaseType));
        }

        // Determine if "type" inherits from "baseType", ignoring constructed types, and dealing
        // only with original types.
        public static bool InheritsFromIgnoringConstruction(
            this ITypeSymbol type, ITypeSymbol baseType)
        {
            var originalBaseType = baseType.OriginalDefinition;

            // We could just call GetBaseTypes and foreach over it, but this
            // is a hot path in Find All References. This avoid the allocation
            // of the enumerator type.
            var currentBaseType = type.BaseType;
            while (currentBaseType != null)
            {
                if (SymbolEquivalenceComparer.Instance.Equals(currentBaseType.OriginalDefinition, originalBaseType))
                {
                    return true;
                }

                currentBaseType = currentBaseType.BaseType;
            }

            return false;
        }

        public static bool ImplementsIgnoringConstruction(
            this ITypeSymbol type, ITypeSymbol interfaceType)
        {
            var originalInterfaceType = interfaceType.OriginalDefinition;
            return type.AllInterfaces.Any(t => SymbolEquivalenceComparer.Instance.Equals(t.OriginalDefinition, originalInterfaceType));
        }

        public static bool Implements(
            this ITypeSymbol type, ITypeSymbol interfaceType)
        {
            return type.AllInterfaces.Contains(t => SymbolEquivalenceComparer.Instance.Equals(t, interfaceType));
        }

        public static bool IsAttribute(this ITypeSymbol symbol)
        {
            for (var b = symbol.BaseType; b != null; b = b.BaseType)
            {
                if (b.MetadataName == "Attribute" &&
                    b.ContainingType == null &&
                    b.ContainingNamespace != null &&
                    b.ContainingNamespace.Name == "System" &&
                    b.ContainingNamespace.ContainingNamespace != null &&
                    b.ContainingNamespace.ContainingNamespace.IsGlobalNamespace)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsFormattableString([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            return symbol?.MetadataName == "FormattableString"
                && symbol.ContainingType == null
                && symbol.ContainingNamespace?.Name == "System"
                && symbol.ContainingNamespace.ContainingNamespace?.IsGlobalNamespace == true;
        }

        public static ITypeSymbol? RemoveUnavailableTypeParameters(
            this ITypeSymbol? type,
            Compilation compilation,
            IEnumerable<ITypeParameterSymbol> availableTypeParameters)
        {
            return type?.RemoveUnavailableTypeParameters(compilation, availableTypeParameters.Select(t => t.Name).ToSet());
        }

        private static ITypeSymbol? RemoveUnavailableTypeParameters(
            this ITypeSymbol? type,
            Compilation compilation,
            ISet<string> availableTypeParameterNames)
        {
            return type?.Accept(new UnavailableTypeParameterRemover(compilation, availableTypeParameterNames));
        }

        public static ITypeSymbol? RemoveAnonymousTypes(
            this ITypeSymbol? type,
            Compilation compilation)
        {
            return type?.Accept(new AnonymousTypeRemover(compilation));
        }

        public static ITypeSymbol? ReplaceTypeParametersBasedOnTypeConstraints(
            this ITypeSymbol? type,
            Compilation compilation,
            IEnumerable<ITypeParameterSymbol> availableTypeParameters,
            Solution solution,
            CancellationToken cancellationToken)
        {
            return type?.Accept(new ReplaceTypeParameterBasedOnTypeConstraintVisitor(compilation, availableTypeParameters.Select(t => t.Name).ToSet(), solution, cancellationToken));
        }

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

        public static ITypeSymbol? SubstituteTypes<TType1, TType2>(
            this ITypeSymbol? type,
            IDictionary<TType1, TType2> mapping,
            Compilation compilation)
            where TType1 : ITypeSymbol
            where TType2 : ITypeSymbol
        {
            return type.SubstituteTypes(mapping, new CompilationTypeGenerator(compilation));
        }

        public static ITypeSymbol? SubstituteTypes<TType1, TType2>(
            this ITypeSymbol? type,
            IDictionary<TType1, TType2> mapping,
            ITypeGenerator typeGenerator)
            where TType1 : ITypeSymbol
            where TType2 : ITypeSymbol
        {
            return type?.Accept(new SubstituteTypesVisitor<TType1, TType2>(mapping, typeGenerator));
        }

        public static bool IsUnexpressibleTypeParameterConstraint(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol.IsSealed || typeSymbol.IsValueType)
            {
                return true;
            }

            switch (typeSymbol.TypeKind)
            {
                case TypeKind.Array:
                case TypeKind.Delegate:
                    return true;
            }

            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Array:
                case SpecialType.System_Delegate:
                case SpecialType.System_MulticastDelegate:
                case SpecialType.System_Enum:
                case SpecialType.System_ValueType:
                    return true;
            }

            return false;
        }

        public static bool IsNumericType([NotNullWhen(returnValue: true)] this ITypeSymbol? type)
        {
            if (type != null)
            {
                switch (type.SpecialType)
                {
                    case SpecialType.System_Byte:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Int16:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_UInt64:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Decimal:
                        return true;
                }
            }

            return false;
        }

        public static Accessibility DetermineMinimalAccessibility(this ITypeSymbol typeSymbol)
        {
            return typeSymbol.Accept(MinimalAccessibilityVisitor.Instance);
        }

        public static bool ContainsAnonymousType([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            switch (symbol)
            {
                case IArrayTypeSymbol a: return ContainsAnonymousType(a.ElementType);
                case IPointerTypeSymbol p: return ContainsAnonymousType(p.PointedAtType);
                case INamedTypeSymbol n: return ContainsAnonymousType(n);
                default: return false;
            }
        }

        private static bool ContainsAnonymousType(INamedTypeSymbol type)
        {
            if (type.IsAnonymousType)
            {
                return true;
            }

            foreach (var typeArg in type.GetAllTypeArguments())
            {
                if (ContainsAnonymousType(typeArg))
                {
                    return true;
                }
            }

            return false;
        }

        public static string CreateParameterName(this ITypeSymbol type, bool capitalize = false)
        {
            while (true)
            {
                switch (type)
                {
                    case IArrayTypeSymbol arrayType:
                        type = arrayType.ElementType;
                        continue;
                    case IPointerTypeSymbol pointerType:
                        type = pointerType.PointedAtType;
                        continue;
                }

                break;
            }

            var shortName = GetParameterName(type);
            return capitalize ? shortName.ToPascalCase() : shortName.ToCamelCase();
        }

        private static string GetParameterName(ITypeSymbol? type)
        {
            if (type == null || type.IsAnonymousType() || type.IsTupleType)
            {
                return DefaultParameterName;
            }

            if (type.IsSpecialType() || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return DefaultBuiltInParameterName;
            }

            var shortName = type.GetShortName();
            return shortName.Length == 0
                ? DefaultParameterName
                : shortName;
        }

        public static bool IsSpecialType([NotNullWhen(returnValue: true)] this ITypeSymbol? symbol)
        {
            if (symbol != null)
            {
                switch (symbol.SpecialType)
                {
                    case SpecialType.System_Object:
                    case SpecialType.System_Void:
                    case SpecialType.System_Boolean:
                    case SpecialType.System_SByte:
                    case SpecialType.System_Byte:
                    case SpecialType.System_Decimal:
                    case SpecialType.System_Single:
                    case SpecialType.System_Double:
                    case SpecialType.System_Int16:
                    case SpecialType.System_Int32:
                    case SpecialType.System_Int64:
                    case SpecialType.System_Char:
                    case SpecialType.System_String:
                    case SpecialType.System_UInt16:
                    case SpecialType.System_UInt32:
                    case SpecialType.System_UInt64:
                        return true;
                }
            }

            return false;
        }

        public static bool CanSupportCollectionInitializer(this ITypeSymbol typeSymbol, ISymbol within)
        {
            return
                typeSymbol.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable) &&
                typeSymbol.GetBaseTypesAndThis()
                    .Union(typeSymbol.GetOriginalInterfacesAndTheirBaseInterfaces())
                    .SelectAccessibleMembers<IMethodSymbol>(WellKnownMemberNames.CollectionInitializerAddMethodName, within ?? typeSymbol)
                    .OfType<IMethodSymbol>()
                    .Any(m => m.Parameters.Any());
        }

        public static INamedTypeSymbol? GetDelegateType(this ITypeSymbol? typeSymbol, Compilation compilation)
        {
            if (typeSymbol != null)
            {
                var expressionOfT = compilation.ExpressionOfTType();
                if (typeSymbol.OriginalDefinition.Equals(expressionOfT))
                {
                    var typeArgument = ((INamedTypeSymbol)typeSymbol).TypeArguments[0];
                    return typeArgument as INamedTypeSymbol;
                }

                if (typeSymbol.IsDelegateType())
                {
                    return typeSymbol as INamedTypeSymbol;
                }
            }

            return null;
        }

        public static IEnumerable<T> GetAccessibleMembersInBaseTypes<T>(this ITypeSymbol containingType, ISymbol within) where T : class, ISymbol
        {
            if (containingType == null)
            {
                return SpecializedCollections.EmptyEnumerable<T>();
            }

            var types = containingType.GetBaseTypes();
            return types.SelectMany(x => x.GetMembers().OfType<T>().Where(m => m.IsAccessibleWithin(within)));
        }

        public static ImmutableArray<T> GetAccessibleMembersInThisAndBaseTypes<T>(this ITypeSymbol? containingType, ISymbol within) where T : class, ISymbol
        {
            if (containingType == null)
            {
                return ImmutableArray<T>.Empty;
            }

            return containingType.GetBaseTypesAndThis().SelectAccessibleMembers<T>(within).ToImmutableArray();
        }

        public static bool? AreMoreSpecificThan(this IList<ITypeSymbol> t1, IList<ITypeSymbol> t2)
        {
            if (t1.Count != t2.Count)
            {
                return null;
            }

            // For t1 to be more specific than t2, it has to be not less specific in every member,
            // and more specific in at least one.

            bool? result = null;
            for (var i = 0; i < t1.Count; ++i)
            {
                var r = t1[i].IsMoreSpecificThan(t2[i]);
                if (r == null)
                {
                    // We learned nothing. Do nothing.
                }
                else if (result == null)
                {
                    // We have found the first more specific type. See if
                    // all the rest on this side are not less specific.
                    result = r;
                }
                else if (result != r)
                {
                    // We have more specific types on both left and right, so we 
                    // cannot succeed in picking a better type list. Bail out now.
                    return null;
                }
            }

            return result;
        }

        private static IEnumerable<T> SelectAccessibleMembers<T>(this IEnumerable<ITypeSymbol>? types, ISymbol within) where T : class, ISymbol
        {
            if (types == null)
            {
                return ImmutableArray<T>.Empty;
            }

            return types.SelectMany(x => x.GetMembers().OfType<T>().Where(m => m.IsAccessibleWithin(within)));
        }

        private static IEnumerable<T> SelectAccessibleMembers<T>(this IEnumerable<ITypeSymbol>? types, string memberName, ISymbol within) where T : class, ISymbol
        {
            if (types == null)
            {
                return ImmutableArray<T>.Empty;
            }

            return types.SelectMany(x => x.GetMembers(memberName).OfType<T>().Where(m => m.IsAccessibleWithin(within)));
        }

        private static bool? IsMoreSpecificThan(this ITypeSymbol t1, ITypeSymbol t2)
        {
            // SPEC: A type parameter is less specific than a non-type parameter. 

            var isTypeParameter1 = t1 is ITypeParameterSymbol;
            var isTypeParameter2 = t2 is ITypeParameterSymbol;

            if (isTypeParameter1 && !isTypeParameter2)
            {
                return false;
            }

            if (!isTypeParameter1 && isTypeParameter2)
            {
                return true;
            }

            if (isTypeParameter1)
            {
                Debug.Assert(isTypeParameter2);
                return null;
            }

            if (t1.TypeKind != t2.TypeKind)
            {
                return null;
            }

            // There is an identity conversion between the types and they are both substitutions on type parameters.
            // They had better be the same kind.

            // UNDONE: Strip off the dynamics.

            // SPEC: An array type is more specific than another
            // SPEC: array type (with the same number of dimensions) 
            // SPEC: if the element type of the first is
            // SPEC: more specific than the element type of the second.

            if (t1 is IArrayTypeSymbol)
            {
                var arr1 = (IArrayTypeSymbol)t1;
                var arr2 = (IArrayTypeSymbol)t2;

                // We should not have gotten here unless there were identity conversions
                // between the two types.

                return arr1.ElementType.IsMoreSpecificThan(arr2.ElementType);
            }

            // SPEC EXTENSION: We apply the same rule to pointer types. 

            if (t1 is IPointerTypeSymbol)
            {
                var p1 = (IPointerTypeSymbol)t1;
                var p2 = (IPointerTypeSymbol)t2;
                return p1.PointedAtType.IsMoreSpecificThan(p2.PointedAtType);
            }

            // SPEC: A constructed type is more specific than another
            // SPEC: constructed type (with the same number of type arguments) if at least one type
            // SPEC: argument is more specific and no type argument is less specific than the
            // SPEC: corresponding type argument in the other. 

            var n1 = t1 as INamedTypeSymbol;
            var n2 = t2 as INamedTypeSymbol;

            if (n1 == null)
            {
                return null;
            }

            // We should not have gotten here unless there were identity conversions between the
            // two types.

            var allTypeArgs1 = n1.GetAllTypeArguments().ToList();
            var allTypeArgs2 = n2.GetAllTypeArguments().ToList();

            return allTypeArgs1.AreMoreSpecificThan(allTypeArgs2);
        }

        public static bool IsOrDerivesFromExceptionType([NotNullWhen(returnValue: true)] this ITypeSymbol? type, Compilation compilation)
        {
            if (type != null)
            {
                switch (type.Kind)
                {
                    case SymbolKind.NamedType:
                        foreach (var baseType in type.GetBaseTypesAndThis())
                        {
                            if (baseType.Equals(compilation.ExceptionType()))
                            {
                                return true;
                            }
                        }

                        break;

                    case SymbolKind.TypeParameter:
                        foreach (var constraint in ((ITypeParameterSymbol)type).ConstraintTypes)
                        {
                            if (constraint.IsOrDerivesFromExceptionType(compilation))
                            {
                                return true;
                            }
                        }

                        break;
                }
            }

            return false;
        }

        public static bool IsEnumType(this ITypeSymbol type)
        {
            return type.IsValueType && type.TypeKind == TypeKind.Enum;
        }

        public static bool? IsMutableValueType(this ITypeSymbol type)
        {
            if (type.IsNullable())
            {
                // Nullable<T> can only be mutable if T is mutable. This case ensures types like 'int?' are treated as
                // immutable.
                type = type.GetTypeArguments()[0];
            }

            switch (type.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    return false;

                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                    return false;

                case SpecialType.System_DateTime:
                    return false;

                default:
                    break;
            }

            if (type.IsErrorType())
            {
                return null;
            }

            if (type.TypeKind != TypeKind.Struct)
            {
                return false;
            }

            var hasPrivateField = false;
            foreach (var member in type.GetMembers())
            {
                if (!(member is IFieldSymbol fieldSymbol))
                {
                    continue;
                }

                hasPrivateField |= fieldSymbol.DeclaredAccessibility == Accessibility.Private;
                if (!fieldSymbol.IsConst && !fieldSymbol.IsReadOnly && !fieldSymbol.IsStatic)
                {
                    return true;
                }
            }

            if (!hasPrivateField)
            {
                // Some reference assemblies omit information about private fields. If we can't be sure the field is
                // immutable, treat it as potentially mutable.
                foreach (var attributeData in type.ContainingAssembly.GetAttributes())
                {
                    if (attributeData.AttributeClass.Name == nameof(ReferenceAssemblyAttribute)
                        && attributeData.AttributeClass.ToNameDisplayString() == typeof(ReferenceAssemblyAttribute).FullName)
                    {
                        return null;
                    }
                }
            }

            return false;
        }

        public static bool IsDisposable([NotNullWhen(returnValue: true)] this ITypeSymbol? type, [NotNullWhen(returnValue: true)] ITypeSymbol? iDisposableType)
            => iDisposableType != null &&
               (Equals(iDisposableType, type) ||
                type?.AllInterfaces.Contains(iDisposableType) == true);
    }
}
