// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static partial class ISymbolExtensions
    {
        /// <summary>
        /// Checks if 'symbol' would be accessible from within 'within' when loaded
        /// into the same program. An optional qualifier of type 'throughType' is used to resolve
        /// protected access for instance members.
        /// </summary>
        /// <remarks>
        /// <para>The following areas of imprecision may exist in the results of this API:</para>
        /// <para>For assembly identity, we depend on equality of the assembly's <see cref="IAssemblySymbol.Identity"/>.
        /// This corresponds to the compiler and CLR notions of the identity of assemblies. It is possible
        /// to produce distinct assemblies that share the same assembly identity, though it is not possible
        /// to load such symbols into the same CLR instance.</para>
        /// <para>We compare <see cref="INamedTypeSymbol"/> based on the identity of the containing
        /// assembly (see above) and their metadata name, which includes the metadata name of the enclosing
        /// namespaces. This may produce incorrect results for symbols loaded by the VB compiler,
        /// which merges namespaces when importing an assembly. Consequently, the metadata name of the namespace
        /// may not be correct for some of the contained types, and types that are distinct
        /// may appear to have the same fully-qualified name (see https://github.com/dotnet/roslyn/issues/26546).
        /// In that case this API may treat them as the same type.</para>
        /// <para>It is advised to avoid the use of this API within the compilers, as the compilers have additional
        /// requirements for access checking that are not satisfied by this implementation, including the
        /// avoidance of infinite recursion that would result from the use of the ISymbol APIs here,
        /// the requirement that all symbols that it checks are part of the same compilation,
        /// and additional returned details (from the compiler's internal APIs) that are helpful for more precisely
        /// diagnosing reasons for accessibility failure.</para>
        /// </remarks>
        public static bool IsAccessibleWithin(
            this ISymbol symbol,
            ISymbol within,
            ITypeSymbol throughType = null)
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
                    return isMemberAccessible(symbol.ContainingType, symbol.DeclaredAccessibility, within, symbol.IsStatic ? null : throughType);

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
                if (sameAssembly(assemblyWantingAccess, declaringAssembly))
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

            bool sameAssembly(IAssemblySymbol a1, IAssemblySymbol a2)
            {
                return a2 != null && a1.Identity.Equals(a2.Identity);
            }

            bool sameOriginalNamedType(INamedTypeSymbol t1, INamedTypeSymbol t2)
            {
                Debug.Assert(t1 != null);
                Debug.Assert(t1.IsDefinition);
                Debug.Assert(t2?.IsDefinition != false);

                // We expect a given named type definition to satisfy reference identity.
                // But we relax this to permit a type to be represented by multiple symbols (e.g. separate compilations,
                // or in "hypothetical" scenarios used by the IDE).
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

                bool sameNamespace(INamespaceSymbol n1, INamespaceSymbol n2)
                {
                    Debug.Assert(n1 != null);
                    // Note that the same symbol is expected to have the identical name as itself, despite VB language rules.
                    return n2 != null && n1.MetadataName == n2.MetadataName && sameOriginalSymbol(n1.ContainingSymbol, n2.ContainingSymbol);
                }
            }
        }

        /// <summary>
        /// Given that an assembly with identity assemblyGrantingAccessIdentity granted access to assemblyWantingAccess,
        /// check the public keys to ensure the internals-visible-to check should succeed. This is used by both the
        /// C# and VB implementations as a helper to implement `bool IAssemblySymbol.GivesAccessTo(IAssemblySymbol toAssembly)`.
        /// </summary>
        internal static IVTConclusion PerformIVTCheck(
            this IAssemblySymbol assemblyWantingAccess,
            ImmutableArray<byte> assemblyWantingAccessKey,
            ImmutableArray<byte> grantedToPublicKey,
            AssemblyIdentity assemblyGrantingAccessIdentity)
        {
            // This gets a bit complicated. Let's break it down.
            //
            // First off, let's assume that the "other" assembly is Smith.DLL, that the "this"
            // assembly is "Jones.DLL", and that Smith has named Jones as a friend (that is a precondition
            // to calling this method. Whether we allow Jones to see internals of Smith depends on these four factors:
            //
            // q1) Is Smith strong-named?
            // q2) Did Smith name Jones as a friend via a strong name?
            // q3) Is Jones strong-named?
            // q4) Does Smith give a strong-name for Jones that matches our strong name?
            //
            // Before we dive into the details, we should mention two additional facts:
            //
            // * If the answer to q1 is "yes", and Smith was compiled by a Roslyn compiler, then q2 must be "yes" also.
            //   Strong-named Smith must only be friends with strong-named Jones. See the blog article
            //   http://blogs.msdn.com/b/ericlippert/archive/2009/06/04/alas-smith-and-jones.aspx
            //   for an explanation of why this feature is desirable.
            //
            //   Now, just because the compiler enforces this rule does not mean that we will never run into
            //   a scenario where Smith is strong-named and names Jones via a weak name. Not all assemblies
            //   were compiled with a Roslyn compiler. We still need to deal sensibly with this situation.
            //   We do so by ignoring the problem; if strong-named Smith extends friendship to weak-named
            //   Jones then we're done; any assembly named Jones is a friend of Smith.
            //
            //   Incidentally, the C# compiler produces error CS1726, ERR_FriendAssemblySNReq, and VB produces
            //   the error VB31535, ERR_FriendAssemblyStrongNameRequired, when compiling 
            //   a strong-named Smith that names a weak-named Jones as its friend.
            //
            // * If the answer to q1 is "no" and the answer to q3 is "yes" then we are in a situation where
            //   strong-named Jones is referencing weak-named Smith, which is illegal. In the dev10 compiler
            //   we do not give an error about this until emit time. In Roslyn we have a new error, CS7029,
            //   which we give before emit time when we detect that weak-named Smith has given friend access
            //   to strong-named Jones, which then references Smith. However, we still want to give friend
            //   access to Jones for the purposes of semantic analysis.
            //
            // TODO: Roslyn does not yet give an error in other circumstances whereby a strong-named assembly
            // TODO: references a weak-named assembly.
            //
            // Let's make a chart that illustrates all the possible answers to these four questions, and
            // what the resulting accessibility should be:
            //
            // case q1  q2  q3  q4  Result                 Explanation
            // 1    YES YES YES YES SUCCESS          Smith has named this strong-named Jones as a friend.
            // 2    YES YES YES NO  NO MATCH         Smith has named a different strong-named Jones as a friend.
            // 3    YES YES NO  NO  NO MATCH         Smith has named a strong-named Jones as a friend, but this Jones is weak-named.
            // 4    YES NO  YES NO  SUCCESS          Smith has improperly (*) named any Jones as its friend. But we honor its offer of friendship.
            // 5    YES NO  NO  NO  SUCCESS          Smith has improperly (*) named any Jones as its friend. But we honor its offer of friendship.
            // 6    NO  YES YES YES SUCCESS, BAD REF Smith has named this strong-named Jones as a friend, but Jones should not be referring to a weak-named Smith.
            // 7    NO  YES YES NO  NO MATCH         Smith has named a different strong-named Jones as a friend.
            // 8    NO  YES NO  NO  NO MATCH         Smith has named a strong-named Jones as a friend, but this Jones is weak-named.
            // 9    NO  NO  YES NO  SUCCESS, BAD REF Smith has named any Jones as a friend, but Jones should not be referring to a weak-named Smith.
            // 10   NO  NO  NO  NO  SUCCESS          Smith has named any Jones as its friend.
            //                                     
            // (*) Smith was not built with a Roslyn compiler, which would have prevented this.
            //
            // This method never returns NoRelationshipClaimed because if control got here, then we assume
            // (as a precondition) that Smith named Jones as a friend somehow.

            bool q1 = assemblyGrantingAccessIdentity.IsStrongName;
            bool q2 = !grantedToPublicKey.IsDefaultOrEmpty;
            bool q3 = !assemblyWantingAccessKey.IsDefaultOrEmpty;
            bool q4 = (q2 & q3) && ByteSequenceComparer.Equals(grantedToPublicKey, assemblyWantingAccessKey);

            // Cases 2, 3, 7 and 8:
            if (q2 && !q4)
            {
                return IVTConclusion.PublicKeyDoesntMatch;
            }

            // Cases 6 and 9:
            if (!q1 && q3)
            {
                return IVTConclusion.OneSignedOneNot;
            }

            // Cases 1, 4, 5 and 10:
            return IVTConclusion.Match;
        }
    }
}
