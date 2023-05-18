// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceOrdinaryMethodSymbol : SourceOrdinaryMethodSymbolBase
    {
        public static SourceOrdinaryMethodSymbol CreateMethodSymbol(
            NamedTypeSymbol containingType,
            Binder bodyBinder,
            MethodDeclarationSyntax syntax,
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics)
        {
            var interfaceSpecifier = syntax.ExplicitInterfaceSpecifier;
            var nameToken = syntax.Identifier;

            TypeSymbol explicitInterfaceType;
            var name = ExplicitInterfaceHelpers.GetMemberNameAndInterfaceSymbol(bodyBinder, interfaceSpecifier, nameToken.ValueText, diagnostics, out explicitInterfaceType, aliasQualifierOpt: out _);
            var location = new SourceLocation(nameToken);

            var methodKind = interfaceSpecifier == null
                ? MethodKind.Ordinary
                : MethodKind.ExplicitInterfaceImplementation;

            // Use a smaller type for the common case of non-generic, non-partial, non-explicit-impl methods.

            return explicitInterfaceType is null && !syntax.Modifiers.Any(SyntaxKind.PartialKeyword) && syntax.Arity == 0
                ? new SourceOrdinaryMethodSymbolSimple(containingType, name, location, syntax, methodKind, isNullableAnalysisEnabled, diagnostics)
                : new SourceOrdinaryMethodSymbolComplex(containingType, explicitInterfaceType, name, location, syntax, methodKind, isNullableAnalysisEnabled, diagnostics);
        }

        private SourceOrdinaryMethodSymbol(
            NamedTypeSymbol containingType,
            string name,
            Location location,
            MethodDeclarationSyntax syntax,
            MethodKind methodKind,
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics) :
            base(containingType,
                 name,
                 location,
                 syntax,
                 methodKind,
                 refKind: syntax.ReturnType.SkipScoped(out _).GetRefKindInLocalOrReturn(diagnostics),
                 isIterator: SyntaxFacts.HasYieldOperations(syntax.Body),
                 isExtensionMethod: syntax.ParameterList.Parameters.FirstOrDefault() is ParameterSyntax firstParam &&
                                    !firstParam.IsArgList &&
                                    firstParam.Modifiers.Any(SyntaxKind.ThisKeyword),
                 isReadOnly: false,
                 hasAnyBody: syntax.Body != null || syntax.ExpressionBody != null,
                 isExpressionBodied: syntax is { Body: null, ExpressionBody: not null },
                 isNullableAnalysisEnabled: isNullableAnalysisEnabled,
                 isVarArg: syntax.ParameterList.Parameters.Any(p => p.IsArgList),
                 diagnostics)
        {
            Debug.Assert(diagnostics.DiagnosticBag is object);

            Debug.Assert(syntax.ReturnType is not ScopedTypeSyntax);

            if (syntax.Arity == 0)
            {
                ReportErrorIfHasConstraints(syntax.ConstraintClauses, diagnostics.DiagnosticBag);
            }

            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            var withTypeParamsBinder = this.DeclaringCompilation.GetBinderFactory(syntax.SyntaxTree).GetBinder(syntax.ReturnType, syntax, this);

            // Constraint checking for parameter and return types must be delayed until
            // the method has been added to the containing type member list since
            // evaluating the constraints may depend on accessing this method from
            // the container (comparing this method to others to find overrides for
            // instance). Constraints are checked in AfterAddingTypeMembersChecks.
            var signatureBinder = withTypeParamsBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

            ImmutableArray<ParameterSymbol> parameters = ParameterHelpers.MakeParameters(
                signatureBinder, this, syntax.ParameterList, out _,
                allowRefOrOut: true,
                allowThis: true,
                addRefReadOnlyModifier: IsVirtual || IsAbstract,
                diagnostics: diagnostics).Cast<SourceParameterSymbol, ParameterSymbol>();

            var returnTypeSyntax = syntax.ReturnType;
            Debug.Assert(returnTypeSyntax is not ScopedTypeSyntax);

            returnTypeSyntax = returnTypeSyntax.SkipScoped(out _).SkipRef();
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
                    Binder.CheckFeatureAvailability(
                        syntax.ConstraintClauses[0].WhereKeyword, MessageID.IDS_OverrideWithConstraints, diagnostics);

                    declaredConstraints = signatureBinder.WithAdditionalFlags(BinderFlags.GenericConstraintsClause | BinderFlags.SuppressConstraintChecks).
                                              BindTypeParameterConstraintClauses(this, TypeParameters, syntax.TypeParameterList, syntax.ConstraintClauses,
                                                                                 diagnostics, performOnlyCycleSafeValidation: false, isForOverride: true);
                    Debug.Assert(declaredConstraints.All(clause => clause.ConstraintTypes.IsEmpty));
                }

                // Force resolution of nullable type parameter used in the signature of an override or explicit interface implementation
                // based on constraints specified by the declaration.
                foreach (var param in parameters)
                {
                    forceMethodTypeParameters(param.TypeWithAnnotations, this, declaredConstraints);
                }

                forceMethodTypeParameters(returnType, this, declaredConstraints);
            }

            return (returnType, parameters, declaredConstraints);

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

        protected sealed override void ExtensionMethodChecks(BindingDiagnosticBag diagnostics)
        {
            // errors relevant for extension methods
            if (IsExtensionMethod)
            {
                var syntax = GetSyntax();
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
                    diagnostics.Add(ErrorCode.ERR_RefExtensionMustBeValueTypeOrConstrainedToOne, _location, Name);
                }
                else if (parameter0RefKind == RefKind.In && parameter0Type.TypeKind != TypeKind.Struct)
                {
                    diagnostics.Add(ErrorCode.ERR_InExtensionMustBeValueType, _location, Name);
                }
                else if ((object)ContainingType.ContainingType != null)
                {
                    diagnostics.Add(ErrorCode.ERR_ExtensionMethodsDecl, _location, ContainingType.Name);
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
                    diagnostics.Add(ErrorCode.ERR_BadExtensionMeth, _location);
                }
                else
                {
                    // Verify ExtensionAttribute is available.
                    var attributeConstructor = Binder.GetWellKnownTypeMember(DeclaringCompilation, WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor, out var useSiteInfo);

                    var thisKeyword = syntax.ParameterList.Parameters[0].Modifiers.FirstOrDefault(SyntaxKind.ThisKeyword);
                    if ((object)attributeConstructor == null)
                    {
                        var memberDescriptor = WellKnownMembers.GetDescriptor(WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor);
                        // do not use Binder.ReportUseSiteErrorForAttributeCtor in this case, because we'll need to report a special error id, not a generic use site error.
                        diagnostics.Add(
                            ErrorCode.ERR_ExtensionAttrNotFound,
                            thisKeyword.GetLocation(),
                            memberDescriptor.DeclaringTypeMetadataName);
                    }
                    else
                    {
                        diagnostics.Add(useSiteInfo, thisKeyword);
                    }
                }
            }
        }

        protected sealed override Location ReturnTypeLocation => GetSyntax().ReturnType.Location;

        internal MethodDeclarationSyntax GetSyntax()
        {
            Debug.Assert(syntaxReferenceOpt != null);
            return (MethodDeclarationSyntax)syntaxReferenceOpt.GetSyntax();
        }

        internal sealed override ExecutableCodeBinder TryGetBodyBinder(BinderFactory binderFactoryOpt = null, bool ignoreAccessibility = false)
        {
            return TryGetBodyBinderFromSyntax(binderFactoryOpt, ignoreAccessibility);
        }

        protected sealed override void CompleteAsyncMethodChecksBetweenStartAndFinish()
        {
            if (IsPartialDefinition)
            {
                DeclaringCompilation.SymbolDeclaredEvent(this);
            }
        }

        protected sealed override int GetParameterCountFromSyntax() => GetSyntax().ParameterList.ParameterCount;

        internal static void InitializePartialMethodParts(SourceOrdinaryMethodSymbol definition, SourceOrdinaryMethodSymbol implementation)
        {
            Debug.Assert(definition.IsPartialDefinition);
            Debug.Assert(implementation.IsPartialImplementation);

            // Thse casts must succeed as being partial means we would have created the uncommon forms.
            SourceOrdinaryMethodSymbolComplex.InitializePartialMethodParts(
                (SourceOrdinaryMethodSymbolComplex)definition,
                (SourceOrdinaryMethodSymbolComplex)implementation);
        }

        /// <summary>
        /// If this is a partial implementation part returns the definition part and vice versa.
        /// </summary>
        internal abstract SourceOrdinaryMethodSymbol OtherPartOfPartial { get; }

        /// <summary>
        /// Returns true if this symbol represents a partial method definition (the part that specifies a signature but no body).
        /// </summary>
        internal bool IsPartialDefinition
        {
            get
            {
                return this.IsPartial && !HasAnyBody && !HasExternModifier;
            }
        }

        /// <summary>
        /// Returns true if this symbol represents a partial method implementation (the part that specifies both signature and body).
        /// </summary>
        internal bool IsPartialImplementation
        {
            get
            {
                return this.IsPartial && (HasAnyBody || HasExternModifier);
            }
        }

        /// <summary>
        /// True if this is a partial method that doesn't have an implementation part.
        /// </summary>
        internal bool IsPartialWithoutImplementation
        {
            get
            {
                return this.IsPartialDefinition && this.OtherPartOfPartial is null;
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
                return this.IsPartialImplementation ? this.OtherPartOfPartial : null;
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
                return this.IsPartialDefinition ? this.OtherPartOfPartial : null;
            }
        }

        public sealed override MethodSymbol PartialDefinitionPart
        {
            get
            {
                return SourcePartialDefinition;
            }
        }

        public sealed override MethodSymbol PartialImplementationPart
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
                    ? this.OtherPartOfPartial?.IsExtern ?? false
                    : HasExternModifier;
            }
        }

        public sealed override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            ref var lazyDocComment = ref expandIncludes ? ref this.lazyExpandedDocComment : ref this.lazyDocComment;
            return SourceDocumentationCommentUtils.GetAndCacheDocumentationComment(this, expandIncludes, ref lazyDocComment);
        }

        protected sealed override SourceMemberMethodSymbol BoundAttributesSource
        {
            get
            {
                return this.SourcePartialDefinition;
            }
        }

        internal sealed override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
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

        protected sealed override DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, BindingDiagnosticBag diagnostics)
        {
            var syntax = GetSyntax();
            return ModifierUtils.MakeAndCheckNonTypeMemberModifiers(isOrdinaryMethod: true, isForInterfaceMember: ContainingType.IsInterface,
                                                                    syntax.Modifiers, defaultAccess: DeclarationModifiers.None, allowedModifiers, GetFirstLocation(), diagnostics, out _);
        }

        internal sealed override void ForceComplete(SourceLocation locationOpt, CancellationToken cancellationToken)
        {
            var implementingPart = this.SourcePartialImplementation;
            if ((object)implementingPart != null)
            {
                implementingPart.ForceComplete(locationOpt, cancellationToken);
            }

            base.ForceComplete(locationOpt, cancellationToken);
        }

        public sealed override bool IsDefinedInSourceTree(
            SyntaxTree tree,
            TextSpan? definedWithinSpan,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // Since only the declaring (and not the implementing) part of a partial method appears in the member
            // list, we need to ensure we complete the implementation part when needed.
            Debug.Assert(this.DeclaringSyntaxReferences.Length == 1);
            return
                IsDefinedInSourceTree(this.SyntaxRef, tree, definedWithinSpan) ||
                this.SourcePartialImplementation?.IsDefinedInSourceTree(tree, definedWithinSpan, cancellationToken) == true;
        }

        protected abstract override void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, BindingDiagnosticBag diagnostics);

        protected sealed override void PartialMethodChecks(BindingDiagnosticBag diagnostics)
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
        private static void PartialMethodChecks(SourceOrdinaryMethodSymbol definition, SourceOrdinaryMethodSymbol implementation, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!ReferenceEquals(definition, implementation));

            MethodSymbol constructedDefinition = definition.ConstructIfGeneric(TypeMap.TypeParametersAsTypeSymbolsWithIgnoredAnnotations(implementation.TypeParameters));
            bool hasTypeDifferences = !constructedDefinition.ReturnTypeWithAnnotations.Equals(implementation.ReturnTypeWithAnnotations, TypeCompareKind.AllIgnoreOptions);
            if (hasTypeDifferences)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodReturnTypeDifference, implementation.GetFirstLocation());
            }
            else if (MemberSignatureComparer.ConsideringTupleNamesCreatesDifference(definition, implementation))
            {
                hasTypeDifferences = true;
                diagnostics.Add(ErrorCode.ERR_PartialMethodInconsistentTupleNames, implementation.GetFirstLocation(), definition, implementation);
            }

            if (definition.RefKind != implementation.RefKind)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodRefReturnDifference, implementation.GetFirstLocation());
            }

            if (definition.IsStatic != implementation.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodStaticDifference, implementation.GetFirstLocation());
            }

            if (definition.IsDeclaredReadOnly != implementation.IsDeclaredReadOnly)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodReadOnlyDifference, implementation.GetFirstLocation());
            }

            if (definition.IsExtensionMethod != implementation.IsExtensionMethod)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodExtensionDifference, implementation.GetFirstLocation());
            }

            if (definition.IsUnsafe != implementation.IsUnsafe && definition.CompilationAllowsUnsafe()) // Don't cascade.
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodUnsafeDifference, implementation.GetFirstLocation());
            }

            if (definition.IsParams() != implementation.IsParams())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodParamsDifference, implementation.GetFirstLocation());
            }

            if (definition.HasExplicitAccessModifier != implementation.HasExplicitAccessModifier
                || definition.DeclaredAccessibility != implementation.DeclaredAccessibility)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodAccessibilityDifference, implementation.GetFirstLocation());
            }

            if (definition.IsVirtual != implementation.IsVirtual
                || definition.IsOverride != implementation.IsOverride
                || definition.IsSealed != implementation.IsSealed
                || definition.IsNew != implementation.IsNew)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodExtendedModDifference, implementation.GetFirstLocation());
            }

            PartialMethodConstraintsChecks(definition, implementation, diagnostics);

            if (SourceMemberContainerTypeSymbol.CheckValidScopedOverride(
                constructedDefinition,
                implementation,
                diagnostics,
                static (diagnostics, implementedMethod, implementingMethod, implementingParameter, blameAttributes, arg) =>
                {
                    diagnostics.Add(ErrorCode.ERR_ScopedMismatchInParameterOfPartial, implementingMethod.GetFirstLocation(), new FormattedSymbol(implementingParameter, SymbolDisplayFormat.ShortFormat));
                },
                extraArgument: (object)null,
                allowVariance: false,
                invokedAsExtensionMethod: false))
            {
                hasTypeDifferences = true;
            }

            if (SourceMemberContainerTypeSymbol.CheckValidNullableMethodOverride(
                implementation.DeclaringCompilation,
                constructedDefinition,
                implementation,
                diagnostics,
                static (diagnostics, implementedMethod, implementingMethod, topLevel, arg) =>
                {
                    // report only if this is an unsafe *nullability* difference
                    diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInReturnTypeOnPartial, implementingMethod.GetFirstLocation());
                },
                static (diagnostics, implementedMethod, implementingMethod, implementingParameter, blameAttributes, arg) =>
                {
                    diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInParameterTypeOnPartial, implementingMethod.GetFirstLocation(), new FormattedSymbol(implementingParameter, SymbolDisplayFormat.ShortFormat));
                },
                extraArgument: (object)null))
            {
                hasTypeDifferences = true;
            }

            if ((!hasTypeDifferences && !MemberSignatureComparer.PartialMethodsStrictComparer.Equals(definition, implementation)) ||
                hasDifferencesInParameterOrTypeParameterName(definition, implementation))
            {
                diagnostics.Add(ErrorCode.WRN_PartialMethodTypeDifference, implementation.GetFirstLocation(),
                    new FormattedSymbol(definition, SymbolDisplayFormat.MinimallyQualifiedFormat),
                    new FormattedSymbol(implementation, SymbolDisplayFormat.MinimallyQualifiedFormat));
            }

            static bool hasDifferencesInParameterOrTypeParameterName(SourceOrdinaryMethodSymbol definition, SourceOrdinaryMethodSymbol implementation)
            {
                return !definition.Parameters.SequenceEqual(implementation.Parameters, (a, b) => a.Name == b.Name) ||
                    !definition.TypeParameters.SequenceEqual(implementation.TypeParameters, (a, b) => a.Name == b.Name);
            }
        }

        private static void PartialMethodConstraintsChecks(SourceOrdinaryMethodSymbol definition, SourceOrdinaryMethodSymbol implementation, BindingDiagnosticBag diagnostics)
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
                    diagnostics.Add(ErrorCode.ERR_PartialMethodInconsistentConstraints, implementation.GetFirstLocation(), implementation, typeParameter2.Name);
                }
                else if (!MemberSignatureComparer.HaveSameNullabilityInConstraints(typeParameter1, typeMap1, typeParameter2, typeMap2))
                {
                    diagnostics.Add(ErrorCode.WRN_NullabilityMismatchInConstraintsOnPartialImplementation, implementation.GetFirstLocation(), implementation, typeParameter2.Name);
                }
            }
        }

        internal sealed override bool CallsAreOmitted(SyntaxTree syntaxTree)
        {
            if (this.IsPartialWithoutImplementation)
            {
                return true;
            }

            return base.CallsAreOmitted(syntaxTree);
        }

        internal sealed override bool GenerateDebugInfo => !IsAsync && !IsIterator;

        private sealed class SourceOrdinaryMethodSymbolSimple : SourceOrdinaryMethodSymbol
        {
            // Avoid adding fields here if possible.  This 'simple' type handles the majority of source methods in any
            // compilation.  So any fields here can add significantly to heap usage.  In measurements, there are roughly
            // 25:1 more 'simple' methods than 'complex' methods. So consider placing new data in
            // SourceOrdinaryMethodSymbolComplex instead if it is state for rare methods.

            public SourceOrdinaryMethodSymbolSimple(
                NamedTypeSymbol containingType,
                string name,
                Location location,
                MethodDeclarationSyntax syntax,
                MethodKind methodKind,
                bool isNullableAnalysisEnabled,
                BindingDiagnosticBag diagnostics)
                : base(containingType, name, location, syntax, methodKind, isNullableAnalysisEnabled, diagnostics)
            {
            }

            internal sealed override SourceOrdinaryMethodSymbol OtherPartOfPartial
                => null;

            protected sealed override TypeSymbol ExplicitInterfaceType
                => null;

            protected sealed override MethodSymbol FindExplicitlyImplementedMethod(BindingDiagnosticBag diagnostics)
                => null;

            public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
                => ImmutableArray<TypeParameterSymbol>.Empty;

            public sealed override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
                => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

            public sealed override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
                => ImmutableArray<TypeParameterConstraintKind>.Empty;

            protected sealed override void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
            {
            }
        }

        /// <summary>
        /// Specialized subclass of SourceOrdinaryMethodSymbol for less common cases.  Specifically, we only use this
        /// for methods that are:
        /// <list type="number">
        /// <item>Generic.</item>
        /// <item>An explicit interface implementation.</item>
        /// <item>Partial.</item>
        /// </list>
        /// </summary>
        private sealed class SourceOrdinaryMethodSymbolComplex : SourceOrdinaryMethodSymbol
        {
            private readonly TypeSymbol _explicitInterfaceType;

            private readonly TypeParameterInfo _typeParameterInfo;

            /// <summary>
            /// If this symbol represents a partial method definition or implementation part, its other part (if any).
            /// This should be set, if at all, before this symbol appears among the members of its owner.  
            /// The implementation part is not listed among the "members" of the enclosing type.
            /// </summary>
            private SourceOrdinaryMethodSymbol _otherPartOfPartial;

            public SourceOrdinaryMethodSymbolComplex(
                NamedTypeSymbol containingType,
                TypeSymbol explicitInterfaceType,
                string name,
                Location location,
                MethodDeclarationSyntax syntax,
                MethodKind methodKind,
                bool isNullableAnalysisEnabled,
                BindingDiagnosticBag diagnostics)
                : base(containingType, name, location, syntax, methodKind, isNullableAnalysisEnabled, diagnostics)
            {
                _explicitInterfaceType = explicitInterfaceType;

                // Compute the type parameters.  If empty (the common case), directly point at the singleton to reduce
                // the amount of pointers-to-arrays this type needs to store.
                var typeParameters = MakeTypeParameters(syntax, diagnostics);
                _typeParameterInfo = typeParameters.IsEmpty
                    ? TypeParameterInfo.Empty
                    : new TypeParameterInfo { LazyTypeParameters = typeParameters };
            }

            protected sealed override TypeSymbol ExplicitInterfaceType => _explicitInterfaceType;
            internal sealed override SourceOrdinaryMethodSymbol OtherPartOfPartial => _otherPartOfPartial;

            internal static void InitializePartialMethodParts(SourceOrdinaryMethodSymbolComplex definition, SourceOrdinaryMethodSymbolComplex implementation)
            {
                Debug.Assert(definition.IsPartialDefinition);
                Debug.Assert(implementation.IsPartialImplementation);

                Debug.Assert(definition._otherPartOfPartial is null || definition._otherPartOfPartial == implementation);
                Debug.Assert(implementation._otherPartOfPartial is null || implementation._otherPartOfPartial == definition);

                definition._otherPartOfPartial = implementation;
                implementation._otherPartOfPartial = definition;
            }

            protected sealed override MethodSymbol FindExplicitlyImplementedMethod(BindingDiagnosticBag diagnostics)
            {
                var syntax = GetSyntax();
                return this.FindExplicitlyImplementedMethod(isOperator: false, _explicitInterfaceType, syntax.Identifier.ValueText, syntax.ExplicitInterfaceSpecifier, diagnostics);
            }

            public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
            {
                get
                {
                    Debug.Assert(!_typeParameterInfo.LazyTypeParameters.IsDefault);
                    return _typeParameterInfo.LazyTypeParameters;
                }
            }

            public sealed override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
            {
                if (_typeParameterInfo.LazyTypeParameterConstraintTypes.IsDefault)
                {
                    GetTypeParameterConstraintKinds();

                    var diagnostics = BindingDiagnosticBag.GetInstance();
                    var syntax = GetSyntax();
                    var withTypeParametersBinder =
                        this.DeclaringCompilation
                        .GetBinderFactory(syntax.SyntaxTree)
                        .GetBinder(syntax.ReturnType, syntax, this);
                    var constraints = this.MakeTypeParameterConstraintTypes(
                        withTypeParametersBinder,
                        TypeParameters,
                        syntax.TypeParameterList,
                        syntax.ConstraintClauses,
                        diagnostics);
                    if (ImmutableInterlocked.InterlockedInitialize(
                            ref _typeParameterInfo.LazyTypeParameterConstraintTypes,
                            constraints))
                    {
                        this.AddDeclarationDiagnostics(diagnostics);
                    }
                    diagnostics.Free();
                }

                return _typeParameterInfo.LazyTypeParameterConstraintTypes;
            }

            public sealed override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
            {
                if (_typeParameterInfo.LazyTypeParameterConstraintKinds.IsDefault)
                {
                    var syntax = GetSyntax();
                    var withTypeParametersBinder =
                        this.DeclaringCompilation
                        .GetBinderFactory(syntax.SyntaxTree)
                        .GetBinder(syntax.ReturnType, syntax, this);
                    var constraints = this.MakeTypeParameterConstraintKinds(
                        withTypeParametersBinder,
                        TypeParameters,
                        syntax.TypeParameterList,
                        syntax.ConstraintClauses);

                    ImmutableInterlocked.InterlockedInitialize(
                        ref _typeParameterInfo.LazyTypeParameterConstraintKinds,
                        constraints);
                }

                return _typeParameterInfo.LazyTypeParameterConstraintKinds;
            }

            protected sealed override void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
            {
                if ((object)_explicitInterfaceType != null)
                {
                    var syntax = this.GetSyntax();
                    Debug.Assert(syntax.ExplicitInterfaceSpecifier != null);
                    _explicitInterfaceType.CheckAllConstraints(DeclaringCompilation, conversions, new SourceLocation(syntax.ExplicitInterfaceSpecifier.Name), diagnostics);
                }
            }

            private ImmutableArray<TypeParameterSymbol> MakeTypeParameters(MethodDeclarationSyntax syntax, BindingDiagnosticBag diagnostics)
            {
                if (syntax.Arity == 0)
                {
                    return ImmutableArray<TypeParameterSymbol>.Empty;
                }

                Debug.Assert(syntax.TypeParameterList != null);

                MessageID.IDS_FeatureGenerics.CheckFeatureAvailability(diagnostics, syntax.TypeParameterList.LessThanToken);

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

                    SourceMemberContainerTypeSymbol.ReportReservedTypeName(identifier.Text, this.DeclaringCompilation, diagnostics.DiagnosticBag, location);

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
        }
    }
}
