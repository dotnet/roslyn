// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editing;
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

        public static ISymbol OverriddenMember(this ISymbol symbol)
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
            return symbol.TypeSwitch(
                (IEventSymbol @event) => @event.ExplicitInterfaceImplementations.As<ISymbol>(),
                (IMethodSymbol method) => method.ExplicitInterfaceImplementations.As<ISymbol>(),
                (IPropertySymbol property) => property.ExplicitInterfaceImplementations.As<ISymbol>(),
                _ => ImmutableArray.Create<ISymbol>());
        }

        public static bool IsOverridable(this ISymbol symbol)
        {
            return
                symbol != null &&
                symbol.ContainingType != null &&
                symbol.ContainingType.TypeKind == TypeKind.Class &&
                (symbol.IsVirtual || symbol.IsAbstract || symbol.IsOverride) &&
                !symbol.IsSealed;
        }

        public static bool IsImplementableMember(this ISymbol symbol)
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

                if (symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).MethodKind == MethodKind.Ordinary)
                {
                    return true;
                }
            }

            return false;
        }

        public static INamedTypeSymbol GetContainingTypeOrThis(this ISymbol symbol)
        {
            if (symbol is INamedTypeSymbol)
            {
                return (INamedTypeSymbol)symbol;
            }

            return symbol.ContainingType;
        }

        public static bool IsPointerType(this ISymbol symbol)
        {
            return symbol is IPointerTypeSymbol;
        }

        public static bool IsErrorType(this ISymbol symbol)
        {
            return (symbol as ITypeSymbol)?.IsErrorType() == true;
        }

        public static bool IsModuleType(this ISymbol symbol)
        {
            return (symbol as ITypeSymbol)?.IsModuleType() == true;
        }

        public static bool IsInterfaceType(this ISymbol symbol)
        {
            return (symbol as ITypeSymbol)?.IsInterfaceType() == true;
        }

        public static bool IsArrayType(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.ArrayType;
        }

        public static bool IsAnonymousFunction(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.AnonymousFunction;
        }

        public static bool IsKind(this ISymbol symbol, SymbolKind kind)
        {
            return symbol.MatchesKind(kind);
        }

        public static bool MatchesKind(this ISymbol symbol, SymbolKind kind)
        {
            return symbol?.Kind == kind;
        }

        public static bool MatchesKind(this ISymbol symbol, SymbolKind kind1, SymbolKind kind2)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2);
        }

        public static bool MatchesKind(this ISymbol symbol, SymbolKind kind1, SymbolKind kind2, SymbolKind kind3)
        {
            return symbol != null
                && (symbol.Kind == kind1 || symbol.Kind == kind2 || symbol.Kind == kind3);
        }

        public static bool MatchesKind(this ISymbol symbol, params SymbolKind[] kinds)
        {
            return symbol != null
                && kinds.Contains(symbol.Kind);
        }

        public static bool IsReducedExtension(this ISymbol symbol)
        {
            return symbol is IMethodSymbol && ((IMethodSymbol)symbol).MethodKind == MethodKind.ReducedExtension;
        }

        public static bool IsExtensionMethod(this ISymbol symbol)
        {
            return symbol.Kind == SymbolKind.Method && ((IMethodSymbol)symbol).IsExtensionMethod;
        }

        public static bool IsModuleMember(this ISymbol symbol)
        {
            return symbol != null && symbol.ContainingSymbol is INamedTypeSymbol && symbol.ContainingType.TypeKind == TypeKind.Module;
        }

        public static bool IsConstructor(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Constructor;
        }

        public static bool IsStaticConstructor(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.StaticConstructor;
        }

        public static bool IsDestructor(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Destructor;
        }

        public static bool IsUserDefinedOperator(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.UserDefinedOperator;
        }

        public static bool IsConversion(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Conversion;
        }

        public static bool IsOrdinaryMethod(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind == MethodKind.Ordinary;
        }

        public static bool IsDelegateType(this ISymbol symbol)
        {
            return symbol is ITypeSymbol && ((ITypeSymbol)symbol).TypeKind == TypeKind.Delegate;
        }

        public static bool IsAnonymousType(this ISymbol symbol)
        {
            return symbol is INamedTypeSymbol && ((INamedTypeSymbol)symbol).IsAnonymousType;
        }

        public static bool IsNormalAnonymousType(this ISymbol symbol)
        {
            return symbol.IsAnonymousType() && !symbol.IsDelegateType();
        }

        public static bool IsAnonymousDelegateType(this ISymbol symbol)
        {
            return symbol.IsAnonymousType() && symbol.IsDelegateType();
        }

        public static bool IsAnonymousTypeProperty(this ISymbol symbol)
        {
            return symbol is IPropertySymbol && symbol.ContainingType.IsNormalAnonymousType();
        }

        public static bool IsIndexer(this ISymbol symbol)
        {
            return (symbol as IPropertySymbol)?.IsIndexer == true;
        }

        public static bool IsWriteableFieldOrProperty(this ISymbol symbol)
        {
            var fieldSymbol = symbol as IFieldSymbol;
            if (fieldSymbol != null)
            {
                return !fieldSymbol.IsReadOnly
                    && !fieldSymbol.IsConst;
            }

            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null)
            {
                return !propertySymbol.IsReadOnly;
            }

            return false;
        }

        public static ITypeSymbol GetMemberType(this ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    return ((IFieldSymbol)symbol).Type;
                case SymbolKind.Property:
                    return ((IPropertySymbol)symbol).Type;
                case SymbolKind.Method:
                    return ((IMethodSymbol)symbol).ReturnType;
                case SymbolKind.Event:
                    return ((IEventSymbol)symbol).Type;
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

        public static ISymbol GetOriginalUnreducedDefinition(this ISymbol symbol)
        {
            if (symbol.IsReducedExtension())
            {
                // note: ReducedFrom is only a method definition and includes no type arguments.
                symbol = ((IMethodSymbol)symbol).GetConstructedReducedFrom();
            }

            if (symbol.IsFunctionValue())
            {
                var method = symbol.ContainingSymbol as IMethodSymbol;
                if (method != null)
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

            var parameter = symbol as IParameterSymbol;
            if (parameter != null)
            {
                var method = parameter.ContainingSymbol as IMethodSymbol;
                if (method?.IsReducedExtension() == true)
                {
                    symbol = method.GetConstructedReducedFrom().Parameters[parameter.Ordinal + 1];
                }
            }

            return symbol?.OriginalDefinition;
        }

        public static bool IsFunctionValue(this ISymbol symbol)
        {
            return symbol is ILocalSymbol && ((ILocalSymbol)symbol).IsFunctionValue;
        }

        public static bool IsThisParameter(this ISymbol symbol)
        {
            return symbol != null && symbol.Kind == SymbolKind.Parameter && ((IParameterSymbol)symbol).IsThis;
        }

        public static ISymbol ConvertThisParameterToType(this ISymbol symbol)
        {
            if (symbol.IsThisParameter())
            {
                return ((IParameterSymbol)symbol).Type;
            }

            return symbol;
        }

        public static bool IsParams(this ISymbol symbol)
        {
            var parameters = symbol.GetParameters();
            return parameters.Length > 0 && parameters[parameters.Length - 1].IsParams;
        }

        public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol symbol)
        {
            return symbol.TypeSwitch(
                (IMethodSymbol m) => m.Parameters,
                (IPropertySymbol nt) => nt.Parameters,
                _ => ImmutableArray.Create<IParameterSymbol>());
        }

        public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol symbol)
        {
            return symbol.TypeSwitch(
                (IMethodSymbol m) => m.TypeParameters,
                (INamedTypeSymbol nt) => nt.TypeParameters,
                _ => ImmutableArray.Create<ITypeParameterSymbol>());
        }

        public static ImmutableArray<ITypeSymbol> GetTypeArguments(this ISymbol symbol)
        {
            return symbol.TypeSwitch(
                (IMethodSymbol m) => m.TypeArguments,
                (INamedTypeSymbol nt) => nt.TypeArguments,
                _ => ImmutableArray.Create<ITypeSymbol>());
        }

        public static ImmutableArray<ITypeSymbol> GetAllTypeArguments(this ISymbol symbol)
        {
            var results = ImmutableArray.CreateBuilder<ITypeSymbol>();
            results.AddRange(symbol.GetTypeArguments());

            var containingType = symbol.ContainingType;
            while (containingType != null)
            {
                results.AddRange(containingType.GetTypeArguments());
                containingType = containingType.ContainingType;
            }

            return results.AsImmutable();
        }

        public static bool IsAttribute(this ISymbol symbol)
        {
            return (symbol as ITypeSymbol)?.IsAttribute() == true;
        }

        /// <summary>
        /// Returns true if this symbol contains anything unsafe within it.  for example
        /// List&lt;int*[]&gt; is unsafe, as it "int* Foo { get; }"
        /// </summary>
        public static bool IsUnsafe(this ISymbol member)
        {
            // TODO(cyrusn): Defer to compiler code to handle this once it can.
            return member?.Accept(new IsUnsafeVisitor()) == true;
        }

        public static ITypeSymbol ConvertToType(
            this ISymbol symbol,
            Compilation compilation,
            bool extensionUsedAsInstance = false)
        {
            var type = symbol as ITypeSymbol;
            if (type != null)
            {
                return type;
            }

            var method = symbol as IMethodSymbol;
            if (method != null && !method.Parameters.Any(p => p.RefKind != RefKind.None))
            {
                // Convert the symbol to Func<...> or Action<...>
                if (method.ReturnsVoid)
                {
                    var count = extensionUsedAsInstance ? method.Parameters.Length - 1 : method.Parameters.Length;
                    var skip = extensionUsedAsInstance ? 1 : 0;
                    count = Math.Max(0, count);
                    if (count == 0)
                    {
                        // Action
                        return compilation.ActionType();
                    }
                    else
                    {
                        // Action<TArg1, ..., TArgN>
                        var actionName = "System.Action`" + count;
                        var actionType = compilation.GetTypeByMetadataName(actionName);

                        if (actionType != null)
                        {
                            var types = method.Parameters
                                .Skip(skip)
                                .Select(p =>
                                    p.Type == null ?
                                    compilation.GetSpecialType(SpecialType.System_Object) :
                                    p.Type)
                                .ToArray();
                            return actionType.Construct(types);
                        }
                    }
                }
                else
                {
                    // Func<TArg1,...,TArgN,TReturn>
                    //
                    // +1 for the return type.
                    var count = extensionUsedAsInstance ? method.Parameters.Length - 1 : method.Parameters.Length;
                    var skip = extensionUsedAsInstance ? 1 : 0;
                    var functionName = "System.Func`" + (count + 1);
                    var functionType = compilation.GetTypeByMetadataName(functionName);

                    if (functionType != null)
                    {
                        var types = method.Parameters
                            .Skip(skip)
                            .Select(p => p.Type)
                            .Concat(method.ReturnType)
                            .Select(t =>
                                t == null ?
                                compilation.GetSpecialType(SpecialType.System_Object) :
                                t)
                            .ToArray();
                        return functionType.Construct(types);
                    }
                }
            }

            // Otherwise, just default to object.
            return compilation.ObjectType;
        }

        public static bool IsStaticType(this ISymbol symbol)
        {
            return symbol != null && symbol.Kind == SymbolKind.NamedType && symbol.IsStatic;
        }

        public static bool IsNamespace(this ISymbol symbol)
        {
            return symbol?.Kind == SymbolKind.Namespace;
        }

        public static bool IsOrContainsAccessibleAttribute(this ISymbol symbol, ISymbol withinType, IAssemblySymbol withinAssembly)
        {
            var alias = symbol as IAliasSymbol;
            if (alias != null)
            {
                symbol = alias.Target;
            }

            var namespaceOrType = symbol as INamespaceOrTypeSymbol;
            if (namespaceOrType == null)
            {
                return false;
            }

            if (namespaceOrType.IsAttribute() && namespaceOrType.IsAccessibleWithin(withinType ?? withinAssembly))
            {
                return true;
            }

            return namespaceOrType.GetMembers().Any(nt => nt.IsOrContainsAccessibleAttribute(withinType, withinAssembly));
        }

        public static IEnumerable<IPropertySymbol> GetValidAnonymousTypeProperties(this ISymbol symbol)
        {
            Contract.ThrowIfFalse(symbol.IsNormalAnonymousType());
            return ((INamedTypeSymbol)symbol).GetMembers().OfType<IPropertySymbol>().Where(p => p.CanBeReferencedByName);
        }

        public static Accessibility ComputeResultantAccessibility(this ISymbol symbol, ITypeSymbol finalDestination)
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
            IMethodSymbol editorBrowsableAttributeConstructor = null,
            List<IMethodSymbol> typeLibTypeAttributeConstructors = null,
            List<IMethodSymbol> typeLibFuncAttributeConstructors = null,
            List<IMethodSymbol> typeLibVarAttributeConstructors = null,
            INamedTypeSymbol hideModuleNameAttribute = null)
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
            IMethodSymbol editorBrowsableAttributeConstructor,
            List<IMethodSymbol> typeLibTypeAttributeConstructors,
            List<IMethodSymbol> typeLibFuncAttributeConstructors,
            List<IMethodSymbol> typeLibVarAttributeConstructors,
            INamedTypeSymbol hideModuleNameAttribute)
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
            ISymbol symbol, Compilation compilation, INamedTypeSymbol hideModuleNameAttribute, ImmutableArray<AttributeData> attributes = default(ImmutableArray<AttributeData>))
        {
            if (!symbol.IsModuleType())
            {
                return false;
            }

            attributes = attributes.IsDefault ? symbol.GetAttributes() : attributes;
            hideModuleNameAttribute = hideModuleNameAttribute ?? compilation.HideModuleNameAttribute();
            foreach (var attribute in attributes)
            {
                if (attribute.AttributeClass == hideModuleNameAttribute)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBrowsingProhibitedByEditorBrowsableAttribute(
            ISymbol symbol, ImmutableArray<AttributeData> attributes, bool hideAdvancedMembers, Compilation compilation, IMethodSymbol constructor)
        {
            constructor = constructor ?? EditorBrowsableHelpers.GetSpecialEditorBrowsableAttributeConstructor(compilation);
            if (constructor == null)
            {
                return false;
            }

            foreach (var attribute in attributes)
            {
                if (attribute.AttributeConstructor == constructor &&
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
            ISymbol symbol, ImmutableArray<AttributeData> attributes, Compilation compilation, List<IMethodSymbol> constructors)
        {
            return IsBrowsingProhibitedByTypeLibAttributeWorker(
                symbol,
                attributes,
                constructors ?? EditorBrowsableHelpers.GetSpecialTypeLibTypeAttributeConstructors(compilation),
                TypeLibTypeFlagsFHidden);
        }

        private static bool IsBrowsingProhibitedByTypeLibFuncAttribute(
            ISymbol symbol, ImmutableArray<AttributeData> attributes, Compilation compilation, List<IMethodSymbol> constructors)
        {
            return IsBrowsingProhibitedByTypeLibAttributeWorker(
                symbol,
                attributes,
                constructors ?? EditorBrowsableHelpers.GetSpecialTypeLibFuncAttributeConstructors(compilation),
                TypeLibFuncFlagsFHidden);
        }

        private static bool IsBrowsingProhibitedByTypeLibVarAttribute(
            ISymbol symbol, ImmutableArray<AttributeData> attributes, Compilation compilation, List<IMethodSymbol> constructors)
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
                        if (attribute.AttributeConstructor == constructor)
                        {
                            var actualFlags = 0;

                            // Check for both constructor signatures. The constructor that takes a TypeLib*Flags reports an int argument.
                            var argumentValue = attribute.ConstructorArguments.First().Value;

                            if (argumentValue is int)
                            {
                                actualFlags = (int)argumentValue;
                            }
                            else if (argumentValue is short)
                            {
                                actualFlags = (short)argumentValue;
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

        public static bool IsAccessor(this ISymbol symbol)
        {
            return symbol.IsPropertyAccessor() || symbol.IsEventAccessor();
        }

        public static bool IsPropertyAccessor(this ISymbol symbol)
        {
            return (symbol as IMethodSymbol)?.MethodKind.IsPropertyAccessor() == true;
        }

        public static bool IsEventAccessor(this ISymbol symbol)
        {
            var method = symbol as IMethodSymbol;
            return method != null &&
                (method.MethodKind == MethodKind.EventAdd ||
                 method.MethodKind == MethodKind.EventRaise ||
                 method.MethodKind == MethodKind.EventRemove);
        }

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

        public static ITypeSymbol GetSymbolType(this ISymbol symbol)
        {
            var localSymbol = symbol as ILocalSymbol;
            if (localSymbol != null)
            {
                return localSymbol.Type;
            }

            var fieldSymbol = symbol as IFieldSymbol;
            if (fieldSymbol != null)
            {
                return fieldSymbol.Type;
            }

            var propertySymbol = symbol as IPropertySymbol;
            if (propertySymbol != null)
            {
                return propertySymbol.Type;
            }

            var parameterSymbol = symbol as IParameterSymbol;
            if (parameterSymbol != null)
            {
                return parameterSymbol.Type;
            }

            var aliasSymbol = symbol as IAliasSymbol;
            if (aliasSymbol != null)
            {
                return aliasSymbol.Target as ITypeSymbol;
            }

            return symbol as ITypeSymbol;
        }

        public static DocumentationComment GetDocumentationComment(this ISymbol symbol, CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            string xmlText = symbol.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
            return string.IsNullOrEmpty(xmlText) ? DocumentationComment.Empty : DocumentationComment.FromXmlFragment(xmlText);
        }

        /// <summary>
        /// If the <paramref name="symbol"/> is a method symbol, returns True if the method's return type is "awaitable".
        /// If the <paramref name="symbol"/> is a type symbol, returns True if that type is "awaitable".
        /// An "awaitable" is any type that exposes a GetAwaiter method which returns a valid "awaiter". This GetAwaiter method may be an instance method or an extension method.
        /// </summary>
        public static bool IsAwaitable(this ISymbol symbol, SemanticModel semanticModel, int position)
        {
            IMethodSymbol methodSymbol = symbol as IMethodSymbol;
            ITypeSymbol typeSymbol = null;

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

                // dynamic
                if (methodSymbol.ReturnType.TypeKind == TypeKind.Dynamic &&
                    methodSymbol.MethodKind != MethodKind.BuiltinOperator)
                {
                    return true;
                }
            }

            // otherwise: needs valid GetAwaiter
            var potentialGetAwaiters = semanticModel.LookupSymbols(position,
                                                                   container: typeSymbol ?? methodSymbol.ReturnType.OriginalDefinition,
                                                                   name: WellKnownMemberNames.GetAwaiter,
                                                                   includeReducedExtensionMethods: true);
            var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where(x => !x.Parameters.Any());
            return getAwaiters.Any(VerifyGetAwaiter);
        }

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

        public static IList<SymbolDisplayPart> ToAwaitableParts(this ISymbol symbol, string awaitKeyword, string initializedVariableName, SemanticModel semanticModel, int position)
        {
            var spacePart = new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");
            var parts = new List<SymbolDisplayPart>();

            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Text, null, $"\r\n{WorkspacesResources.Usage}\r\n  "));

            var returnType = symbol.InferAwaitableReturnType(semanticModel, position);
            returnType = returnType != null && returnType.SpecialType != SpecialType.System_Void ? returnType : null;
            if (returnType != null)
            {
                if (semanticModel.Language == "C#")
                {
                    parts.AddRange(returnType.ToMinimalDisplayParts(semanticModel, position));
                    parts.Add(spacePart);
                    parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LocalName, null, initializedVariableName));
                }
                else
                {
                    parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, "Dim"));
                    parts.Add(spacePart);
                    parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LocalName, null, initializedVariableName));
                    parts.Add(spacePart);
                    parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, "as"));
                    parts.Add(spacePart);
                    parts.AddRange(returnType.ToMinimalDisplayParts(semanticModel, position));
                }

                parts.Add(spacePart);
                parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "="));
                parts.Add(spacePart);
            }

            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, awaitKeyword));
            parts.Add(spacePart);
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "("));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, symbol.GetParameters().Any() ? "..." : ""));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ")"));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, semanticModel.Language == "C#" ? ";" : ""));

            return parts;
        }

        public static ITypeSymbol InferAwaitableReturnType(this ISymbol symbol, SemanticModel semanticModel, int position)
        {
            var methodSymbol = symbol as IMethodSymbol;
            if (methodSymbol == null)
            {
                return null;
            }

            var returnType = methodSymbol.ReturnType;
            if (returnType == null)
            {
                return null;
            }

            var potentialGetAwaiters = semanticModel.LookupSymbols(position, container: returnType, name: WellKnownMemberNames.GetAwaiter, includeReducedExtensionMethods: true);
            var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where(x => !x.Parameters.Any());
            if (!getAwaiters.Any())
            {
                return null;
            }

            var getResults = getAwaiters.SelectMany(g => semanticModel.LookupSymbols(position, container: g.ReturnType, name: WellKnownMemberNames.GetResult));

            var getResult = getResults.OfType<IMethodSymbol>().FirstOrDefault(g => !g.IsStatic);
            if (getResult == null)
            {
                return null;
            }

            return getResult.ReturnType;
        }

        /// <summary>
        /// First, remove symbols from the set if they are overridden by other symbols in the set.
        /// If a symbol is overridden only by symbols outside of the set, then it is not removed. 
        /// This is useful for filtering out symbols that cannot be accessed in a given context due
        /// to the existence of overriding members. Second, remove remaining symbols that are
        /// unsupported (e.g. pointer types in VB) or not editor browsable based on the EditorBrowsable
        /// attribute.
        /// </summary>
        public static IEnumerable<T> FilterToVisibleAndBrowsableSymbols<T>(this IEnumerable<T> symbols, bool hideAdvancedMembers, Compilation compilation) where T : ISymbol
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
            Func<Location, bool> isSymbolDefinedInSource = l => l.IsInSource;
            return symbols.Where(s =>
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

        private static IEnumerable<T> RemoveOverriddenSymbolsWithinSet<T>(this IEnumerable<T> symbols) where T : ISymbol
        {
            HashSet<ISymbol> overriddenSymbols = new HashSet<ISymbol>();

            foreach (var symbol in symbols)
            {
                if (symbol.OverriddenMember() != null && !overriddenSymbols.Contains(symbol.OverriddenMember()))
                {
                    overriddenSymbols.Add(symbol.OverriddenMember());
                }
            }

            return symbols.Where(s => !overriddenSymbols.Contains(s));
        }

        public static IEnumerable<T> FilterToVisibleAndBrowsableSymbolsAndNotUnsafeSymbols<T>(this IEnumerable<T> symbols, bool hideAdvancedMembers, Compilation compilation) where T : ISymbol
        {
            return symbols.FilterToVisibleAndBrowsableSymbols(hideAdvancedMembers, compilation).Where(s => !s.IsUnsafe());
        }
    }
}
