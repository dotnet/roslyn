// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Unlike <see cref="SourceOrdinaryMethodSymbol"/>, this type doesn't depend
    /// on any specific kind of syntax node associated with it. Any syntax node is good enough
    /// for it.
    /// </summary>
    internal abstract class SourceOrdinaryMethodSymbolBase : SourceMemberMethodSymbol
    {
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly string _name;

        private ImmutableArray<MethodSymbol> _lazyExplicitInterfaceImplementations;
        private ImmutableArray<CustomModifier> _lazyRefCustomModifiers;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations _lazyReturnType;

        protected SourceOrdinaryMethodSymbolBase(
            NamedTypeSymbol containingType,
            string name,
            Location location,
            CSharpSyntaxNode syntax,
            MethodKind methodKind,
            bool isIterator,
            bool isExtensionMethod,
            bool isPartial,
            bool hasBody,
            DiagnosticBag diagnostics) :
            base(containingType,
                 syntax.GetReference(),
                 location,
                 isIterator)
        {
            _name = name;

            // The following two values are used to compute and store the initial value of the flags
            // However, these two components are placeholders; the correct value will be
            // computed lazily later and then the flags will be fixed up.
            const bool returnsVoid = false;

            DeclarationModifiers declarationModifiers;
            (declarationModifiers, HasExplicitAccessModifier) = this.MakeModifiers(methodKind, isPartial, hasBody, location, diagnostics);

            var isMetadataVirtualIgnoringModifiers = methodKind == MethodKind.ExplicitInterfaceImplementation; //explicit impls must be marked metadata virtual

            this.MakeFlags(methodKind, declarationModifiers, returnsVoid, isExtensionMethod, isMetadataVirtualIgnoringModifiers);

            _typeParameters = MakeTypeParameters(syntax, diagnostics);

            CheckFeatureAvailabilityAndRuntimeSupport(syntax, location, hasBody, diagnostics);

            if (hasBody)
            {
                CheckModifiersForBody(location, diagnostics);
            }

            var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers, this, isExplicitInterfaceImplementation: methodKind == MethodKind.ExplicitInterfaceImplementation);
            if (info != null)
            {
                diagnostics.Add(info, location);
            }
        }

        protected abstract ImmutableArray<TypeParameterSymbol> MakeTypeParameters(CSharpSyntaxNode node, DiagnosticBag diagnostics);

        public override bool ReturnsVoid
        {
            get
            {
                LazyMethodChecks();
                return base.ReturnsVoid;
            }
        }

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            Debug.Assert(this.MethodKind != MethodKind.UserDefinedOperator, "SourceUserDefinedOperatorSymbolBase overrides this");

            ImmutableArray<TypeParameterConstraintClause> declaredConstraints;
            bool isVararg;
            (_lazyReturnType, _lazyParameters, isVararg, declaredConstraints) = MakeParametersAndBindReturnType(diagnostics);

            // set ReturnsVoid flag
            this.SetReturnsVoid(_lazyReturnType.IsVoidType());

            this.CheckEffectiveAccessibility(_lazyReturnType, _lazyParameters, diagnostics);

            var location = locations[0];
            // Checks taken from MemberDefiner::defineMethod
            if (this.Name == WellKnownMemberNames.DestructorName && this.ParameterCount == 0 && this.Arity == 0 && this.ReturnsVoid)
            {
                diagnostics.Add(ErrorCode.WRN_FinalizeMethod, location);
            }

            ExtensionMethodChecks(diagnostics);

            if (IsPartial)
            {
                if (MethodKind == MethodKind.ExplicitInterfaceImplementation)
                {
                    diagnostics.Add(ErrorCode.ERR_PartialMethodNotExplicit, location);
                }

                if (!ContainingType.IsPartial())
                {
                    diagnostics.Add(ErrorCode.ERR_PartialMethodOnlyInPartialClass, location);
                }
            }

            if (!IsPartial)
            {
                LazyAsyncMethodChecks(CancellationToken.None);
                Debug.Assert(state.HasComplete(CompletionPart.FinishAsyncMethodChecks));
            }

            // The runtime will not treat this method as an override or implementation of another
            // method unless both the signatures and the custom modifiers match.  Hence, in the
            // case of overrides and *explicit* implementations, we need to copy the custom modifiers
            // that are in the signature of the overridden/implemented method.  (From source, we know
            // that there can only be one such method, so there are no conflicts.)  This is
            // unnecessary for implicit implementations because, if the custom modifiers don't match,
            // we'll insert a bridge method (an explicit implementation that delegates to the implicit
            // implementation) with the correct custom modifiers 
            // (see SourceMemberContainerTypeSymbol.SynthesizeInterfaceMemberImplementation).

            // This value may not be correct, but we need something while we compute this.OverriddenMethod.
            // May be re-assigned below.
            Debug.Assert(_lazyReturnType.CustomModifiers.IsEmpty);
            _lazyRefCustomModifiers = ImmutableArray<CustomModifier>.Empty;

            MethodSymbol overriddenOrExplicitlyImplementedMethod = null;

            // Note: we're checking if the syntax indicates explicit implementation rather,
            // than if explicitInterfaceType is null because we don't want to look for an
            // overridden property if this is supposed to be an explicit implementation.
            if (MethodKind != MethodKind.ExplicitInterfaceImplementation)
            {
                Debug.Assert(_lazyExplicitInterfaceImplementations.IsDefault);
                _lazyExplicitInterfaceImplementations = ImmutableArray<MethodSymbol>.Empty;

                // If this method is an override, we may need to copy custom modifiers from
                // the overridden method (so that the runtime will recognize it as an override).
                // We check for this case here, while we can still modify the parameters and
                // return type without losing the appearance of immutability.
                if (this.IsOverride)
                {
                    // This computation will necessarily be performed with partially incomplete
                    // information.  There is no way we can determine the complete signature
                    // (i.e. including custom modifiers) until we have found the method that
                    // this method overrides.  To accommodate this, MethodSymbol.OverriddenOrHiddenMembers
                    // is written to allow relaxed matching of custom modifiers for source methods,
                    // on the assumption that they will be updated appropriately.
                    overriddenOrExplicitlyImplementedMethod = this.OverriddenMethod;

                    if ((object)overriddenOrExplicitlyImplementedMethod != null)
                    {
                        CustomModifierUtils.CopyMethodCustomModifiers(overriddenOrExplicitlyImplementedMethod, this, out _lazyReturnType,
                                                                      out _lazyRefCustomModifiers,
                                                                      out _lazyParameters, alsoCopyParamsModifier: true);
                    }
                }
                else if (RefKind == RefKind.RefReadOnly)
                {
                    var modifierType = Binder.GetWellKnownType(DeclaringCompilation, WellKnownType.System_Runtime_InteropServices_InAttribute, diagnostics, ReturnTypeLocation);

                    _lazyRefCustomModifiers = ImmutableArray.Create(CSharpCustomModifier.CreateRequired(modifierType));
                }
            }
            else if ((object)ExplicitInterfaceType != null)
            {
                //do this last so that it can assume the method symbol is constructed (except for ExplicitInterfaceImplementation)
                overriddenOrExplicitlyImplementedMethod = FindExplicitlyImplementedMethod(diagnostics);

                if ((object)overriddenOrExplicitlyImplementedMethod != null)
                {
                    Debug.Assert(_lazyExplicitInterfaceImplementations.IsDefault);
                    _lazyExplicitInterfaceImplementations = ImmutableArray.Create<MethodSymbol>(overriddenOrExplicitlyImplementedMethod);

                    CustomModifierUtils.CopyMethodCustomModifiers(overriddenOrExplicitlyImplementedMethod, this, out _lazyReturnType,
                                                                  out _lazyRefCustomModifiers,
                                                                  out _lazyParameters, alsoCopyParamsModifier: false);
                    this.FindExplicitlyImplementedMemberVerification(overriddenOrExplicitlyImplementedMethod, diagnostics);
                    TypeSymbol.CheckNullableReferenceTypeMismatchOnImplementingMember(this.ContainingType, this, overriddenOrExplicitlyImplementedMethod, isExplicit: true, diagnostics);
                }
                else
                {
                    Debug.Assert(_lazyExplicitInterfaceImplementations.IsDefault);
                    _lazyExplicitInterfaceImplementations = ImmutableArray<MethodSymbol>.Empty;

                    Debug.Assert(_lazyReturnType.CustomModifiers.IsEmpty);
                }
            }

            if (!declaredConstraints.IsDefault && overriddenOrExplicitlyImplementedMethod is object)
            {
                for (int i = 0; i < declaredConstraints.Length; i++)
                {
                    var typeParameter = _typeParameters[i];
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

                    diagnostics.Add(report, typeParameter.Locations[0], this, typeParameter,
                                    overriddenOrExplicitlyImplementedMethod.TypeParameters[i], overriddenOrExplicitlyImplementedMethod);
                }
            }

            CheckModifiers(MethodKind == MethodKind.ExplicitInterfaceImplementation, isVararg, HasAnyBody, location, diagnostics);
        }

        protected abstract (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(DiagnosticBag diagnostics);

        protected abstract void ExtensionMethodChecks(DiagnosticBag diagnostics);

        protected abstract MethodSymbol FindExplicitlyImplementedMethod(DiagnosticBag diagnostics);

        protected abstract Location ReturnTypeLocation { get; }

        protected abstract TypeSymbol ExplicitInterfaceType { get; }

        protected abstract bool HasAnyBody { get; }

        protected sealed override void LazyAsyncMethodChecks(CancellationToken cancellationToken)
        {
            Debug.Assert(this.IsPartial == state.HasComplete(CompletionPart.FinishMethodChecks),
                "Partial methods complete method checks during construction.  " +
                "Other methods can't complete method checks before executing this method.");

            if (!this.IsAsync)
            {
                CompleteAsyncMethodChecks(diagnosticsOpt: null, cancellationToken: cancellationToken);
                return;
            }

            DiagnosticBag diagnostics = DiagnosticBag.GetInstance();
            AsyncMethodChecks(diagnostics);

            CompleteAsyncMethodChecks(diagnostics, cancellationToken);
            diagnostics.Free();
        }

        private void CompleteAsyncMethodChecks(DiagnosticBag diagnosticsOpt, CancellationToken cancellationToken)
        {
            if (state.NotePartComplete(CompletionPart.StartAsyncMethodChecks))
            {
                if (diagnosticsOpt != null)
                {
                    AddDeclarationDiagnostics(diagnosticsOpt);
                }

                CompleteAsyncMethodChecksBetweenStartAndFinish();
                state.NotePartComplete(CompletionPart.FinishAsyncMethodChecks);
            }
            else
            {
                state.SpinWaitComplete(CompletionPart.FinishAsyncMethodChecks, cancellationToken);
            }
        }

        protected abstract void CompleteAsyncMethodChecksBetweenStartAndFinish();

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return this.locations;
            }
        }

        internal override int ParameterCount
        {
            get
            {
                if (!_lazyParameters.IsDefault)
                {
                    int result = _lazyParameters.Length;
                    Debug.Assert(result == GetParameterCountFromSyntax());
                    return result;
                }

                return GetParameterCountFromSyntax();
            }
        }

        protected abstract int GetParameterCountFromSyntax();

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                LazyMethodChecks();
                return _lazyParameters;
            }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        public abstract override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken));

        internal override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return MethodKind == MethodKind.ExplicitInterfaceImplementation;
            }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                LazyMethodChecks();
                return _lazyExplicitInterfaceImplementations;
            }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                LazyMethodChecks();
                return _lazyRefCustomModifiers;
            }
        }

        public override string Name
        {
            get
            {
                return _name;
            }
        }

        protected abstract override SourceMemberMethodSymbol BoundAttributesSource { get; }

        internal abstract override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations();

        internal bool HasExplicitAccessModifier { get; }

        private (DeclarationModifiers mods, bool hasExplicitAccessMod) MakeModifiers(MethodKind methodKind, bool isPartial, bool hasBody, Location location, DiagnosticBag diagnostics)
        {
            bool isInterface = this.ContainingType.IsInterface;
            bool isExplicitInterfaceImplementation = methodKind == MethodKind.ExplicitInterfaceImplementation;
            var defaultAccess = isInterface && isPartial && !isExplicitInterfaceImplementation ? DeclarationModifiers.Public : DeclarationModifiers.Private;

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
                    // This is needed to make sure we can detect 'public' modifier specified explicitly and
                    // check it against language version below.
                    defaultAccess = DeclarationModifiers.None;

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
            else if (isInterface)
            {
                Debug.Assert(isExplicitInterfaceImplementation);
                allowedModifiers |= DeclarationModifiers.Abstract;
            }

            allowedModifiers |= DeclarationModifiers.Extern | DeclarationModifiers.Async;

            if (ContainingType.IsStructType())
            {
                allowedModifiers |= DeclarationModifiers.ReadOnly;
            }

            // In order to detect whether explicit accessibility mods were provided, we pass the default value
            // for 'defaultAccess' and manually add in the 'defaultAccess' flags after the call.
            bool hasExplicitAccessMod;
            DeclarationModifiers mods = MakeDeclarationModifiers(allowedModifiers, diagnostics);
            if ((mods & DeclarationModifiers.AccessibilityMask) == 0)
            {
                hasExplicitAccessMod = false;
                mods |= defaultAccess;
            }
            else
            {
                hasExplicitAccessMod = true;
            }

            this.CheckUnsafeModifier(mods, diagnostics);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(hasBody, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            mods = AddImpliedModifiers(mods, isInterface, methodKind, hasBody);
            return (mods, hasExplicitAccessMod);
        }

        protected abstract DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, DiagnosticBag diagnostics);

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

        internal bool HasExtendedPartialModifier => (DeclarationModifiers & PartialMethodExtendedModifierMask) != 0;

        private void CheckModifiers(bool isExplicitInterfaceImplementation, bool isVararg, bool hasBody, Location location, DiagnosticBag diagnostics)
        {
            bool isExplicitInterfaceImplementationInInterface = isExplicitInterfaceImplementation && ContainingType.IsInterface;

            if (IsPartial && HasExplicitAccessModifier)
            {
                Binder.CheckFeatureAvailability(SyntaxNode, MessageID.IDS_FeatureExtendedPartialMethods, diagnostics, location);
            }

            if (IsPartial && IsAbstract)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodInvalidModifier, location);
            }
            else if (IsPartial && !HasExplicitAccessModifier && !ReturnsVoid)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodWithNonVoidReturnMustHaveAccessMods, location, this);
            }
            else if (IsPartial && !HasExplicitAccessModifier && HasExtendedPartialModifier)
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodWithExtendedModMustHaveAccessMods, location, this);
            }
            else if (IsPartial && !HasExplicitAccessModifier && Parameters.Any(p => p.RefKind == RefKind.Out))
            {
                diagnostics.Add(ErrorCode.ERR_PartialMethodWithOutParamMustHaveAccessMods, location, this);
            }
            else if (this.DeclaredAccessibility == Accessibility.Private && (IsVirtual || (IsAbstract && !isExplicitInterfaceImplementationInInterface) || IsOverride))
            {
                diagnostics.Add(ErrorCode.ERR_VirtualPrivate, location, this);
            }
            else if (IsStatic && (IsOverride || IsVirtual || IsAbstract))
            {
                // A static member '{0}' cannot be marked as override, virtual, or abstract
                diagnostics.Add(ErrorCode.ERR_StaticNotVirtual, location, this);
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
            else if (!ContainingType.IsInterfaceType() && _lazyReturnType.IsStatic)
            {
                // '{0}': static types cannot be used as return types
                diagnostics.Add(ErrorCode.ERR_ReturnTypeIsStaticClass, location, _lazyReturnType.Type);
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
                // '{0}' is abstract but it is contained in non-abstract class '{1}'
                diagnostics.Add(ErrorCode.ERR_AbstractInConcreteClass, location, this, ContainingType);
            }
            else if (IsVirtual && ContainingType.IsSealed)
            {
                // '{0}' is a new virtual member in sealed class '{1}'
                diagnostics.Add(ErrorCode.ERR_NewVirtualInSealed, location, this, ContainingType);
            }
            else if (!hasBody && IsAsync)
            {
                diagnostics.Add(ErrorCode.ERR_BadAsyncLacksBody, location);
            }
            else if (!hasBody && !IsExtern && !IsAbstract && !IsPartial && !IsExpressionBodied)
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
            else if (isVararg && (IsGenericMethod || ContainingType.IsGenericType || _lazyParameters.Length > 0 && _lazyParameters[_lazyParameters.Length - 1].IsParams))
            {
                diagnostics.Add(ErrorCode.ERR_BadVarargs, location);
            }
            else if (isVararg && IsAsync)
            {
                diagnostics.Add(ErrorCode.ERR_VarargsAsync, location);
            }
        }

        internal override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);

            if (this.IsExtensionMethod)
            {
                // No need to check if [Extension] attribute was explicitly set since
                // we'll issue CS1112 error in those cases and won't generate IL.
                var compilation = this.DeclaringCompilation;

                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor));
            }
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, DiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            var location = ReturnTypeLocation;
            var compilation = DeclaringCompilation;

            Debug.Assert(location != null);

            // Check constraints on return type and parameters. Note: Dev10 uses the
            // method name location for any such errors. We'll do the same for return
            // type errors but for parameter errors, we'll use the parameter location.
            CheckConstraintsForExplicitInterfaceType(conversions, diagnostics);

            this.ReturnType.CheckAllConstraints(compilation, conversions, this.Locations[0], diagnostics);

            foreach (var parameter in this.Parameters)
            {
                parameter.Type.CheckAllConstraints(compilation, conversions, parameter.Locations[0], diagnostics);
            }

            PartialMethodChecks(diagnostics);

            if (RefKind == RefKind.RefReadOnly)
            {
                compilation.EnsureIsReadOnlyAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            ParameterHelpers.EnsureIsReadOnlyAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);

            if (ReturnType.ContainsNativeInteger())
            {
                compilation.EnsureNativeIntegerAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            ParameterHelpers.EnsureNativeIntegerAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);

            if (compilation.ShouldEmitNullableAttributes(this) && ReturnTypeWithAnnotations.NeedsNullableAttribute())
            {
                compilation.EnsureNullableAttributeExists(diagnostics, location, modifyCompilation: true);
            }

            ParameterHelpers.EnsureNullableAttributeExists(compilation, this, Parameters, diagnostics, modifyCompilation: true);
        }

        protected abstract void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, DiagnosticBag diagnostics);

        protected abstract void PartialMethodChecks(DiagnosticBag diagnostics);
    }
}
