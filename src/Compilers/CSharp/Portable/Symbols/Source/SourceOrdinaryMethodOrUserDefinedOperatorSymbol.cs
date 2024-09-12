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
    internal abstract class SourceOrdinaryMethodOrUserDefinedOperatorSymbol : SourceMemberMethodSymbol
    {
        private ImmutableArray<MethodSymbol> _lazyExplicitInterfaceImplementations;
        private ImmutableArray<CustomModifier> _lazyRefCustomModifiers;
        private ImmutableArray<ParameterSymbol> _lazyParameters;
        private TypeWithAnnotations _lazyReturnType;

        protected SourceOrdinaryMethodOrUserDefinedOperatorSymbol(NamedTypeSymbol containingType, SyntaxReference syntaxReferenceOpt, Location location, bool isIterator, (DeclarationModifiers declarationModifiers, Flags flags) modifiersAndFlags)
            : base(containingType, syntaxReferenceOpt, location, isIterator, modifiersAndFlags)
        {
        }

        protected abstract Location ReturnTypeLocation { get; }

        public sealed override bool ReturnsVoid
        {
            get
            {
                LazyMethodChecks();
                return base.ReturnsVoid;
            }
        }

        protected MethodSymbol? MethodChecks(TypeWithAnnotations returnType, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag diagnostics)
        {
            _lazyReturnType = returnType;
            _lazyParameters = parameters;

            // set ReturnsVoid flag
            this.SetReturnsVoid(_lazyReturnType.IsVoidType());

            this.CheckEffectiveAccessibility(_lazyReturnType, _lazyParameters, diagnostics);
            this.CheckFileTypeUsage(_lazyReturnType, _lazyParameters, diagnostics);

            // Checks taken from MemberDefiner::defineMethod
            if (this.Name == WellKnownMemberNames.DestructorName && this.ParameterCount == 0 && this.Arity == 0 && this.ReturnsVoid)
            {
                diagnostics.Add(ErrorCode.WRN_FinalizeMethod, _location);
            }

            ExtensionMethodChecks(diagnostics);

            if (IsPartial)
            {
                if (MethodKind == MethodKind.ExplicitInterfaceImplementation)
                {
                    diagnostics.Add(ErrorCode.ERR_PartialMethodNotExplicit, _location);
                }

                if (!ContainingType.IsPartial())
                {
                    diagnostics.Add(ErrorCode.ERR_PartialMethodOnlyInPartialClass, _location);
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

            MethodSymbol? overriddenOrExplicitlyImplementedMethod = null;

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
            else if (ExplicitInterfaceType is not null)
            {
                //do this last so that it can assume the method symbol is constructed (except for ExplicitInterfaceImplementation)
                overriddenOrExplicitlyImplementedMethod = FindExplicitlyImplementedMethod(diagnostics);

                if (overriddenOrExplicitlyImplementedMethod is not null)
                {
                    Debug.Assert(_lazyExplicitInterfaceImplementations.IsDefault);
                    _lazyExplicitInterfaceImplementations = ImmutableArray.Create<MethodSymbol>(overriddenOrExplicitlyImplementedMethod);

                    CustomModifierUtils.CopyMethodCustomModifiers(overriddenOrExplicitlyImplementedMethod, this, out _lazyReturnType,
                                                                  out _lazyRefCustomModifiers,
                                                                  out _lazyParameters, alsoCopyParamsModifier: false);
                    this.FindExplicitlyImplementedMemberVerification(overriddenOrExplicitlyImplementedMethod, diagnostics);
                    TypeSymbol.CheckModifierMismatchOnImplementingMember(this.ContainingType, this, overriddenOrExplicitlyImplementedMethod, isExplicit: true, diagnostics);
                }
                else
                {
                    Debug.Assert(_lazyExplicitInterfaceImplementations.IsDefault);
                    _lazyExplicitInterfaceImplementations = ImmutableArray<MethodSymbol>.Empty;

                    Debug.Assert(_lazyReturnType.CustomModifiers.IsEmpty);
                }
            }

            return overriddenOrExplicitlyImplementedMethod;
        }

        protected abstract void ExtensionMethodChecks(BindingDiagnosticBag diagnostics);

        protected abstract MethodSymbol? FindExplicitlyImplementedMethod(BindingDiagnosticBag diagnostics);

        protected abstract TypeSymbol? ExplicitInterfaceType { get; }

        internal sealed override int ParameterCount
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

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                LazyMethodChecks();
                return _lazyParameters;
            }
        }

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                LazyMethodChecks();
                return _lazyReturnType;
            }
        }

        internal sealed override bool IsExplicitInterfaceImplementation
        {
            get
            {
                return MethodKind == MethodKind.ExplicitInterfaceImplementation;
            }
        }

        public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get
            {
                LazyMethodChecks();
                return _lazyExplicitInterfaceImplementations;
            }
        }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                LazyMethodChecks();
                return _lazyRefCustomModifiers;
            }
        }

        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            // Defer computing location to avoid unnecessary allocations in most cases.
            Location? returnTypeLocation = null;
            var compilation = DeclaringCompilation;

            // Check constraints on return type and parameters. Note: Dev10 uses the
            // method name location for any such errors. We'll do the same for return
            // type errors but for parameter errors, we'll use the parameter location.
            CheckConstraintsForExplicitInterfaceType(conversions, diagnostics);

            this.ReturnType.CheckAllConstraints(compilation, conversions, this.GetFirstLocation(), diagnostics);

            foreach (var parameter in this.Parameters)
            {
                parameter.Type.CheckAllConstraints(compilation, conversions, parameter.GetFirstLocation(), diagnostics);
            }

            PartialMethodChecks(diagnostics);

            if (RefKind == RefKind.RefReadOnly)
            {
                compilation.EnsureIsReadOnlyAttributeExists(diagnostics, getReturnTypeLocation(), modifyCompilation: true);
            }

            ParameterHelpers.EnsureRefKindAttributesExist(compilation, Parameters, diagnostics, modifyCompilation: true);

            if (compilation.ShouldEmitNativeIntegerAttributes(ReturnType))
            {
                compilation.EnsureNativeIntegerAttributeExists(diagnostics, getReturnTypeLocation(), modifyCompilation: true);
            }

            ParameterHelpers.EnsureNativeIntegerAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);

            ParameterHelpers.EnsureScopedRefAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);

            if (compilation.ShouldEmitNullableAttributes(this) && ReturnTypeWithAnnotations.NeedsNullableAttribute())
            {
                compilation.EnsureNullableAttributeExists(diagnostics, getReturnTypeLocation(), modifyCompilation: true);
            }

            ParameterHelpers.EnsureNullableAttributeExists(compilation, this, Parameters, diagnostics, modifyCompilation: true);

            Location getReturnTypeLocation()
            {
                returnTypeLocation ??= this.ReturnTypeLocation;
                return returnTypeLocation;
            }
        }

        protected abstract void CheckConstraintsForExplicitInterfaceType(ConversionsBase conversions, BindingDiagnosticBag diagnostics);

        protected abstract void PartialMethodChecks(BindingDiagnosticBag diagnostics);
    }
}
