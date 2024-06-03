// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
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
                 isIterator: SyntaxFacts.HasYieldOperations(syntax.Body),
                 MakeModifiersAndFlags(containingType, location, syntax, methodKind, isNullableAnalysisEnabled, diagnostics, out bool hasExplicitAccessMod))
        {
            Debug.Assert(diagnostics.DiagnosticBag is object);

            this.CheckUnsafeModifier(DeclarationModifiers, diagnostics);

            HasExplicitAccessModifier = hasExplicitAccessMod;
            bool hasAnyBody = syntax.HasAnyBody();

            CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasAnyBody, diagnostics);

            if (hasAnyBody)
            {
                CheckModifiersForBody(location, diagnostics);
            }

            ModifierUtils.CheckAccessibility(this.DeclarationModifiers, this, isExplicitInterfaceImplementation: methodKind == MethodKind.ExplicitInterfaceImplementation, diagnostics, location);

            Debug.Assert(syntax.ReturnType is not ScopedTypeSyntax);

            if (syntax.Arity == 0)
            {
                ReportErrorIfHasConstraints(syntax.ConstraintClauses, diagnostics.DiagnosticBag);
            }

            CheckForBlockAndExpressionBody(
                syntax.Body, syntax.ExpressionBody, syntax, diagnostics);
        }

        private static (DeclarationModifiers, Flags) MakeModifiersAndFlags(
            NamedTypeSymbol containingType, Location location, MethodDeclarationSyntax syntax, MethodKind methodKind,
            bool isNullableAnalysisEnabled, BindingDiagnosticBag diagnostics, out bool hasExplicitAccessMod)
        {
            (DeclarationModifiers declarationModifiers, hasExplicitAccessMod) = MakeModifiers(syntax, containingType, methodKind, hasBody: syntax.HasAnyBody(), location, diagnostics);
            Flags flags = new Flags(
                                    methodKind, refKind: syntax.ReturnType.SkipScoped(out _).GetRefKindInLocalOrReturn(diagnostics),
                                    declarationModifiers,
                                    returnsVoid: false, // The correct value will be computed lazily later and then the flags will be fixed up.
                                    returnsVoidIsSet: false,
                                    hasAnyBody: syntax.HasAnyBody(), isExpressionBodied: syntax.IsExpressionBodied(),
                                    isExtensionMethod: syntax.ParameterList.Parameters.FirstOrDefault() is ParameterSyntax firstParam &&
                                                       !firstParam.IsArgList &&
                                                       firstParam.Modifiers.Any(SyntaxKind.ThisKeyword),
                                    isNullableAnalysisEnabled: isNullableAnalysisEnabled,
                                    isVararg: syntax.IsVarArg(),
                                    isExplicitInterfaceImplementation: methodKind == MethodKind.ExplicitInterfaceImplementation,
                                    hasThisInitializer: false);

            return (declarationModifiers, flags);
        }

        private bool HasAnyBody => flags.HasAnyBody;

        private (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
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
                else if (parameter0RefKind is RefKind.In or RefKind.RefReadOnlyParameter && parameter0Type.TypeKind != TypeKind.Struct)
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

        internal SourceOrdinaryMethodSymbol SourcePartialDefinition
        {
            get
            {
                return this.IsPartialImplementation ? this.OtherPartOfPartial : null;
            }
        }

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
            // Attributes on partial methods are owned by the definition part.
            // If this symbol has a non-null PartialDefinitionPart, we should have accessed this method through that definition symbol instead
            Debug.Assert(PartialDefinitionPart is null);

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

        private static DeclarationModifiers MakeDeclarationModifiers(MethodDeclarationSyntax syntax, NamedTypeSymbol containingType, Location location, DeclarationModifiers allowedModifiers, BindingDiagnosticBag diagnostics)
        {
            return ModifierUtils.MakeAndCheckNonTypeMemberModifiers(isOrdinaryMethod: true, isForInterfaceMember: containingType.IsInterface,
                                                                    syntax.Modifiers, defaultAccess: DeclarationModifiers.None, allowedModifiers, location, diagnostics, out _);
        }

        internal sealed override void ForceComplete(SourceLocation locationOpt, Predicate<Symbol> filter, CancellationToken cancellationToken)
        {
            var implementingPart = this.SourcePartialImplementation;
            if ((object)implementingPart != null)
            {
                implementingPart.ForceComplete(locationOpt, filter, cancellationToken);
            }

            base.ForceComplete(locationOpt, filter, cancellationToken);
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
        /// <remarks>
        /// This method is analogous to <see cref="SourcePropertySymbol.PartialPropertyChecks" />.
        /// Whenever new checks are added to this method, the other method should also have those checks added, if applicable.
        /// </remarks>
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
                diagnostics.Add(ErrorCode.ERR_PartialMemberInconsistentTupleNames, implementation.GetFirstLocation(), definition, implementation);
            }

            if (definition.RefKind != implementation.RefKind)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberRefReturnDifference, implementation.GetFirstLocation());
            }

            if (definition.IsStatic != implementation.IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberStaticDifference, implementation.GetFirstLocation());
            }

            if (definition.IsDeclaredReadOnly != implementation.IsDeclaredReadOnly)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberReadOnlyDifference, implementation.GetFirstLocation());
            }

            if (definition.IsExtensionMethod != implementation.IsExtensionMethod)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodExtensionDifference, implementation.GetFirstLocation());
            }

            if (definition.IsUnsafe != implementation.IsUnsafe && definition.CompilationAllowsUnsafe()) // Don't cascade.
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberUnsafeDifference, implementation.GetFirstLocation());
            }

            if (definition.IsParams() != implementation.IsParams())
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberParamsDifference, implementation.GetFirstLocation());
            }

            if (definition.HasExplicitAccessModifier != implementation.HasExplicitAccessModifier
                || definition.DeclaredAccessibility != implementation.DeclaredAccessibility)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberAccessibilityDifference, implementation.GetFirstLocation());
            }

            if (definition.IsVirtual != implementation.IsVirtual
                || definition.IsOverride != implementation.IsOverride
                || definition.IsSealed != implementation.IsSealed
                || definition.IsNew != implementation.IsNew)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberExtendedModDifference, implementation.GetFirstLocation());
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

#nullable enable
        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(this.MethodKind != MethodKind.UserDefinedOperator, "SourceUserDefinedOperatorSymbolBase overrides this");

            var (returnType, parameters, declaredConstraints) = MakeParametersAndBindReturnType(diagnostics);

            MethodSymbol? overriddenOrExplicitlyImplementedMethod = MethodChecks(returnType, parameters, diagnostics);

            if (!declaredConstraints.IsDefault && overriddenOrExplicitlyImplementedMethod is object)
            {
                for (int i = 0; i < declaredConstraints.Length; i++)
                {
                    var typeParameter = this.TypeParameters[i];
                    ErrorCode report;

                    switch (declaredConstraints[i].Constraints & (TypeParameterConstraintKind.ReferenceType | TypeParameterConstraintKind.ValueType | TypeParameterConstraintKind.Default))
                    {
                        case TypeParameterConstraintKind.ReferenceType:
                            if (!typeParameter.IsReferenceType)
                            {
                                report = ErrorCode.ERR_OverrideRefConstraintNotSatisfied;
                                break;
                            }
                            continue;
                        case TypeParameterConstraintKind.ValueType:
                            if (!typeParameter.IsNonNullableValueType())
                            {
                                report = ErrorCode.ERR_OverrideValConstraintNotSatisfied;
                                break;
                            }
                            continue;
                        case TypeParameterConstraintKind.Default:
                            if (typeParameter.IsReferenceType || typeParameter.IsValueType)
                            {
                                report = ErrorCode.ERR_OverrideDefaultConstraintNotSatisfied;
                                break;
                            }
                            continue;
                        default:
                            continue;
                    }

                    diagnostics.Add(report, typeParameter.GetFirstLocation(), this, typeParameter,
                                    overriddenOrExplicitlyImplementedMethod.TypeParameters[i], overriddenOrExplicitlyImplementedMethod);
                }
            }

            CheckModifiers(MethodKind == MethodKind.ExplicitInterfaceImplementation, _location, diagnostics);
        }
#nullable disable

        // Consider moving this to flags to save space
        internal bool HasExplicitAccessModifier { get; }

        private static (DeclarationModifiers mods, bool hasExplicitAccessMod) MakeModifiers(MethodDeclarationSyntax syntax, NamedTypeSymbol containingType, MethodKind methodKind, bool hasBody, Location location, BindingDiagnosticBag diagnostics)
        {
            bool isInterface = containingType.IsInterface;
            bool isExplicitInterfaceImplementation = methodKind == MethodKind.ExplicitInterfaceImplementation;

            // This is needed to make sure we can detect 'public' modifier specified explicitly and
            // check it against language version below.
            var defaultAccess = isInterface && !isExplicitInterfaceImplementation ? DeclarationModifiers.None : DeclarationModifiers.Private;

            // Check that the set of modifiers is allowed
            var allowedModifiers = DeclarationModifiers.Partial | DeclarationModifiers.Unsafe;
            var defaultInterfaceImplementationModifiers = DeclarationModifiers.None;

            if (!isExplicitInterfaceImplementation)
            {
                allowedModifiers |= DeclarationModifiers.New |
                                    DeclarationModifiers.Sealed |
                                    DeclarationModifiers.Abstract |
                                    DeclarationModifiers.Static |
                                    DeclarationModifiers.Virtual |
                                    DeclarationModifiers.AccessibilityMask;

                if (!isInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Override;
                }
                else
                {
                    defaultInterfaceImplementationModifiers |= DeclarationModifiers.Sealed |
                                                               DeclarationModifiers.Abstract |
                                                               DeclarationModifiers.Static |
                                                               DeclarationModifiers.Virtual |
                                                               DeclarationModifiers.Extern |
                                                               DeclarationModifiers.Async |
                                                               DeclarationModifiers.Partial |
                                                               DeclarationModifiers.AccessibilityMask;
                }
            }
            else
            {
                Debug.Assert(isExplicitInterfaceImplementation);

                if (isInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Abstract;
                }

                allowedModifiers |= DeclarationModifiers.Static;
            }

            allowedModifiers |= DeclarationModifiers.Extern | DeclarationModifiers.Async;

            if (containingType.IsStructType())
            {
                allowedModifiers |= DeclarationModifiers.ReadOnly;
            }

            // In order to detect whether explicit accessibility mods were provided, we pass the default value
            // for 'defaultAccess' and manually add in the 'defaultAccess' flags after the call.
            bool hasExplicitAccessMod;
            DeclarationModifiers mods = MakeDeclarationModifiers(syntax, containingType, location, allowedModifiers, diagnostics);
            if ((mods & DeclarationModifiers.AccessibilityMask) == 0)
            {
                hasExplicitAccessMod = false;
                mods |= defaultAccess;
            }
            else
            {
                hasExplicitAccessMod = true;
            }

            ModifierUtils.CheckFeatureAvailabilityForStaticAbstractMembersInInterfacesIfNeeded(mods, isExplicitInterfaceImplementation, location, diagnostics);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(hasBody, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            mods = AddImpliedModifiers(mods, isInterface, methodKind, hasBody);
            return (mods, hasExplicitAccessMod);
        }

        private static DeclarationModifiers AddImpliedModifiers(DeclarationModifiers mods, bool containingTypeIsInterface, MethodKind methodKind, bool hasBody)
        {
            // Let's overwrite modifiers for interface and explicit interface implementation methods with what they are supposed to be. 
            // Proper errors must have been reported by now.
            if (containingTypeIsInterface)
            {
                mods = ModifierUtils.AdjustModifiersForAnInterfaceMember(mods, hasBody,
                                                                         methodKind == MethodKind.ExplicitInterfaceImplementation);
            }
            else if (methodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                mods = (mods & ~DeclarationModifiers.AccessibilityMask) | DeclarationModifiers.Private;
            }
            return mods;
        }

        private const DeclarationModifiers PartialMethodExtendedModifierMask =
            DeclarationModifiers.Virtual |
            DeclarationModifiers.Override |
            DeclarationModifiers.New |
            DeclarationModifiers.Sealed |
            DeclarationModifiers.Extern;

        private bool HasExtendedPartialModifier => (DeclarationModifiers & PartialMethodExtendedModifierMask) != 0;

        private void CheckModifiers(bool isExplicitInterfaceImplementation, Location location, BindingDiagnosticBag diagnostics)
        {
            bool isVararg = this.IsVararg;

            Debug.Assert(!IsStatic || !IsOverride); // Otherwise should have been reported and cleared earlier.
            Debug.Assert(!IsStatic || ContainingType.IsInterface || (!IsAbstract && !IsVirtual)); // Otherwise should have been reported and cleared earlier.

            bool isExplicitInterfaceImplementationInInterface = isExplicitInterfaceImplementation && ContainingType.IsInterface;

            if (IsPartial && HasExplicitAccessModifier)
            {
                Binder.CheckFeatureAvailability(SyntaxNode, MessageID.IDS_FeatureExtendedPartialMethods, diagnostics, location);
            }

            if (IsPartial && IsAbstract)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMemberCannotBeAbstract, location);
            }
            else if (IsPartial && !HasExplicitAccessModifier && !ReturnsVoid)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, location, this);
            }
            else if (IsPartial && !HasExplicitAccessModifier && HasExtendedPartialModifier)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, location, this);
            }
            else if (IsPartial && !HasExplicitAccessModifier && Parameters.Any(static p => p.RefKind == RefKind.Out))
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, location, this);
            }
            else if (this.DeclaredAccessibility == Accessibility.Private && (IsVirtual || (IsAbstract && !isExplicitInterfaceImplementationInInterface) || IsOverride))
            {
                diagnostics.Add(ErrorCode.ERR_VirtualPrivate, location, this);
            }
            else if (IsOverride && (IsNew || IsVirtual))
            {
                // A member '{0}' marked as override cannot be marked as new or virtual
                diagnostics.Add(ErrorCode.ERR_OverrideNotNew, location, this);
            }
            else if (IsSealed && !IsOverride && !(isExplicitInterfaceImplementationInInterface && IsAbstract))
            {
                // '{0}' cannot be sealed because it is not an override
                diagnostics.Add(ErrorCode.ERR_SealedNonOverride, location, this);
            }
            else if (IsSealed && ContainingType.TypeKind == TypeKind.Struct)
            {
                // The modifier '{0}' is not valid for this item
                diagnostics.Add(ErrorCode.ERR_BadMemberFlag, location, SyntaxFacts.GetText(SyntaxKind.SealedKeyword));
            }
            else if (ReturnType.IsStatic)
            {
                // '{0}': static types cannot be used as return types
                diagnostics.Add(ErrorFacts.GetStaticClassReturnCode(ContainingType.IsInterfaceType()), location, ReturnType);
            }
            else if (IsAbstract && IsExtern)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractAndExtern, location, this);
            }
            else if (IsAbstract && IsSealed && !isExplicitInterfaceImplementationInInterface)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractAndSealed, location, this);
            }
            else if (IsAbstract && IsVirtual)
            {
                diagnostics.Add(ErrorCode.ERR_AbstractNotVirtual, location, this.Kind.Localize(), this);
            }
            else if (IsAbstract && ContainingType.TypeKind == TypeKind.Struct)
            {
                // The modifier '{0}' is not valid for this item
                diagnostics.Add(ErrorCode.ERR_BadMemberFlag, location, SyntaxFacts.GetText(SyntaxKind.AbstractKeyword));
            }
            else if (IsVirtual && ContainingType.TypeKind == TypeKind.Struct)
            {
                // The modifier '{0}' is not valid for this item
                diagnostics.Add(ErrorCode.ERR_BadMemberFlag, location, SyntaxFacts.GetText(SyntaxKind.VirtualKeyword));
            }
            else if (IsStatic && IsDeclaredReadOnly)
            {
                // Static member '{0}' cannot be marked 'readonly'.
                diagnostics.Add(ErrorCode.ERR_StaticMemberCantBeReadOnly, location, this);
            }
            else if (IsAbstract && !ContainingType.IsAbstract && (ContainingType.TypeKind == TypeKind.Class || ContainingType.TypeKind == TypeKind.Submission))
            {
                // '{0}' is abstract but it is contained in non-abstract type '{1}'
                diagnostics.Add(ErrorCode.ERR_AbstractInConcreteClass, location, this, ContainingType);
            }
            else if (IsVirtual && ContainingType.IsSealed)
            {
                // '{0}' is a new virtual member in sealed type '{1}'
                diagnostics.Add(ErrorCode.ERR_NewVirtualInSealed, location, this, ContainingType);
            }
            else if (!HasAnyBody && IsAsync)
            {
                diagnostics.Add(ErrorCode.ERR_BadAsyncLacksBody, location);
            }
            else if (!HasAnyBody && !IsExtern && !IsAbstract && !IsPartial && !IsExpressionBodied)
            {
                diagnostics.Add(ErrorCode.ERR_ConcreteMissingBody, location, this);
            }
            else if (ContainingType.IsSealed && this.DeclaredAccessibility.HasProtected() && !this.IsOverride)
            {
                diagnostics.Add(AccessCheck.GetProtectedMemberInSealedTypeError(ContainingType), location, this);
            }
            else if (ContainingType.IsStatic && !IsStatic)
            {
                diagnostics.Add(ErrorCode.ERR_InstanceMemberInStaticClass, location, Name);
            }
            else if (isVararg && (IsGenericMethod || ContainingType.IsGenericType || Parameters.Length > 0 && Parameters[Parameters.Length - 1].IsParams))
            {
                diagnostics.Add(ErrorCode.ERR_BadVarargs, location);
            }
            else if (isVararg && IsAsync)
            {
                diagnostics.Add(ErrorCode.ERR_VarargsAsync, location);
            }
        }

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

                Debug.Assert(definition._otherPartOfPartial is not { } alreadySetImplPart || alreadySetImplPart == implementation);
                Debug.Assert(implementation._otherPartOfPartial is not { } alreadySetDefPart || alreadySetDefPart == definition);

                definition._otherPartOfPartial = implementation;
                implementation._otherPartOfPartial = definition;

                Debug.Assert(definition._otherPartOfPartial == implementation);
                Debug.Assert(implementation._otherPartOfPartial == definition);
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
