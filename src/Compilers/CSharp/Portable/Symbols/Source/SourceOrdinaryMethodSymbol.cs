// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SourceOrdinaryMethodSymbol : SourceOrdinaryMethodSymbolBase
    {
        private readonly TypeSymbol _explicitInterfaceType;
        private readonly bool _isExpressionBodied;
        private readonly bool _hasAnyBody;
        private readonly RefKind _refKind;
        private bool _lazyIsVararg;

        /// <summary>
        /// A collection of type parameter constraints, populated when
        /// constraints for the first type parameter is requested.
        /// Initialized in two steps. Hold a copy if accessing during initialization.
        /// </summary>
        private ImmutableArray<TypeParameterConstraintClause> _lazyTypeParameterConstraints;

        /// <summary>
        /// If this symbol represents a partial method definition or implementation part, its other part (if any).
        /// This should be set, if at all, before this symbol appears among the members of its owner.  
        /// The implementation part is not listed among the "members" of the enclosing type.
        /// </summary>
        private SourceOrdinaryMethodSymbol _otherPartOfPartial;

        public static SourceOrdinaryMethodSymbol CreateMethodSymbol(
            NamedTypeSymbol containingType,
            Binder bodyBinder,
            MethodDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            var interfaceSpecifier = syntax.ExplicitInterfaceSpecifier;
            var nameToken = syntax.Identifier;

            TypeSymbol explicitInterfaceType;
            string discardedAliasQualifier;
            var name = ExplicitInterfaceHelpers.GetMemberNameAndInterfaceSymbol(bodyBinder, interfaceSpecifier, nameToken.ValueText, diagnostics, out explicitInterfaceType, out discardedAliasQualifier);
            var location = new SourceLocation(nameToken);

            var methodKind = interfaceSpecifier == null
                ? MethodKind.Ordinary
                : MethodKind.ExplicitInterfaceImplementation;

            return new SourceOrdinaryMethodSymbol(containingType, explicitInterfaceType, name, location, syntax, methodKind, diagnostics);
        }

        private SourceOrdinaryMethodSymbol(
            NamedTypeSymbol containingType,
            TypeSymbol explicitInterfaceType,
            string name,
            Location location,
            MethodDeclarationSyntax syntax,
            MethodKind methodKind,
            DiagnosticBag diagnostics) :
            base(containingType,
                 name,
                 location,
                 syntax,
                 methodKind,
                 isIterator: SyntaxFacts.HasYieldOperations(syntax.Body),
                 isExtensionMethod: syntax.ParameterList.Parameters.FirstOrDefault() is ParameterSyntax firstParam &&
                                    !firstParam.IsArgList &&
                                    firstParam.Modifiers.Any(SyntaxKind.ThisKeyword),
                 isPartial: syntax.Modifiers.IndexOf(SyntaxKind.PartialKeyword) < 0,
                 hasBody: syntax.Body != null || syntax.ExpressionBody != null,
                 diagnostics)
        {
            _explicitInterfaceType = explicitInterfaceType;

            bool hasBlockBody = syntax.Body != null;
            _isExpressionBodied = !hasBlockBody && syntax.ExpressionBody != null;
            bool hasBody = hasBlockBody || _isExpressionBodied;
            _hasAnyBody = hasBody;
            _refKind = syntax.ReturnType.GetRefKind();

            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
        }

        protected override ImmutableArray<TypeParameterSymbol> MakeTypeParameters(CSharpSyntaxNode node, DiagnosticBag diagnostics)
        {
            var syntax = (MethodDeclarationSyntax)node;
            if (syntax.Arity == 0)
            {
                ReportErrorIfHasConstraints(syntax.ConstraintClauses, diagnostics);
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
            else
            {
                return MakeTypeParameters(syntax, diagnostics);
            }
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            var withTypeParamsBinder = this.DeclaringCompilation.GetBinderFactory(syntax.SyntaxTree).GetBinder(syntax.ReturnType, syntax, this);

            SyntaxToken arglistToken;

            // Constraint checking for parameter and return types must be delayed until
            // the method has been added to the containing type member list since
            // evaluating the constraints may depend on accessing this method from
            // the container (comparing this method to others to find overrides for
            // instance). Constraints are checked in AfterAddingTypeMembersChecks.
            var signatureBinder = withTypeParamsBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

            ImmutableArray<ParameterSymbol> parameters = ParameterHelpers.MakeParameters(
                signatureBinder, this, syntax.ParameterList, out arglistToken,
                allowRefOrOut: true,
                allowThis: true,
                addRefReadOnlyModifier: IsVirtual || IsAbstract,
                diagnostics: diagnostics);

            _lazyIsVararg = (arglistToken.Kind() == SyntaxKind.ArgListKeyword);
            RefKind refKind;
            var returnTypeSyntax = syntax.ReturnType.SkipRef(out refKind);
            TypeWithAnnotations returnType = signatureBinder.BindType(returnTypeSyntax, diagnostics);

            // span-like types are returnable in general
            if (returnType.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                if (returnType.SpecialType == SpecialType.System_TypedReference &&
                    (this.ContainingType.SpecialType == SpecialType.System_TypedReference || this.ContainingType.SpecialType == SpecialType.System_ArgIterator))
                {
                    // Two special cases: methods in the special types TypedReference and ArgIterator are allowed to return TypedReference
                }
                else
                {
                    // The return type of a method, delegate, or function pointer cannot be '{0}'
                    diagnostics.Add(ErrorCode.ERR_MethodReturnCantBeRefAny, syntax.ReturnType.Location, returnType.Type);
                }
            }

            Debug.Assert(this.RefKind == RefKind.None || !returnType.IsVoidType() || returnTypeSyntax.HasErrors);

            ImmutableArray<TypeParameterConstraintClause> declaredConstraints = default;

            if (this.Arity != 0 && (syntax.ExplicitInterfaceSpecifier != null || IsOverride))
            {
                // When a generic method overrides a generic method declared in a base class, or is an 
                // explicit interface member implementation of a method in a base interface, the method
                // shall not specify any type-parameter-constraints-clauses, except for a struct, class, or default constraint.
                // In these cases, the type parameters of the method inherit constraints from the method being overridden or 
                // implemented.
                if (syntax.ConstraintClauses.Count > 0)
                {
                    Binder.CheckFeatureAvailability(syntax.SyntaxTree, MessageID.IDS_OverrideWithConstraints, diagnostics,
                                                    syntax.ConstraintClauses[0].WhereKeyword.GetLocation());

                    IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverride = null;
                    declaredConstraints = signatureBinder.WithAdditionalFlags(BinderFlags.GenericConstraintsClause | BinderFlags.SuppressConstraintChecks).
                                              BindTypeParameterConstraintClauses(this, TypeParameters, syntax.TypeParameterList, syntax.ConstraintClauses,
                                                                                 ref isValueTypeOverride,
                                                                                 diagnostics, isForOverride: true);
                }

                // Force resolution of nullable type parameter used in the signature of an override or explicit interface implementation
                // based on constraints specified by the declaration.
                foreach (var param in parameters)
                {
                    forceMethodTypeParameters(param.TypeWithAnnotations, this, declaredConstraints);
                }

                forceMethodTypeParameters(returnType, this, declaredConstraints);
            }

            return (returnType, parameters, _lazyIsVararg, declaredConstraints);

            static void forceMethodTypeParameters(TypeWithAnnotations type, SourceOrdinaryMethodSymbol method, ImmutableArray<TypeParameterConstraintClause> declaredConstraints)
            {
                type.VisitType(null, (type, args, unused2) =>
                {
                    if (type.DefaultType is TypeParameterSymbol typeParameterSymbol && typeParameterSymbol.DeclaringMethod == (object)args.method)
                    {
                        var asValueType = args.declaredConstraints.IsDefault ||
                            (args.declaredConstraints[typeParameterSymbol.Ordinal].Constraints & (TypeParameterConstraintKind.ReferenceType | TypeParameterConstraintKind.Default)) == 0;
                        type.TryForceResolve(asValueType);
                    }
                    return false;
                }, typePredicate: null, arg: (method, declaredConstraints), canDigThroughNullable: false, useDefaultType: true);
            }
        }

        protected override void ExtensionMethodChecks(DiagnosticBag diagnostics)
        {
            // errors relevant for extension methods
            if (IsExtensionMethod)
            {
                var syntax = GetSyntax();
                var location = locations[0];
                var parameter0Type = this.Parameters[0].TypeWithAnnotations;
                var parameter0RefKind = this.Parameters[0].RefKind;
                if (!parameter0Type.Type.IsValidExtensionParameterType())
                {
                    // Duplicate Dev10 behavior by selecting the parameter type.
                    var parameterSyntax = syntax.ParameterList.Parameters[0];
                    Debug.Assert(parameterSyntax.Type != null);
                    var loc = parameterSyntax.Type.Location;
                    diagnostics.Add(ErrorCode.ERR_BadTypeforThis, loc, parameter0Type.Type);
                }
                else if (parameter0RefKind == RefKind.Ref && !parameter0Type.Type.IsValueType)
                {
                    diagnostics.Add(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, location, Name);
                }
                else if (parameter0RefKind == RefKind.In && parameter0Type.TypeKind != TypeKind.Struct)
                {
                    diagnostics.Add(ErrorCode.ERR_InExtensionMustBeValueType, location, Name);
                }
                else if ((object)ContainingType.ContainingType != null)
                {
                    diagnostics.Add(ErrorCode.ERR_ExtensionMethodsDecl, location, ContainingType.Name);
                }
                else if (!ContainingType.IsScriptClass && !(ContainingType.IsStatic && ContainingType.Arity == 0))
                {
                    // Duplicate Dev10 behavior by selecting the containing type identifier. However if there
                    // is no containing type (in the interactive case for instance), select the method identifier.
                    var typeDecl = syntax.Parent as TypeDeclarationSyntax;
                    var identifier = (typeDecl != null) ? typeDecl.Identifier : syntax.Identifier;
                    var loc = identifier.GetLocation();
                    diagnostics.Add(ErrorCode.ERR_BadExtensionAgg, loc);
                }
                else if (!IsStatic)
                {
                    diagnostics.Add(ErrorCode.ERR_BadExtensionMeth, location);
                }
                else
                {
                    // Verify ExtensionAttribute is available.
                    var attributeConstructor = DeclaringCompilation.GetWellKnownTypeMember(WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor);
                    if ((object)attributeConstructor == null)
                    {
                        var memberDescriptor = WellKnownMembers.GetDescriptor(WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor);
                        // do not use Binder.ReportUseSiteErrorForAttributeCtor in this case, because we'll need to report a special error id, not a generic use site error.
                        diagnostics.Add(
                            ErrorCode.ERR_ExtensionAttrNotFound,
                            syntax.ParameterList.Parameters[0].Modifiers.FirstOrDefault(SyntaxKind.ThisKeyword).GetLocation(),
                            memberDescriptor.DeclaringTypeMetadataName);
                    }
                }
            }
        }

        protected override MethodSymbol FindExplicitlyImplementedMethod(DiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            return this.FindExplicitlyImplementedMethod(_explicitInterfaceType, syntax.Identifier.ValueText, syntax.ExplicitInterfaceSpecifier, diagnostics);
        }

        protected override Location ReturnTypeLocation => GetSyntax().ReturnType.Location;

        protected override TypeSymbol ExplicitInterfaceType => _explicitInterfaceType;

        protected override bool HasAnyBody => _hasAnyBody;

        internal MethodDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (MethodDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        protected override void CompleteAsyncMethodChecksBetweenStartAndFinish()
        {
            if (IsPartialDefinition)
            {
                DeclaringCompilation.SymbolDeclaredEvent(this);
            }
        }

        public override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses()
        {
            if (_lazyTypeParameterConstraints.IsDefault)
            {
                var diagnostics = DiagnosticBag.GetInstance();
                var syntax = GetSyntax();
                var withTypeParametersBinder =
                    this.DeclaringCompilation
                    .GetBinderFactory(syntax.SyntaxTree)
                    .GetBinder(syntax.ReturnType, syntax, this);
                var constraints = this.MakeTypeParameterConstraints(
                    withTypeParametersBinder,
                    TypeParameters,
                    syntax.TypeParameterList,
                    syntax.ConstraintClauses,
                    syntax.Identifier.GetLocation(),
                    diagnostics);
                if (ImmutableInterlocked.InterlockedInitialize(ref _lazyTypeParameterConstraints, constraints))
                {
                    this.AddDeclarationDiagnostics(diagnostics);
                }
                diagnostics.Free();
            }

            return _lazyTypeParameterConstraints;
        }

        public override bool IsVararg
        {
            get
            {
                LazyMethodChecks();
                return _lazyIsVararg;
            }
        }

        protected override int GetParameterCountFromSyntax() => GetSyntax().ParameterList.ParameterCount;

        public override RefKind RefKind
        {
            get
            {
                return _refKind;
            }
        }

        internal static void InitializePartialMethodParts(SourceOrdinaryMethodSymbol definition, SourceOrdinaryMethodSymbol implementation)
        {
            Debug.Assert(definition.IsPartialDefinition);
            Debug.Assert(implementation.IsPartialImplementation);
            Debug.Assert((object)definition._otherPartOfPartial == null);
            Debug.Assert((object)implementation._otherPartOfPartial == null);

            definition._otherPartOfPartial = implementation;
            implementation._otherPartOfPartial = definition;
        }

        /// <summary>
        /// If this is a partial implementation part returns the definition part and vice versa.
        /// </summary>
        internal SourceOrdinaryMethodSymbol OtherPartOfPartial
        {
            get { return _otherPartOfPartial; }
        }

        /// <summary>
        /// Returns true if this symbol represents a partial method definition (the part that specifies a signature but no body).
        /// </summary>
        internal bool IsPartialDefinition
        {
            get
            {
                return this.IsPartial && !_hasAnyBody && !HasExternModifier;
            }
        }

        /// <summary>
        /// Returns true if this symbol represents a partial method implementation (the part that specifies both signature and body).
        /// </summary>
        internal bool IsPartialImplementation
        {
            get
            {
                return this.IsPartial && (_hasAnyBody || HasExternModifier);
            }
        }

        /// <summary>
        /// True if this is a partial method that doesn't have an implementation part.
        /// </summary>
        internal bool IsPartialWithoutImplementation
        {
            get
            {
                return this.IsPartialDefinition && (object)_otherPartOfPartial == null;
            }
        }

        /// <summary>
        /// Returns the implementation part of a partial method definition, 
        /// or null if this is not a partial method or it is the definition part.
        /// </summary>
        internal SourceOrdinaryMethodSymbol SourcePartialDefinition
        {
            get
            {
                return this.IsPartialImplementation ? _otherPartOfPartial : null;
            }
        }

        /// <summary>
        /// Returns the definition part of a partial method implementation, 
        /// or null if this is not a partial method or it is the implementation part.
        /// </summary>
        internal SourceOrdinaryMethodSymbol SourcePartialImplementation
        {
            get
            {
                return this.IsPartialDefinition ? _otherPartOfPartial : null;
            }
        }

        public override MethodSymbol PartialDefinitionPart
        {
            get
            {
                return SourcePartialDefinition;
            }
        }

        public override MethodSymbol PartialImplementationPart
        {
            get
            {
                return SourcePartialImplementation;
            }
        }

        public sealed override bool IsExtern
        {
            get
            {
                return IsPartialDefinition
                    ? _otherPartOfPartial?.IsExtern ?? false
                    : HasExternModifier;
            }
        }

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            ref var lazyDocComment = ref expandIncludes ? ref this.lazyExpandedDocComment : ref this.lazyDocComment;
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(SourcePartialImplementation ?? this, expandIncludes, ref lazyDocComment);
        }

        protected override SourceMemberMethodSymbol BoundAttributesSource
        {
            get
            {
                return this.SourcePartialDefinition;
            }
        }

        internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            if ((object)this.SourcePartialImplementation != null)
            {
                return OneOrMany.Create(ImmutableArray.Create(AttributeDeclarationSyntaxList, this.SourcePartialImplementation.AttributeDeclarationSyntaxList));
            }
            else
            {
                return OneOrMany.Create(AttributeDeclarationSyntaxList);
            }
        }

        private SyntaxList<AttributeListSyntax> AttributeDeclarationSyntaxList
        {
            get
            {
                var sourceContainer = this.ContainingType as SourceMemberContainerTypeSymbol;
                if ((object)sourceContainer != null && sourceContainer.AnyMemberHasAttributes)
                {
                    return this.GetSyntax().AttributeLists;
                }

                return default(SyntaxList<AttributeListSyntax>);
            }
        }

        internal override bool IsExpressionBodied
        {
            get { return _isExpressionBodied; }
        }

        protected override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, DiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            return ModifierUtils.MakeAndCheckNontypeMemberModifiers(syntax.Modifiers, defaultAccess: DeclarationModifiers.None, allowedModifiers, Locations[0], diagnostics, out _);
        }

        private ImmutableArray<TypeParameterSymbol> MakeTypeParameters(MethodDeclarationSyntax syntax, DiagnosticBag diagnostics)
        {
            Debug.Assert(syntax.TypeParameterList != null);

            OverriddenMethodTypeParameterMapBase typeMap = null;
            if (this.IsOverride)
            {
                typeMap = new OverriddenMethodTypeParameterMap(this);
            }
            else if (this.IsExplicitInterfaceImplementation)
            {
                typeMap = new ExplicitInterfaceMethodTypeParameterMap(this);
            }

            var typeParameters = syntax.TypeParameterList.Parameters;
            var result = ArrayBuilder<TypeParameterSymbol>.GetInstance();

            for (int ordinal = 0; ordinal < typeParameters.Count; ordinal++)
            {
                var parameter = typeParameters[ordinal];
                if (parameter.VarianceKeyword.Kind() != SyntaxKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_IllegalVarianceSyntax, parameter.VarianceKeyword.GetLocation());
                }

                var identifier = parameter.Identifier;
                var location = identifier.GetLocation();
                var name = identifier.ValueText;

                // Note: It is not an error to have a type parameter named the same as its enclosing method: void M<M>() {}

                for (int i = 0; i < result.Count; i++)
                {
                    if (name == result[i].Name)
                    {
                        diagnostics.Add(ErrorCode.ERR_DuplicateTypeParameter, location, name);
                        break;
                    }
                }

                var tpEnclosing = ContainingType.FindEnclosingTypeParameter(name);
                if ((object)tpEnclosing != null)
                {
                    // Type parameter '{0}' has the same name as the type parameter from outer type '{1}'
                    diagnostics.Add(ErrorCode.WRN_TypeParameterSameAsOuterTypeParameter, location, name, tpEnclosing.ContainingType);
                }

                var syntaxRefs = ImmutableArray.Create(parameter.GetReference());
                var locations = ImmutableArray.Create(location);
                var typeParameter = (typeMap != null) ?
                    (TypeParameterSymbol)new SourceOverridingMethodTypeParameterSymbol(
                        typeMap,
                        name,
                        ordinal,
                        locations,
                        syntaxRefs) :
                    new SourceMethodTypeParameterSymbol(
                        this,
                        name,
                        ordinal,
                        locations,
                        syntaxRefs);

                result.Add(typeParameter);
            }

            return result.ToImmutableAndFree();
        }

        internal override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            var implementingPart = this.SourcePartialImplementation;
            if ((object)implementingPart != null)
            {
                implementingPart.ForceComplete(locationOpt, cancellationToken);
            }

            base.ForceComplete(locationOpt, cancellationToken);
        }

        internal override bool IsDefinedInSourceTree(
            SyntaxTree tree,
            TextSpan? definedWithinSpan,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Since only the declaring (and not the implementing) part of a partial method appears in the member
            // list, we need to ensure we complete the implementation part when needed.
            return
                base.IsDefinedInSourceTree(tree, definedWithinSpan, cancellationToken) ||
                this.SourcePartialImplementation?.IsDefinedInSourceTree(tree, definedWithinSpan, cancellationToken) == true;
        }

        protected override void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            if ((object)_explicitInterfaceType != null)
            {
                var syntax = this.GetSyntax();
                Debug.Assert(syntax.ExplicitInterfaceSpecifier != null);
                _explicitInterfaceType.CheckAllConstraints(DeclaringCompilation, conversions, new SourceLocation(syntax.ExplicitInterfaceSpecifier.Name), diagnostics);
            }
        }

        protected override void PartialMethodChecks(DiagnosticBag diagnostics)
        {
            var implementingPart = this.SourcePartialImplementation;
            if ((object)implementingPart != null)
            {
                PartialMethodChecks(this, implementingPart, diagnostics);
            }
        }

        /// <summary>
        /// Report differences between the defining and implementing
        /// parts of a partial method. Diagnostics are reported on the
        /// implementing part, matching Dev10 behavior.
        /// </summary>
        private static void PartialMethodChecks(SourceOrdinaryMethodSymbol definition, SourceOrdinaryMethodSymbol implementation, DiagnosticBag diagnostics)
        {
            Debug.Assert(!ReferenceEquals(definition, implementation));

            MethodSymbol constructedDefinition = definition.ConstructIfGeneric(implementation.TypeArgumentsWithAnnotations);
            bool returnTypesEqual = constructedDefinition.ReturnTypeWithAnnotations.Equals(implementation.ReturnTypeWithAnnotations, TypeCompareKind.AllIgnoreOptions);
            if (!returnTypesEqual
                && !SourceMemberContainerTypeSymbol.IsOrContainsErrorType(implementation.ReturnType)
                && !SourceMemberContainerTypeSymbol.IsOrContainsErrorType(definition.ReturnType))
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodReturnTypeDifference, implementation.Locations[0]);
            }

            if (definition.RefKind != implementation.RefKind)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodRefReturnDifference, implementation.Locations[0]);
            }

            if (definition.IsStatic != implementation.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodStaticDifference, implementation.Locations[0]);
            }

            if (definition.IsDeclaredReadOnly != implementation.IsDeclaredReadOnly)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodReadOnlyDifference, implementation.Locations[0]);
            }

            if (definition.IsExtensionMethod != implementation.IsExtensionMethod)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodExtensionDifference, implementation.Locations[0]);
            }

            if (definition.IsUnsafe != implementation.IsUnsafe && definition.CompilationAllowsUnsafe()) // Don't cascade.
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodUnsafeDifference, implementation.Locations[0]);
            }

            if (definition.IsParams() != implementation.IsParams())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodParamsDifference, implementation.Locations[0]);
            }

            if (definition.HasExplicitAccessModifier != implementation.HasExplicitAccessModifier
                || definition.DeclaredAccessibility != implementation.DeclaredAccessibility)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodAccessibilityDifference, implementation.Locations[0]);
            }

            if (definition.IsVirtual != implementation.IsVirtual
                || definition.IsOverride != implementation.IsOverride
                || definition.IsSealed != implementation.IsSealed
                || definition.IsNew != implementation.IsNew)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodExtendedModDifference, implementation.Locations[0]);
            }

            PartialMethodConstraintsChecks(definition, implementation, diagnostics);

            SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                implementation.DeclaringCompilation,
                constructedDefinition,
                implementation,
                diagnostics,
                (diagnostics, implementedMethod, implementingMethod, topLevel, returnTypesEqual) =>
                {
                    if (returnTypesEqual)
                    {
                        // report only if this is an unsafe *nullability* difference
                        diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, implementingMethod.Locations[0]);
                    }
                },
                (diagnostics, implementedMethod, implementingMethod, implementingParameter, blameAttributes, arg) =>
                {
                    diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, implementingMethod.Locations[0], new FormattedSymbol(implementingParameter, SymbolDisplayFormat.ShortFormat));
                },
                extraArgument: returnTypesEqual);
        }

        private static void PartialMethodConstraintsChecks(SourceOrdinaryMethodSymbol definition, SourceOrdinaryMethodSymbol implementation, DiagnosticBag diagnostics)
        {
            Debug.Assert(!ReferenceEquals(definition, implementation));
            Debug.Assert(definition.Arity == implementation.Arity);

            var typeParameters1 = definition.TypeParameters;

            int arity = typeParameters1.Length;
            if (arity == 0)
            {
                return;
            }

            var typeParameters2 = implementation.TypeParameters;
            var indexedTypeParameters = IndexedTypeParameterSymbol.Take(arity);
            var typeMap1 = new TypeMap(typeParameters1, indexedTypeParameters, allowAlpha: true);
            var typeMap2 = new TypeMap(typeParameters2, indexedTypeParameters, allowAlpha: true);

            // Report any mismatched method constraints.
            for (int i = 0; i < arity; i++)
            {
                var typeParameter1 = typeParameters1[i];
                var typeParameter2 = typeParameters2[i];

                if (!MemberSignatureComparer.HaveSameConstraints(typeParameter1, typeMap1, typeParameter2, typeMap2))
                {
                    diagnostics.Add(ErrorCode.ERR_PartialMethodInconsistentConstraints, implementation.Locations[0], implementation, typeParameter2.Name);
                }
                else if (!MemberSignatureComparer.HaveSameNullabilityInConstraints(typeParameter1, typeMap1, typeParameter2, typeMap2))
                {
                    diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInConstraintsOnPartialImplementation, implementation.Locations[0], implementation, typeParameter2.Name);
                }
            }
        }

        internal override bool CallsAreOmitted(SyntaxTree syntaxTree)
        {
            if (this.IsPartialWithoutImplementation)
            {
                return true;
            }

            return base.CallsAreOmitted(syntaxTree);
        }

        internal override bool GenerateDebugInfo => !IsAsync && !IsIterator;
    }
}
