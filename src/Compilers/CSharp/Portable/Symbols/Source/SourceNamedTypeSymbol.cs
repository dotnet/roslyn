// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    // This is a type symbol associated with a type definition in source code.
    // That is, for a generic type C<T> this is the instance type C<T>.  
    internal sealed partial class SourceNamedTypeSymbol : SourceMemberContainerTypeSymbol, IAttributeTargetSymbol
    {
        private ImmutableArray<TypeParameterSymbol> _lazyTypeParameters;

        /// <summary>
        /// A collection of type parameter constraints, populated when
        /// constraints for the first type parameter are requested.
        /// </summary>
        private ImmutableArray<TypeParameterConstraintClause> _lazyTypeParameterConstraints;

        private CustomAttributesBag<CSharpAttributeData> _lazyCustomAttributesBag;

        private string _lazyDocComment;
        private string _lazyExpandedDocComment;

        private ThreeState _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.Unknown;

        protected override Location GetCorrespondingBaseListLocation(NamedTypeSymbol @base)
        {
            Location backupLocation = null;
            var unusedDiagnostics = DiagnosticBag.GetInstance();

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
                    TypeSymbol bt = baseBinder.BindType(t, unusedDiagnostics).Type;

                    if (TypeSymbol.Equals(bt, @base, TypeCompareKind.ConsiderEverything2))
                    {
                        unusedDiagnostics.Free();
                        return t.GetLocation();
                    }
                }
            }
            unusedDiagnostics.Free();
            return backupLocation;
        }

        internal SourceNamedTypeSymbol(NamespaceOrTypeSymbol containingSymbol, MergedTypeDeclaration declaration, DiagnosticBag diagnostics)
            : base(containingSymbol, declaration, diagnostics)
        {
            Debug.Assert(declaration.Kind == DeclarationKind.Struct ||
                         declaration.Kind == DeclarationKind.Interface ||
                         declaration.Kind == DeclarationKind.Enum ||
                         declaration.Kind == DeclarationKind.Delegate ||
                         declaration.Kind == DeclarationKind.Class);

            if (containingSymbol.Kind == SymbolKind.NamedType)
            {
                // Nested types are never unified.
                _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.False;
            }
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

        private ImmutableArray<TypeParameterSymbol> MakeTypeParameters(DiagnosticBag diagnostics)
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

                bool isInterfaceOrDelegate = typeKind == SyntaxKind.InterfaceDeclaration || typeKind == SyntaxKind.DelegateDeclaration;
                var parameterBuilder = new List<TypeParameterBuilder>();
                parameterBuilders1.Add(parameterBuilder);
                int i = 0;
                foreach (var tp in tpl.Parameters)
                {
                    if (tp.VarianceKeyword.Kind() != SyntaxKind.None &&
                        !isInterfaceOrDelegate)
                    {
                        diagnostics.Add(ErrorCode.ERR_IllegalVarianceSyntax, tp.VarianceKeyword.GetLocation());
                    }

                    var name = typeParameterNames[i];
                    var location = new SourceLocation(tp.Identifier);
                    var varianceKind = typeParameterVarianceKeywords[i];
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
        /// Returns the constraint clause for the given type parameter.
        /// </summary>
        internal TypeParameterConstraintClause GetTypeParameterConstraintClause(int ordinal)
        {
            var clauses = _lazyTypeParameterConstraints;
            if (clauses.IsDefault)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                if (ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameterConstraints, MakeTypeParameterConstraints(diagnostics)))
                {
                    this.AddDeclarationDiagnostics(diagnostics);
                }
                diagnostics.Free();
                clauses = _lazyTypeParameterConstraints;
            }

            return (clauses.Length > 0) ? clauses[ordinal] : TypeParameterConstraintClause.Empty;
        }

        private ImmutableArray<TypeParameterConstraintClause> MakeTypeParameterConstraints(DiagnosticBag diagnostics)
        {
            var typeParameters = this.TypeParameters;
            var results = ImmutableArray<TypeParameterConstraintClause>.Empty;

            int arity = typeParameters.Length;
            if (arity > 0)
            {
                bool skipPartialDeclarationsWithoutConstraintClauses = false;

                foreach (var decl in declaration.Declarations)
                {
                    if (GetConstraintClauses((CSharpSyntaxNode)decl.SyntaxReference.GetSyntax(), out _).Count != 0)
                    {
                        skipPartialDeclarationsWithoutConstraintClauses = true;
                        break;
                    }
                }

                IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverride = null;
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

                        constraints = binder.BindTypeParameterConstraintClauses(this, typeParameters, typeParameterList, constraintClauses, ref isValueTypeOverride, diagnostics);
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

                results = MergeConstraintsForPartialDeclarations(results, otherPartialClauses, isValueTypeOverride, diagnostics);

                if (results.ContainsOnlyEmptyConstraintClauses())
                {
                    results = ImmutableArray<TypeParameterConstraintClause>.Empty;
                }

                otherPartialClauses?.Free();
            }

            return results;
        }

        private static SyntaxList<TypeParameterConstraintClauseSyntax> GetConstraintClauses(CSharpSyntaxNode node, out TypeParameterListSyntax typeParameterList)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
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
        private ImmutableArray<TypeParameterConstraintClause> MergeConstraintsForPartialDeclarations(ImmutableArray<TypeParameterConstraintClause> constraintClauses,
                                                                                                     ArrayBuilder<ImmutableArray<TypeParameterConstraintClause>> otherPartialClauses,
                                                                                                     IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverride,
                                                                                                     DiagnosticBag diagnostics)
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
                ArrayBuilder<TypeWithAnnotations> mergedConstraintTypes = null;
                SmallDictionary<TypeWithAnnotations, int> originalConstraintTypesMap = null;

                // Constraints defined on multiple partial declarations.
                // Report any mismatched constraints.
                bool report = false;
                foreach (ImmutableArray<TypeParameterConstraintClause> otherPartialConstraints in otherPartialClauses)
                {
                    if (!mergeConstraints(ref mergedKind, originalConstraintTypes, ref originalConstraintTypesMap, ref mergedConstraintTypes, otherPartialConstraints[i], isValueTypeOverride))
                    {
                        report = true;
                    }
                }

                if (report)
                {
                    // "Partial declarations of '{0}' have inconsistent constraints for type parameter '{1}'"
                    diagnostics.Add(ErrorCode.ERR_PartialWrongConstraints, Locations[0], this, typeParameters[i]);
                }

                if (constraint.Constraints != mergedKind || mergedConstraintTypes != null)
                {
                    Debug.Assert((constraint.Constraints & (TypeParameterConstraintKind.AllNonNullableKinds | TypeParameterConstraintKind.NotNull)) ==
                                 (mergedKind & (TypeParameterConstraintKind.AllNonNullableKinds | TypeParameterConstraintKind.NotNull)));
                    Debug.Assert((mergedKind & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) == 0 ||
                                 (constraint.Constraints & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) != 0);
                    Debug.Assert((constraint.Constraints & TypeParameterConstraintKind.AllReferenceTypeKinds) == (mergedKind & TypeParameterConstraintKind.AllReferenceTypeKinds) ||
                                 (constraint.Constraints & TypeParameterConstraintKind.AllReferenceTypeKinds) == TypeParameterConstraintKind.ReferenceType);
#if DEBUG
                    if (mergedConstraintTypes != null)
                    {
                        Debug.Assert(originalConstraintTypes.Length == mergedConstraintTypes.Count);

                        for (int j = 0; j < originalConstraintTypes.Length; j++)
                        {
                            Debug.Assert(originalConstraintTypes[j].Equals(mergedConstraintTypes[j], TypeCompareKind.ObliviousNullableModifierMatchesAny, isValueTypeOverride));
                        }
                    }
#endif
                    if (builder == null)
                    {
                        builder = ArrayBuilder<TypeParameterConstraintClause>.GetInstance(constraintClauses.Length);
                        builder.AddRange(constraintClauses);
                    }

                    builder[i] = TypeParameterConstraintClause.Create(mergedKind,
                                                                      mergedConstraintTypes?.ToImmutableAndFree() ?? originalConstraintTypes);
                }
            }

            if (builder != null)
            {
                constraintClauses = builder.ToImmutableAndFree();
            }

            return constraintClauses;

            static bool mergeConstraints(ref TypeParameterConstraintKind mergedKind, ImmutableArray<TypeWithAnnotations> originalConstraintTypes,
                                         ref SmallDictionary<TypeWithAnnotations, int> originalConstraintTypesMap, ref ArrayBuilder<TypeWithAnnotations> mergedConstraintTypes,
                                         TypeParameterConstraintClause clause, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverride)
            {
                bool result = true;

                if ((mergedKind & (TypeParameterConstraintKind.AllNonNullableKinds | TypeParameterConstraintKind.NotNull)) != (clause.Constraints & (TypeParameterConstraintKind.AllNonNullableKinds | TypeParameterConstraintKind.NotNull)))
                {
                    result = false;
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
                            result = false;
                        }
                    }
                }

                if (originalConstraintTypes.Length == 0)
                {
                    if (clause.ConstraintTypes.Length == 0)
                    {
                        // Try merging nullability of implied 'object' constraint
                        if (((mergedKind | clause.Constraints) & ~(TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType | TypeParameterConstraintKind.Constructor)) == 0 &&
                            (mergedKind & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) != 0 && // 'object~'
                            (clause.Constraints & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) == 0)   // 'object?' 
                        {
                            // Merged value is 'object?'
                            mergedKind &= ~TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType;
                        }

                        return result;
                    }

                    return false;
                }
                else if (clause.ConstraintTypes.Length == 0)
                {
                    return false;
                }

                originalConstraintTypesMap ??= toDictionary(originalConstraintTypes,
                                                            new TypeWithAnnotations.EqualsComparer(TypeCompareKind.IgnoreNullableModifiersForReferenceTypes, isValueTypeOverride));
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

                    if (!constraintType1.Equals(constraintType2, TypeCompareKind.ObliviousNullableModifierMatchesAny, isValueTypeOverride))
                    {
                        // Nullability mismatch that doesn't involve oblivious
                        result = false;
                        continue;
                    }

                    if (!constraintType1.Equals(constraintType2, TypeCompareKind.ConsiderEverything, isValueTypeOverride))
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
                if (_lazyTypeParameters.IsDefault)
                {
                    var diagnostics = DiagnosticBag.GetInstance();
                    if (ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameters, MakeTypeParameters(diagnostics)))
                    {
                        AddDeclarationDiagnostics(diagnostics);
                    }

                    diagnostics.Free();
                }

                return _lazyTypeParameters;
            }
        }

        #endregion

        #region Attributes

        internal ImmutableArray<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            return declaration.GetAttributeDeclarations();
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
                        return AttributeLocation.Type;

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

        /// <summary>
        /// Returns data decoded from special early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        /// </summary>
        /// <remarks>
        /// Forces binding and decoding of attributes.
        /// </remarks>
        internal CommonTypeEarlyWellKnownAttributeData GetEarlyDecodedWellKnownAttributeData()
        {
            var attributesBag = _lazyCustomAttributesBag;
            if (attributesBag == null || !attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed)
            {
                attributesBag = this.GetAttributesBag();
            }

            return (CommonTypeEarlyWellKnownAttributeData)attributesBag.EarlyDecodedWellKnownAttributeData;
        }

        internal override CSharpAttributeData EarlyDecodeWellKnownAttribute(ref EarlyDecodeWellKnownAttributeArguments<EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation> arguments)
        {
            bool hasAnyDiagnostics;
            CSharpAttributeData boundAttribute;

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ComImportAttribute))
            {
                boundAttribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, out hasAnyDiagnostics);
                if (!boundAttribute.HasErrors)
                {
                    arguments.GetOrCreateData<CommonTypeEarlyWellKnownAttributeData>().HasComImportAttribute = true;
                    if (!hasAnyDiagnostics)
                    {
                        return boundAttribute;
                    }
                }

                return null;
            }

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CodeAnalysisEmbeddedAttribute))
            {
                boundAttribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, out hasAnyDiagnostics);
                if (!boundAttribute.HasErrors)
                {
                    arguments.GetOrCreateData<CommonTypeEarlyWellKnownAttributeData>().HasCodeAnalysisEmbeddedAttribute = true;
                    if (!hasAnyDiagnostics)
                    {
                        return boundAttribute;
                    }
                }

                return null;
            }

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ConditionalAttribute))
            {
                boundAttribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, out hasAnyDiagnostics);
                if (!boundAttribute.HasErrors)
                {
                    string name = boundAttribute.GetConstructorArgument<string>(0, SpecialType.System_String);
                    arguments.GetOrCreateData<CommonTypeEarlyWellKnownAttributeData>().AddConditionalSymbol(name);
                    if (!hasAnyDiagnostics)
                    {
                        return boundAttribute;
                    }
                }

                return null;
            }

            ObsoleteAttributeData obsoleteData;
            if (EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(ref arguments, out boundAttribute, out obsoleteData))
            {
                if (obsoleteData != null)
                {
                    arguments.GetOrCreateData<CommonTypeEarlyWellKnownAttributeData>().ObsoleteAttributeData = obsoleteData;
                }

                return boundAttribute;
            }

            if (CSharpAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.AttributeUsageAttribute))
            {
                boundAttribute = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, out hasAnyDiagnostics);
                if (!boundAttribute.HasErrors)
                {
                    AttributeUsageInfo info = this.DecodeAttributeUsageAttribute(boundAttribute, arguments.AttributeSyntax, diagnose: false);
                    if (!info.IsNull)
                    {
                        var typeData = arguments.GetOrCreateData<CommonTypeEarlyWellKnownAttributeData>();
                        if (typeData.AttributeUsageInfo.IsNull)
                        {
                            typeData.AttributeUsageInfo = info;
                        }

                        if (!hasAnyDiagnostics)
                        {
                            return boundAttribute;
                        }
                    }
                }

                return null;
            }

            return base.EarlyDecodeWellKnownAttribute(ref arguments);
        }

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            Debug.Assert(this.SpecialType == SpecialType.System_Object || this.DeclaringCompilation.IsAttributeType(this));

            CommonTypeEarlyWellKnownAttributeData data = this.GetEarlyDecodedWellKnownAttributeData();
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
                    var data = (CommonTypeEarlyWellKnownAttributeData)lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData;
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

        internal sealed override void DecodeWellKnownAttribute(ref DecodeWellKnownAttributeArguments<AttributeSyntax, CSharpAttributeData, AttributeLocation> arguments)
        {
            Debug.Assert((object)arguments.AttributeSyntaxOpt != null);

            var attribute = arguments.Attribute;
            Debug.Assert(!attribute.HasErrors);
            Debug.Assert(arguments.SymbolPart == AttributeLocation.None);

            if (attribute.IsTargetAttribute(this, AttributeDescription.AttributeUsageAttribute))
            {
                DecodeAttributeUsageAttribute(attribute, arguments.AttributeSyntaxOpt, diagnose: true, diagnosticsOpt: arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DefaultMemberAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasDefaultMemberAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CoClassAttribute))
            {
                DecodeCoClassAttribute(ref arguments);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ConditionalAttribute))
            {
                ValidateConditionalAttribute(attribute, arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.GuidAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().GuidString = attribute.DecodeGuidAttribute(arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SpecialNameAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasSpecialNameAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SerializableAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasSerializableAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ExcludeFromCodeCoverageAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasExcludeFromCodeCoverageAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.StructLayoutAttribute))
            {
                AttributeData.DecodeStructLayoutAttribute<TypeWellKnownAttributeData, AttributeSyntax, CSharpAttributeData, AttributeLocation>(
                    ref arguments, this.DefaultMarshallingCharSet, defaultAutoLayoutSize: 0, messageProvider: MessageProvider.Instance);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SuppressUnmanagedCodeSecurityAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasSuppressUnmanagedCodeSecurityAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.ClassInterfaceAttribute))
            {
                attribute.DecodeClassInterfaceAttribute(arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.InterfaceTypeAttribute))
            {
                attribute.DecodeInterfaceTypeAttribute(arguments.AttributeSyntaxOpt, arguments.Diagnostics);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.WindowsRuntimeImportAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasWindowsRuntimeImportAttribute = true;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.RequiredAttributeAttribute))
            {
                // CS1608: The Required attribute is not permitted on C# types
                arguments.Diagnostics.Add(ErrorCode.ERR_CantUseRequiredAttribute, arguments.AttributeSyntaxOpt.Name.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.CaseSensitiveExtensionAttribute))
            {
                // ExtensionAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitExtension, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.DynamicAttribute))
            {
                // DynamicAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitDynamicAttr, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsReadOnlyAttribute))
            {
                // IsReadOnlyAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsReadOnlyAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsUnmanagedAttribute))
            {
                // IsUnmanagedAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsUnmanagedAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.IsByRefLikeAttribute))
            {
                // IsByRefLikeAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitReservedAttr, arguments.AttributeSyntaxOpt.Location, AttributeDescription.IsByRefLikeAttribute.FullName);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.TupleElementNamesAttribute))
            {
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitTupleElementNamesAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.SecurityCriticalAttribute)
                || attribute.IsTargetAttribute(this, AttributeDescription.SecuritySafeCriticalAttribute))
            {
                arguments.GetOrCreateData<TypeWellKnownAttributeData>().HasSecurityCriticalAttributes = true;
            }
            else if (_lazyIsExplicitDefinitionOfNoPiaLocalType == ThreeState.Unknown && attribute.IsTargetAttribute(this, AttributeDescription.TypeIdentifierAttribute))
            {
                _lazyIsExplicitDefinitionOfNoPiaLocalType = ThreeState.True;
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NullableAttribute))
            {
                // NullableAttribute should not be set explicitly.
                arguments.Diagnostics.Add(ErrorCode.ERR_ExplicitNullableAttribute, arguments.AttributeSyntaxOpt.Location);
            }
            else if (attribute.IsTargetAttribute(this, AttributeDescription.NullableContextAttribute))
            {
                ReportExplicitUseOfNullabilityAttribute(in arguments, AttributeDescription.NullableContextAttribute);
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
            ImmutableArray<SyntaxList<AttributeListSyntax>> attributeLists = GetAttributeDeclarations();

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
        private AttributeUsageInfo DecodeAttributeUsageAttribute(CSharpAttributeData attribute, AttributeSyntax node, bool diagnose, DiagnosticBag diagnosticsOpt = null)
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
                        CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(0, node);
                        diagnosticsOpt.Add(ErrorCode.ERR_InvalidAttributeArgument, attributeArgumentSyntax.Location, node.GetErrorDisplayName());
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
                CommonTypeEarlyWellKnownAttributeData data = this.GetEarlyDecodedWellKnownAttributeData();
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

        private void ValidateConditionalAttribute(CSharpAttributeData attribute, AttributeSyntax node, DiagnosticBag diagnostics)
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
                    CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(0, node);
                    diagnostics.Add(ErrorCode.ERR_BadArgumentToAttribute, attributeArgumentSyntax.Location, node.GetErrorDisplayName());
                }
            }
        }

        internal override bool HasSpecialName
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                return data != null && data.HasSpecialNameAttribute;
            }
        }

        internal override bool HasCodeAnalysisEmbeddedAttribute
        {
            get
            {
                var data = GetEarlyDecodedWellKnownAttributeData();
                return data != null && data.HasCodeAnalysisEmbeddedAttribute;
            }
        }

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

        internal override bool IsDirectlyExcludedFromCodeCoverage =>
            GetDecodedWellKnownAttributeData()?.HasExcludeFromCodeCoverageAttribute == true;

        private bool HasInstanceFields()
        {
            var members = this.GetMembersUnordered();
            for (var i = 0; i < members.Length; i++)
            {
                var m = members[i];
                if (!m.IsStatic)
                {
                    switch (m.Kind)
                    {
                        case SymbolKind.Field:
                            return true;

                        case SymbolKind.Event:
                            if (((EventSymbol)m).AssociatedField != null)
                            {
                                return true;
                            }
                            break;
                    }
                }
            }

            return false;
        }

        internal sealed override TypeLayout Layout
        {
            get
            {
                var data = GetDecodedWellKnownAttributeData();
                if (data != null && data.HasStructLayoutAttribute)
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

        internal override void PostDecodeWellKnownAttributes(ImmutableArray<CSharpAttributeData> boundAttributes, ImmutableArray<AttributeSyntax> allAttributeSyntaxNodes, DiagnosticBag diagnostics, AttributeLocation symbolPart, WellKnownAttributeData decodedData)
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
                    int index = boundAttributes.IndexOfAttribute(this, AttributeDescription.ComImportAttribute);
                    diagnostics.Add(ErrorCode.ERR_ComImportWithoutUuidAttribute, allAttributeSyntaxNodes[index].Name.Location, this.Name);
                }

                if (this.TypeKind == TypeKind.Class)
                {
                    var baseType = this.BaseTypeNoUseSiteDiagnostics;
                    if ((object)baseType != null && baseType.SpecialType != SpecialType.System_Object)
                    {
                        // CS0424: '{0}': a class with the ComImport attribute cannot specify a base class
                        diagnostics.Add(ErrorCode.ERR_ComImportWithBase, this.Locations[0], this.Name);
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
                int index = boundAttributes.IndexOfAttribute(this, AttributeDescription.CoClassAttribute);
                diagnostics.Add(ErrorCode.WRN_CoClassWithoutComImport, allAttributeSyntaxNodes[index].Location, this.Name);
            }

            // Report ERR_DefaultMemberOnIndexedType if type has a default member attribute and has indexers.
            if (data != null && data.HasDefaultMemberAttribute && this.Indexers.Any())
            {
                Debug.Assert(boundAttributes.Any());

                int index = boundAttributes.IndexOfAttribute(this, AttributeDescription.DefaultMemberAttribute);
                diagnostics.Add(ErrorCode.ERR_DefaultMemberOnIndexedType, allAttributeSyntaxNodes[index].Name.Location);
            }

            base.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData);
        }

        /// <remarks>
        /// These won't be returned by GetAttributes on source methods, but they
        /// will be returned by GetAttributes on metadata symbols.
        /// </remarks>
        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
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

                // If user specified an Obsolete attribute, we cannot emit ours.
                // NB: we do not check the kind of deprecation. 
                //     we will not emit Obsolete even if Deprecated or Experimental was used.
                //     we do not want to get into a scenario where different kinds of deprecation are combined together.
                //
                if (obsoleteData == null && !this.IsRestrictedType(ignoreSpanLikeTypes: true))
                {
                    AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_ObsoleteAttribute__ctor,
                        ImmutableArray.Create(
                            new TypedConstant(compilation.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, PEModule.ByRefLikeMarker), // message
                            new TypedConstant(compilation.GetSpecialType(SpecialType.System_Boolean), TypedConstantKind.Primitive, true)), // error=true
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
        }

        #endregion
    }
}
