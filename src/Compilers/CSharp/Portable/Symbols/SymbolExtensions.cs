// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
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
        public static NamedTypeSymbol ConstructIfGeneric(this NamedTypeSymbol type, ImmutableArray<TypeWithModifiers> typeArguments)
        {
            Debug.Assert(type.TypeParameters.IsEmpty == (typeArguments.Length == 0));
            return type.TypeParameters.IsEmpty ? type : type.Construct(typeArguments, unbound: false);
        }

        public static bool IsNestedType(this Symbol symbol)
        {
            return symbol is NamedTypeSymbol && (object)symbol.ContainingType != null;
        }

        public static bool Any<T>(this ImmutableArray<T> array, SymbolKind kind)
            where T : Symbol
        {
            for (int i = 0, n = array.Length; i < n; i++)
            {
                var item = array[i];
                if ((object)item != null && item.Kind == kind)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns true if the members of superType are accessible from subType due to inheritance.
        /// </summary>
        public static bool IsAccessibleViaInheritance(this TypeSymbol superType, TypeSymbol subType, ref HashSet<DiagnosticInfo> useSiteDiagnostics)
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
            var originalSuperType = superType.OriginalDefinition;
            for (TypeSymbol current = subType;
                (object)current != null;
                current = (current.Kind == SymbolKind.TypeParameter) ? ((TypeParameterSymbol)current).EffectiveBaseClass(ref useSiteDiagnostics) : current.BaseTypeWithDefinitionUseSiteDiagnostics(ref useSiteDiagnostics))
            {
                if (ReferenceEquals(current.OriginalDefinition, originalSuperType))
                {
                    return true;
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

        internal static TDestination EnsureCSharpSymbolOrNull<TSource, TDestination>(this TSource symbol, string paramName)
            where TSource : ISymbol
            where TDestination : Symbol, TSource
        {
            var csSymbol = symbol as TDestination;

            if ((object)csSymbol == null && (object)symbol != null)
            {
                throw new ArgumentException(CSharpResources.NotACSharpSymbol, paramName);
            }

            return csSymbol;
        }

        internal struct ReplacedMemberEnumerable
        {
            private readonly Symbol _symbol;

            internal ReplacedMemberEnumerable(Symbol symbol)
            {
                _symbol = symbol;
            }

            public ReplacedMemberEnumerator GetEnumerator()
            {
                return new ReplacedMemberEnumerator(_symbol);
            }
        }

        internal struct ReplacedMemberEnumerator
        {
            private Symbol _symbol;

            internal ReplacedMemberEnumerator(Symbol symbol)
            {
                _symbol = symbol;
            }

            public bool MoveNext()
            {
                _symbol = _symbol.Replaced;
                return (object)_symbol != null;
            }

            public Symbol Current
            {
                get { return _symbol; }
            }
        }

        internal static ReplacedMemberEnumerable GetReplacedMembers(this Symbol symbol)
        {
            return new ReplacedMemberEnumerable(symbol);
        }

        internal static ImmutableArray<TypeSymbol> ToTypes(this ImmutableArray<TypeWithModifiers> typesWithModifiers, out bool hasModifiers)
        {
            hasModifiers = typesWithModifiers.Any(a => !a.CustomModifiers.IsDefaultOrEmpty);
            return typesWithModifiers.SelectAsArray(a => a.Type);
        }
    }
}
