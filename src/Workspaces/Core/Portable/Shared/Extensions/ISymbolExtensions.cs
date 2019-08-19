// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class ISymbolExtensions
    {
        public static string ToNameDisplayString(this ISymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormats.NameFormat);
        }

        public static string ToSignatureDisplayString(this ISymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormats.SignatureFormat);
        }

        public static bool HasPublicResultantVisibility(this ISymbol symbol)
            => symbol.GetResultantVisibility() == SymbolVisibility.Public;

        public static SymbolVisibility GetResultantVisibility(this ISymbol symbol)
        {
            // Start by assuming it's visible.
            var visibility = SymbolVisibility.Public;

            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    // Aliases are uber private.  They're only visible in the same file that they
                    // were declared in.
                    return SymbolVisibility.Private;

                case SymbolKind.Parameter:
                    // Parameters are only as visible as their containing symbol
                    return GetResultantVisibility(symbol.ContainingSymbol);

                case SymbolKind.TypeParameter:
                    // Type Parameters are private.
                    return SymbolVisibility.Private;
            }

            while (symbol != null && symbol.Kind != SymbolKind.Namespace)
            {
                switch (symbol.DeclaredAccessibility)
                {
                    // If we see anything private, then the symbol is private.
                    case Accessibility.NotApplicable:
                    case Accessibility.Private:
                        return SymbolVisibility.Private;

                    // If we see anything internal, then knock it down from public to
                    // internal.
                    case Accessibility.Internal:
                    case Accessibility.ProtectedAndInternal:
                        visibility = SymbolVisibility.Internal;
                        break;

                        // For anything else (Public, Protected, ProtectedOrInternal), the
                        // symbol stays at the level we've gotten so far.
                }

                symbol = symbol.ContainingSymbol;
            }

            return visibility;
        }

        public static ISymbol? OverriddenMember(this ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Event:
                    return ((IEventSymbol)symbol).OverriddenEvent;

                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).OverriddenMethod;

                case SymbolKind.Property:
                    return ((IPropertySymbol)symbol).OverriddenProperty;
            }

            return null;
        }

        public static ImmutableArray<ISymbol> ExplicitInterfaceImplementations(this ISymbol symbol)
        {
            switch (symbol)
            {
                case IEventSymbol @event: return ImmutableArray<ISymbol>.CastUp(@event.ExplicitInterfaceImplementations);
                case IMethodSymbol method: return ImmutableArray<ISymbol>.CastUp(method.ExplicitInterfaceImplementations);
                case IPropertySymbol property: return ImmutableArray<ISymbol>.CastUp(property.ExplicitInterfaceImplementations);
                default: return ImmutableArray.Create<ISymbol>();
            }
        }

        public static ImmutableArray<TSymbol> ExplicitOrImplicitInterfaceImplementations<TSymbol>(this TSymbol symbol)
            where TSymbol : ISymbol
        {
            var containingType = symbol.ContainingType;
            var allMembersInAllInterfaces = containingType.AllInterfaces.SelectMany(i => i.GetMembers().OfType<TSymbol>());
            var membersImplementingAnInterfaceMember = allMembersInAllInterfaces.Where(
                memberInInterface => symbol.Equals(containingType.FindImplementationForInterfaceMember(memberInInterface)));
            return membersImplementingAnInterfaceMember.Cast<TSymbol>().ToImmutableArrayOrEmpty();
        }

        public static bool IsOverridable([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            // Members can only have overrides if they are virtual, abstract or override and is not
            // sealed.
            return symbol?.ContainingType?.TypeKind == TypeKind.Class &&
                   (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) &&
                   !symbol.IsSealed;
        }

        public static bool IsImplementableMember([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            if (symbol != null &&
                symbol.ContainingType != null &&
                symbol.ContainingType.TypeKind == TypeKind.Interface)
            {
                if (symbol.Kind == SymbolKind.Event)
                {
                    return true;
                }

                if (symbol.Kind == SymbolKind.Property)
                {
                    return true;
                }

                if (symbol.Kind == SymbolKind.Method)
                {
                    var methodSymbol = (IMethodSymbol)symbol;
                    if (methodSymbol.MethodKind == MethodKind.Ordinary ||
                        methodSymbol.MethodKind == MethodKind.PropertyGet ||
                        methodSymbol.MethodKind == MethodKind.PropertySet)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static INamedTypeSymbol? GetContainingTypeOrThis(this ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                return namedType;
            }

            return symbol.ContainingType;
        }

        public static bool IsPointerType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol is IPointerTypeSymbol;
        }

        public static bool IsErrorType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => (symbol as ITypeSymbol)?.TypeKind == TypeKind.Error;

        public static bool IsModuleType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as ITypeSymbol)?.IsModuleType() == true;
        }

        public static bool IsInterfaceType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as ITypeSymbol)?.IsInterfaceType() == true;
        }

        public static bool IsArrayType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol?.Kind == SymbolKind.ArrayType;
        }

        public static bool IsTupleType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as ITypeSymbol)?.IsTupleType ?? false;
        }

        public static bool IsAnonymousFunction([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.AnonymousFunction;
        }

        public static bool IsKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, SymbolKind kind)
        {
            return symbol.MatchesKind(kind);
        }

        public static bool MatchesKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, SymbolKind kind)
        {
            return symbol?.Kind == kind;
        }

        public static bool MatchesKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, SymbolKind kind1, SymbolKind kind2)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2);
        }

        public static bool MatchesKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, SymbolKind kind1, SymbolKind kind2, SymbolKind kind3)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2 || symbol.Kind == kind3);
        }

        public static bool MatchesKind([NotNullWhen(returnValue: true)] this ISymbol? symbol, params SymbolKind[] kinds)
        {
            return symbol != null
                && kinds.Contains(symbol.Kind);
        }

        public static bool IsReducedExtension([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.ReducedExtension;
        }

        public static bool IsEnumMember([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol?.Kind == SymbolKind.Field && symbol.ContainingType.IsEnumType();
        }

        public static bool IsExtensionMethod(this ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).IsExtensionMethod;
        }

        public static bool IsLocalFunction([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol != null && symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.LocalFunction;
        }

        public static bool IsModuleMember([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol != null && symbol.ContainingSymbol is INamedTypeSymbol && symbol.ContainingType.TypeKind == TypeKind.Module;
        }

        public static bool IsConstructor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Constructor;
        }

        public static bool IsStaticConstructor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.StaticConstructor;
        }

        public static bool IsDestructor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Destructor;
        }

        public static bool IsUserDefinedOperator([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.UserDefinedOperator;
        }

        public static bool IsConversion([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Conversion;
        }

        public static bool IsOrdinaryMethod([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Ordinary;
        }

        public static bool IsOrdinaryMethodOrLocalFunction([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            if (!(symbol is IMethodSymbol method))
            {
                return false;
            }

            return method.MethodKind == MethodKind.Ordinary
                || method.MethodKind == MethodKind.LocalFunction;
        }

        public static bool IsDelegateType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol is ITypeSymbol && ((ITypeSymbol)symbol).TypeKind == TypeKind.Delegate;
        }

        public static bool IsAnonymousType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol is INamedTypeSymbol && ((INamedTypeSymbol)symbol).IsAnonymousType;
        }

        public static bool IsNormalAnonymousType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol.IsAnonymousType() && !symbol.IsDelegateType();
        }

        public static bool IsAnonymousDelegateType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol.IsAnonymousType() && symbol.IsDelegateType();
        }

        public static bool IsAnonymousTypeProperty([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol is IPropertySymbol && symbol.ContainingType.IsNormalAnonymousType();

        public static bool IsTupleField([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol is IFieldSymbol && symbol.ContainingType.IsTupleType;

        public static bool IsIndexer([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IPropertySymbol)?.IsIndexer == true;
        }

        public static bool IsWriteableFieldOrProperty([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            switch (symbol)
            {
                case IFieldSymbol fieldSymbol:
                    return !fieldSymbol.IsReadOnly && !fieldSymbol.IsConst;
                case IPropertySymbol propertySymbol:
                    return !propertySymbol.IsReadOnly;
            }

            return false;
        }

        public static ITypeSymbol? GetMemberType(this ISymbol symbol)
        {
            switch (symbol)
            {
                case IFieldSymbol fieldSymbol:
                    return fieldSymbol.GetTypeWithAnnotatedNullability();
                case IPropertySymbol propertySymbol:
                    return propertySymbol.GetTypeWithAnnotatedNullability();
                case IMethodSymbol methodSymbol:
                    return methodSymbol.GetReturnTypeWithAnnotatedNullability();
                case IEventSymbol eventSymbol:
                    return eventSymbol.GetTypeWithAnnotatedNullability();
            }

            return null;
        }

        public static int GetArity(this ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    return ((INamedTypeSymbol)symbol).Arity;
                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).Arity;
                default:
                    return 0;
            }
        }

        [return: NotNullIfNotNull(parameterName: "symbol")]
        public static ISymbol? GetOriginalUnreducedDefinition(this ISymbol? symbol)
        {
            if (symbol.IsReducedExtension())
            {
                // note: ReducedFrom is only a method definition and includes no type arguments.
                symbol = ((IMethodSymbol)symbol).GetConstructedReducedFrom();
            }

            if (symbol.IsFunctionValue())
            {
                if (symbol.ContainingSymbol is IMethodSymbol method)
                {
                    symbol = method;

                    if (method.AssociatedSymbol != null)
                    {
                        symbol = method.AssociatedSymbol;
                    }
                }
            }

            if (symbol.IsNormalAnonymousType() || symbol.IsAnonymousTypeProperty())
            {
                return symbol;
            }

            if (symbol is IParameterSymbol parameter)
            {
                var method = parameter.ContainingSymbol as IMethodSymbol;
                if (method?.IsReducedExtension() == true)
                {
                    symbol = method.GetConstructedReducedFrom().Parameters[parameter.Ordinal + 1];
                }
            }

            return symbol?.OriginalDefinition;
        }

        public static bool IsFunctionValue([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol is ILocalSymbol && ((ILocalSymbol)symbol).IsFunctionValue;
        }

        public static bool IsThisParameter([NotNullWhen(returnValue: true)] this ISymbol? symbol)
            => symbol?.Kind == SymbolKind.Parameter && ((IParameterSymbol)symbol).IsThis;

        [return: NotNullIfNotNull(parameterName: "symbol")]
        public static ISymbol? ConvertThisParameterToType(this ISymbol? symbol)
        {
            if (symbol.IsThisParameter())
            {
                return ((IParameterSymbol)symbol).Type;
            }

            return symbol;
        }

        public static bool IsParams([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            var parameters = symbol.GetParameters();
            return parameters.Length > 0 && parameters[parameters.Length - 1].IsParams;
        }

        public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol? symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol m: return m.Parameters;
                case IPropertySymbol nt: return nt.Parameters;
                default: return ImmutableArray<IParameterSymbol>.Empty;
            }
        }

        public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol? symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol m: return m.TypeParameters;
                case INamedTypeSymbol nt: return nt.TypeParameters;
                default: return ImmutableArray<ITypeParameterSymbol>.Empty;
            }
        }

        public static ImmutableArray<ITypeParameterSymbol> GetAllTypeParameters(this ISymbol? symbol)
        {
            var results = ArrayBuilder<ITypeParameterSymbol>.GetInstance();

            while (symbol != null)
            {
                results.AddRange(symbol.GetTypeParameters());
                symbol = symbol.ContainingType;
            }

            return results.ToImmutableAndFree();
        }

        public static ImmutableArray<ITypeSymbol> GetTypeArguments(this ISymbol? symbol)
        {
            switch (symbol)
            {
                case IMethodSymbol m: return m.TypeArguments.ZipAsArray(m.TypeArgumentNullableAnnotations, (t, n) => t.WithNullability(n));
                case INamedTypeSymbol nt: return nt.TypeArguments.ZipAsArray(nt.TypeArgumentNullableAnnotations, (t, n) => t.WithNullability(n));
                default: return ImmutableArray.Create<ITypeSymbol>();
            }
        }

        public static ImmutableArray<ITypeSymbol> GetAllTypeArguments(this ISymbol symbol)
        {
            var results = ArrayBuilder<ITypeSymbol>.GetInstance();
            results.AddRange(symbol.GetTypeArguments());

            var containingType = symbol.ContainingType;
            while (containingType != null)
            {
                results.AddRange(containingType.GetTypeArguments());
                containingType = containingType.ContainingType;
            }

            return results.ToImmutableAndFree();
        }

        public static bool IsAttribute([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as ITypeSymbol)?.IsAttribute() == true;
        }

        /// <summary>
        /// Returns true if this symbol contains anything unsafe within it.  for example
        /// List&lt;int*[]&gt; is unsafe, as it "int* Goo { get; }"
        /// </summary>
        public static bool IsUnsafe([NotNullWhen(returnValue: true)] this ISymbol? member)
        {
            // TODO(cyrusn): Defer to compiler code to handle this once it can.
            return member?.Accept(new IsUnsafeVisitor()) == true;
        }

        public static ITypeSymbol ConvertToType(
            this ISymbol symbol,
            Compilation compilation,
            bool extensionUsedAsInstance = false)
        {
            if (symbol is ITypeSymbol type)
            {
                return type;
            }

            if (symbol is IMethodSymbol method && method.Parameters.All(p => p.RefKind == RefKind.None))
            {
                var count = extensionUsedAsInstance ? Math.Max(0, method.Parameters.Length - 1) : method.Parameters.Length;
                var skip = extensionUsedAsInstance ? 1 : 0;

                string WithArity(string typeName, int arity) => arity > 0 ? typeName + '`' + arity : typeName;

                // Convert the symbol to Func<...> or Action<...>
                var delegateType = compilation.GetTypeByMetadataName(method.ReturnsVoid
                    ? WithArity("System.Action", count)
                    : WithArity("System.Func", count + 1));

                if (delegateType != null)
                {
                    var types = method.Parameters
                        .Skip(skip)
                        .Select(p => (p.Type ?? compilation.GetSpecialType(SpecialType.System_Object)).WithNullability(p.NullableAnnotation));

                    if (!method.ReturnsVoid)
                    {
                        // +1 for the return type.
                        types = types.Concat((method.ReturnType ?? compilation.GetSpecialType(SpecialType.System_Object)).WithNullability(method.ReturnNullableAnnotation));
                    }

                    return delegateType.TryConstruct(types.ToArray());
                }
            }

            // Otherwise, just default to object.
            return compilation.ObjectType;
        }

        public static bool IsStaticType([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol != null && symbol.Kind == SymbolKind.NamedType && symbol.IsStatic;
        }

        public static bool IsNamespace([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol?.Kind == SymbolKind.Namespace;
        }

        public static bool IsOrContainsAccessibleAttribute(
            [NotNullWhen(returnValue: true)] this ISymbol? symbol, ISymbol withinType, IAssemblySymbol withinAssembly, CancellationToken cancellationToken)
        {
            var namespaceOrType = symbol is IAliasSymbol alias ? alias.Target : symbol as INamespaceOrTypeSymbol;
            if (namespaceOrType == null)
            {
                return false;
            }

            // PERF: Avoid allocating a lambda capture
            foreach (var type in namespaceOrType.GetAllTypes(cancellationToken))
            {
                if (type.IsAttribute() && type.IsAccessibleWithin(withinType ?? withinAssembly))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<IPropertySymbol> GetValidAnonymousTypeProperties(this ISymbol symbol)
        {
            Contract.ThrowIfFalse(symbol.IsNormalAnonymousType());
            return ((INamedTypeSymbol)symbol).GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName);
        }

        public static Accessibility ComputeResultantAccessibility(this ISymbol? symbol, ITypeSymbol finalDestination)
        {
            if (symbol == null)
            {
                return Accessibility.Private;
            }

            switch (symbol.DeclaredAccessibility)
            {
                default:
                    return symbol.DeclaredAccessibility;
                case Accessibility.ProtectedAndInternal:
                    return symbol.ContainingAssembly.GivesAccessTo(finalDestination.ContainingAssembly)
                        ? Accessibility.ProtectedAndInternal
                        : Accessibility.Internal;
                case Accessibility.ProtectedOrInternal:
                    return symbol.ContainingAssembly.GivesAccessTo(finalDestination.ContainingAssembly)
                        ? Accessibility.ProtectedOrInternal
                        : Accessibility.Protected;
            }
        }

        /// <returns>
        /// Returns true if symbol is a local variable and its declaring syntax node is 
        /// after the current position, false otherwise (including for non-local symbols)
        /// </returns>
        public static bool IsInaccessibleLocal(this ISymbol symbol, int position)
        {
            if (symbol.Kind != SymbolKind.Local)
            {
                return false;
            }

            // Implicitly declared locals (with Option Explicit Off in VB) are scoped to the entire
            // method and should always be considered accessible from within the same method.
            if (symbol.IsImplicitlyDeclared)
            {
                return false;
            }

            var declarationSyntax = symbol.DeclaringSyntaxReferences.Select(r => r.GetSyntax()).FirstOrDefault();
            return declarationSyntax != null && position < declarationSyntax.SpanStart;
        }

        /// <summary>
        /// Checks a given symbol for browsability based on its declaration location, attributes 
        /// explicitly limiting browsability, and whether showing of advanced members is enabled. 
        /// The optional attribute constructor parameters may be used to specify the symbols of the
        /// constructors of the various browsability limiting attributes because finding these 
        /// repeatedly over a large list of symbols can be slow. If providing these constructor 
        /// symbols, they should be in the format provided by 
        /// EditorBrowsableHelpers.GetSpecial*AttributeConstructor(). If these are not provided,
        /// they will be found in the compilation.
        /// </summary>
        public static bool IsEditorBrowsable(
            this ISymbol symbol,
            bool hideAdvancedMembers,
            Compilation compilation,
            IMethodSymbol? editorBrowsableAttributeConstructor = null,
            List<IMethodSymbol>? typeLibTypeAttributeConstructors = null,
            List<IMethodSymbol>? typeLibFuncAttributeConstructors = null,
            List<IMethodSymbol>? typeLibVarAttributeConstructors = null,
            INamedTypeSymbol? hideModuleNameAttribute = null)
        {
            // Namespaces can't have attributes, so just return true here.  This also saves us a 
            // costly check if this namespace has any locations in source (since a merged namespace
            // needs to go collect all the locations).
            if (symbol.Kind == SymbolKind.Namespace)
            {
                return true;
            }

            // check for IsImplicitlyDeclared so we don't spend time examining VB's embedded types.
            // This saves a few percent in typing scenarios.  An implicitly declared symbol can't
            // have attributes, so it can't be hidden by them.
            if (symbol.IsImplicitlyDeclared)
            {
                return true;
            }

            // Ignore browsability limiting attributes if the symbol is declared in source.
            // Check all locations since some of VB's embedded My symbols are declared in 
            // both source and the MyTemplateLocation.
            if (symbol.Locations.All(loc => loc.IsInSource))
            {
                // The HideModuleNameAttribute still applies to Modules defined in source
                return !IsBrowsingProhibitedByHideModuleNameAttribute(symbol, compilation, hideModuleNameAttribute);
            }

            return !IsBrowsingProhibited(
                symbol,
                hideAdvancedMembers,
                compilation,
                editorBrowsableAttributeConstructor,
                typeLibTypeAttributeConstructors,
                typeLibFuncAttributeConstructors,
                typeLibVarAttributeConstructors,
                hideModuleNameAttribute);
        }

        private static bool IsBrowsingProhibited(
            ISymbol symbol,
            bool hideAdvancedMembers,
            Compilation compilation,
            IMethodSymbol? editorBrowsableAttributeConstructor,
            List<IMethodSymbol>? typeLibTypeAttributeConstructors,
            List<IMethodSymbol>? typeLibFuncAttributeConstructors,
            List<IMethodSymbol>? typeLibVarAttributeConstructors,
            INamedTypeSymbol? hideModuleNameAttribute)
        {
            var attributes = symbol.GetAttributes();
            if (attributes.Length == 0)
            {
                return false;
            }

            return IsBrowsingProhibitedByEditorBrowsableAttribute(symbol, attributes, hideAdvancedMembers, compilation, editorBrowsableAttributeConstructor)
                || IsBrowsingProhibitedByTypeLibTypeAttribute(symbol, attributes, compilation, typeLibTypeAttributeConstructors)
                || IsBrowsingProhibitedByTypeLibFuncAttribute(symbol, attributes, compilation, typeLibFuncAttributeConstructors)
                || IsBrowsingProhibitedByTypeLibVarAttribute(symbol, attributes, compilation, typeLibVarAttributeConstructors)
                || IsBrowsingProhibitedByHideModuleNameAttribute(symbol, compilation, hideModuleNameAttribute, attributes);
        }

        private static bool IsBrowsingProhibitedByHideModuleNameAttribute(
            ISymbol symbol, Compilation compilation, INamedTypeSymbol? hideModuleNameAttribute, ImmutableArray<AttributeData> attributes = default)
        {
            if (!symbol.IsModuleType())
            {
                return false;
            }

            attributes = attributes.IsDefault ? symbol.GetAttributes() : attributes;
            hideModuleNameAttribute ??= compilation.HideModuleNameAttribute();
            foreach (var attribute in attributes)
            {
                if (Equals(attribute.AttributeClass, hideModuleNameAttribute))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBrowsingProhibitedByEditorBrowsableAttribute(
            ISymbol symbol, ImmutableArray<AttributeData> attributes, bool hideAdvancedMembers, Compilation compilation, IMethodSymbol? constructor)
        {
            constructor ??= EditorBrowsableHelpers.GetSpecialEditorBrowsableAttributeConstructor(compilation);
            if (constructor == null)
            {
                return false;
            }

            foreach (var attribute in attributes)
            {
                if (Equals(attribute.AttributeConstructor, constructor) &&
                    attribute.ConstructorArguments.Length == 1 &&
                    attribute.ConstructorArguments.First().Value is int)
                {
                    var state = (EditorBrowsableState)attribute.ConstructorArguments.First().Value;

                    if (EditorBrowsableState.Never == state ||
                        (hideAdvancedMembers && EditorBrowsableState.Advanced == state))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsBrowsingProhibitedByTypeLibTypeAttribute(
            ISymbol symbol, ImmutableArray<AttributeData> attributes, Compilation compilation, List<IMethodSymbol>? constructors)
        {
            return IsBrowsingProhibitedByTypeLibAttributeWorker(
                symbol,
                attributes,
                constructors ?? EditorBrowsableHelpers.GetSpecialTypeLibTypeAttributeConstructors(compilation),
                TypeLibTypeFlagsFHidden);
        }

        private static bool IsBrowsingProhibitedByTypeLibFuncAttribute(
            ISymbol symbol, ImmutableArray<AttributeData> attributes, Compilation compilation, List<IMethodSymbol>? constructors)
        {
            return IsBrowsingProhibitedByTypeLibAttributeWorker(
                symbol,
                attributes,
                constructors ?? EditorBrowsableHelpers.GetSpecialTypeLibFuncAttributeConstructors(compilation),
                TypeLibFuncFlagsFHidden);
        }

        private static bool IsBrowsingProhibitedByTypeLibVarAttribute(
            ISymbol symbol, ImmutableArray<AttributeData> attributes, Compilation compilation, List<IMethodSymbol>? constructors)
        {
            return IsBrowsingProhibitedByTypeLibAttributeWorker(
                symbol,
                attributes,
                constructors ?? EditorBrowsableHelpers.GetSpecialTypeLibVarAttributeConstructors(compilation),
                TypeLibVarFlagsFHidden);
        }

        private const int TypeLibTypeFlagsFHidden = 0x0010;
        private const int TypeLibFuncFlagsFHidden = 0x0040;
        private const int TypeLibVarFlagsFHidden = 0x0040;

        private static bool IsBrowsingProhibitedByTypeLibAttributeWorker(
            ISymbol symbol, ImmutableArray<AttributeData> attributes, List<IMethodSymbol> attributeConstructors, int hiddenFlag)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.ConstructorArguments.Length == 1)
                {
                    foreach (var constructor in attributeConstructors)
                    {
                        if (Equals(attribute.AttributeConstructor, constructor))
                        {
                            var actualFlags = 0;

                            // Check for both constructor signatures. The constructor that takes a TypeLib*Flags reports an int argument.
                            var argumentValue = attribute.ConstructorArguments.First().Value;

                            if (argumentValue is int i)
                            {
                                actualFlags = i;
                            }
                            else if (argumentValue is short sh)
                            {
                                actualFlags = sh;
                            }
                            else
                            {
                                continue;
                            }

                            if ((actualFlags & hiddenFlag) == hiddenFlag)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public static bool IsAccessor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return symbol.IsPropertyAccessor() || symbol.IsEventAccessor();
        }

        public static bool IsPropertyAccessor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind.IsPropertyAccessor() == true;
        }

        public static bool IsEventAccessor([NotNullWhen(returnValue: true)] this ISymbol? symbol)
        {
            var method = symbol as IMethodSymbol;
            return method != null &&
                (method.MethodKind == MethodKind.EventAdd ||
                 method.MethodKind == MethodKind.EventRaise ||
                 method.MethodKind == MethodKind.EventRemove);
        }

        public static bool IsFromSource(this ISymbol symbol)
            => symbol.Locations.Any() && symbol.Locations.All(location => location.IsInSource);

        public static bool IsNonImplicitAndFromSource(this ISymbol symbol)
            => !symbol.IsImplicitlyDeclared && symbol.IsFromSource();

        public static DeclarationModifiers GetSymbolModifiers(this ISymbol symbol)
        {
            return new DeclarationModifiers(
                isStatic: symbol.IsStatic,
                isAbstract: symbol.IsAbstract,
                isUnsafe: symbol.IsUnsafe(),
                isVirtual: symbol.IsVirtual,
                isOverride: symbol.IsOverride,
                isSealed: symbol.IsSealed);
        }

        public static ITypeSymbol? GetSymbolType(this ISymbol? symbol)
        {
            switch (symbol)
            {
                case ILocalSymbol localSymbol:
                    return localSymbol.GetTypeWithAnnotatedNullability();
                case IFieldSymbol fieldSymbol:
                    return fieldSymbol.GetTypeWithAnnotatedNullability();
                case IPropertySymbol propertySymbol:
                    return propertySymbol.GetTypeWithAnnotatedNullability();
                case IParameterSymbol parameterSymbol:
                    return parameterSymbol.GetTypeWithAnnotatedNullability();
                case IAliasSymbol aliasSymbol:
                    return aliasSymbol.Target as ITypeSymbol;
            }

            return symbol as ITypeSymbol;
        }

        public static DocumentationComment GetDocumentationComment(
            this ISymbol symbol,
            Compilation compilation,
            CultureInfo? preferredCulture = null,
            bool expandIncludes = false,
            bool expandInheritdoc = false,
            CancellationToken cancellationToken = default)
        {
            var xmlText = symbol.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
            if (expandInheritdoc)
            {
                if (string.IsNullOrEmpty(xmlText) && IsEligibleForAutomaticInheritdoc(symbol))
                {
                    xmlText = $@"<doc><inheritdoc/></doc>";
                }

                try
                {
                    var element = XElement.Parse(xmlText, LoadOptions.PreserveWhitespace);
                    element.ReplaceNodes(RewriteMany(symbol, compilation, element.Nodes().ToArray(), cancellationToken));
                    xmlText = element.ToString(SaveOptions.DisableFormatting);
                }
                catch
                {
                }
            }

            return string.IsNullOrEmpty(xmlText) ? DocumentationComment.Empty : DocumentationComment.FromXmlFragment(xmlText);

            static bool IsEligibleForAutomaticInheritdoc(ISymbol symbol)
            {
                // Only the following symbols are eligible to inherit documentation without an <inheritdoc/> element:
                //
                // * Members that override an inherited member
                // * Members that implement an interface member
                if (symbol.IsOverride)
                {
                    return true;
                }

                if (symbol.ContainingType is null)
                {
                    // Observed with certain implicit operators, such as operator==(void*, void*).
                    return false;
                }

                switch (symbol.Kind)
                {
                    case SymbolKind.Method:
                    case SymbolKind.Property:
                    case SymbolKind.Event:
                        if (symbol.ExplicitOrImplicitInterfaceImplementations().Any())
                        {
                            return true;
                        }

                        break;

                    default:
                        break;
                }

                return false;
            }
        }

        private static XNode[] RewriteInheritdocElements(ISymbol symbol, Compilation compilation, XNode node, CancellationToken cancellationToken)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                var element = (XElement)node;
                if (ElementNameIs(element, DocumentationCommentXmlNames.InheritdocElementName))
                {
                    var rewritten = RewriteInheritdocElement(symbol, compilation, element, cancellationToken);
                    if (rewritten is object)
                    {
                        return rewritten;
                    }
                }
            }

            var container = node as XContainer;
            if (container == null)
            {
                return new XNode[] { Copy(node, copyAttributeAnnotations: false) };
            }

            var oldNodes = container.Nodes();

            // Do this after grabbing the nodes, so we don't see copies of them.
            container = Copy(container, copyAttributeAnnotations: false);

            // WARN: don't use node after this point - use container since it's already been copied.

            if (oldNodes != null)
            {
                XNode[] rewritten = RewriteMany(symbol, compilation, oldNodes.ToArray(), cancellationToken);
                container.ReplaceNodes(rewritten);
            }

            return new XNode[] { container };
        }

        private static XNode[] RewriteMany(ISymbol symbol, Compilation compilation, XNode[] nodes, CancellationToken cancellationToken)
        {
            var result = new List<XNode>();
            foreach (var child in nodes)
            {
                result.AddRange(RewriteInheritdocElements(symbol, compilation, child, cancellationToken));
            }

            return result.ToArray();
        }

        private static XNode[]? RewriteInheritdocElement(ISymbol memberSymbol, Compilation compilation, XElement element, CancellationToken cancellationToken)
        {
            var crefAttribute = element.Attribute(XName.Get(DocumentationCommentXmlNames.CrefAttributeName));
            var pathAttribute = element.Attribute(XName.Get(DocumentationCommentXmlNames.PathAttributeName));

            var candidate = GetCandidateSymbol(memberSymbol);
            var hasCandidateCref = candidate is object;

            var hasCrefAttribute = crefAttribute is object;
            var hasPathAttribute = pathAttribute is object;
            if (!hasCrefAttribute && !hasCandidateCref)
            {
                // No cref available
                return null;
            }

            ISymbol symbol;
            if (crefAttribute is null)
            {
                symbol = candidate;
            }
            else
            {
                var crefValue = crefAttribute.Value;
                symbol = DocumentationCommentId.GetFirstSymbolForDeclarationId(crefValue, compilation);
                if (symbol is null)
                {
                    return null;
                }
            }

            try
            {
                var inheritedDocumentation = GetDocumentationComment(symbol, compilation, preferredCulture: null, expandIncludes: true, expandInheritdoc: true, cancellationToken);
                if (inheritedDocumentation == DocumentationComment.Empty)
                {
                    return Array.Empty<XNode>();
                }

                var document = XDocument.Parse(inheritedDocumentation.FullXmlFragment);
                string xpathValue;
                if (string.IsNullOrEmpty(pathAttribute?.Value))
                {
                    xpathValue = BuildXPathForElement(element.Parent);
                }
                else
                {
                    xpathValue = pathAttribute!.Value;
                    if (xpathValue.StartsWith("/"))
                    {
                        // Account for the root <doc> or <member> element
                        xpathValue = "/*" + xpathValue;
                    }
                }

                var loadedElements = TrySelectNodes(document, xpathValue);
                if (loadedElements is null)
                {
                    return Array.Empty<XNode>();
                }

                if (loadedElements?.Length > 0)
                {
                    // change the current XML file path for nodes contained in the document:
                    // prototype(inheritdoc): what should the file path be?
                    var result = RewriteMany(symbol, compilation, loadedElements, cancellationToken);

                    // The elements could be rewritten away if they are includes that refer to invalid
                    // (but existing and accessible) XML files.  If this occurs, behave as if we
                    // had failed to find any XPath results (as in Dev11).
                    if (result.Length > 0)
                    {
                        return result;
                    }
                }

                return null;
            }
            catch (XmlException)
            {
                return Array.Empty<XNode>();
            }

            // Local functions
            static ISymbol GetCandidateSymbol(ISymbol memberSymbol)
            {
                if (memberSymbol.ExplicitInterfaceImplementations().Any())
                {
                    return memberSymbol.ExplicitInterfaceImplementations().First();
                }
                else if (memberSymbol.IsOverride)
                {
                    return memberSymbol.OverriddenMember();
                }

                if (memberSymbol is IMethodSymbol methodSymbol)
                {
                    if (methodSymbol.MethodKind == MethodKind.Constructor || methodSymbol.MethodKind == MethodKind.StaticConstructor)
                    {
                        var baseType = memberSymbol.ContainingType.BaseType;
                        return baseType.Constructors.Where(c => IsSameSignature(methodSymbol, c)).FirstOrDefault();
                    }
                    else
                    {
                        // check for implicit interface
                        return methodSymbol.ExplicitOrImplicitInterfaceImplementations().FirstOrDefault();
                    }
                }
                else if (memberSymbol is INamedTypeSymbol typeSymbol)
                {
                    if (typeSymbol.TypeKind == TypeKind.Class)
                    {
                        // prototype(inheritdoc): when does base class take precedence over interface?
                        return typeSymbol.BaseType;
                    }
                    else if (typeSymbol.TypeKind == TypeKind.Interface)
                    {
                        return typeSymbol.Interfaces.FirstOrDefault();
                    }
                    else
                    {
                        // This includes structs, enums, and delegates as mentioned in the inheritdoc spec
                        return null;
                    }
                }

                return memberSymbol.ExplicitOrImplicitInterfaceImplementations().FirstOrDefault();
            }

            static bool IsSameSignature(IMethodSymbol left, IMethodSymbol right)
            {
                if (left.Parameters.Length != right.Parameters.Length)
                {
                    return false;
                }

                if (left.IsStatic != right.IsStatic)
                {
                    return false;
                }

                if (!left.ReturnType.Equals(right.ReturnType))
                {
                    return false;
                }

                for (int i = 0; i < left.Parameters.Length; i++)
                {
                    if (!left.Parameters[i].Type.Equals(right.Parameters[i].Type))
                    {
                        return false;
                    }
                }

                return true;
            }

            static string BuildXPathForElement(XElement element)
            {
                if (ElementNameIs(element, "member") || ElementNameIs(element, "doc"))
                {
                    // Avoid string concatenation allocations for inheritdoc as a top-level element
                    return "/*/node()[not(self::overloads)]";
                }

                var path = "/node()[not(self::overloads)]";
                for (var current = element; current != null; current = current.Parent)
                {
                    var currentName = current.Name.ToString();
                    if (ElementNameIs(current, "member") || ElementNameIs(current, "doc"))
                    {
                        // Allow <member> and <doc> to be used interchangeably
                        currentName = "*";
                    }

                    path = "/" + currentName + path;
                }

                return path;
            }
        }

        private static TNode Copy<TNode>(TNode node, bool copyAttributeAnnotations)
            where TNode : XNode
        {
            XNode copy;

            // Documents can't be added to containers, so our usual copy trick won't work.
            if (node.NodeType == XmlNodeType.Document)
            {
                copy = new XDocument(((XDocument)(object)node));
            }
            else
            {
                XContainer temp = new XElement("temp");
                temp.Add(node);
                copy = temp.LastNode;
                temp.RemoveNodes();
            }

            Debug.Assert(copy != node);
            Debug.Assert(copy.Parent == null); // Otherwise, when we give it one, it will be copied.

            // Copy annotations, the above doesn't preserve them.
            // We need to preserve Location annotations as well as line position annotations.
            CopyAnnotations(node, copy);

            // We also need to preserve line position annotations for all attributes
            // since we report errors with attribute locations.
            if (copyAttributeAnnotations && node.NodeType == XmlNodeType.Element)
            {
                var sourceElement = (XElement)(object)node;
                var targetElement = (XElement)copy;

                var sourceAttributes = sourceElement.Attributes().GetEnumerator();
                var targetAttributes = targetElement.Attributes().GetEnumerator();
                while (sourceAttributes.MoveNext() && targetAttributes.MoveNext())
                {
                    Debug.Assert(sourceAttributes.Current.Name == targetAttributes.Current.Name);
                    CopyAnnotations(sourceAttributes.Current, targetAttributes.Current);
                }
            }

            return (TNode)copy;
        }

        private static void CopyAnnotations(XObject source, XObject target)
        {
            foreach (var annotation in source.Annotations<object>())
            {
                target.AddAnnotation(annotation);
            }
        }

        private static XNode[]? TrySelectNodes(XNode node, string xpath)
        {
            try
            {
                var xpathResult = (IEnumerable)System.Xml.XPath.Extensions.XPathEvaluate(node, xpath);

                // Throws InvalidOperationException if the result of the XPath is an XDocument:
                return xpathResult?.Cast<XNode>().ToArray();
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (XPathException)
            {
                return null;
            }
        }

        private static bool ElementNameIs(XElement element, string name)
        {
            return string.IsNullOrEmpty(element.Name.NamespaceName) && DocumentationCommentXmlNames.ElementEquals(element.Name.LocalName, name);
        }

        /// <summary>
        /// If the <paramref name="symbol"/> is a method symbol, returns <see langword="true"/> if the method's return type is "awaitable", but not if it's <see langword="dynamic"/>.
        /// If the <paramref name="symbol"/> is a type symbol, returns <see langword="true"/> if that type is "awaitable".
        /// An "awaitable" is any type that exposes a GetAwaiter method which returns a valid "awaiter". This GetAwaiter method may be an instance method or an extension method.
        /// </summary>
        public static bool IsAwaitableNonDynamic([NotNullWhen(returnValue: true)] this ISymbol? symbol, SemanticModel semanticModel, int position)
        {
            var methodSymbol = symbol as IMethodSymbol;
            ITypeSymbol? typeSymbol = null;

            if (methodSymbol == null)
            {
                typeSymbol = symbol as ITypeSymbol;
                if (typeSymbol == null)
                {
                    return false;
                }
            }
            else
            {
                if (methodSymbol.ReturnType == null)
                {
                    return false;
                }
            }

            // otherwise: needs valid GetAwaiter
            var potentialGetAwaiters = semanticModel.LookupSymbols(position,
                                                                   container: typeSymbol ?? methodSymbol!.ReturnType.OriginalDefinition,
                                                                   name: WellKnownMemberNames.GetAwaiter,
                                                                   includeReducedExtensionMethods: true);
            var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where(x => !x.Parameters.Any());
            return getAwaiters.Any(VerifyGetAwaiter);
        }

        public static bool IsValidGetAwaiter(this IMethodSymbol symbol)
            => symbol.Name == WellKnownMemberNames.GetAwaiter &&
            VerifyGetAwaiter(symbol);

        private static bool VerifyGetAwaiter(IMethodSymbol getAwaiter)
        {
            var returnType = getAwaiter.ReturnType;
            if (returnType == null)
            {
                return false;
            }

            // bool IsCompleted { get }
            if (!returnType.GetMembers().OfType<IPropertySymbol>().Any(p => p.Name == WellKnownMemberNames.IsCompleted && p.Type.SpecialType == SpecialType.System_Boolean && p.GetMethod != null))
            {
                return false;
            }

            var methods = returnType.GetMembers().OfType<IMethodSymbol>();

            // NOTE: (vladres) The current version of C# Spec, §7.7.7.3 'Runtime evaluation of await expressions', requires that
            // NOTE: the interface method INotifyCompletion.OnCompleted or ICriticalNotifyCompletion.UnsafeOnCompleted is invoked
            // NOTE: (rather than any OnCompleted method conforming to a certain pattern).
            // NOTE: Should this code be updated to match the spec?

            // void OnCompleted(Action) 
            // Actions are delegates, so we'll just check for delegates.
            if (!methods.Any(x => x.Name == WellKnownMemberNames.OnCompleted && x.ReturnsVoid && x.Parameters.Length == 1 && x.Parameters.First().Type.TypeKind == TypeKind.Delegate))
            {
                return false;
            }

            // void GetResult() || T GetResult()
            return methods.Any(m => m.Name == WellKnownMemberNames.GetResult && !m.Parameters.Any());
        }

        /// <summary>
        /// First, remove symbols from the set if they are overridden by other symbols in the set.
        /// If a symbol is overridden only by symbols outside of the set, then it is not removed. 
        /// This is useful for filtering out symbols that cannot be accessed in a given context due
        /// to the existence of overriding members. Second, remove remaining symbols that are
        /// unsupported (e.g. pointer types in VB) or not editor browsable based on the EditorBrowsable
        /// attribute.
        /// </summary>
        public static ImmutableArray<T> FilterToVisibleAndBrowsableSymbols<T>(
            this ImmutableArray<T> symbols, bool hideAdvancedMembers, Compilation compilation) where T : ISymbol
        {
            symbols = symbols.RemoveOverriddenSymbolsWithinSet();

            // Since all symbols are from the same compilation, find the required attribute
            // constructors once and reuse.

            var editorBrowsableAttributeConstructor = EditorBrowsableHelpers.GetSpecialEditorBrowsableAttributeConstructor(compilation);
            var typeLibTypeAttributeConstructors = EditorBrowsableHelpers.GetSpecialTypeLibTypeAttributeConstructors(compilation);
            var typeLibFuncAttributeConstructors = EditorBrowsableHelpers.GetSpecialTypeLibFuncAttributeConstructors(compilation);
            var typeLibVarAttributeConstructors = EditorBrowsableHelpers.GetSpecialTypeLibVarAttributeConstructors(compilation);
            var hideModuleNameAttribute = compilation.HideModuleNameAttribute();

            // PERF: HasUnsupportedMetadata may require recreating the syntax tree to get the base class, so first
            // check to see if we're referencing a symbol defined in source.
            bool isSymbolDefinedInSource(Location l) => l.IsInSource;
            return symbols.WhereAsArray(s =>
                (s.Locations.Any(isSymbolDefinedInSource) || !s.HasUnsupportedMetadata) &&
                !s.IsDestructor() &&
                s.IsEditorBrowsable(
                    hideAdvancedMembers,
                    compilation,
                    editorBrowsableAttributeConstructor,
                    typeLibTypeAttributeConstructors,
                    typeLibFuncAttributeConstructors,
                    typeLibVarAttributeConstructors,
                    hideModuleNameAttribute));
        }

        private static ImmutableArray<T> RemoveOverriddenSymbolsWithinSet<T>(this ImmutableArray<T> symbols) where T : ISymbol
        {
            var overriddenSymbols = new HashSet<ISymbol>();

            foreach (var symbol in symbols)
            {
                var overriddenMember = symbol.OverriddenMember();
                if (overriddenMember != null && !overriddenSymbols.Contains(overriddenMember))
                {
                    overriddenSymbols.Add(overriddenMember);
                }
            }

            return symbols.WhereAsArray(s => !overriddenSymbols.Contains(s));
        }

        public static ImmutableArray<T> FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols<T>(
            this ImmutableArray<T> symbols, bool hideAdvancedMembers, Compilation compilation) where T : ISymbol
        {
            return symbols.FilterToVisibleAndBrowsableSymbols(hideAdvancedMembers, compilation)
                .WhereAsArray(s => !s.IsUnsafe());
        }
    }
}
