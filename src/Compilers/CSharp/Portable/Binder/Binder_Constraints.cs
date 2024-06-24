// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        /// <summary>
        /// Return a collection of bound constraint clauses indexed by type parameter
        /// ordinal. All constraint clauses are bound, even if there are multiple constraints
        /// for the same type parameter, or constraints for unrecognized type parameters.
        /// Extra constraints are not included in the returned collection however.
        /// </summary>
        internal ImmutableArray<TypeParameterConstraintClause> BindTypeParameterConstraintClauses(
            Symbol containingSymbol,
            ImmutableArray<TypeParameterSymbol> typeParameters,
            TypeParameterListSyntax typeParameterList,
            SyntaxList<TypeParameterConstraintClauseSyntax> clauses,
            BindingDiagnosticBag diagnostics,
            bool performOnlyCycleSafeValidation,
            bool isForOverride = false)
        {
            Debug.Assert(this.Flags.Includes(BinderFlags.GenericConstraintsClause));
            RoslynDebug.Assert((object)containingSymbol != null);
            Debug.Assert((containingSymbol.Kind == SymbolKind.NamedType) || (containingSymbol.Kind == SymbolKind.Method));
            Debug.Assert(typeParameters.Length > 0);
            Debug.Assert(clauses.Count > 0);

            int n = typeParameters.Length;

            // Create a map from type parameter name to ordinal.
            // No need to report duplicate names since duplicates
            // are reported when the type parameters are bound.
            var names = new Dictionary<string, int>(n, StringOrdinalComparer.Instance);
            foreach (var typeParameter in typeParameters)
            {
                var name = typeParameter.Name;
                if (!names.ContainsKey(name))
                {
                    names.Add(name, names.Count);
                }
            }

            // An array of constraint clauses, one for each type parameter, indexed by ordinal.
            var results = ArrayBuilder<TypeParameterConstraintClause?>.GetInstance(n, fillWithValue: null);
            var syntaxNodes = ArrayBuilder<ArrayBuilder<TypeConstraintSyntax>?>.GetInstance(n, fillWithValue: null);

            // Bind each clause and add to the results.
            foreach (var clause in clauses)
            {
                var name = clause.Name.Identifier.ValueText;
                RoslynDebug.Assert(name is object);
                int ordinal;
                if (names.TryGetValue(name, out ordinal))
                {
                    Debug.Assert(ordinal >= 0);
                    Debug.Assert(ordinal < n);

                    (TypeParameterConstraintClause constraintClause, ArrayBuilder<TypeConstraintSyntax>? typeConstraintNodes) = this.BindTypeParameterConstraints(typeParameterList.Parameters[ordinal], clause, isForOverride, diagnostics);
                    if (results[ordinal] == null)
                    {
                        results[ordinal] = constraintClause;
                        syntaxNodes[ordinal] = typeConstraintNodes;
                    }
                    else
                    {
                        // "A constraint clause has already been specified for type parameter '{0}'. ..."
                        diagnostics.Add(ErrorCode.ERR_DuplicateConstraintClause, clause.Name.Location, name);
                        typeConstraintNodes?.Free();
                    }
                }
                else
                {
                    // Unrecognized type parameter. Don't bother binding the constraints
                    // (the ": I<U>" in "where U : I<U>") since that will lead to additional
                    // errors ("type or namespace 'U' could not be found") if the type
                    // parameter is referenced in the constraints.

                    // "'{1}' does not define type parameter '{0}'"
                    diagnostics.Add(ErrorCode.ERR_TyVarNotFoundInConstraint, clause.Name.Location, name, containingSymbol.ConstructedFrom());
                }
            }

            // Add default values for type parameters without constraint clauses.
            for (int i = 0; i < n; i++)
            {
                if (results[i] == null)
                {
                    results[i] = GetDefaultTypeParameterConstraintClause(typeParameterList.Parameters[i], isForOverride);
                }
            }

            RemoveInvalidConstraints(typeParameters, results!, syntaxNodes, performOnlyCycleSafeValidation, diagnostics);

            foreach (var typeConstraintsSyntaxes in syntaxNodes)
            {
                typeConstraintsSyntaxes?.Free();
            }

            syntaxNodes.Free();

            return results.ToImmutableAndFree()!;
        }

        /// <summary>
        /// Bind and return a single type parameter constraint clause along with syntax nodes corresponding to type constraints.
        /// </summary>
        private (TypeParameterConstraintClause, ArrayBuilder<TypeConstraintSyntax>?) BindTypeParameterConstraints(
            TypeParameterSyntax typeParameterSyntax, TypeParameterConstraintClauseSyntax constraintClauseSyntax, bool isForOverride, BindingDiagnosticBag diagnostics)
        {
            var constraints = TypeParameterConstraintKind.None;
            ArrayBuilder<TypeWithAnnotations>? constraintTypes = null;
            ArrayBuilder<TypeConstraintSyntax>? syntaxBuilder = null;
            SeparatedSyntaxList<TypeParameterConstraintSyntax> constraintsSyntax = constraintClauseSyntax.Constraints;
            Debug.Assert(!InExecutableBinder); // Cannot eagerly report diagnostics handled by LazyMissingNonNullTypesContextDiagnosticInfo 
            bool hasTypeLikeConstraint = false;
            bool reportedOverrideWithConstraints = false;

            for (int i = 0, n = constraintsSyntax.Count; i < n; i++)
            {
                var syntax = constraintsSyntax[i];
                switch (syntax.Kind())
                {
                    case SyntaxKind.ClassConstraint:
                        hasTypeLikeConstraint = true;

                        if (i != 0)
                        {
                            if (!reportedOverrideWithConstraints)
                            {
                                reportTypeConstraintsMustBeUniqueAndFirst(syntax, diagnostics);
                            }

                            if (isForOverride && (constraints & (TypeParameterConstraintKind.ValueType | TypeParameterConstraintKind.ReferenceType)) != 0)
                            {
                                continue;
                            }
                        }

                        var constraintSyntax = (ClassOrStructConstraintSyntax)syntax;
                        SyntaxToken questionToken = constraintSyntax.QuestionToken;
                        if (questionToken.IsKind(SyntaxKind.QuestionToken))
                        {
                            constraints |= TypeParameterConstraintKind.NullableReferenceType;

                            if (isForOverride)
                            {
                                reportOverrideWithConstraints(ref reportedOverrideWithConstraints, syntax, diagnostics);
                            }
                            else if (diagnostics.DiagnosticBag is DiagnosticBag diagnosticBag)
                            {
                                LazyMissingNonNullTypesContextDiagnosticInfo.AddAll(this, questionToken, type: null, diagnosticBag);
                            }
                        }
                        else if (isForOverride || AreNullableAnnotationsEnabled(constraintSyntax.ClassOrStructKeyword))
                        {
                            constraints |= TypeParameterConstraintKind.NotNullableReferenceType;
                        }
                        else
                        {
                            constraints |= TypeParameterConstraintKind.ReferenceType;
                        }

                        continue;
                    case SyntaxKind.StructConstraint:
                        hasTypeLikeConstraint = true;

                        if (i != 0)
                        {
                            if (!reportedOverrideWithConstraints)
                            {
                                reportTypeConstraintsMustBeUniqueAndFirst(syntax, diagnostics);
                            }

                            if (isForOverride && (constraints & (TypeParameterConstraintKind.ValueType | TypeParameterConstraintKind.ReferenceType)) != 0)
                            {
                                continue;
                            }
                        }

                        constraints |= TypeParameterConstraintKind.ValueType;
                        continue;
                    case SyntaxKind.ConstructorConstraint:
                        if (isForOverride)
                        {
                            reportOverrideWithConstraints(ref reportedOverrideWithConstraints, syntax, diagnostics);
                            continue;
                        }

                        if ((constraints & TypeParameterConstraintKind.ValueType) != 0)
                        {
                            diagnostics.Add(ErrorCode.ERR_NewBoundWithVal, syntax.GetFirstToken().GetLocation());
                        }
                        if ((constraints & TypeParameterConstraintKind.Unmanaged) != 0)
                        {
                            diagnostics.Add(ErrorCode.ERR_NewBoundWithUnmanaged, syntax.GetFirstToken().GetLocation());
                        }

                        if (i != n - 1 && constraintsSyntax[i + 1].Kind() != SyntaxKind.AllowsConstraintClause)
                        {
                            diagnostics.Add(ErrorCode.ERR_NewBoundMustBeLast, syntax.GetFirstToken().GetLocation());
                        }

                        constraints |= TypeParameterConstraintKind.Constructor;
                        continue;
                    case SyntaxKind.DefaultConstraint:
                        CheckFeatureAvailability(syntax, MessageID.IDS_FeatureDefaultTypeParameterConstraint, diagnostics);

                        if (!isForOverride)
                        {
                            diagnostics.Add(ErrorCode.ERR_DefaultConstraintOverrideOnly, syntax.GetLocation());
                        }

                        if (i != 0)
                        {
                            if (!reportedOverrideWithConstraints)
                            {
                                reportTypeConstraintsMustBeUniqueAndFirst(syntax, diagnostics);
                            }

                            if (isForOverride && (constraints & (TypeParameterConstraintKind.ValueType | TypeParameterConstraintKind.ReferenceType)) != 0)
                            {
                                continue;
                            }
                        }

                        constraints |= TypeParameterConstraintKind.Default;
                        continue;
                    case SyntaxKind.TypeConstraint:
                        if (isForOverride)
                        {
                            reportOverrideWithConstraints(ref reportedOverrideWithConstraints, syntax, diagnostics);
                        }
                        else
                        {
                            hasTypeLikeConstraint = true;

                            if (constraintTypes == null)
                            {
                                constraintTypes = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                                syntaxBuilder = ArrayBuilder<TypeConstraintSyntax>.GetInstance();
                            }

                            var typeConstraintSyntax = (TypeConstraintSyntax)syntax;
                            var typeSyntax = typeConstraintSyntax.Type;

                            var type = BindTypeOrConstraintKeyword(typeSyntax, diagnostics, out ConstraintContextualKeyword keyword);

                            switch (keyword)
                            {
                                case ConstraintContextualKeyword.Unmanaged:
                                    if (i != 0)
                                    {
                                        reportTypeConstraintsMustBeUniqueAndFirst(typeSyntax, diagnostics);
                                        continue;
                                    }

                                    // This should produce diagnostics if the types are missing
                                    GetWellKnownType(WellKnownType.System_Runtime_InteropServices_UnmanagedType, diagnostics, typeSyntax);
                                    GetSpecialType(SpecialType.System_ValueType, diagnostics, typeSyntax);

                                    constraints |= TypeParameterConstraintKind.Unmanaged;
                                    continue;

                                case ConstraintContextualKeyword.NotNull:
                                    if (i != 0)
                                    {
                                        reportTypeConstraintsMustBeUniqueAndFirst(typeSyntax, diagnostics);
                                    }

                                    constraints |= TypeParameterConstraintKind.NotNull;
                                    continue;

                                case ConstraintContextualKeyword.None:
                                    break;
                                default:
                                    throw ExceptionUtilities.UnexpectedValue(keyword);
                            }

                            constraintTypes.Add(type);
                            syntaxBuilder!.Add(typeConstraintSyntax);
                        }
                        continue;

                    case SyntaxKind.AllowsConstraintClause:

                        if (isForOverride)
                        {
                            reportOverrideWithConstraints(ref reportedOverrideWithConstraints, syntax, diagnostics);
                            continue;
                        }

                        if (i != n - 1)
                        {
                            diagnostics.Add(ErrorCode.ERR_AllowsClauseMustBeLast, syntax.GetFirstToken().GetLocation());
                        }

                        bool hasRefStructConstraint = false;

                        foreach (var allowsConstraint in ((AllowsConstraintClauseSyntax)syntax).Constraints)
                        {
                            if (allowsConstraint.Kind() == SyntaxKind.RefStructConstraint)
                            {
                                if (hasRefStructConstraint)
                                {
                                    diagnostics.Add(ErrorCode.ERR_RefStructConstraintAlreadySpecified, allowsConstraint);
                                }
                                else
                                {
                                    CheckFeatureAvailability(allowsConstraint, MessageID.IDS_FeatureAllowsRefStructConstraint, diagnostics);

                                    if (!Compilation.Assembly.RuntimeSupportsByRefLikeGenerics)
                                    {
                                        Error(diagnostics, ErrorCode.ERR_RuntimeDoesNotSupportByRefLikeGenerics, allowsConstraint);
                                    }

                                    constraints |= TypeParameterConstraintKind.AllowByRefLike;
                                    hasRefStructConstraint = true;
                                }
                            }
                        }

                        continue;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                }
            }

            if (!isForOverride && !hasTypeLikeConstraint && !AreNullableAnnotationsEnabled(typeParameterSyntax.Identifier))
            {
                constraints |= TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType;
            }

            Debug.Assert(!isForOverride ||
                         (constraints & (TypeParameterConstraintKind.ReferenceType | TypeParameterConstraintKind.ValueType)) != (TypeParameterConstraintKind.ReferenceType | TypeParameterConstraintKind.ValueType));

            return (TypeParameterConstraintClause.Create(constraints, constraintTypes?.ToImmutableAndFree() ?? ImmutableArray<TypeWithAnnotations>.Empty), syntaxBuilder);

            static void reportOverrideWithConstraints(ref bool reportedOverrideWithConstraints, TypeParameterConstraintSyntax syntax, BindingDiagnosticBag diagnostics)
            {
                if (!reportedOverrideWithConstraints)
                {
                    diagnostics.Add(ErrorCode.ERR_OverrideWithConstraints, syntax.GetLocation());
                    reportedOverrideWithConstraints = true;
                }
            }

            static void reportTypeConstraintsMustBeUniqueAndFirst(CSharpSyntaxNode syntax, BindingDiagnosticBag diagnostics)
            {
                diagnostics.Add(ErrorCode.ERR_TypeConstraintsMustBeUniqueAndFirst, syntax.GetLocation());
            }
        }

        internal ImmutableArray<TypeParameterConstraintClause> GetDefaultTypeParameterConstraintClauses(TypeParameterListSyntax typeParameterList)
        {
            var builder = ArrayBuilder<TypeParameterConstraintClause>.GetInstance(typeParameterList.Parameters.Count);

            foreach (TypeParameterSyntax typeParameterSyntax in typeParameterList.Parameters)
            {
                builder.Add(GetDefaultTypeParameterConstraintClause(typeParameterSyntax));
            }

            return builder.ToImmutableAndFree();
        }

        private TypeParameterConstraintClause GetDefaultTypeParameterConstraintClause(TypeParameterSyntax typeParameterSyntax, bool isForOverride = false)
        {
            return isForOverride || AreNullableAnnotationsEnabled(typeParameterSyntax.Identifier) ? TypeParameterConstraintClause.Empty : TypeParameterConstraintClause.ObliviousNullabilityIfReferenceType;
        }

        /// <summary>
        /// Constraints are checked for invalid types, duplicate types, and accessibility. 
        /// </summary>
        private static void RemoveInvalidConstraints(
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ArrayBuilder<TypeParameterConstraintClause> constraintClauses,
            ArrayBuilder<ArrayBuilder<TypeConstraintSyntax>?> syntaxNodes,
            bool performOnlyCycleSafeValidation,
            BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(typeParameters.Length > 0);
            Debug.Assert(typeParameters.Length == constraintClauses.Count);
            int n = typeParameters.Length;
            for (int i = 0; i < n; i++)
            {
                constraintClauses[i] = RemoveInvalidConstraints(typeParameters[i], constraintClauses[i], syntaxNodes[i], performOnlyCycleSafeValidation, diagnostics);
            }
        }

        private static TypeParameterConstraintClause RemoveInvalidConstraints(
            TypeParameterSymbol typeParameter,
            TypeParameterConstraintClause constraintClause,
            ArrayBuilder<TypeConstraintSyntax>? syntaxNodesOpt,
            bool performOnlyCycleSafeValidation,
            BindingDiagnosticBag diagnostics)
        {
            if (syntaxNodesOpt != null)
            {
                var constraintTypes = constraintClause.ConstraintTypes;
                Symbol containingSymbol = typeParameter.ContainingSymbol;

                var constraintTypeBuilder = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                int n = constraintTypes.Length;

                for (int i = 0; i < n; i++)
                {
                    var constraintType = constraintTypes[i];
                    var syntax = syntaxNodesOpt[i];
                    // Only valid constraint types are included in ConstraintTypes
                    // since, in general, it may be difficult to support all invalid types.
                    // In the future, we may want to include some invalid types
                    // though so the public binding API has the most information.
                    if (IsValidConstraint(typeParameter, syntax, constraintType, constraintClause.Constraints, constraintTypeBuilder, performOnlyCycleSafeValidation, diagnostics))
                    {
                        if (!performOnlyCycleSafeValidation)
                        {
                            CheckConstraintTypeVisibility(containingSymbol, syntax.Location, constraintType, diagnostics);
                        }

                        constraintTypeBuilder.Add(constraintType);
                    }
                }

                if (constraintTypeBuilder.Count < n)
                {
                    return TypeParameterConstraintClause.Create(constraintClause.Constraints, constraintTypeBuilder.ToImmutableAndFree());
                }

                constraintTypeBuilder.Free();
            }

            return constraintClause;
        }

        private static void CheckConstraintTypeVisibility(
            Symbol containingSymbol,
            Location location,
            TypeWithAnnotations constraintType,
            BindingDiagnosticBag diagnostics)
        {
            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, containingSymbol.ContainingAssembly);
            if (!containingSymbol.IsNoMoreVisibleThan(constraintType, ref useSiteInfo))
            {
                // "Inconsistent accessibility: constraint type '{1}' is less accessible than '{0}'"
                diagnostics.Add(ErrorCode.ERR_BadVisBound, location, containingSymbol, constraintType.Type);
            }

            if (constraintType.Type.HasFileLocalTypes())
            {
                // if the containing symbol of the constraint is a member, we need to ensure the nearest containing type is a file-local type.
                var possibleFileType = containingSymbol switch
                {
                    TypeSymbol typeSymbol => typeSymbol,
                    LocalFunctionSymbol => null,
                    MethodSymbol method => (TypeSymbol)method.ContainingSymbol,
                    _ => throw ExceptionUtilities.UnexpectedValue(containingSymbol)
                };
                Debug.Assert(possibleFileType?.IsDefinition != false);
                if (possibleFileType?.HasFileLocalTypes() == false)
                {
                    diagnostics.Add(ErrorCode.ERR_FileTypeDisallowedInSignature, location, constraintType.Type, containingSymbol);
                }
            }

            diagnostics.Add(location, useSiteInfo);
        }

        /// <summary>
        /// Returns true if the constraint is valid. Otherwise
        /// returns false and generates a diagnostic.
        /// </summary>
        private static bool IsValidConstraint(
            TypeParameterSymbol typeParameter,
            TypeConstraintSyntax syntax,
            TypeWithAnnotations type,
            TypeParameterConstraintKind constraints,
            ArrayBuilder<TypeWithAnnotations> constraintTypes,
            bool performOnlyCycleSafeValidation,
            BindingDiagnosticBag diagnostics)
        {
            if (!isValidConstraintType(typeParameter, syntax, type, performOnlyCycleSafeValidation, diagnostics))
            {
                return false;
            }

            if (!performOnlyCycleSafeValidation && constraintTypes.Contains(c => type.Equals(c, TypeCompareKind.AllIgnoreOptions)))
            {
                // "Duplicate constraint '{0}' for type parameter '{1}'"
                Error(diagnostics, ErrorCode.ERR_DuplicateBound, syntax, type.Type.SetUnknownNullabilityForReferenceTypes(), typeParameter.Name);
                return false;
            }

            if (!type.DefaultType.IsTypeParameter() && // Doing an explicit check for type parameter on unresolved type to avoid cycles while calculating TypeKind. An unresolved type parameter cannot resolve to a class.   
                type.TypeKind == TypeKind.Class)
            {
                // If there is already a struct or class constraint (class constraint could be
                // 'class' or explicit type), report an error and drop this class. If we don't
                // drop this additional class, we may end up with conflicting class constraints.

                if (constraintTypes.Count > 0)
                {
                    // "The class type constraint '{0}' must come before any other constraints"
                    Error(diagnostics, ErrorCode.ERR_ClassBoundNotFirst, syntax, type.Type);
                    return false;
                }

                if ((constraints & (TypeParameterConstraintKind.ReferenceType)) != 0)
                {
                    switch (type.SpecialType)
                    {
                        case SpecialType.System_Enum:
                        case SpecialType.System_Delegate:
                        case SpecialType.System_MulticastDelegate:
                            break;

                        default:
                            // "'{0}': cannot specify both a constraint class and the 'class' or 'struct' constraint"
                            Error(diagnostics, ErrorCode.ERR_RefValBoundWithClass, syntax, type.Type);
                            return false;
                    }
                }
                else if (type.SpecialType != SpecialType.System_Enum)
                {
                    if ((constraints & TypeParameterConstraintKind.ValueType) != 0)
                    {
                        // "'{0}': cannot specify both a constraint class and the 'class' or 'struct' constraint"
                        Error(diagnostics, ErrorCode.ERR_RefValBoundWithClass, syntax, type.Type);
                        return false;
                    }
                    else if ((constraints & TypeParameterConstraintKind.Unmanaged) != 0)
                    {
                        // "'{0}': cannot specify both a constraint class and the 'unmanaged' constraint"
                        Error(diagnostics, ErrorCode.ERR_UnmanagedBoundWithClass, syntax, type.Type);
                        return false;
                    }
                }
            }

            return true;

            // Returns true if the type is a valid constraint type.
            // Otherwise returns false and generates a diagnostic.
            static bool isValidConstraintType(TypeParameterSymbol typeParameter, TypeConstraintSyntax syntax, TypeWithAnnotations typeWithAnnotations, bool performOnlyCycleSafeValidation, BindingDiagnosticBag diagnostics)
            {
                if (typeWithAnnotations.NullableAnnotation == NullableAnnotation.Annotated && performOnlyCycleSafeValidation &&
                    typeWithAnnotations.DefaultType is TypeParameterSymbol typeParameterInConstraint && typeParameterInConstraint.ContainingSymbol == (object)typeParameter.ContainingSymbol)
                {
                    return true;
                }

                TypeSymbol type = typeWithAnnotations.Type;

                switch (type.SpecialType)
                {
                    case SpecialType.System_Enum:
                        CheckFeatureAvailability(syntax, MessageID.IDS_FeatureEnumGenericTypeConstraint, diagnostics);
                        break;

                    case SpecialType.System_Delegate:
                    case SpecialType.System_MulticastDelegate:
                        CheckFeatureAvailability(syntax, MessageID.IDS_FeatureDelegateGenericTypeConstraint, diagnostics);
                        break;

                    case SpecialType.System_Object:
                    case SpecialType.System_ValueType:
                    case SpecialType.System_Array:
                        // "Constraint cannot be special class '{0}'"
                        Error(diagnostics, ErrorCode.ERR_SpecialTypeAsBound, syntax, type);
                        return false;
                }

                switch (type.TypeKind)
                {
                    case TypeKind.Error:
                    case TypeKind.TypeParameter:
                        return true;

                    case TypeKind.Interface:
                        break;

                    case TypeKind.Dynamic:
                        // "Constraint cannot be the dynamic type"
                        Error(diagnostics, ErrorCode.ERR_DynamicTypeAsBound, syntax);
                        return false;

                    case TypeKind.Class:
                        if (type.IsSealed)
                        {
                            goto case TypeKind.Struct;
                        }
                        else if (type.IsStatic)
                        {
                            // "'{0}': static classes cannot be used as constraints"
                            Error(diagnostics, ErrorCode.ERR_ConstraintIsStaticClass, syntax, type);
                            return false;
                        }
                        break;

                    case TypeKind.Delegate:
                    case TypeKind.Enum:
                    case TypeKind.Struct:
                        // "'{0}' is not a valid constraint. A type used as a constraint must be an interface, a non-sealed class or a type parameter."
                        Error(diagnostics, ErrorCode.ERR_BadBoundType, syntax, type);
                        return false;

                    case TypeKind.Array:
                    case TypeKind.Pointer:
                    case TypeKind.FunctionPointer:
                    case TypeKind.Extension:
                        // "Invalid constraint type. A type used as a constraint must be an interface, a non-sealed class or a type parameter."
                        Error(diagnostics, ErrorCode.ERR_BadConstraintType, syntax.GetLocation());
                        return false;

                    case TypeKind.Submission:
                    // script class is synthesized, never used as a constraint

                    default:
                        throw ExceptionUtilities.UnexpectedValue(type.TypeKind);
                }

                if (type.ContainsDynamic())
                {
                    // "Constraint cannot be a dynamic type '{0}'"
                    Error(diagnostics, ErrorCode.ERR_ConstructedDynamicTypeAsBound, syntax, type);
                    return false;
                }

                return true;
            }
        }
    }
}
