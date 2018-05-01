// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static partial class ISymbolExtensions
    {
        /// <summary>
        /// Checks if 'symbol' is accessible from within 'within', with
        /// an optional qualifier of type 'throughTypeOpt' to be used to resolve
        /// protected access.
        /// </summary>
        /// <remarks>
        /// The following areas of imprecision may exist in the results of this API:
        /// <para>For assembly identity, we depend on Equals <see cref="IEquatable{T}.Equals(T)"/>/>.
        /// Assembly symbols that represent the same assembly imported into different language compilers
        /// may not compare equal, and this may prevent them from appearing to be related through
        /// this API. See https://github.com/dotnet/roslyn/issues/26542 .</para>
        /// <para>We compare <see cref="INamedTypeSymbol"/> based on the identity of the containing
        /// assembly (see above) and their metadata name, which includes the metadata name of the enclosing
        /// namespaces. Due to the behavior of the VB compiler (https://github.com/dotnet/roslyn/issues/26546)
        /// it merges namespaces when importing an assembly. Consequently, the metadata name of the namespace
        /// may not be correct for some of the contained types, and types that are distinct
        /// may appear to have the same fully-qualified name. In that case this API may treat them as the same type.</para>
        /// </remarks>
        public static bool IsAccessibleWithin(
            this ISymbol symbol,
            ISymbol within,
            ITypeSymbol throughTypeOpt = null)
        {
            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (within == null)
            {
                throw new ArgumentNullException(nameof(within));
            }

            if (!(within is INamedTypeSymbol || within is IAssemblySymbol))
            {
                throw new ArgumentException(CodeAnalysisResources.IsAccessibleBadWithin, nameof(within));
            }

            switch (symbol.Kind)
            {
                case SymbolKind.Alias:
                    return IsAccessibleWithin(((IAliasSymbol)symbol).Target, within);
                case SymbolKind.ArrayType:
                    return IsAccessibleWithin(((IArrayTypeSymbol)symbol).ElementType, within);
                case SymbolKind.PointerType:
                    return IsAccessibleWithin(((IPointerTypeSymbol)symbol).PointedAtType, within);
                case SymbolKind.ErrorType:
                    // Error types arise from error recovery. We permit access to enable further analysis.
                    return true;
                case SymbolKind.NamedType:
                    return isNamedTypeAccessibleWithin((INamedTypeSymbol)symbol, within);
                case SymbolKind.TypeParameter:
                case SymbolKind.Parameter:
                case SymbolKind.Local:
                case SymbolKind.Label:
                case SymbolKind.Namespace:
                case SymbolKind.DynamicType:
                case SymbolKind.Assembly:
                case SymbolKind.NetModule:
                case SymbolKind.RangeVariable:
                case SymbolKind.Discard:
                case SymbolKind.Preprocessing:
                    // These types of symbols are always accessible (if visible).
                    return true;
                case SymbolKind.Method:
                    var method = (IMethodSymbol)symbol;
                    if (method.MethodKind == MethodKind.BuiltinOperator)
                    {
                        return true;
                    }

                    goto case SymbolKind.Field;

                case SymbolKind.Property:
                case SymbolKind.Event:
                case SymbolKind.Field:
                    return isMemberAccessible(symbol.ContainingType, symbol.DeclaredAccessibility, within, symbol.IsStatic ? null : throughTypeOpt);

                default:
                    throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
            }

            bool isNamedTypeAccessibleWithin(INamedTypeSymbol type, ISymbol within0)
            {
                Debug.Assert(within0 is INamedTypeSymbol || within0 is IAssemblySymbol);
                Debug.Assert(type != null);

                if (!type.IsDefinition)
                {
                    foreach (var typeArg in type.TypeArguments)
                    {
                        if (!IsAccessibleWithin(typeArg, within0))
                        {
                            return false;
                        }
                    }
                }

                var containingType = type.ContainingType;
                if (containingType == null)
                {
                    return isNonNestedTypeAccessible(type.ContainingAssembly, type.DeclaredAccessibility, within0);
                }
                else
                {
                    return isMemberAccessible(containingType, type.DeclaredAccessibility, within0, throughTypeOpt0: null);
                }
            }

            bool isNonNestedTypeAccessible(IAssemblySymbol declaringAssembly, Accessibility declaredAccessibility, ISymbol within0)
            {
                Debug.Assert(within0 is INamedTypeSymbol || within0 is IAssemblySymbol);
                Debug.Assert(declaringAssembly != null);

                switch (declaredAccessibility)
                {
                    case Accessibility.Public:
                        // Public symbols are always accessible from any context
                        return true;

                    case Accessibility.Private:
                    case Accessibility.Protected:
                    case Accessibility.ProtectedAndInternal:
                        // Shouldn't happen except in error cases as these access levels cannot be used to declare top-level types.
                        return false;

                    case Accessibility.Internal:
                    case Accessibility.ProtectedOrInternal:
                        // An internal type is accessible if we're in the same assembly or we have
                        // friend access to the assembly it was defined in.
                        var withinAssembly = (within0 as IAssemblySymbol) ?? ((INamedTypeSymbol)within0).ContainingAssembly;
                        return hasInternalAccessTo(assemblyWantingAccess: withinAssembly, declaringAssembly);

                    case Accessibility.NotApplicable:
                    default:
                        throw ExceptionUtilities.UnexpectedValue(declaredAccessibility);
                }
            }

            bool hasInternalAccessTo(IAssemblySymbol assemblyWantingAccess, IAssemblySymbol declaringAssembly)
            {
                if (assemblyWantingAccess.Equals(declaringAssembly))
                {
                    return true;
                }

                if (assemblyWantingAccess.IsInteractive && declaringAssembly.IsInteractive)
                {
                    return true;
                }

                return declaringAssembly.GivesAccessTo(assemblyWantingAccess);
            }

            // Is a member with declared accessibility "declaredAccessibility" accessible from within
            // "within", which must be a named type or an assembly.
            bool isMemberAccessible(INamedTypeSymbol declaringType, Accessibility declaredAccessibility, ISymbol within0, ITypeSymbol throughTypeOpt0)
            {
                Debug.Assert(within0 is INamedTypeSymbol || within0 is IAssemblySymbol);
                Debug.Assert(declaringType != null);

                // This is a shortcut optimization of the more complex test for the most common situation.
                if (within0 == declaringType)
                {
                    return true;
                }

                // A nested symbol is only accessible to us if its container is accessible as well.
                if (!IsAccessibleWithin(declaringType, within0, throughTypeOpt0))
                {
                    return false;
                }

                switch (declaredAccessibility)
                {
                    case Accessibility.Public:
                    case Accessibility.NotApplicable:
                        return true;

                    case Accessibility.Private:
                        if (declaringType.TypeKind == TypeKind.Submission)
                        {
                            return true;
                        }

                        return within0 != null && isPrivateSymbolAccessible(within0, declaringType);
                }

                var withinType = within0 as INamedTypeSymbol;
                var withinAssembly = withinType?.ContainingAssembly ?? (IAssemblySymbol)within0;
                switch (declaredAccessibility)
                {
                    case Accessibility.Internal:
                        return
                            hasInternalAccessTo(assemblyWantingAccess: withinAssembly, declaringType.ContainingAssembly);

                    case Accessibility.ProtectedAndInternal:
                        return
                            isProtectedSymbolAccessible(withinType, throughTypeOpt0, declaringType) &&
                            hasInternalAccessTo(assemblyWantingAccess: withinAssembly, declaringType.ContainingAssembly);

                    case Accessibility.ProtectedOrInternal:
                        return
                            isProtectedSymbolAccessible(withinType, throughTypeOpt0, declaringType) ||
                            hasInternalAccessTo(assemblyWantingAccess: withinAssembly, declaringType.ContainingAssembly);

                    case Accessibility.Protected:
                        return
                            isProtectedSymbolAccessible(withinType, throughTypeOpt0, declaringType);

                    default:
                        throw ExceptionUtilities.UnexpectedValue(declaredAccessibility);
                }
            }

            bool isProtectedSymbolAccessible(INamedTypeSymbol withinType, ITypeSymbol throughTypeOpt0, INamedTypeSymbol declaringType)
            {
                INamedTypeSymbol originalDeclaringType = declaringType.OriginalDefinition;
                if (originalDeclaringType.TypeKind == TypeKind.Submission)
                {
                    return true;
                }

                if (withinType == null)
                {
                    // If we're not within a type, we can't access a protected symbol
                    return false;
                }

                if (isNestedWithinOriginalCDeclaringType(withinType, originalDeclaringType))
                {
                    return true;
                }

                var originalThroughTypeOpt = throughTypeOpt0?.OriginalDefinition;
                for (INamedTypeSymbol current = withinType.OriginalDefinition; current != null; current = current.ContainingType)
                {
                    if (inheritsFromIgnoringConstruction(current, originalDeclaringType))
                    {
                        if (originalThroughTypeOpt == null || inheritsFromIgnoringConstruction(originalThroughTypeOpt, current))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            bool isPrivateSymbolAccessible(ISymbol within0, INamedTypeSymbol declaringType)
            {
                var withinType = within0 as INamedTypeSymbol;

                // A private symbol is accessible if we're (optionally nested) inside the type that it
                // was defined in.
                return withinType != null && isNestedWithinOriginalCDeclaringType(withinType, declaringType.OriginalDefinition);
            }

            bool isNestedWithinOriginalCDeclaringType(INamedTypeSymbol type, INamedTypeSymbol originalDeclatingType)
            {
                Debug.Assert(originalDeclatingType.IsDefinition);
                Debug.Assert(type != null);

                for (var current = type.OriginalDefinition; current != null; current = current.ContainingType)
                {
                    if (sameOriginalNamedType(current, originalDeclatingType))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool inheritsFromIgnoringConstruction(ITypeSymbol type, INamedTypeSymbol baseType)
            {
                Debug.Assert(type.IsDefinition);
                Debug.Assert(baseType.IsDefinition);

                for (var current = type; current != null; current = current.BaseType?.OriginalDefinition)
                {
                    if (sameOriginalNamedType(baseType, current as INamedTypeSymbol))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool sameOriginalNamedType(INamedTypeSymbol t1, INamedTypeSymbol t2)
            {
                Debug.Assert(t1 != null);
                Debug.Assert(t1.IsDefinition);
                Debug.Assert(t2?.IsDefinition != false);

                // We expect a given named type definition to satisfy reference identity.
                // But we relax this to permit a type to be represented by multiple symbols (e.g. separate compilations).
                // Note that the same symbol is expected to have the identical name as itself, despite VB language rules.
                return t1 == t2 || t2 != null && t1.MetadataName == t2.MetadataName && t1.Arity == t2.Arity && sameOriginalSymbol(t1.ContainingSymbol, t2.ContainingSymbol);

                bool sameOriginalSymbol(ISymbol s1, ISymbol s2)
                {
                    if (s1 == s2)
                    {
                        return true;
                    }

                    if (s1 == null || s2 == null)
                    {
                        return false;
                    }

                    switch (s1.Kind)
                    {
                        case SymbolKind.NamedType:
                            return sameOriginalNamedType(s1 as INamedTypeSymbol, s2 as INamedTypeSymbol);
                        case SymbolKind.Namespace:
                            return sameNamespace(s1 as INamespaceSymbol, s2 as INamespaceSymbol);
                        case SymbolKind.Assembly:
                            return sameAssembly(s1 as IAssemblySymbol, s2 as IAssemblySymbol);
                        case SymbolKind.NetModule:
                            return sameModule(s1 as IModuleSymbol, s2 as IModuleSymbol);
                        default:
                            return false;
                    }
                }

                bool sameModule(IModuleSymbol m1, IModuleSymbol m2)
                {
                    Debug.Assert(m1 != null);
                    // We don't need to check the module name, as modules are effectively merged in the containing assembly.
                    return m2 != null && sameOriginalSymbol(m1.ContainingSymbol, m2.ContainingSymbol);
                }

                bool sameAssembly(IAssemblySymbol a1, IAssemblySymbol a2)
                {
                    return a1.Equals(a2);
                }

                bool sameNamespace(INamespaceSymbol n1, INamespaceSymbol n2)
                {
                    Debug.Assert(n1 != null);
                    // Note that the same symbol is expected to have the identical name as itself, despite VB language rules.
                    return n2 != null && n1.MetadataName == n2.MetadataName && sameOriginalSymbol(n1.ContainingSymbol, n2.ContainingSymbol);
                }
            }
        }
    }
}
