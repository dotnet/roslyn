// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

using static System.Linq.ImmutableArrayExtensions;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal static partial class SymbolExtensions
    {
        /// <summary>
        /// Does the compilation this symbol belongs to output to a winmdobj?
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public static bool IsCompilationOutputWinMdObj(this Symbol symbol)
        {
            var comp = symbol.DeclaringCompilation;
            return comp != null && comp.Options.OutputKind == OutputKind.WindowsRuntimeMetadata;
        }

        /// <summary>
        /// Returns a constructed named type symbol if 'type' is generic, otherwise just returns 'type'
        /// </summary>
        public static NamedTypeSymbol ConstructIfGeneric(this NamedTypeSymbol type, ImmutableArray<TypeWithAnnotations> typeArguments)
        {
            Debug.Assert(type.TypeParameters.IsEmpty == (typeArguments.Length == 0));
            return type.TypeParameters.IsEmpty ? type : type.Construct(typeArguments, unbound: false);
        }

        public static bool IsNestedType(this Symbol symbol)
        {
            return symbol is NamedTypeSymbol && (object)symbol.ContainingType != null;
        }

        /// <summary>
        /// Returns true if the members of superType are accessible from subType due to inheritance.
        /// </summary>
        public static bool IsAccessibleViaInheritance(this NamedTypeSymbol superType, NamedTypeSymbol subType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            // NOTE: we don't use strict inheritance.  Instead we ignore constructed generic types
            // and only consider the unconstructed types.  Ecma-334, 4th edition contained the
            // following text supporting this (although, for instance members) in 10.5.3 Protected
            // access for instance members:
            //    In the context of generics (25.1.6), the rules for accessing protected and
            //    protected internal instance members are augmented by the following:
            //    o  Within a generic class G, access to an inherited protected instance member M
            //       using a primary-expression of the form E.M is permitted if the type of E is a
            //       class type constructed from G or a class type derived from a class type
            //       constructed from G.
            // This text is missing in the current version of the spec, but we believe this is accidental.
            NamedTypeSymbol originalSuperType = superType.OriginalDefinition;
            for (NamedTypeSymbol current = subType;
                (object)current != null;
                current = current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (ReferenceEquals(current.OriginalDefinition, originalSuperType))
                {
                    return true;
                }
            }

            if (originalSuperType.IsInterface)
            {
                foreach (NamedTypeSymbol current in subType.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
                {
                    if (ReferenceEquals(current.OriginalDefinition, originalSuperType))
                    {
                        return true;
                    }
                }
            }

            // The method returns true for superType == subType.
            // Two different submission type symbols semantically represent a single type, so we should also return true.
            return superType.TypeKind == TypeKind.Submission && subType.TypeKind == TypeKind.Submission;
        }

        public static bool IsNoMoreVisibleThan(this Symbol symbol, TypeSymbol type, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return type.IsAtLeastAsVisibleAs(symbol, ref useSiteDiagnostics);
        }

        public static bool IsNoMoreVisibleThan(this Symbol symbol, TypeWithAnnotations type, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
        {
            return type.IsAtLeastAsVisibleAs(symbol, ref useSiteDiagnostics);
        }

        public static LocalizableErrorArgument GetKindText(this Symbol symbol)
        {
            return symbol.Kind.Localize();
        }

        /// <summary>
        /// The immediately containing namespace or named type, or null
        /// if the containing symbol is neither a namespace or named type.
        /// </summary>
        internal static NamespaceOrTypeSymbol ContainingNamespaceOrType(this Symbol symbol)
        {
            var containingSymbol = symbol.ContainingSymbol;
            if ((object)containingSymbol != null)
            {
                switch (containingSymbol.Kind)
                {
                    case SymbolKind.Namespace:
                    case SymbolKind.NamedType:
                    case SymbolKind.ErrorType:
                        return (NamespaceOrTypeSymbol)containingSymbol;
                }
            }
            return null;
        }

        internal static Symbol ContainingNonLambdaMember(this Symbol containingMember)
        {
            while ((object)containingMember != null && containingMember.Kind == SymbolKind.Method)
            {
                MethodSymbol method = (MethodSymbol)containingMember;
                if (method.MethodKind != MethodKind.AnonymousFunction && method.MethodKind != MethodKind.LocalFunction) break;
                containingMember = containingMember.ContainingSymbol;
            }

            return containingMember;
        }

        internal static ParameterSymbol EnclosingThisSymbol(this Symbol containingMember)
        {
            Symbol symbol = containingMember;
            while (true)
            {
                NamedTypeSymbol type;

                switch (symbol.Kind)
                {
                    case SymbolKind.Method:
                        MethodSymbol method = (MethodSymbol)symbol;

                        // skip lambdas:
                        if (method.MethodKind == MethodKind.AnonymousFunction || method.MethodKind == MethodKind.LocalFunction)
                        {
                            symbol = method.ContainingSymbol;
                            continue;
                        }

                        return method.ThisParameter;

                    case SymbolKind.Field:
                        // "this" in field initializer:
                        type = symbol.ContainingType;
                        break;

                    case SymbolKind.NamedType:
                        // "this" in global statement:
                        type = (NamedTypeSymbol)symbol;
                        break;

                    default:
                        return null;
                }

                // "this" can be accessed in a lambda in a field initializer if the initializer is 
                // a script field initializer or global statement because these are initialized 
                // after the call to the base constructor.
                return type.IsScriptClass ? type.InstanceConstructors.Single().ThisParameter : null;
            }
        }

        public static Symbol ConstructedFrom(this Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.NamedType:
                    return ((NamedTypeSymbol)symbol).ConstructedFrom;

                case SymbolKind.Method:
                    return ((MethodSymbol)symbol).ConstructedFrom;

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        /// <summary>
        /// Returns true if all type parameter references within the given
        /// type belong to containingSymbol or its containing types.
        /// </summary>
        public static bool IsContainingSymbolOfAllTypeParameters(this Symbol containingSymbol, TypeSymbol type)
        {
            return (object)type.VisitType(s_hasInvalidTypeParameterFunc, containingSymbol) == null;
        }

        /// <summary>
        /// Returns true if all type parameter references within the given
        /// types belong to containingSymbol or its containing types.
        /// </summary>
        public static bool IsContainingSymbolOfAllTypeParameters(this Symbol containingSymbol, ImmutableArray<TypeSymbol> types)
        {
            return types.All(containingSymbol.IsContainingSymbolOfAllTypeParameters);
        }

        private static readonly Func<TypeSymbol, Symbol, bool, bool> s_hasInvalidTypeParameterFunc = (type, containingSymbol, unused) => HasInvalidTypeParameter(type, containingSymbol);

        private static bool HasInvalidTypeParameter(TypeSymbol type, Symbol containingSymbol)
        {
            if (type.TypeKind == TypeKind.TypeParameter)
            {
                var symbol = type.ContainingSymbol;
                for (; ((object)containingSymbol != null) && (containingSymbol.Kind != SymbolKind.Namespace); containingSymbol = containingSymbol.ContainingSymbol)
                {
                    if (containingSymbol == symbol)
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        public static bool IsTypeOrTypeAlias(this Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.ArrayType:
                case SymbolKind.DynamicType:
                case SymbolKind.ErrorType:
                case SymbolKind.NamedType:
                case SymbolKind.PointerType:
                case SymbolKind.TypeParameter:
                    return true;
                case SymbolKind.Alias:
                    return IsTypeOrTypeAlias(((AliasSymbol)symbol).Target);
                default:
                    Debug.Assert(!(symbol is TypeSymbol));
                    return false;
            }
        }

        internal static bool CompilationAllowsUnsafe(this Symbol symbol)
        {
            return symbol.DeclaringCompilation.Options.AllowUnsafe;
        }

        internal static void CheckUnsafeModifier(this Symbol symbol, DeclarationModifiers modifiers, DiagnosticBag diagnostics)
        {
            symbol.CheckUnsafeModifier(modifiers, symbol.Locations[0], diagnostics);
        }

        internal static void CheckUnsafeModifier(this Symbol symbol, DeclarationModifiers modifiers, Location errorLocation, DiagnosticBag diagnostics)
        {
            if (((modifiers & DeclarationModifiers.Unsafe) == DeclarationModifiers.Unsafe) && !symbol.CompilationAllowsUnsafe())
            {
                Debug.Assert(errorLocation != null);
                diagnostics.Add(ErrorCode.ERR_IllegalUnsafe, errorLocation);
            }
        }

        /// <summary>
        /// Does the top level type containing this symbol have 'Microsoft.CodeAnalysis.Embedded' attribute?
        /// </summary>
        public static bool IsHiddenByCodeAnalysisEmbeddedAttribute(this Symbol symbol)
        {
            // Only upper-level types should be checked 
            var upperLevelType = symbol.Kind == SymbolKind.NamedType ? (NamedTypeSymbol)symbol : symbol.ContainingType;
            if ((object)upperLevelType == null)
            {
                return false;
            }

            while ((object)upperLevelType.ContainingType != null)
            {
                upperLevelType = upperLevelType.ContainingType;
            }

            return upperLevelType.HasCodeAnalysisEmbeddedAttribute;
        }

        public static bool MustCallMethodsDirectly(this Symbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Property:
                    return ((PropertySymbol)symbol).MustCallMethodsDirectly;
                case SymbolKind.Event:
                    return ((EventSymbol)symbol).MustCallMethodsDirectly;
                default:
                    return false;
            }
        }

        public static int GetArity(this Symbol symbol)
        {
            if ((object)symbol != null)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.NamedType:
                        return ((NamedTypeSymbol)symbol).Arity;
                    case SymbolKind.Method:
                        return ((MethodSymbol)symbol).Arity;
                }
            }

            return 0;
        }

        internal static CSharpSyntaxNode GetNonNullSyntaxNode(this Symbol symbol)
        {
            if ((object)symbol != null)
            {
                SyntaxReference reference = symbol.DeclaringSyntaxReferences.FirstOrDefault();

                if (reference == null && symbol.IsImplicitlyDeclared)
                {
                    Symbol containingSymbol = symbol.ContainingSymbol;
                    if ((object)containingSymbol != null)
                    {
                        reference = containingSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                    }
                }

                if (reference != null)
                {
                    return (CSharpSyntaxNode)reference.GetSyntax();
                }
            }

            return (CSharpSyntaxNode)CSharpSyntaxTree.Dummy.GetRoot();
        }

        internal static Symbol EnsureCSharpSymbolOrNull(this ISymbol symbol, string paramName)
        {
            var csSymbol = symbol as Symbols.PublicModel.Symbol;

            if (csSymbol is null)
            {
                if (symbol is object)
                {
                    throw new ArgumentException(CSharpResources.NotACSharpSymbol, paramName);
                }

                return null;
            }

            return csSymbol.UnderlyingSymbol;
        }

        internal static AssemblySymbol EnsureCSharpSymbolOrNull(this IAssemblySymbol symbol, string paramName)
        {
            return (AssemblySymbol)EnsureCSharpSymbolOrNull((ISymbol)symbol, paramName);
        }

        internal static NamespaceOrTypeSymbol EnsureCSharpSymbolOrNull(this INamespaceOrTypeSymbol symbol, string paramName)
        {
            return (NamespaceOrTypeSymbol)EnsureCSharpSymbolOrNull((ISymbol)symbol, paramName);
        }

        internal static NamespaceSymbol EnsureCSharpSymbolOrNull(this INamespaceSymbol symbol, string paramName)
        {
            return (NamespaceSymbol)EnsureCSharpSymbolOrNull((ISymbol)symbol, paramName);
        }

        internal static TypeSymbol EnsureCSharpSymbolOrNull(this ITypeSymbol symbol, string paramName)
        {
            return (TypeSymbol)EnsureCSharpSymbolOrNull((ISymbol)symbol, paramName);
        }

        internal static NamedTypeSymbol EnsureCSharpSymbolOrNull(this INamedTypeSymbol symbol, string paramName)
        {
            return (NamedTypeSymbol)EnsureCSharpSymbolOrNull((ISymbol)symbol, paramName);
        }

        internal static TypeParameterSymbol EnsureCSharpSymbolOrNull(this ITypeParameterSymbol symbol, string paramName)
        {
            return (TypeParameterSymbol)EnsureCSharpSymbolOrNull((ISymbol)symbol, paramName);
        }

        internal static EventSymbol EnsureCSharpSymbolOrNull(this IEventSymbol symbol, string paramName)
        {
            return (EventSymbol)EnsureCSharpSymbolOrNull((ISymbol)symbol, paramName);
        }

        internal static TypeWithAnnotations GetTypeOrReturnType(this Symbol symbol)
        {
            RefKind refKind;
            TypeWithAnnotations returnType;
            ImmutableArray<CustomModifier> customModifiers_Ignored;
            GetTypeOrReturnType(symbol, out refKind, out returnType, out customModifiers_Ignored);
            return returnType;
        }

        internal static void GetTypeOrReturnType(this Symbol symbol, out RefKind refKind, out TypeWithAnnotations returnType,
                                                 out ImmutableArray<CustomModifier> refCustomModifiers)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Field:
                    FieldSymbol field = (FieldSymbol)symbol;
                    refKind = RefKind.None;
                    returnType = field.TypeWithAnnotations;
                    refCustomModifiers = ImmutableArray<CustomModifier>.Empty;
                    break;
                case SymbolKind.Method:
                    MethodSymbol method = (MethodSymbol)symbol;
                    refKind = method.RefKind;
                    returnType = method.ReturnTypeWithAnnotations;
                    refCustomModifiers = method.RefCustomModifiers;
                    break;
                case SymbolKind.Property:
                    PropertySymbol property = (PropertySymbol)symbol;
                    refKind = property.RefKind;
                    returnType = property.TypeWithAnnotations;
                    refCustomModifiers = property.RefCustomModifiers;
                    break;
                case SymbolKind.Event:
                    EventSymbol @event = (EventSymbol)symbol;
                    refKind = RefKind.None;
                    returnType = @event.TypeWithAnnotations;
                    refCustomModifiers = ImmutableArray<CustomModifier>.Empty;
                    break;
                case SymbolKind.Local:
                    LocalSymbol local = (LocalSymbol)symbol;
                    refKind = local.RefKind;
                    returnType = local.TypeWithAnnotations;
                    refCustomModifiers = ImmutableArray<CustomModifier>.Empty;
                    break;
                case SymbolKind.Parameter:
                    ParameterSymbol parameter = (ParameterSymbol)symbol;
                    refKind = parameter.RefKind;
                    returnType = parameter.TypeWithAnnotations;
                    refCustomModifiers = parameter.RefCustomModifiers;
                    break;
                case SymbolKind.ErrorType:
                    refKind = RefKind.None;
                    returnType = TypeWithAnnotations.Create((TypeSymbol)symbol);
                    refCustomModifiers = ImmutableArray<CustomModifier>.Empty;
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }
        }

        internal static bool IsImplementableInterfaceMember(this Symbol symbol)
        {
            return !symbol.IsStatic && !symbol.IsSealed && (symbol.IsAbstract || symbol.IsVirtual) && (symbol.ContainingType?.IsInterface ?? false);
        }

        internal static bool RequiresInstanceReceiver(this Symbol symbol)
        {
            return symbol.Kind switch
            {
                SymbolKind.Method => ((MethodSymbol)symbol).RequiresInstanceReceiver,
                SymbolKind.Property => ((PropertySymbol)symbol).RequiresInstanceReceiver,
                SymbolKind.Field => ((FieldSymbol)symbol).RequiresInstanceReceiver,
                SymbolKind.Event => ((EventSymbol)symbol).RequiresInstanceReceiver,
                _ => throw new ArgumentException("only methods, properties, fields and events can take a receiver", nameof(symbol)),
            };
        }

        private static TISymbol GetPublicSymbol<TISymbol>(this Symbol symbolOpt) where TISymbol : ISymbol
        {
            return (TISymbol)symbolOpt?.ISymbol;
        }

        internal static ISymbol GetPublicSymbol(this Symbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<ISymbol>();
        }

        internal static IMethodSymbol GetPublicSymbol(this MethodSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IMethodSymbol>();
        }

        internal static IPropertySymbol GetPublicSymbol(this PropertySymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IPropertySymbol>();
        }

        internal static INamedTypeSymbol GetPublicSymbol(this NamedTypeSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<INamedTypeSymbol>();
        }

        internal static INamespaceSymbol GetPublicSymbol(this NamespaceSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<INamespaceSymbol>();
        }

        internal static ITypeSymbol GetPublicSymbol(this TypeSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<ITypeSymbol>();
        }

        internal static ILocalSymbol GetPublicSymbol(this LocalSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<ILocalSymbol>();
        }

        internal static IAssemblySymbol GetPublicSymbol(this AssemblySymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IAssemblySymbol>();
        }

        internal static INamespaceOrTypeSymbol GetPublicSymbol(this NamespaceOrTypeSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<INamespaceOrTypeSymbol>();
        }

        internal static IDiscardSymbol GetPublicSymbol(this DiscardSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IDiscardSymbol>();
        }

        internal static IFieldSymbol GetPublicSymbol(this FieldSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IFieldSymbol>();
        }

        internal static IParameterSymbol GetPublicSymbol(this ParameterSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IParameterSymbol>();
        }

        internal static IRangeVariableSymbol GetPublicSymbol(this RangeVariableSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IRangeVariableSymbol>();
        }

        internal static ILabelSymbol GetPublicSymbol(this LabelSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<ILabelSymbol>();
        }

        internal static IAliasSymbol GetPublicSymbol(this AliasSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IAliasSymbol>();
        }

        internal static IModuleSymbol GetPublicSymbol(this ModuleSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IModuleSymbol>();
        }

        internal static ITypeParameterSymbol GetPublicSymbol(this TypeParameterSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<ITypeParameterSymbol>();
        }

        internal static IArrayTypeSymbol GetPublicSymbol(this ArrayTypeSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IArrayTypeSymbol>();
        }

        internal static IPointerTypeSymbol GetPublicSymbol(this PointerTypeSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IPointerTypeSymbol>();
        }

        internal static IEventSymbol GetPublicSymbol(this EventSymbol symbolOpt)
        {
            return symbolOpt.GetPublicSymbol<IEventSymbol>();
        }

        internal static IEnumerable<ISymbol> GetPublicSymbols(this IEnumerable<Symbol> symbols)
        {
            return symbols.Select(p => p.GetPublicSymbol<ISymbol>());
        }

        private static ImmutableArray<TISymbol> GetPublicSymbols<TISymbol>(this ImmutableArray<Symbol> symbols) where TISymbol : ISymbol
        {
            if (symbols.IsDefault)
            {
                return default;
            }

            return symbols.SelectAsArray(p => p.GetPublicSymbol<TISymbol>());
        }

        internal static ImmutableArray<ISymbol> GetPublicSymbols(this ImmutableArray<Symbol> symbols)
        {
            return GetPublicSymbols<ISymbol>(symbols);
        }

        internal static ImmutableArray<IPropertySymbol> GetPublicSymbols(this ImmutableArray<PropertySymbol> symbols)
        {
            return GetPublicSymbols<IPropertySymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<ITypeSymbol> GetPublicSymbols(this ImmutableArray<TypeSymbol> symbols)
        {
            return GetPublicSymbols<ITypeSymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<INamedTypeSymbol> GetPublicSymbols(this ImmutableArray<NamedTypeSymbol> symbols)
        {
            return GetPublicSymbols<INamedTypeSymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<ILocalSymbol> GetPublicSymbols(this ImmutableArray<LocalSymbol> symbols)
        {
            return GetPublicSymbols<ILocalSymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<IEventSymbol> GetPublicSymbols(this ImmutableArray<EventSymbol> symbols)
        {
            return GetPublicSymbols<IEventSymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<ITypeParameterSymbol> GetPublicSymbols(this ImmutableArray<TypeParameterSymbol> symbols)
        {
            return GetPublicSymbols<ITypeParameterSymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<IParameterSymbol> GetPublicSymbols(this ImmutableArray<ParameterSymbol> symbols)
        {
            return GetPublicSymbols<IParameterSymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<IMethodSymbol> GetPublicSymbols(this ImmutableArray<MethodSymbol> symbols)
        {
            return GetPublicSymbols<IMethodSymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<IAssemblySymbol> GetPublicSymbols(this ImmutableArray<AssemblySymbol> symbols)
        {
            return GetPublicSymbols<IAssemblySymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<IFieldSymbol> GetPublicSymbols(this ImmutableArray<FieldSymbol> symbols)
        {
            return GetPublicSymbols<IFieldSymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static ImmutableArray<INamespaceSymbol> GetPublicSymbols(this ImmutableArray<NamespaceSymbol> symbols)
        {
            return GetPublicSymbols<INamespaceSymbol>(StaticCast<Symbol>.From(symbols));
        }

        internal static TSymbol GetSymbol<TSymbol>(this ISymbol symbolOpt) where TSymbol : Symbol
        {
            return (TSymbol)((PublicModel.Symbol)symbolOpt)?.UnderlyingSymbol;
        }

        internal static Symbol GetSymbol(this ISymbol symbolOpt)
        {
            return symbolOpt.GetSymbol<Symbol>();
        }

        internal static TypeSymbol GetSymbol(this ITypeSymbol symbolOpt)
        {
            return symbolOpt.GetSymbol<TypeSymbol>();
        }

        internal static NamedTypeSymbol GetSymbol(this INamedTypeSymbol symbolOpt)
        {
            return symbolOpt.GetSymbol<NamedTypeSymbol>();
        }

        internal static AliasSymbol GetSymbol(this IAliasSymbol symbolOpt)
        {
            return symbolOpt.GetSymbol<AliasSymbol>();
        }

        internal static LocalSymbol GetSymbol(this ILocalSymbol symbolOpt)
        {
            return symbolOpt.GetSymbol<LocalSymbol>();
        }

        internal static AssemblySymbol GetSymbol(this IAssemblySymbol symbolOpt)
        {
            return symbolOpt.GetSymbol<AssemblySymbol>();
        }

        internal static MethodSymbol GetSymbol(this IMethodSymbol symbolOpt)
        {
            return symbolOpt.GetSymbol<MethodSymbol>();
        }

        internal static PropertySymbol GetSymbol(this IPropertySymbol symbolOpt)
        {
            return symbolOpt.GetSymbol<PropertySymbol>();
        }
    }
}
