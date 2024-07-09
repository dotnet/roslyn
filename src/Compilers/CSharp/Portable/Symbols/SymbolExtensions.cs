// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

        public static bool IsNestedType([NotNullWhen(true)] this Symbol? symbol)
        {
            return symbol is NamedTypeSymbol && (object?)symbol.ContainingType != null;
        }

        /// <summary>
        /// Returns true if the members of superType are accessible from subType due to inheritance.
        /// </summary>
        public static bool IsAccessibleViaInheritance(this NamedTypeSymbol superType, NamedTypeSymbol subType, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
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
            for (NamedTypeSymbol? current = subType;
                (object?)current != null;
                current = current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteInfo))
            {
                if (ReferenceEquals(current.OriginalDefinition, originalSuperType))
                {
                    return true;
                }
            }

            if (originalSuperType.IsInterface)
            {
                foreach (NamedTypeSymbol current in subType.AllInterfacesWithDefinitionUseSiteDiagnostics(ref useSiteInfo))
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

        public static bool IsNoMoreVisibleThan(this Symbol symbol, TypeSymbol type, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return type.IsAtLeastAsVisibleAs(symbol, ref useSiteInfo);
        }

        public static bool IsNoMoreVisibleThan(this Symbol symbol, TypeWithAnnotations type, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo)
        {
            return type.IsAtLeastAsVisibleAs(symbol, ref useSiteInfo);
        }

        internal static void AddUseSiteInfo(this Symbol? symbol, ref CompoundUseSiteInfo<AssemblySymbol> useSiteInfo, bool addDiagnostics = true)
        {
            if (symbol is null)
            {
                return;
            }

            if (!useSiteInfo.AccumulatesDiagnostics)
            {
                Debug.Assert(!useSiteInfo.AccumulatesDependencies);
                return;
            }

            var info = symbol.GetUseSiteInfo();

            if (addDiagnostics)
            {
                useSiteInfo.AddDiagnostics(info);
            }

            useSiteInfo.AddDependencies(info);
        }

        public static LocalizableErrorArgument GetKindText(this Symbol symbol)
        {
            return symbol.Kind.Localize();
        }

        /// <summary>
        /// The immediately containing namespace or named type, or null
        /// if the containing symbol is neither a namespace or named type.
        /// </summary>
        internal static NamespaceOrTypeSymbol? ContainingNamespaceOrType(this Symbol symbol)
        {
            var containingSymbol = symbol.ContainingSymbol;
            if ((object?)containingSymbol != null)
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

        internal static Symbol? ContainingNonLambdaMember(this Symbol? containingMember)
        {
            while (containingMember is object && containingMember.Kind == SymbolKind.Method)
            {
                var method = (MethodSymbol)containingMember;
                if (method.MethodKind != MethodKind.AnonymousFunction && method.MethodKind != MethodKind.LocalFunction) break;
                containingMember = containingMember.ContainingSymbol;
            }

            return containingMember;
        }

        internal static ParameterSymbol? EnclosingThisSymbol(this Symbol containingMember)
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
                case SymbolKind.ErrorType:
                    return ((NamedTypeSymbol)symbol).ConstructedFrom;

                case SymbolKind.Method:
                    return ((MethodSymbol)symbol).ConstructedFrom;

                default:
                    return symbol;
            }
        }

        public static bool IsSourceParameterWithEnumeratorCancellationAttribute(this ParameterSymbol parameter)
        {
            switch (parameter)
            {
                case SourceComplexParameterSymbolBase source:
                    return source.HasEnumeratorCancellationAttribute;
                case SynthesizedComplexParameterSymbol synthesized:
                    return synthesized.HasEnumeratorCancellationAttribute;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns true if all type parameter references within the given
        /// type belong to containingSymbol or its containing types.
        /// </summary>
        public static bool IsContainingSymbolOfAllTypeParameters(this Symbol containingSymbol, TypeSymbol type)
        {
            return type.VisitType(s_hasInvalidTypeParameterFunc, containingSymbol) is null;
        }

        /// <summary>
        /// Returns true if all type parameter references within the given
        /// types belong to containingSymbol or its containing types.
        /// </summary>
        public static bool IsContainingSymbolOfAllTypeParameters(this Symbol containingSymbol, ImmutableArray<TypeSymbol> types)
        {
            return types.All(containingSymbol.IsContainingSymbolOfAllTypeParameters);
        }

        private static readonly Func<TypeSymbol, Symbol, bool, bool> s_hasInvalidTypeParameterFunc =
            (type, containingSymbol, unused) => HasInvalidTypeParameter(type, containingSymbol);

        private static bool HasInvalidTypeParameter(TypeSymbol type, Symbol? containingSymbol)
        {
            if (type.TypeKind == TypeKind.TypeParameter)
            {
                var symbol = type.ContainingSymbol;
                for (; ((object?)containingSymbol != null) && (containingSymbol.Kind != SymbolKind.Namespace); containingSymbol = containingSymbol.ContainingSymbol)
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
                case SymbolKind.FunctionPointerType:
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

        internal static void CheckUnsafeModifier(this Symbol symbol, DeclarationModifiers modifiers, BindingDiagnosticBag diagnostics)
        {
            symbol.CheckUnsafeModifier(modifiers, symbol.GetFirstLocation(), diagnostics);
        }

        internal static void CheckUnsafeModifier(this Symbol symbol, DeclarationModifiers modifiers, Location errorLocation, BindingDiagnosticBag diagnostics)
            => CheckUnsafeModifier(symbol, modifiers, errorLocation, diagnostics.DiagnosticBag);

        internal static void CheckUnsafeModifier(this Symbol symbol, DeclarationModifiers modifiers, Location errorLocation, DiagnosticBag? diagnostics)
        {
            if (diagnostics != null &&
                (modifiers & DeclarationModifiers.Unsafe) == DeclarationModifiers.Unsafe &&
                !symbol.CompilationAllowsUnsafe())
            {
                RoslynDebug.Assert(errorLocation != null);
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
            if ((object?)upperLevelType == null)
            {
                return false;
            }

            while ((object?)upperLevelType.ContainingType != null)
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

        public static int GetArity(this Symbol? symbol)
        {
            if (symbol is object)
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

        internal static CSharpSyntaxNode GetNonNullSyntaxNode(this Symbol? symbol)
        {
            if (symbol is object)
            {
                SyntaxReference? reference = symbol.DeclaringSyntaxReferences.FirstOrDefault();

                if (reference == null && symbol.IsImplicitlyDeclared)
                {
                    Symbol? containingSymbol = symbol.ContainingSymbol;
                    if ((object?)containingSymbol != null)
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

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static Symbol? EnsureCSharpSymbolOrNull(this ISymbol? symbol, string paramName)
        {
            var csSymbol = symbol as PublicModel.Symbol;

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

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static AssemblySymbol? EnsureCSharpSymbolOrNull(this IAssemblySymbol? symbol, string paramName)
        {
            return (AssemblySymbol?)EnsureCSharpSymbolOrNull((ISymbol?)symbol, paramName);
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static NamespaceOrTypeSymbol? EnsureCSharpSymbolOrNull(this INamespaceOrTypeSymbol? symbol, string paramName)
        {
            return (NamespaceOrTypeSymbol?)EnsureCSharpSymbolOrNull((ISymbol?)symbol, paramName);
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static NamespaceSymbol? EnsureCSharpSymbolOrNull(this INamespaceSymbol? symbol, string paramName)
        {
            return (NamespaceSymbol?)EnsureCSharpSymbolOrNull((ISymbol?)symbol, paramName);
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static TypeSymbol? EnsureCSharpSymbolOrNull(this ITypeSymbol? symbol, string paramName)
        {
            return (TypeSymbol?)EnsureCSharpSymbolOrNull((ISymbol?)symbol, paramName);
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static NamedTypeSymbol? EnsureCSharpSymbolOrNull(this INamedTypeSymbol? symbol, string paramName)
        {
            return (NamedTypeSymbol?)EnsureCSharpSymbolOrNull((ISymbol?)symbol, paramName);
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static TypeParameterSymbol? EnsureCSharpSymbolOrNull(this ITypeParameterSymbol? symbol, string paramName)
        {
            return (TypeParameterSymbol?)EnsureCSharpSymbolOrNull((ISymbol?)symbol, paramName);
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static EventSymbol? EnsureCSharpSymbolOrNull(this IEventSymbol? symbol, string paramName)
        {
            return (EventSymbol?)EnsureCSharpSymbolOrNull((ISymbol?)symbol, paramName);
        }

        internal static TypeWithAnnotations GetTypeOrReturnType(this Symbol symbol)
        {
            TypeWithAnnotations returnType;
            GetTypeOrReturnType(symbol, refKind: out _, out returnType, refCustomModifiers: out _);
            return returnType;
        }

        internal static FlowAnalysisAnnotations GetFlowAnalysisAnnotations(this PropertySymbol property)
        {
            var annotations = property.GetOwnOrInheritedGetMethod()?.ReturnTypeFlowAnalysisAnnotations ?? FlowAnalysisAnnotations.None;
            if (property.GetOwnOrInheritedSetMethod()?.Parameters.Last().FlowAnalysisAnnotations is { } setterAnnotations)
            {
                annotations |= setterAnnotations;
            }
            else if (property is SourcePropertySymbolBase sourceProperty)
            {
                // When an auto-property without a setter has an AllowNull annotation,
                // we need to search for its flow analysis annotations in a more roundabout way
                // in order to properly handle assignment to the property (e.g. in a constructor).
                if (sourceProperty.HasAllowNull)
                {
                    annotations |= FlowAnalysisAnnotations.AllowNull;
                }
                if (sourceProperty.HasDisallowNull)
                {
                    annotations |= FlowAnalysisAnnotations.DisallowNull;
                }
            }

            return annotations;
        }

        internal static FlowAnalysisAnnotations GetFlowAnalysisAnnotations(this Symbol? symbol)
        {
            return symbol switch
            {
                MethodSymbol method => method.ReturnTypeFlowAnalysisAnnotations,
                PropertySymbol property => property.GetFlowAnalysisAnnotations(),
                ParameterSymbol parameter => parameter.FlowAnalysisAnnotations,
                FieldSymbol field => field.FlowAnalysisAnnotations,
                _ => FlowAnalysisAnnotations.None
            };
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
            return !symbol.IsSealed && (symbol.IsAbstract || symbol.IsVirtual) && (symbol.ContainingType?.IsInterface ?? false);
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

        [return: NotNullIfNotNull(nameof(symbol))]
        private static TISymbol? GetPublicSymbol<TISymbol>(this Symbol? symbol)
            where TISymbol : class, ISymbol
        {
            return (TISymbol?)symbol?.ISymbol;
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static ISymbol? GetPublicSymbol(this Symbol? symbol)
        {
            return symbol.GetPublicSymbol<ISymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IMethodSymbol? GetPublicSymbol(this MethodSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IMethodSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IPropertySymbol? GetPublicSymbol(this PropertySymbol? symbol)
        {
            return symbol.GetPublicSymbol<IPropertySymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static INamedTypeSymbol? GetPublicSymbol(this NamedTypeSymbol? symbol)
        {
            return symbol.GetPublicSymbol<INamedTypeSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static INamespaceSymbol? GetPublicSymbol(this NamespaceSymbol? symbol)
        {
            return symbol.GetPublicSymbol<INamespaceSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static ITypeSymbol? GetPublicSymbol(this TypeSymbol? symbol)
        {
            return symbol.GetPublicSymbol<ITypeSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static ILocalSymbol? GetPublicSymbol(this LocalSymbol? symbol)
        {
            return symbol.GetPublicSymbol<ILocalSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IAssemblySymbol? GetPublicSymbol(this AssemblySymbol? symbol)
        {
            return symbol.GetPublicSymbol<IAssemblySymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static INamespaceOrTypeSymbol? GetPublicSymbol(this NamespaceOrTypeSymbol? symbol)
        {
            return symbol.GetPublicSymbol<INamespaceOrTypeSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IDiscardSymbol? GetPublicSymbol(this DiscardSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IDiscardSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IFieldSymbol? GetPublicSymbol(this FieldSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IFieldSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IParameterSymbol? GetPublicSymbol(this ParameterSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IParameterSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IRangeVariableSymbol? GetPublicSymbol(this RangeVariableSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IRangeVariableSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static ILabelSymbol? GetPublicSymbol(this LabelSymbol? symbol)
        {
            return symbol.GetPublicSymbol<ILabelSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IAliasSymbol? GetPublicSymbol(this AliasSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IAliasSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IModuleSymbol? GetPublicSymbol(this ModuleSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IModuleSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static ITypeParameterSymbol? GetPublicSymbol(this TypeParameterSymbol? symbol)
        {
            return symbol.GetPublicSymbol<ITypeParameterSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IArrayTypeSymbol? GetPublicSymbol(this ArrayTypeSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IArrayTypeSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IPointerTypeSymbol? GetPublicSymbol(this PointerTypeSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IPointerTypeSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IFunctionPointerTypeSymbol? GetPublicSymbol(this FunctionPointerTypeSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IFunctionPointerTypeSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static IEventSymbol? GetPublicSymbol(this EventSymbol? symbol)
        {
            return symbol.GetPublicSymbol<IEventSymbol>();
        }

        internal static IEnumerable<ISymbol?> GetPublicSymbols(this IEnumerable<Symbol?> symbols)
        {
            return symbols.Select(p => p.GetPublicSymbol<ISymbol>());
        }

        private static ImmutableArray<TISymbol> GetPublicSymbols<TISymbol>(this ImmutableArray<Symbol> symbols)
            where TISymbol : class, ISymbol
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

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static TSymbol? GetSymbol<TSymbol>(this ISymbol? symbol)
            where TSymbol : Symbol
        {
            return (TSymbol?)((PublicModel.Symbol?)symbol)?.UnderlyingSymbol;
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static Symbol? GetSymbol(this ISymbol? symbol)
        {
            return symbol.GetSymbol<Symbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static TypeSymbol? GetSymbol(this ITypeSymbol? symbol)
        {
            return symbol.GetSymbol<TypeSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static NamedTypeSymbol? GetSymbol(this INamedTypeSymbol? symbol)
        {
            return symbol.GetSymbol<NamedTypeSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static AliasSymbol? GetSymbol(this IAliasSymbol? symbol)
        {
            return symbol.GetSymbol<AliasSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static LocalSymbol? GetSymbol(this ILocalSymbol? symbol)
        {
            return symbol.GetSymbol<LocalSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static AssemblySymbol? GetSymbol(this IAssemblySymbol? symbol)
        {
            return symbol.GetSymbol<AssemblySymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static MethodSymbol? GetSymbol(this IMethodSymbol? symbol)
        {
            return symbol.GetSymbol<MethodSymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static PropertySymbol? GetSymbol(this IPropertySymbol? symbol)
        {
            return symbol.GetSymbol<PropertySymbol>();
        }

        [return: NotNullIfNotNull(nameof(symbol))]
        internal static FunctionPointerTypeSymbol? GetSymbol(this IFunctionPointerTypeSymbol? symbol)
        {
            return symbol.GetSymbol<FunctionPointerTypeSymbol>();
        }

        internal static bool IsRequired(this Symbol symbol) => symbol is FieldSymbol { IsRequired: true } or PropertySymbol { IsRequired: true };

        internal static bool ShouldCheckRequiredMembers(this MethodSymbol method)
            => method is { MethodKind: MethodKind.Constructor, HasSetsRequiredMembers: false };

        internal static int GetOverloadResolutionPriority(this Symbol symbol)
        {
            Debug.Assert(symbol is MethodSymbol or PropertySymbol);
            return symbol is MethodSymbol method ? method.OverloadResolutionPriority : ((PropertySymbol)symbol).OverloadResolutionPriority;
        }
    }
}
