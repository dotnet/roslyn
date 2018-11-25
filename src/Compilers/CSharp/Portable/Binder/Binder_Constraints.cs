﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            SyntaxList<TypeParameterConstraintClauseSyntax> clauses,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(this.Flags.Includes(BinderFlags.GenericConstraintsClause));
            Debug.Assert((object)containingSymbol != null);
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
            var results = new TypeParameterConstraintClause[n];

            // Bind each clause and add to the results.
            foreach (var clause in clauses)
            {
                var name = clause.Name.Identifier.ValueText;
                int ordinal;
                if (names.TryGetValue(name, out ordinal))
                {
                    Debug.Assert(ordinal >= 0);
                    Debug.Assert(ordinal < n);

                    var constraintClause = this.BindTypeParameterConstraints(name, clause.Constraints, diagnostics);
                    if (results[ordinal] == null)
                    {
                        results[ordinal] = constraintClause;
                    }
                    else
                    {
                        // "A constraint clause has already been specified for type parameter '{0}'. ..."
                        diagnostics.Add(ErrorCode.ERR_DuplicateConstraintClause, clause.Name.Location, name);
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

            return results.AsImmutableOrNull();
        }

        /// <summary>
        /// Bind and return a single type parameter constraint clause.
        /// </summary>
        private TypeParameterConstraintClause BindTypeParameterConstraints(
            string name, SeparatedSyntaxList<TypeParameterConstraintSyntax> constraintsSyntax, DiagnosticBag diagnostics)
        {
            var constraints = TypeParameterConstraintKind.None;
            var constraintTypes = ArrayBuilder<TypeSymbol>.GetInstance();

            for (int i = 0, n = constraintsSyntax.Count; i < n; i++)
            {
                var syntax = constraintsSyntax[i];
                switch (syntax.Kind())
                {
                    case SyntaxKind.ClassConstraint:
                        if (i != 0)
                        {
                            diagnostics.Add(ErrorCode.ERR_RefValBoundMustBeFirst, syntax.GetFirstToken().GetLocation());
                        }

                        constraints |= TypeParameterConstraintKind.ReferenceType;
                        continue;
                    case SyntaxKind.StructConstraint:
                        if (i != 0)
                        {
                            diagnostics.Add(ErrorCode.ERR_RefValBoundMustBeFirst, syntax.GetFirstToken().GetLocation());
                        }

                        constraints |= TypeParameterConstraintKind.ValueType;
                        continue;
                    case SyntaxKind.ConstructorConstraint:
                        if ((constraints & TypeParameterConstraintKind.ValueType) != 0)
                        {
                            diagnostics.Add(ErrorCode.ERR_NewBoundWithVal, syntax.GetFirstToken().GetLocation());
                        }
                        if ((constraints & TypeParameterConstraintKind.Unmanaged) != 0)
                        {
                            diagnostics.Add(ErrorCode.ERR_NewBoundWithUnmanaged, syntax.GetFirstToken().GetLocation());
                        }

                        if (i != n - 1)
                        {
                            diagnostics.Add(ErrorCode.ERR_NewBoundMustBeLast, syntax.GetFirstToken().GetLocation());
                        }

                        constraints |= TypeParameterConstraintKind.Constructor;
                        continue;
                    case SyntaxKind.TypeConstraint:
                        {
                            var typeConstraintSyntax = (TypeConstraintSyntax)syntax;
                            var typeSyntax = typeConstraintSyntax.Type;
                            var typeSyntaxKind = typeSyntax.Kind();

                            // For pointer types, don't report this error. It is already reported during binding typeSyntax below.
                            if (typeSyntaxKind != SyntaxKind.PredefinedType && typeSyntaxKind != SyntaxKind.PointerType && !SyntaxFacts.IsName(typeSyntax.Kind()))
                            {
                                diagnostics.Add(ErrorCode.ERR_BadConstraintType, typeSyntax.GetLocation());
                            }

                            var type = BindTypeOrUnmanagedKeyword(typeSyntax, diagnostics, out var isUnmanaged);

                            if (isUnmanaged)
                            {
                                if (constraints != 0 || constraintTypes.Any())
                                {
                                    diagnostics.Add(ErrorCode.ERR_UnmanagedConstraintMustBeFirst, typeSyntax.GetLocation());
                                    continue;
                                }

                                // This should produce diagnostics if the types are missing
                                GetWellKnownType(WellKnownType.System_Runtime_InteropServices_UnmanagedType, diagnostics, typeSyntax);
                                GetSpecialType(SpecialType.System_ValueType, diagnostics, typeSyntax);

                                constraints |= TypeParameterConstraintKind.Unmanaged;
                                continue;
                            }
                            else
                            {
                                // Only valid constraint types are included in ConstraintTypes
                                // since, in general, it may be difficult to support all invalid types.
                                // In the future, we may want to include some invalid types
                                // though so the public binding API has the most information.
                                if (!IsValidConstraintType(typeConstraintSyntax, type, diagnostics))
                                {
                                    continue;
                                }

                                if (constraintTypes.Contains(type))
                                {
                                    // "Duplicate constraint '{0}' for type parameter '{1}'"
                                    Error(diagnostics, ErrorCode.ERR_DuplicateBound, syntax, type, name);
                                    continue;
                                }

                                if (type.TypeKind == TypeKind.Class)
                                {
                                    // If there is already a struct or class constraint (class constraint could be
                                    // 'class' or explicit type), report an error and drop this class. If we don't
                                    // drop this additional class, we may end up with conflicting class constraints.

                                    if (constraintTypes.Count > 0)
                                    {
                                        // "The class type constraint '{0}' must come before any other constraints"
                                        Error(diagnostics, ErrorCode.ERR_ClassBoundNotFirst, syntax, type);
                                        continue;
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
                                                Error(diagnostics, ErrorCode.ERR_RefValBoundWithClass, syntax, type);
                                                continue;
                                        }
                                    }
                                    else if (type.SpecialType != SpecialType.System_Enum)
                                    {
                                        if ((constraints & TypeParameterConstraintKind.ValueType) != 0)
                                        {
                                            // "'{0}': cannot specify both a constraint class and the 'class' or 'struct' constraint"
                                            Error(diagnostics, ErrorCode.ERR_RefValBoundWithClass, syntax, type);
                                            continue;
                                        }
                                        else if ((constraints & TypeParameterConstraintKind.Unmanaged) != 0)
                                        {
                                            // "'{0}': cannot specify both a constraint class and the 'unmanaged' constraint"
                                            Error(diagnostics, ErrorCode.ERR_UnmanagedBoundWithClass, syntax, type);
                                            continue;
                                        }
                                    }
                                }
                            }

                            constraintTypes.Add(type);
                        }
                        continue;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(syntax.Kind());
                }
            }

            return new TypeParameterConstraintClause(constraints, constraintTypes.ToImmutableAndFree());
        }

        /// <summary>
        /// Returns true if the type is a valid constraint type.
        /// Otherwise returns false and generates a diagnostic.
        /// </summary>
        private static bool IsValidConstraintType(TypeConstraintSyntax syntax, TypeSymbol type, DiagnosticBag diagnostics)
        {
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
                    // CS0706 already reported by parser.
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
