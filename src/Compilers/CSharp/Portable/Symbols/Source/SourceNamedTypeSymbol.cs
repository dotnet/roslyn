// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // This is a type symbol associated with a type definition in source code.
    // That is, for a generic type C<T> this is the instance type C<T>.  
    internal sealed partial class SourceNamedTypeSymbol : SourceMemberContainerTypeSymbol, IAttributeTargetSymbol
    {
        private readonly TypeParameterInfo _typeParameterInfo;

        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;

        private string _lazyDocComment;
        private string _lazyExpandedDocComment;

        private ThreeState _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.Unknown;

        protected override Location GetCorrespondingBaseListLocation(NamedTypeSymbol @base)
        {
            Location backupLocation = null;

            foreach (SyntaxReference part in SyntaxReferences)
            {
                TypeDeclarationSyntax typeBlock = (TypeDeclarationSyntax)part.GetSyntax();
                BaseListSyntax bases = typeBlock.BaseList;
                if (bases == null)
                {
                    continue;
                }
                SeparatedSyntaxList<BaseTypeSyntax> inheritedTypeDecls = bases.Types;

                var baseBinder = this.DeclaringCompilation.GetBinder(bases);
                baseBinder = baseBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

                if ((object)backupLocation == null)
                {
                    backupLocation = inheritedTypeDecls[0].Type.GetLocation();
                }

                foreach (BaseTypeSyntax baseTypeSyntax in inheritedTypeDecls)
                {
                    TypeSyntax t = baseTypeSyntax.Type;
                    TypeSymbol bt = baseBinder.BindType(t, BindingDiagnosticBag.Discarded).Type;

                    if (TypeSymbol.Equals(bt, @base, TypeCompareKind.ConsiderEverything2))
                    {
                        return t.GetLocation();
                    }
                }
            }

            return backupLocation;
        }

        internal SourceNamedTypeSymbol(NamespaceOrTypeSymbol containingSymbol, MergedTypeDeclaration declaration, BindingDiagnosticBag diagnostics, TupleExtraData tupleData = null)
            : base(containingSymbol, declaration, diagnostics, tupleData)
        {
            switch (declaration.Kind)
            {
                case DeclarationKind.Struct:
                case DeclarationKind.Interface:
                case DeclarationKind.Enum:
                case DeclarationKind.Delegate:
                case DeclarationKind.Class:
                case DeclarationKind.Record:
                case DeclarationKind.RecordStruct:
                case DeclarationKind.Extension:
                    break;
                default:
                    Debug.Assert(false, "bad declaration kind");
                    break;
            }

            if (containingSymbol.Kind == SymbolKind.NamedType)
            {
                // Nested types are never unified.
                _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.False;
            }

            _typeParameterInfo = declaration.Arity == 0
                ? TypeParameterInfo.Empty
                : new TypeParameterInfo();
        }

        protected override NamedTypeSymbol WithTupleDataCore(TupleExtraData newData)
        {
            return new SourceNamedTypeSymbol(ContainingType, declaration, BindingDiagnosticBag.Discarded, newData);
        }

        #region Syntax

        private static SyntaxToken GetName(CSharpSyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.EnumDeclaration:
                    return ((EnumDeclarationSyntax)node).Identifier;
                case SyntaxKind.DelegateDeclaration:
                    return ((DelegateDeclarationSyntax)node).Identifier;
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                    return ((BaseTypeDeclarationSyntax)node).Identifier;
                default:
                    return default(SyntaxToken);
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            ref var lazyDocComment = ref expandIncludes ? ref _lazyExpandedDocComment : ref _lazyDocComment;
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref lazyDocComment);
        }

        #endregion

        #region Type Parameters

        private ImmutableArray<TypeParameterSymbol> MakeTypeParameters(BindingDiagnosticBag diagnostics)
        {
            if (declaration.Arity == 0)
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }

            var typeParameterMismatchReported = false;
            var typeParameterNames = new string[declaration.Arity];
            var typeParameterVarianceKeywords = new string[declaration.Arity];
            var parameterBuilders1 = new List<List<TypeParameterBuilder>>();

            foreach (var syntaxRef in this.SyntaxReferences)
            {
                var typeDecl = (CSharpSyntaxNode)syntaxRef.GetSyntax();
                var syntaxTree = syntaxRef.SyntaxTree;

                TypeParameterListSyntax tpl;
                SyntaxKind typeKind = typeDecl.Kind();
                switch (typeKind)
                {
                    case SyntaxKind.ClassDeclaration:
                    case SyntaxKind.StructDeclaration:
                    case SyntaxKind.InterfaceDeclaration:
                    case SyntaxKind.RecordDeclaration:
                    case SyntaxKind.RecordStructDeclaration:
                    case SyntaxKind.ExtensionBlockDeclaration:
                        tpl = ((TypeDeclarationSyntax)typeDecl).TypeParameterList;
                        break;

                    case SyntaxKind.DelegateDeclaration:
                        tpl = ((DelegateDeclarationSyntax)typeDecl).TypeParameterList;
                        break;

                    case SyntaxKind.EnumDeclaration:
                    default:
                        // there is no such thing as a generic enum, so code should never reach here.
                        throw ExceptionUtilities.UnexpectedValue(typeDecl.Kind());
                }

                MessageID.IDS_FeatureGenerics.CheckFeatureAvailability(diagnostics, tpl.LessThanToken);

                bool isInterfaceOrDelegate = typeKind == SyntaxKind.InterfaceDeclaration || typeKind == SyntaxKind.DelegateDeclaration;
                var parameterBuilder = new List<TypeParameterBuilder>();
                parameterBuilders1.Add(parameterBuilder);
                int i = 0;
                foreach (var tp in tpl.Parameters)
                {
                    if (tp.VarianceKeyword.Kind() != SyntaxKind.None)
                    {
                        if (!isInterfaceOrDelegate)
                        {
                            diagnostics.Add(ErrorCode.ERR_IllegalVarianceSyntax, tp.VarianceKeyword.GetLocation());
                        }
                        else
                        {
                            MessageID.IDS_FeatureTypeVariance.CheckFeatureAvailability(diagnostics, tp.VarianceKeyword);
                        }
                    }

                    var name = typeParameterNames[i];
                    var location = new SourceLocation(tp.Identifier);
                    var varianceKind = typeParameterVarianceKeywords[i];

                    ReportReservedTypeName(tp.Identifier.Text, this.DeclaringCompilation, diagnostics.DiagnosticBag, location);

                    if (name == null)
                    {
                        name = typeParameterNames[i] = tp.Identifier.ValueText;
                        varianceKind = typeParameterVarianceKeywords[i] = tp.VarianceKeyword.ValueText;
                        for (int j = 0; j < i; j++)
                        {
                            if (name == typeParameterNames[j])
                            {
                                typeParameterMismatchReported = true;
                                diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                                goto next;
                            }
                        }

                        if (!ReferenceEquals(ContainingType, null))
                        {
                            var tpEnclosing = ContainingType.FindEnclosingTypeParameter(name);
                            if ((object)tpEnclosing != null)
                            {
                                // Type parameter '{0}' has the same name as the type parameter from outer type '{1}'
                                diagnostics.Add(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, location, name, tpEnclosing.ContainingType);
                            }
                        }
next:;
                    }
                    else if (!typeParameterMismatchReported)
                    {
                        // Note: the "this", below, refers to the name of the current class, which includes its type
                        // parameter names.  But the type parameter names have not been computed yet.  Therefore, we
                        // take advantage of the fact that "this" won't undergo "ToString()" until later, when the
                        // diagnostic is printed, by which time the type parameters field will have been filled in.
                        if (varianceKind != tp.VarianceKeyword.ValueText)
                        {
                            // Dev10 reports CS1067, even if names also don't match
                            typeParameterMismatchReported = true;
                            diagnostics.Add(
                                ErrorCode.ERR_PartialWrongTypeParamsVariance,
                                declaration.NameLocations.First(),
                                this); // see comment above
                        }
                        else if (name != tp.Identifier.ValueText)
                        {
                            typeParameterMismatchReported = true;
                            diagnostics.Add(
                                ErrorCode.ERR_PartialWrongTypeParams,
                                declaration.NameLocations.First(),
                                this); // see comment above
                        }
                    }
                    parameterBuilder.Add(new TypeParameterBuilder(syntaxTree.GetReference(tp), this, location));
                    i++;
                }
            }

            var parameterBuilders2 = parameterBuilders1.Transpose(); // type arguments are positional
            var parameters = parameterBuilders2.Select((builders, i) => builders[0].MakeSymbol(i, builders, diagnostics));
            return parameters.AsImmutable();
        }

        /// <summary>
        /// Returns the constraint types for the given type parameter.
        /// </summary>
        internal ImmutableArray<TypeWithAnnotations> GetTypeParameterConstraintTypes(int ordinal)
        {
            var constraintTypes = GetTypeParameterConstraintTypes();
            return (constraintTypes.Length > 0) ? constraintTypes[ordinal] : ImmutableArray<TypeWithAnnotations>.Empty;
        }

        private ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
        {
            if (_typeParameterInfo.LazyTypeParameterConstraintTypes.IsDefault)
            {
                GetTypeParameterConstraintKinds();

                var diagnostics = BindingDiagnosticBag.GetInstance();
                if (ImmutableInterlocked.InterlockedInitialize(
                        ref _typeParameterInfo.LazyTypeParameterConstraintTypes,
                        MakeTypeParameterConstraintTypes(diagnostics)))
                {
                    this.AddDeclarationDiagnostics(diagnostics);
                }
                diagnostics.Free();
            }

            Debug.Assert(!_typeParameterInfo.LazyTypeParameterConstraintTypes.IsDefault);
            return _typeParameterInfo.LazyTypeParameterConstraintTypes;
        }

        /// <summary>
        /// Returns the constraint kind for the given type parameter.
        /// </summary>
        internal TypeParameterConstraintKind GetTypeParameterConstraintKind(int ordinal)
        {
            var constraintKinds = GetTypeParameterConstraintKinds();
            return (constraintKinds.Length > 0) ? constraintKinds[ordinal] : TypeParameterConstraintKind.None;
        }

        private ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
        {
            if (_typeParameterInfo.LazyTypeParameterConstraintKinds.IsDefault)
            {
                ImmutableInterlocked.InterlockedInitialize(
                    ref _typeParameterInfo.LazyTypeParameterConstraintKinds,
                    MakeTypeParameterConstraintKinds());
            }

            Debug.Assert(!_typeParameterInfo.LazyTypeParameterConstraintKinds.IsDefault);
            return _typeParameterInfo.LazyTypeParameterConstraintKinds;
        }

        private ImmutableArray<ImmutableArray<TypeWithAnnotations>> MakeTypeParameterConstraintTypes(BindingDiagnosticBag diagnostics)
        {
            var typeParameters = this.TypeParameters;
            var results = ImmutableArray<TypeParameterConstraintClause>.Empty;

            int arity = typeParameters.Length;
            if (arity > 0)
            {
                bool skipPartialDeclarationsWithoutConstraintClauses = SkipPartialDeclarationsWithoutConstraintClauses();
                ArrayBuilder<ImmutableArray<TypeParameterConstraintClause>> otherPartialClauses = null;

                foreach (var decl in declaration.Declarations)
                {
                    var syntaxRef = decl.SyntaxReference;
                    var constraintClauses = GetConstraintClauses((CSharpSyntaxNode)syntaxRef.GetSyntax(), out TypeParameterListSyntax typeParameterList);

                    if (skipPartialDeclarationsWithoutConstraintClauses && constraintClauses.Count == 0)
                    {
                        continue;
                    }

                    var binderFactory = this.DeclaringCompilation.GetBinderFactory(syntaxRef.SyntaxTree);
                    Binder binder;
                    ImmutableArray<TypeParameterConstraintClause> constraints;

                    if (constraintClauses.Count == 0)
                    {
                        binder = binderFactory.GetBinder(typeParameterList.Parameters[0]);

                        constraints = binder.GetDefaultTypeParameterConstraintClauses(typeParameterList);
                    }
                    else
                    {
                        binder = binderFactory.GetBinder(constraintClauses[0]);

                        // Wrap binder from factory in a generic constraints specific binder 
                        // to avoid checking constraints when binding type names.
                        Debug.Assert(!binder.Flags.Includes(BinderFlags.GenericConstraintsClause));
                        binder = binder.WithContainingMemberOrLambda(this).WithAdditionalFlags(BinderFlags.GenericConstraintsClause | BinderFlags.SuppressConstraintChecks);

                        constraints = binder.BindTypeParameterConstraintClauses(this, typeParameters, typeParameterList, constraintClauses, diagnostics, performOnlyCycleSafeValidation: false);
                    }

                    Debug.Assert(constraints.Length == arity);

                    if (results.Length == 0)
                    {
                        results = constraints;
                    }
                    else
                    {
                        (otherPartialClauses ??= ArrayBuilder<ImmutableArray<TypeParameterConstraintClause>>.GetInstance()).Add(constraints);
                    }
                }

                results = MergeConstraintTypesForPartialDeclarations(results, otherPartialClauses, diagnostics);

                if (results.All(clause => clause.ConstraintTypes.IsEmpty))
                {
                    results = ImmutableArray<TypeParameterConstraintClause>.Empty;
                }

                otherPartialClauses?.Free();
            }

            return results.SelectAsArray(clause => clause.ConstraintTypes);
        }

        private bool SkipPartialDeclarationsWithoutConstraintClauses()
        {
            foreach (var decl in declaration.Declarations)
            {
                if (GetConstraintClauses((CSharpSyntaxNode)decl.SyntaxReference.GetSyntax(), out _).Count != 0)
                {
                    return true;
                }
            }

            return false;
        }

        private ImmutableArray<TypeParameterConstraintKind> MakeTypeParameterConstraintKinds()
        {
            var typeParameters = this.TypeParameters;
            var results = ImmutableArray<TypeParameterConstraintClause>.Empty;

            int arity = typeParameters.Length;
            if (arity > 0)
            {
                bool skipPartialDeclarationsWithoutConstraintClauses = SkipPartialDeclarationsWithoutConstraintClauses();
                ArrayBuilder<ImmutableArray<TypeParameterConstraintClause>> otherPartialClauses = null;

                foreach (var decl in declaration.Declarations)
                {
                    var syntaxRef = decl.SyntaxReference;
                    var constraintClauses = GetConstraintClauses((CSharpSyntaxNode)syntaxRef.GetSyntax(), out TypeParameterListSyntax typeParameterList);

                    if (skipPartialDeclarationsWithoutConstraintClauses && constraintClauses.Count == 0)
                    {
                        continue;
                    }

                    var binderFactory = this.DeclaringCompilation.GetBinderFactory(syntaxRef.SyntaxTree);
                    Binder binder;
                    ImmutableArray<TypeParameterConstraintClause> constraints;

                    if (constraintClauses.Count == 0)
                    {
                        binder = binderFactory.GetBinder(typeParameterList.Parameters[0]);
                        constraints = binder.GetDefaultTypeParameterConstraintClauses(typeParameterList);
                    }
                    else
                    {
                        binder = binderFactory.GetBinder(constraintClauses[0]);

                        // Wrap binder from factory in a generic constraints specific binder 
                        // to avoid checking constraints when binding type names.
                        // Also, suppress type argument binding in constraint types, this helps to avoid cycles while we figure out constraint kinds. 
                        Debug.Assert(!binder.Flags.Includes(BinderFlags.GenericConstraintsClause));
                        binder = binder.WithContainingMemberOrLambda(this).WithAdditionalFlags(BinderFlags.GenericConstraintsClause | BinderFlags.SuppressConstraintChecks | BinderFlags.SuppressTypeArgumentBinding);

                        // We will recompute this diagnostics more accurately later, when binding without BinderFlags.SuppressTypeArgumentBinding  
                        constraints = binder.BindTypeParameterConstraintClauses(this, typeParameters, typeParameterList, constraintClauses, BindingDiagnosticBag.Discarded, performOnlyCycleSafeValidation: true);
                    }

                    Debug.Assert(constraints.Length == arity);

                    if (results.Length == 0)
                    {
                        results = constraints;
                    }
                    else
                    {
                        (otherPartialClauses ??= ArrayBuilder<ImmutableArray<TypeParameterConstraintClause>>.GetInstance()).Add(constraints);
                    }
                }

                results = MergeConstraintKindsForPartialDeclarations(results, otherPartialClauses);
                results = ConstraintsHelper.AdjustConstraintKindsBasedOnConstraintTypes(typeParameters, results);

                if (results.All(clause => clause.Constraints == TypeParameterConstraintKind.None))
                {
                    results = ImmutableArray<TypeParameterConstraintClause>.Empty;
                }

                otherPartialClauses?.Free();
            }

            return results.SelectAsArray(clause => clause.Constraints);
        }

        private static SyntaxList<TypeParameterConstraintClauseSyntax> GetConstraintClauses(CSharpSyntaxNode node, out TypeParameterListSyntax typeParameterList)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                case SyntaxKind.ExtensionBlockDeclaration:
                    var typeDeclaration = (TypeDeclarationSyntax)node;
                    typeParameterList = typeDeclaration.TypeParameterList;
                    return typeDeclaration.ConstraintClauses;
                case SyntaxKind.DelegateDeclaration:
                    var delegateDeclaration = (DelegateDeclarationSyntax)node;
                    typeParameterList = delegateDeclaration.TypeParameterList;
                    return delegateDeclaration.ConstraintClauses;
                default:
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
            }
        }

        /// <summary>
        /// Note, only nullability aspects are merged if possible, other mismatches are treated as failures.
        /// </summary>
        private ImmutableArray<TypeParameterConstraintClause> MergeConstraintTypesForPartialDeclarations(ImmutableArray<TypeParameterConstraintClause> constraintClauses,
                                                                                                         ArrayBuilder<ImmutableArray<TypeParameterConstraintClause>> otherPartialClauses,
                                                                                                         BindingDiagnosticBag diagnostics)
        {
            if (otherPartialClauses == null)
            {
                return constraintClauses;
            }

            ArrayBuilder<TypeParameterConstraintClause> builder = null;
            var typeParameters = TypeParameters;
            int arity = typeParameters.Length;

            Debug.Assert(constraintClauses.Length == arity);

            for (int i = 0; i < arity; i++)
            {
                var constraint = constraintClauses[i];

                ImmutableArray<TypeWithAnnotations> originalConstraintTypes = constraint.ConstraintTypes;
                ArrayBuilder<TypeWithAnnotations> mergedConstraintTypes = null;
                SmallDictionary<TypeWithAnnotations, int> originalConstraintTypesMap = null;

                // Constraints defined on multiple partial declarations.
                // Report any mismatched constraints.
                bool report = (GetTypeParameterConstraintKind(i) & TypeParameterConstraintKind.PartialMismatch) != 0;
                foreach (ImmutableArray<TypeParameterConstraintClause> otherPartialConstraints in otherPartialClauses)
                {
                    if (!mergeConstraints(originalConstraintTypes, ref originalConstraintTypesMap, ref mergedConstraintTypes, otherPartialConstraints[i]))
                    {
                        report = true;
                    }
                }

                if (report)
                {
                    // "Partial declarations of '{0}' have inconsistent constraints for type parameter '{1}'"
                    diagnostics.Add(ErrorCode.ERR_PartialWrongConstraints, GetFirstLocation(), this, typeParameters[i]);
                }

                if (mergedConstraintTypes != null)
                {
#if DEBUG
                    Debug.Assert(originalConstraintTypes.Length == mergedConstraintTypes.Count);

                    for (int j = 0; j < originalConstraintTypes.Length; j++)
                    {
                        Debug.Assert(originalConstraintTypes[j].Equals(mergedConstraintTypes[j], TypeCompareKind.ObliviousNullableModifierMatchesAny));
                    }
#endif
                    if (builder == null)
                    {
                        builder = ArrayBuilder<TypeParameterConstraintClause>.GetInstance(constraintClauses.Length);
                        builder.AddRange(constraintClauses);
                    }

                    builder[i] = TypeParameterConstraintClause.Create(constraint.Constraints,
                                                                      mergedConstraintTypes?.ToImmutableAndFree() ?? originalConstraintTypes);
                }
            }

            if (builder != null)
            {
                constraintClauses = builder.ToImmutableAndFree();
            }

            return constraintClauses;

            static bool mergeConstraints(ImmutableArray<TypeWithAnnotations> originalConstraintTypes,
                                         ref SmallDictionary<TypeWithAnnotations, int> originalConstraintTypesMap, ref ArrayBuilder<TypeWithAnnotations> mergedConstraintTypes,
                                         TypeParameterConstraintClause clause)
            {
                bool result = true;

                if (originalConstraintTypes.Length == 0)
                {
                    if (clause.ConstraintTypes.Length == 0)
                    {
                        return result;
                    }

                    return false;
                }
                else if (clause.ConstraintTypes.Length == 0)
                {
                    return false;
                }

                originalConstraintTypesMap ??= toDictionary(originalConstraintTypes,
                                                            TypeWithAnnotations.EqualsComparer.IgnoreNullableModifiersForReferenceTypesComparer);
                SmallDictionary<TypeWithAnnotations, int> clauseConstraintTypesMap = toDictionary(clause.ConstraintTypes, originalConstraintTypesMap.Comparer);

                foreach (int index1 in originalConstraintTypesMap.Values)
                {
                    TypeWithAnnotations constraintType1 = mergedConstraintTypes?[index1] ?? originalConstraintTypes[index1];
                    int index2;

                    if (!clauseConstraintTypesMap.TryGetValue(constraintType1, out index2))
                    {
                        // No matching type
                        result = false;
                        continue;
                    }

                    TypeWithAnnotations constraintType2 = clause.ConstraintTypes[index2];

                    if (!constraintType1.Equals(constraintType2, TypeCompareKind.ObliviousNullableModifierMatchesAny))
                    {
                        // Nullability mismatch that doesn't involve oblivious
                        result = false;
                        continue;
                    }

                    if (!constraintType1.Equals(constraintType2, TypeCompareKind.ConsiderEverything))
                    {
                        // Mismatch with oblivious, merge
                        if (mergedConstraintTypes == null)
                        {
                            mergedConstraintTypes = ArrayBuilder<TypeWithAnnotations>.GetInstance(originalConstraintTypes.Length);
                            mergedConstraintTypes.AddRange(originalConstraintTypes);
                        }

                        mergedConstraintTypes[index1] = constraintType1.MergeEquivalentTypes(constraintType2, VarianceKind.None);
                    }
                }

                foreach (var constraintType in clauseConstraintTypesMap.Keys)
                {
                    if (!originalConstraintTypesMap.ContainsKey(constraintType))
                    {
                        result = false;
                        break;
                    }
                }

                return result;
            }

            static SmallDictionary<TypeWithAnnotations, int> toDictionary(ImmutableArray<TypeWithAnnotations> constraintTypes, IEqualityComparer<TypeWithAnnotations> comparer)
            {
                var result = new SmallDictionary<TypeWithAnnotations, int>(comparer);

                for (int i = constraintTypes.Length - 1; i >= 0; i--)
                {
                    result[constraintTypes[i]] = i; // Use the first type among the duplicates as the source of the nullable information
                }

                return result;
            }
        }

        /// <summary>
        /// Note, only nullability aspects are merged if possible, other mismatches are treated as failures.
        /// </summary>
        private ImmutableArray<TypeParameterConstraintClause> MergeConstraintKindsForPartialDeclarations(ImmutableArray<TypeParameterConstraintClause> constraintClauses,
                                                                                                         ArrayBuilder<ImmutableArray<TypeParameterConstraintClause>> otherPartialClauses)
        {
            if (otherPartialClauses == null)
            {
                return constraintClauses;
            }

            ArrayBuilder<TypeParameterConstraintClause> builder = null;
            var typeParameters = TypeParameters;
            int arity = typeParameters.Length;

            Debug.Assert(constraintClauses.Length == arity);

            for (int i = 0; i < arity; i++)
            {
                var constraint = constraintClauses[i];

                TypeParameterConstraintKind mergedKind = constraint.Constraints;
                ImmutableArray<TypeWithAnnotations> originalConstraintTypes = constraint.ConstraintTypes;

                foreach (ImmutableArray<TypeParameterConstraintClause> otherPartialConstraints in otherPartialClauses)
                {
                    mergeConstraints(ref mergedKind, originalConstraintTypes, otherPartialConstraints[i]);
                }

                if (constraint.Constraints != mergedKind)
                {
                    Debug.Assert((constraint.Constraints & (TypeParameterConstraintKind.AllNonNullableKinds | TypeParameterConstraintKind.NotNull)) ==
                                 (mergedKind & (TypeParameterConstraintKind.AllNonNullableKinds | TypeParameterConstraintKind.NotNull)));
                    Debug.Assert((mergedKind & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) == 0 ||
                                 (constraint.Constraints & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) != 0);
                    Debug.Assert((constraint.Constraints & TypeParameterConstraintKind.AllReferenceTypeKinds) == (mergedKind & TypeParameterConstraintKind.AllReferenceTypeKinds) ||
                                 (constraint.Constraints & TypeParameterConstraintKind.AllReferenceTypeKinds) == TypeParameterConstraintKind.ReferenceType);

                    if (builder == null)
                    {
                        builder = ArrayBuilder<TypeParameterConstraintClause>.GetInstance(constraintClauses.Length);
                        builder.AddRange(constraintClauses);
                    }

                    builder[i] = TypeParameterConstraintClause.Create(mergedKind, originalConstraintTypes);
                }
            }

            if (builder != null)
            {
                constraintClauses = builder.ToImmutableAndFree();
            }

            return constraintClauses;

            static void mergeConstraints(ref TypeParameterConstraintKind mergedKind, ImmutableArray<TypeWithAnnotations> originalConstraintTypes, TypeParameterConstraintClause clause)
            {
                if ((mergedKind & (TypeParameterConstraintKind.AllNonNullableKinds | TypeParameterConstraintKind.NotNull)) != (clause.Constraints & (TypeParameterConstraintKind.AllNonNullableKinds | TypeParameterConstraintKind.NotNull)))
                {
                    mergedKind |= TypeParameterConstraintKind.PartialMismatch;
                }

                if ((mergedKind & TypeParameterConstraintKind.ReferenceType) != 0 && (clause.Constraints & TypeParameterConstraintKind.ReferenceType) != 0)
                {
                    // Try merging nullability of a 'class' constraint
                    TypeParameterConstraintKind clause1Constraints = mergedKind & TypeParameterConstraintKind.AllReferenceTypeKinds;
                    TypeParameterConstraintKind clause2Constraints = clause.Constraints & TypeParameterConstraintKind.AllReferenceTypeKinds;
                    if (clause1Constraints != clause2Constraints)
                    {
                        if (clause1Constraints == TypeParameterConstraintKind.ReferenceType) // Oblivious
                        {
                            // Take nullability from clause2
                            mergedKind = (mergedKind & (~TypeParameterConstraintKind.AllReferenceTypeKinds)) | clause2Constraints;
                        }
                        else if (clause2Constraints != TypeParameterConstraintKind.ReferenceType)
                        {
                            // Neither nullability is oblivious and they do not match. Cannot merge.
                            mergedKind |= TypeParameterConstraintKind.PartialMismatch;
                        }
                    }
                }

                if (originalConstraintTypes.Length == 0 && clause.ConstraintTypes.Length == 0)
                {
                    // Try merging nullability of implied 'object' constraint
                    if (((mergedKind | clause.Constraints) & ~(TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType | TypeParameterConstraintKind.Constructor)) == 0 &&
                        (mergedKind & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) != 0 && // 'object~'
                        (clause.Constraints & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) == 0)   // 'object?' 
                    {
                        // Merged value is 'object?'
                        mergedKind &= ~TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType;
                    }
                }
            }
        }

        internal sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotationsNoUseSiteDiagnostics
        {
            get
            {
                return GetTypeParametersAsTypeArguments();
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                if (_typeParameterInfo.LazyTypeParameters.IsDefault)
                {
                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    if (ImmutableInterlocked.InterlockedInitialize(
                            ref _typeParameterInfo.LazyTypeParameters,
                            MakeTypeParameters(diagnostics)))
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }

                    diagnostics.Free();
                }

                return _typeParameterInfo.LazyTypeParameters;
            }
        }

        #endregion

        #region Attributes

        /// <summary>
        /// Gets all the attribute lists for this named type.  If <paramref name="quickAttributes"/> is provided
        /// the attribute lists will only be returned if there is reasonable belief that 
        /// the type has one of the attributes specified by <paramref name="quickAttributes"/> on it.
        /// This can avoid going back to syntax if we know the type definitely doesn't have an attribute
        /// on it that could be the one specified by <paramref name="quickAttributes"/>. Pass <see langword="null"/>
        /// to get all attribute declarations.
        /// </summary>
        internal ImmutableArray<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations(QuickAttributes? quickAttributes = null)
        {
            // if the compilation has any global aliases to these quick attributes, then we have to return
            // all the attributes on the decl.  For example, if there is a `global using X = Y;` and 
            // then we have to return any attributes on the type as they might say `[X]`.
            if (quickAttributes != null)
            {
                foreach (var decl in this.DeclaringCompilation.MergedRootDeclaration.Declarations)
                {
                    if (decl is RootSingleNamespaceDeclaration rootNamespaceDecl &&
                        (rootNamespaceDecl.GlobalAliasedQuickAttributes & quickAttributes) != 0)
                    {
                        return declaration.GetAttributeDeclarations(quickAttributes: null);
                    }
                }
            }

            return declaration.GetAttributeDeclarations(quickAttributes);
        }

        IAttributeTargetSymbol IAttributeTargetSymbol.AttributesOwner
        {
            get { return this; }
        }

        AttributeLocation IAttributeTargetSymbol.DefaultAttributeLocation
        {
            get { return AttributeLocation.Type; }
        }

        AttributeLocation IAttributeTargetSymbol.AllowedAttributeLocations
        {
            get
            {
                switch (TypeKind)
                {
                    case TypeKind.Delegate:
                        return AttributeLocation.Type | AttributeLocation.Return;

                    case TypeKind.Enum:
                    case TypeKind.Interface:
                        return AttributeLocation.Type;

                    case TypeKind.Struct:
                    case TypeKind.Class:
                        return AttributeLocation.Type | (HasPrimaryConstructor ? AttributeLocation.Method : 0);

                    default:
                        return AttributeLocation.None;
                }
            }
        }

        /// <summary>
        /// Returns a bag of applied custom attributes and data decoded from well-known attributes. Returns null if there are no attributes applied on the symbol.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private CustomAttributesBag<CSharpAttributeData> GetAttributesBag()
        {
            var bag = _lazyCustomAttributesBag;
            if (bag != null && bag.IsSealed)
            {
                return bag;
            }

            if (LoadAndValidateAttributes(OneOrMany.Create(this.GetAttributeDeclarations()), ref _lazyCustomAttributesBag))
            {
                var completed = state.NotePartComplete(CompletionPart.Attributes);
                Debug.Assert(completed);
            }

            Debug.Assert(_lazyCustomAttributesBag.IsSealed);
            return _lazyCustomAttributesBag;
        }

        /// <summary>
        /// Gets the attributes applied on this symbol.
        /// Returns an empty array if there are no attributes.
        /// </summary>
        public sealed override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return this.GetAttributesBag().Attributes;
        }

        /// <summary>
        /// Returns data decoded from well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        private TypeWellKnownAttributeData GetDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (TypeWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
        }

#nullable enable
        /// <summary>
        /// Returns data decoded from special early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal TypeEarlyWellKnownAttributeData? GetEarlyDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (TypeEarlyWellKnownAttributeData)attributesBag.EarlyDecodedWellKnownAttributeData;
        }

        internal override (CSharpAttributeData?, BoundAttribute?) EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            bool hasAnyDiagnostics;
            CSharpAttributeData? attributeData;
            BoundAttribute? boundAttribute;

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ComImportAttribute))
            {
                (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
                if (!attributeData.HasErrors)
                {
                    arguments.GetOrCreateData<TypeEarlyWellKnownAttributeData>().HasComImportAttribute = true;
                    if (!hasAnyDiagnostics)
                    {
                        return (attributeData, boundAttribute);
                    }
                }

                return (null, null);
            }

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CodeAnalysisEmbeddedAttribute))
            {
                (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
                if (!attributeData.HasErrors)
                {
                    arguments.GetOrCreateData<TypeEarlyWellKnownAttributeData>().HasCodeAnalysisEmbeddedAttribute = true;
                    if (!hasAnyDiagnostics)
                    {
                        return (attributeData, boundAttribute);
                    }
                }

                return (null, null);
            }

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ConditionalAttribute))
            {
                (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
                if (!attributeData.HasErrors)
                {
                    string? name = attributeData.GetConstructorArgument<string>(0, SpecialType.System_String);
                    arguments.GetOrCreateData<TypeEarlyWellKnownAttributeData>().AddConditionalSymbol(name);
                    if (!hasAnyDiagnostics)
                    {
                        return (attributeData, boundAttribute);
                    }
                }

                return (null, null);
            }

            ObsoleteAttributeData? obsoleteData;
            if (EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(ref arguments, out attributeData, out boundAttribute, out obsoleteData))
            {
                if (obsoleteData != null)
                {
                    arguments.GetOrCreateData<TypeEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                }

                return (attributeData, boundAttribute);
            }

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.AttributeUsageAttribute))
            {
                (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
                if (!attributeData.HasErrors)
                {
                    AttributeUsageInfo info = this.DecodeAttributeUsageAttribute(attributeData, arguments.AttributeSyntax, diagnose: false);
                    if (!info.IsNull)
                    {
                        var typeData = arguments.GetOrCreateData<TypeEarlyWellKnownAttributeData>();
                        if (typeData.AttributeUsageInfo.IsNull)
                        {
                            typeData.AttributeUsageInfo = info;
                        }

                        if (!hasAnyDiagnostics)
                        {
                            return (attributeData, boundAttribute);
                        }
                    }
                }

                return (null, null);
            }

            // We want to decode this early because it can influence overload resolution, which could affect attribute binding itself. Consider an attribute with these
            // constructors:
            //
            //   MyAttribute(string s)
            //   MyAttribute(CustomBuilder c) // CustomBuilder has InterpolatedStringHandlerAttribute on the type
            //
            // If it's applied with [MyAttribute($"{1}")], overload resolution rules say that we should prefer the CustomBuilder overload over the string overload. This
            // is an error scenario regardless (non-constant interpolated string), but it's good to get right as it will affect public API results.
            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.InterpolatedStringHandlerAttribute))
            {
                (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
                if (!attributeData.HasErrors)
                {
                    arguments.GetOrCreateData<TypeEarlyWellKnownAttributeData>().HasInterpolatedStringHandlerAttribute = true;
                    if (!hasAnyDiagnostics)
                    {
                        return (attributeData, boundAttribute);
                    }
                }

                return (null, null);
            }

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.InlineArrayAttribute))
            {
                (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
                if (!attributeData.HasErrors)
                {
                    int length = attributeData.GetConstructorArgument<int>(0, SpecialType.System_Int32);

                    arguments.GetOrCreateData<TypeEarlyWellKnownAttributeData>().InlineArrayLength = length > 0 ? length : -1;

                    if (!hasAnyDiagnostics)
                    {
                        return (attributeData, boundAttribute);
                    }
                }

                return (null, null);
            }

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CollectionBuilderAttribute))
            {
                (attributeData, boundAttribute) = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, beforeAttributePartBound: null, afterAttributePartBound: null, out hasAnyDiagnostics);
                if (!attributeData.HasErrors)
                {
                    Debug.Assert(attributeData.CommonConstructorArguments[0].Kind == TypedConstantKind.Type);
                    TypeSymbol? builderType = attributeData.CommonConstructorArguments[0].ValueInternal as TypeSymbol;
                    string? methodName = attributeData.GetConstructorArgument<string>(1, SpecialType.System_String);
                    var data = new CollectionBuilderAttributeData(builderType, methodName);
                    arguments.GetOrCreateData<TypeEarlyWellKnownAttributeData>().CollectionBuilder = data;

                    if (!hasAnyDiagnostics)
                    {
                        return (attributeData, boundAttribute);
                    }
                }

                return (null, null);
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }
#nullable disable

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            Debug.Assert(this.SpecialType == SpecialType.System_Object || this.DeclaringCompilation.IsAttributeType(this));

            TypeEarlyWellKnownAttributeData data = this.GetEarlyDecodedWellKnownAttributeData();
            if (data != null && !data.AttributeUsageInfo.IsNull)
            {
                return data.AttributeUsageInfo;
            }

            return ((object)this.BaseTypeNoUseSiteDiagnostics != null) ? this.BaseTypeNoUseSiteDiagnostics.GetAttributeUsageInfo() : AttributeUsageInfo.Default;
        }

        /// <summary>
        /// Returns data decoded from Obsolete attribute or null if there is no Obsolete attribute.
        /// This property returns ObsoleteAttributeData.Uninitialized if attribute arguments haven't been decoded yet.
        /// </summary>
        internal override ObsoleteAttributeData ObsoleteAttributeData
        {
            get
            {
                var lazyCustomAttributesBag = _lazyCustomAttributesBag;
                if (lazyCustomAttributesBag != null && lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
                {
                    var data = (TypeEarlyWellKnownAttributeData)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData;
                    return data != null ? data.ObsoleteAttributeData : null;
                }

                foreach (var decl in this.declaration.Declarations)
                {
                    if (decl.HasAnyAttributes)
                    {
                        return ObsoleteAttributeData.Uninitialized;
                    }
                }

                return null;
            }
        }

#nullable enable
        protected sealed override void DecodeWellKnownAttributeImpl(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert(arguments.AttributeSyntaxOpt is { });
            var diagnostics = (BindingDiagnosticBag)arguments.Diagnostics;

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(AttributeDescription.AttributeUsageAttribute))
            {
                DecodeAttributeUsageAttribute(attribute, arguments.AttributeSyntaxOpt, diagnose: true, diagnosticsOpt: diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.DefaultMemberAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasDefaultMemberAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.CoClassAttribute))
            {
                DecodeCoClassAttribute(ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ConditionalAttribute))
            {
                ValidateConditionalAttribute(attribute, arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.GuidAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().GuidString = attribute.DecodeGuidAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.SpecialNameAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasSpecialNameAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.SerializableAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasSerializableAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ExcludeFromCodeCoverageAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasExcludeFromCodeCoverageAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.StructLayoutAttribute))
            {
                AttributeData.DecodeStructLayoutAttribute<TypeWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>(
                    ref arguments, this.DefaultMarshallingCharSet, defaultAutoLayoutSize: 0, messageProvider: MessageProvider.Instance);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.SuppressUnmanagedCodeSecurityAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasSuppressUnmanagedCodeSecurityAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ClassInterfaceAttribute))
            {
                attribute.DecodeClassInterfaceAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.InterfaceTypeAttribute))
            {
                attribute.DecodeInterfaceTypeAttribute(arguments.AttributeSyntaxOpt, diagnostics);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.WindowsRuntimeImportAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasWindowsRuntimeImportAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.RequiredAttributeAttribute))
            {
                // CS1608: The Required attribute is not permitted on C# types
                diagnostics.Add(ErrorCode.ERR_CantUseRequiredAttribute, arguments.AttributeSyntaxOpt.Name.Location);
            }
            else if (ReportExplicitUseOfReservedAttributes(in arguments,
                permitted: ReservedAttributes.NullablePublicOnlyAttribute
                    | ReservedAttributes.ScopedRefAttribute
                    | ReservedAttributes.RefSafetyRulesAttribute))
            {
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.SecurityCriticalAttribute)
                || attribute.IsTargetAttribute(AttributeDescription.SecuritySafeCriticalAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasSecurityCriticalAttributes = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.SkipLocalsInitAttribute))
            {
                CSharpAttributeData.DecodeSkipLocalsInitAttribute<TypeWellKnownAttributeData>(DeclaringCompilation, ref arguments);
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.CollectionBuilderAttribute))
            {
                var builderType = attribute.CommonConstructorArguments[0].ValueInternal as TypeSymbol;
                if (!IsValidCollectionBuilderType(builderType))
                {
                    diagnostics.Add(ErrorCode.ERR_CollectionBuilderAttributeInvalidType, arguments.AttributeSyntaxOpt.Name.Location);
                }

                // Ensure dependencies for the builder type are added here since
                // use-site info is ignored in early attribute decoding.
                diagnostics.AddDependencies(builderType);

                string? methodName = attribute.CommonConstructorArguments[1].DecodeValue<string>(SpecialType.System_String);
                if (string.IsNullOrEmpty(methodName))
                {
                    diagnostics.Add(ErrorCode.ERR_CollectionBuilderAttributeInvalidMethodName, arguments.AttributeSyntaxOpt.Name.Location);
                }
            }
            else if (_lazyIsExplicitDefinitionOfNoPiaLocalType == ThreeState.Unknown && attribute.IsTargetAttribute(AttributeDescription.TypeIdentifierAttribute))
            {
                _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.True;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.InlineArrayAttribute))
            {
                int length = attribute.CommonConstructorArguments[0].DecodeValue<int>(SpecialType.System_Int32);

                if (length <= 0)
                {
                    diagnostics.Add(ErrorCode.ERR_InvalidInlineArrayLength, attribute.GetAttributeArgumentLocation(0));
                }

                if (TypeKind != TypeKind.Struct)
                {
                    diagnostics.Add(ErrorCode.ERR_AttributeOnBadSymbolType, arguments.AttributeSyntaxOpt.Name.Location, arguments.AttributeSyntaxOpt.GetErrorDisplayName(), "struct");
                }
                else if (IsRecordStruct)
                {
                    diagnostics.Add(ErrorCode.ERR_InlineArrayAttributeOnRecord, arguments.AttributeSyntaxOpt.Name.Location);
                }
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.CompilerLoweringPreserveAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasCompilerLoweringPreserveAttribute = true;
            }
            else if (attribute.IsTargetAttribute(AttributeDescription.ExtendedLayoutAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasExtendedLayoutAttribute = true;
            }
            else
            {
                var compilation = this.DeclaringCompilation;
                if (attribute.IsSecurityAttribute(compilation))
                {
                    attribute.DecodeSecurityAttribute<TypeWellKnownAttributeData>(this, compilation, ref arguments);
                }
            }
        }

        internal static bool IsValidCollectionBuilderType([NotNullWhen(true)] TypeSymbol? builderType)
        {
            return builderType is NamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Struct, IsGenericType: false };
        }
#nullable disable

        internal override bool IsExplicitDefinitionOfNoPiaLocalType
        {
            get
            {
                if (_lazyIsExplicitDefinitionOfNoPiaLocalType == ThreeState.Unknown)
                {
                    CheckPresenceOfTypeIdentifierAttribute();

                    if (_lazyIsExplicitDefinitionOfNoPiaLocalType == ThreeState.Unknown)
                    {
                        _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.False;
                    }
                }

                Debug.Assert(_lazyIsExplicitDefinitionOfNoPiaLocalType != ThreeState.Unknown);
                return _lazyIsExplicitDefinitionOfNoPiaLocalType == ThreeState.True;
            }
        }

        private void CheckPresenceOfTypeIdentifierAttribute()
        {
            // Have we already decoded well-known attributes?
            if (_lazyCustomAttributesBag?.IsDecodedWellKnownAttributeDataComputed == true)
            {
                return;
            }

            // We want this function to be as cheap as possible, it is called for every top level type
            // and we don't want to bind attributes attached to the declaration unless there is a chance
            // that one of them is TypeIdentifier attribute.
            ImmutableArray<SyntaxList<AttributeListSyntax>> attributeLists = GetAttributeDeclarations(QuickAttributes.TypeIdentifier);

            foreach (SyntaxList<AttributeListSyntax> list in attributeLists)
            {
                var syntaxTree = list.Node.SyntaxTree;
                QuickAttributeChecker checker = this.DeclaringCompilation.GetBinderFactory(list.Node.SyntaxTree).GetBinder(list.Node).QuickAttributeChecker;

                foreach (AttributeListSyntax attrList in list)
                {
                    foreach (AttributeSyntax attr in attrList.Attributes)
                    {
                        if (checker.IsPossibleMatch(attr, QuickAttributes.TypeIdentifier))
                        {
                            // This attribute syntax might be an application of TypeIdentifierAttribute.
                            // Let's bind it.
                            // For simplicity we bind all attributes.
                            GetAttributes();
                            return;
                        }
                    }
                }
            }
        }

        // Process the specified AttributeUsage attribute on the given ownerSymbol
        private AttributeUsageInfo DecodeAttributeUsageAttribute(CSharpAttributeData attribute, AttributeSyntax node, bool diagnose, BindingDiagnosticBag diagnosticsOpt = null)
        {
            Debug.Assert(diagnose == (diagnosticsOpt != null));
            Debug.Assert(!attribute.HasErrors);

            Debug.Assert(!this.IsErrorType());

            // AttributeUsage can only be specified for attribute classes
            if (!this.DeclaringCompilation.IsAttributeType(this))
            {
                if (diagnose)
                {
                    diagnosticsOpt.Add(ErrorCode.ERR_AttributeUsageOnNonAttributeClass, node.Name.Location, node.GetErrorDisplayName());
                }

                return AttributeUsageInfo.Null;
            }
            else
            {
                AttributeUsageInfo info = attribute.DecodeAttributeUsageAttribute();

                // Validate first ctor argument for AttributeUsage specification is a valid AttributeTargets enum member
                if (!info.HasValidAttributeTargets)
                {
                    if (diagnose)
                    {
                        // invalid attribute target
                        diagnosticsOpt.Add(ErrorCode.ERR_InvalidAttributeArgument, attribute.GetAttributeArgumentLocation(0), node.GetErrorDisplayName());
                    }

                    return AttributeUsageInfo.Null;
                }

                return info;
            }
        }

        private void DecodeCoClassAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);

            if (this.IsInterfaceType() && (!arguments.HasDecodedData || (object)((TypeWellKnownAttributeData)arguments.DecodedData).ComImportCoClass == null))
            {
                TypedConstant argument = attribute.CommonConstructorArguments[0];
                Debug.Assert(argument.Kind == TypedConstantKind.Type);

                var coClassType = argument.ValueInternal as NamedTypeSymbol;
                if ((object)coClassType != null && coClassType.TypeKind == TypeKind.Class)
                {
                    arguments.GetOrCreateData<TypeWellKnownAttributeData>().ComImportCoClass = coClassType;
                }
            }
        }

        internal override bool IsComImport
        {
            get
            {
                TypeEarlyWellKnownAttributeData data = this.GetEarlyDecodedWellKnownAttributeData();
                return data != null && data.HasComImportAttribute;
            }
        }

        internal override NamedTypeSymbol ComImportCoClass
        {
            get
            {
                TypeWellKnownAttributeData data = this.GetDecodedWellKnownAttributeData();
                return data != null ? data.ComImportCoClass : null;
            }
        }

#nullable enable
        internal sealed override bool HasCollectionBuilderAttribute(out TypeSymbol? builderType, out string? methodName)
        {
            var attributeData = GetEarlyDecodedWellKnownAttributeData()?.CollectionBuilder;
            if (attributeData == null)
            {
                builderType = null;
                methodName = null;
                return false;
            }

            builderType = attributeData.BuilderType;
            methodName = attributeData.MethodName;
            return true;
        }
#nullable disable

        private void ValidateConditionalAttribute(CSharpAttributeData attribute, AttributeSyntax node, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(this.IsConditional);
            Debug.Assert(!attribute.HasErrors);

            if (!this.DeclaringCompilation.IsAttributeType(this))
            {
                // CS1689: Attribute '{0}' is only valid on methods or attribute classes
                diagnostics.Add(ErrorCode.ERR_ConditionalOnNonAttributeClass, node.Location, node.GetErrorDisplayName());
            }
            else
            {
                string name = attribute.GetConstructorArgument<string>(0, SpecialType.System_String);

                if (name == null || !SyntaxFacts.IsValidIdentifier(name))
                {
                    // CS0633: The argument to the '{0}' attribute must be a valid identifier
                    diagnostics.Add(ErrorCode.ERR_BadArgumentToAttribute, attribute.GetAttributeArgumentLocation(0), node.GetErrorDisplayName());
                }
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                if (this.IsExtension)
                {
                    return true;
                }

                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasSpecialNameAttribute;
            }
        }

        internal override bool HasCodeAnalysisEmbeddedAttribute
        {
            get
            {
                var data = GetEarlyDecodedWellKnownAttributeData();
                return (data != null && data.HasCodeAnalysisEmbeddedAttribute)
                    // If this is Microsoft.CodeAnalysis.EmbeddedAttribute, we'll synthesize EmbeddedAttribute even if it's not applied.
                    || this.IsMicrosoftCodeAnalysisEmbeddedAttribute();
            }
        }

        internal override bool HasCompilerLoweringPreserveAttribute
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasCompilerLoweringPreserveAttribute;
            }
        }

#nullable enable
        internal sealed override bool IsInterpolatedStringHandlerType
            => GetEarlyDecodedWellKnownAttributeData()?.HasInterpolatedStringHandlerAttribute == true;
#nullable disable

        internal sealed override bool ShouldAddWinRTMembers
        {
            get { return false; }
        }

        internal sealed override bool IsWindowsRuntimeImport
        {
            get
            {
                TypeWellKnownAttributeData data = this.GetDecodedWellKnownAttributeData();
                return data != null && data.HasWindowsRuntimeImportAttribute;
            }
        }

        public sealed override bool IsSerializable
        {
            get
            {
                var data = this.GetDecodedWellKnownAttributeData();
                return data != null && data.HasSerializableAttribute;
            }
        }

        public sealed override bool AreLocalsZeroed
        {
            get
            {
                var data = this.GetDecodedWellKnownAttributeData();
                return data?.HasSkipLocalsInitAttribute != true && (ContainingType?.AreLocalsZeroed ?? ContainingModule.AreLocalsZeroed);
            }
        }

        internal override bool GetGuidString(out string guidString)
        {
            guidString = GetDecodedWellKnownAttributeData()?.GuidString;
            return guidString != null;
        }

        internal override bool IsDirectlyExcludedFromCodeCoverage =>
            GetDecodedWellKnownAttributeData()?.HasExcludeFromCodeCoverageAttribute == true;

        private bool HasInstanceFields()
        {
            var fields = this.GetFieldsToEmit();
            foreach (var field in fields)
            {
                if (!field.IsStatic)
                {
                    return true;
                }
            }

            return false;
        }

        internal sealed override TypeLayout Layout
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();

                if (data is { HasExtendedLayoutAttribute: true })
                {
                    return new TypeLayout(LayoutKind.Extended, 0, alignment: 0);
                }

                if (data is { HasStructLayoutAttribute: true })
                {
                    return data.Layout;
                }

                if (this.TypeKind == TypeKind.Struct)
                {
                    // CLI spec 22.37.16:
                    // "A ValueType shall have a non-zero size - either by defining at least one field, or by providing a non-zero ClassSize"
                    // 
                    // Dev11 compiler sets the value to 1 for structs with no instance fields and no size specified.
                    // It does not change the size value if it was explicitly specified to be 0, nor does it report an error.

                    return new TypeLayout(LayoutKind.Sequential, this.HasInstanceFields() ? 0 : 1, alignment: 0);
                }

                return default(TypeLayout);
            }
        }

        internal bool HasStructLayoutAttribute
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasStructLayoutAttribute;
            }
        }

        internal override CharSet MarshallingCharSet
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return (data != null && data.HasStructLayoutAttribute) ? data.MarshallingCharSet : DefaultMarshallingCharSet;
            }
        }

        internal bool HasExtendedLayoutAttribute
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data is { HasExtendedLayoutAttribute: true };
            }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get
            {
                var data = this.GetDecodedWellKnownAttributeData();
                return data != null && data.HasDeclarativeSecurity;
            }
        }

        internal bool HasSecurityCriticalAttributes
        {
            get
            {
                var data = this.GetDecodedWellKnownAttributeData();
                return data != null && data.HasSecurityCriticalAttributes;
            }
        }

        internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            var attributesBag = this.GetAttributesBag();
            var wellKnownData = (TypeWellKnownAttributeData)attributesBag.DecodedWellKnownAttributeData;
            if (wellKnownData != null)
            {
                SecurityWellKnownAttributeData securityData = wellKnownData.SecurityInformation;
                if (securityData != null)
                {
                    return securityData.GetSecurityAttributes(attributesBag.Attributes);
                }
            }

            return null;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            var data = GetEarlyDecodedWellKnownAttributeData();
            return data != null ? data.ConditionalSymbols : ImmutableArray<string>.Empty;
        }

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, BindingDiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
        {
            Debug.Assert(!boundAttributes.IsDefault);
            Debug.Assert(!allAttributeSyntaxNodes.IsDefault);
            Debug.Assert(boundAttributes.Length == allAttributeSyntaxNodes.Length);
            Debug.Assert(_lazyCustomAttributesBag != null);
            Debug.Assert(_lazyCustomAttributesBag.IsDecodedWellKnownAttributeDataComputed);
            Debug.Assert(symbolPart == AttributeLocation.None);

            var data = (TypeWellKnownAttributeData)decodedData;

            if (this.IsComImport)
            {
                Debug.Assert(boundAttributes.Any());

                // Symbol with ComImportAttribute must have a GuidAttribute
                if (data == null || data.GuidString == null)
                {
                    int index = boundAttributes.IndexOfAttribute(AttributeDescription.ComImportAttribute);
                    diagnostics.Add(ErrorCode.ERR_ComImportWithoutUuidAttribute, allAttributeSyntaxNodes[index].Name.Location);
                }

                if (this.TypeKind == TypeKind.Class)
                {
                    var baseType = this.BaseTypeNoUseSiteDiagnostics;
                    if ((object)baseType != null && baseType.SpecialType != SpecialType.System_Object)
                    {
                        // CS0424: '{0}': a class with the ComImport attribute cannot specify a base class
                        diagnostics.Add(ErrorCode.ERR_ComImportWithBase, this.GetFirstLocation(), this.Name);
                    }

                    var initializers = this.StaticInitializers;
                    if (!initializers.IsDefaultOrEmpty)
                    {
                        foreach (var initializerGroup in initializers)
                        {
                            foreach (var singleInitializer in initializerGroup)
                            {
                                if (!singleInitializer.FieldOpt.IsMetadataConstant)
                                {
                                    // CS8028: '{0}': a class with the ComImport attribute cannot specify field initializers.
                                    diagnostics.Add(ErrorCode.ERR_ComImportWithInitializers, singleInitializer.Syntax.GetLocation(), this.Name);
                                }
                            }
                        }
                    }

                    initializers = this.InstanceInitializers;
                    if (!initializers.IsDefaultOrEmpty)
                    {
                        foreach (var initializerGroup in initializers)
                        {
                            foreach (var singleInitializer in initializerGroup)
                            {
                                // CS8028: '{0}': a class with the ComImport attribute cannot specify field initializers.
                                diagnostics.Add(ErrorCode.ERR_ComImportWithInitializers, singleInitializer.Syntax.GetLocation(), this.Name);
                            }
                        }
                    }
                }
            }
            else if ((object)this.ComImportCoClass != null)
            {
                Debug.Assert(boundAttributes.Any());

                // Symbol with CoClassAttribute must have a ComImportAttribute
                int index = boundAttributes.IndexOfAttribute(AttributeDescription.CoClassAttribute);
                diagnostics.Add(ErrorCode.WRN_CoClassWithoutComImport, allAttributeSyntaxNodes[index].Location, this.Name);
            }

            // Report ERR_DefaultMemberOnIndexedType if type has a default member attribute and has indexers.
            if (data != null && data.HasDefaultMemberAttribute && this.Indexers.Any())
            {
                Debug.Assert(boundAttributes.Any());

                int index = boundAttributes.IndexOfAttribute(AttributeDescription.DefaultMemberAttribute);
                diagnostics.Add(ErrorCode.ERR_DefaultMemberOnIndexedType, allAttributeSyntaxNodes[index].Name.Location);
            }

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }

        internal override bool HasInlineArrayAttribute(out int length)
        {
            TypeEarlyWellKnownAttributeData data = this.GetEarlyDecodedWellKnownAttributeData();
            if (data?.InlineArrayLength is > 0 and var lengthFromAttribute)
            {
                length = lengthFromAttribute;
                return true;
            }

            length = 0;
            return false;
        }

        /// <remarks>
        /// These won't be returned by GetAttributes on source methods, but they
        /// will be returned by GetAttributes on metadata symbols.
        /// </remarks>
        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            CSharpCompilation compilation = this.DeclaringCompilation;

            if (this.ContainsExtensionMethods)
            {
                // No need to check if [Extension] attribute was explicitly set since
                // we'll issue CS1112 error in those cases and won't generate IL.
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor));
            }

            if (this.IsRefLikeType)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsByRefLikeAttribute(this));

                var obsoleteData = ObsoleteAttributeData;
                Debug.Assert(obsoleteData != ObsoleteAttributeData.Uninitialized, "getting synthesized attributes before attributes are decoded");

                if (!this.IsRestrictedType(ignoreSpanLikeTypes: true))
                {
                    // If user specified an Obsolete attribute, we cannot emit ours.
                    // NB: we do not check the kind of deprecation. 
                    //     we will not emit Obsolete even if Deprecated or Experimental was used.
                    //     we do not want to get into a scenario where different kinds of deprecation are combined together.
                    //
                    if (obsoleteData == null)
                    {
                        AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_ObsoleteAttribute__ctor,
                            ImmutableArray.Create(
                                new TypedConstant(compilation.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, PEModule.ByRefLikeMarker), // message
                                new TypedConstant(compilation.GetSpecialType(SpecialType.System_Boolean), TypedConstantKind.Primitive, true)), // error=true
                            isOptionalUse: true));
                    }

                    AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute__ctor,
                        ImmutableArray.Create(new TypedConstant(compilation.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, nameof(CompilerFeatureRequiredFeatures.RefStructs))),
                        isOptionalUse: true));
                }
            }

            if (this.IsReadOnly)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(this));
            }

            if (this.Indexers.Any())
            {
                string defaultMemberName = this.Indexers.First().MetadataName; // UNDONE: IndexerNameAttribute
                var defaultMemberNameConstant = new TypedConstant(compilation.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, defaultMemberName);

                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor,
                    ImmutableArray.Create(defaultMemberNameConstant)));
            }

            if (this.declaration.Declarations.All(d => d.IsSimpleProgram))
            {
                AddSynthesizedAttribute(ref attributes,
                    this.DeclaringCompilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }

            if (HasDeclaredRequiredMembers)
            {
                AddSynthesizedAttribute(
                    ref attributes,
                    compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_RequiredMemberAttribute__ctor));
            }

            if (IsClosed)
            {
                AddSynthesizedAttribute(
                    ref attributes,
                    compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_ClosedAttribute__ctor));
            }

            // Add MetadataUpdateOriginalTypeAttribute when a reloadable type is emitted to EnC delta
            if (moduleBuilder.EncSymbolChanges?.IsReplaced(this) == true)
            {
                // Note that we use this source named type symbol in the attribute argument (of System.Type).
                // We do not have access to the original symbol from this compilation. However, System.Type
                // is encoded in the attribute as a string containing a fully qualified type name.
                // The name of the current type symbol as provided by ISymbol.Name is the same as the name of
                // the original type symbol that is being replaced by this type symbol.
                // The "#{generation}" suffix is appended to the TypeDef name in the metadata writer,
                // but not to the attribute value.
                var originalType = this;

                AddSynthesizedAttribute(
                    ref attributes,
                    compilation.TrySynthesizeAttribute(
                        WellKnownMember.System_Runtime_CompilerServices_MetadataUpdateOriginalTypeAttribute__ctor,
                        ImmutableArray.Create(new TypedConstant(compilation.GetWellKnownType(WellKnownType.System_Type), TypedConstantKind.Type, originalType)),
                        isOptionalUse: true));
            }

            if (this.IsMicrosoftCodeAnalysisEmbeddedAttribute() && GetEarlyDecodedWellKnownAttributeData() is null or { HasCodeAnalysisEmbeddedAttribute: false })
            {
                // This is Microsoft.CodeAnalysis.EmbeddedAttribute, and the user didn't manually apply this attribute to itself. Grab the parameterless constructor
                // and apply it; if there isn't a parameterless constructor, there would have been a declaration diagnostic

                var parameterlessConstructor = InstanceConstructors.FirstOrDefault(c => c.ParameterCount == 0);

                if (parameterlessConstructor is not null)
                {
                    AddSynthesizedAttribute(
                        ref attributes,
                        SynthesizedAttributeData.Create(DeclaringCompilation, parameterlessConstructor, arguments: [], namedArguments: []));
                }
            }
        }

        #endregion

        internal override NamedTypeSymbol AsNativeInteger()
        {
            Debug.Assert(this.SpecialType == SpecialType.System_IntPtr || this.SpecialType == SpecialType.System_UIntPtr);
            if (ContainingAssembly.RuntimeSupportsNumericIntPtr)
            {
                return this;
            }

            return ContainingAssembly.GetNativeIntegerType(this);
        }

        internal override NamedTypeSymbol NativeIntegerUnderlyingType => null;

        internal override bool Equals(TypeSymbol t2, TypeCompareKind comparison)
        {
            return t2 is NativeIntegerTypeSymbol nativeInteger ?
                nativeInteger.Equals(this, comparison) :
                base.Equals(t2, comparison);
        }

#nullable enable
        internal bool IsSimpleProgram
        {
            get
            {
                return this.declaration.Declarations.Any(static d => d.IsSimpleProgram);
            }
        }

        public override string MetadataName
        {
            get
            {
                if (IsExtension)
                {
                    Debug.Assert(ExtensionMarkerName is not null);
                    return ExtensionMarkerName;
                }

                return base.MetadataName;
            }
        }

        internal override bool MangleName
        {
            get
            {
                return IsExtension
                    ? false
                    : base.MangleName;
            }
        }

        protected override void AfterMembersCompletedChecks(BindingDiagnosticBag diagnostics)
        {
            base.AfterMembersCompletedChecks(diagnostics);

            // We need to give warnings if Obsolete is applied to any required members and there are constructors where a user would be forced to set that member,
            // unless:
            //  1. We're in an obsolete context ourselves, or
            //  2. All constructors of this type are obsolete or attributed with SetsRequiredMembersAttribute
            // We don't warn for obsolete required members from base types, as the user either has a warning that they're depending on an obsolete constructor/type
            // already, or the original author ignored this warning.

            // Obsolete states should have already been calculated by this point in the pipeline.
            Debug.Assert(ObsoleteKind != ObsoleteAttributeKind.Uninitialized);
            Debug.Assert(GetMembers().All(m => m.ObsoleteKind != ObsoleteAttributeKind.Uninitialized));

            if (ObsoleteKind == ObsoleteAttributeKind.None
                && !GetMembers().All(m => m is not MethodSymbol { MethodKind: MethodKind.Constructor, ObsoleteKind: ObsoleteAttributeKind.None } method
                                         || !method.ShouldCheckRequiredMembers()))
            {
                foreach (var member in GetMembers())
                {
                    if (!member.IsRequired())
                    {
                        continue;
                    }

                    if (member.ObsoleteKind != ObsoleteAttributeKind.None)
                    {
                        // Required member '{0}' should not be attributed with 'ObsoleteAttribute' unless the containing type is obsolete or all constructors are obsolete.
                        diagnostics.Add(ErrorCode.WRN_ObsoleteMembersShouldNotBeRequired, member.GetFirstLocation(), member);
                    }
                }
            }

            if (Indexers.FirstOrDefault() is PropertySymbol indexerSymbol)
            {
                Binder.GetWellKnownTypeMember(DeclaringCompilation, WellKnownMember.System_Reflection_DefaultMemberAttribute__ctor, diagnostics, indexerSymbol.TryGetFirstLocation() ?? GetFirstLocation());
            }

            if (HasStructLayoutAttribute && HasExtendedLayoutAttribute)
            {
                // Use of 'StructLayoutAttribute' and 'ExtendedLayoutAttribute' on the same type is not allowed.
                diagnostics.Add(ErrorCode.ERR_StructLayoutAndExtendedLayout, GetFirstLocation());
            }

            if (TypeKind == TypeKind.Struct && !IsRecordStruct && HasInlineArrayAttribute(out _))
            {
                if (Layout.Kind is not (LayoutKind.Sequential or LayoutKind.Auto))
                {
                    diagnostics.Add(ErrorCode.ERR_InvalidInlineArrayLayout, GetFirstLocation());
                }

                if (TryGetPossiblyUnsupportedByLanguageInlineArrayElementField() is FieldSymbol elementField)
                {
                    bool reported_ERR_InlineArrayUnsupportedElementFieldModifier = false;

                    if (elementField.IsRequired || elementField.IsReadOnly || elementField.IsVolatile || elementField.IsFixedSizeBuffer)
                    {
                        diagnostics.Add(ErrorCode.ERR_InlineArrayUnsupportedElementFieldModifier, elementField.TryGetFirstLocation() ?? GetFirstLocation());
                        reported_ERR_InlineArrayUnsupportedElementFieldModifier = true;
                    }

                    NamedTypeSymbol? index = null;
                    NamedTypeSymbol? range = null;

                    foreach (PropertySymbol indexer in Indexers)
                    {
                        if (indexer.Parameters is [{ Type: { } type }] &&
                            (type.SpecialType == SpecialType.System_Int32 ||
                                type.Equals(index ??= DeclaringCompilation.GetWellKnownType(WellKnownType.System_Index), TypeCompareKind.AllIgnoreOptions) ||
                                type.Equals(range ??= DeclaringCompilation.GetWellKnownType(WellKnownType.System_Range), TypeCompareKind.AllIgnoreOptions)))
                        {
                            diagnostics.Add(ErrorCode.WRN_InlineArrayIndexerNotUsed, indexer.TryGetFirstLocation() ?? GetFirstLocation());
                        }
                    }

                    foreach (var slice in GetMembers(WellKnownMemberNames.SliceMethodName).OfType<MethodSymbol>())
                    {
                        if (Binder.MethodHasValidSliceSignature(slice))
                        {
                            diagnostics.Add(ErrorCode.WRN_InlineArraySliceNotUsed, slice.TryGetFirstLocation() ?? GetFirstLocation());
                            break;
                        }
                    }

                    NamedTypeSymbol? span = null;
                    NamedTypeSymbol? readOnlySpan = null;
                    TypeWithAnnotations elementType = elementField.TypeWithAnnotations;

                    bool fieldSupported = TypeSymbol.IsInlineArrayElementFieldSupported(elementField);
                    if (fieldSupported)
                    {
                        foreach (var conversion in GetMembers().OfType<SourceUserDefinedConversionSymbol>())
                        {
                            TypeSymbol returnType = conversion.ReturnType;
                            TypeSymbol returnTypeOriginalDefinition = returnType.OriginalDefinition;

                            if (conversion.ParameterCount == 1 &&
                                conversion.Parameters[0].Type.Equals(this, TypeCompareKind.AllIgnoreOptions) &&
                                (returnTypeOriginalDefinition.Equals(span ??= DeclaringCompilation.GetWellKnownType(WellKnownType.System_Span_T), TypeCompareKind.AllIgnoreOptions) ||
                                    returnTypeOriginalDefinition.Equals(readOnlySpan ??= DeclaringCompilation.GetWellKnownType(WellKnownType.System_ReadOnlySpan_T), TypeCompareKind.AllIgnoreOptions)) &&
                                Conversions.HasIdentityConversion(((NamedTypeSymbol)returnTypeOriginalDefinition).Construct(ImmutableArray.Create(elementType)), returnType))
                            {
                                diagnostics.Add(ErrorCode.WRN_InlineArrayConversionOperatorNotUsed, conversion.TryGetFirstLocation() ?? GetFirstLocation());
                            }
                        }
                    }

                    if (!reported_ERR_InlineArrayUnsupportedElementFieldModifier)
                    {
                        if (!fieldSupported || elementType.Type.IsPointerOrFunctionPointer() || elementType.IsRestrictedType(ignoreSpanLikeTypes: true))
                        {
                            diagnostics.Add(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, elementField.TryGetFirstLocation() ?? GetFirstLocation());
                        }
                        else if (this.IsRestrictedType())
                        {
                            diagnostics.Add(ErrorCode.WRN_InlineArrayNotSupportedByLanguage, GetFirstLocation());
                        }
                    }
                }
                else
                {
                    diagnostics.Add(ErrorCode.ERR_InvalidInlineArrayFields, GetFirstLocation());
                }

                if (!ContainingAssembly.RuntimeSupportsInlineArrayTypes)
                {
                    diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportInlineArrayTypes, GetFirstLocation());
                }
            }

            if (TypeKind == TypeKind.Struct && HasExtendedLayoutAttribute)
            {
                if (!ContainingAssembly.RuntimeSupportsExtendedLayout)
                {
                    diagnostics.Add(ErrorCode.ERR_RuntimeDoesNotSupportExtendedLayoutTypes, GetFirstLocation());
                }
            }

            if (this.IsMicrosoftCodeAnalysisEmbeddedAttribute())
            {
                // This is a user-defined implementation of the special attribute Microsoft.CodeAnalysis.EmbeddedAttribute. It needs to follow specific rules:
                // 1. It must be internal
                // 2. It must be a class
                // 3. It must be sealed
                // 4. It must be non-static
                // 5. It must have an internal or public parameterless constructor
                // 6. It must inherit from System.Attribute
                // 7. It must be allowed on any type declaration (class, struct, interface, enum, or delegate)
                // 8. It must be non-generic (checked as part of IsMicrosoftCodeAnalysisEmbeddedAttribute, we don't error on this because both types can exist)
                // 9. It cannot have file scope

                const AttributeTargets expectedTargets = AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Delegate;

                if (DeclaredAccessibility != Accessibility.Internal
                    || TypeKind != TypeKind.Class
                    || !IsSealed
                    || IsStatic
                    || IsFileLocal
                    || !InstanceConstructors.Any(c => c is { ParameterCount: 0, DeclaredAccessibility: Accessibility.Internal or Accessibility.Public })
                    || !this.DeclaringCompilation.IsAttributeType(this)
                    || (GetAttributeUsageInfo().ValidTargets & expectedTargets) != expectedTargets)
                {
                    // The type 'Microsoft.CodeAnalysis.EmbeddedAttribute' must be non-generic, internal, non-file, sealed, non-static, have a parameterless constructor, inherit from System.Attribute, and be able to be applied to any type.
                    diagnostics.Add(ErrorCode.ERR_EmbeddedAttributeMustFollowPattern, GetFirstLocation());
                }

            }

            if (IsExtension && ContainingType?.IsExtension != true)
            {
                // If the containing type is an extension, we'll have already reported an error
                if (ContainingType is null || !ContainingType.IsStatic || ContainingType.Arity != 0 || ContainingType.ContainingType is not null)
                {
                    var syntax = (ExtensionBlockDeclarationSyntax)this.GetNonNullSyntaxNode();
                    diagnostics.Add(ErrorCode.ERR_BadExtensionContainingType, syntax.Keyword);
                }
            }
        }
    }
}
