﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal abstract class SourceOrdinaryMethodSymbolBase : SourceOrdinaryMethodOrUserDefinedOperatorSymbol
    {
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly string _name;

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
            bool isNullableAnalysisEnabled,
            BindingDiagnosticBag diagnostics) :
            base(containingType,
                 syntax.GetReference(),
                 location,
                 isIterator: isIterator)
        {
            Debug.Assert(diagnostics.DiagnosticBag is object);

            _name = name;

            // The following two values are used to compute and store the initial value of the flags
            // However, these two components are placeholders; the correct value will be
            // computed lazily later and then the flags will be fixed up.
            const bool returnsVoid = false;

            DeclarationModifiers declarationModifiers;
            (declarationModifiers, HasExplicitAccessModifier) = this.MakeModifiers(methodKind, isPartial, hasBody, location, diagnostics);

            //explicit impls must be marked metadata virtual unless static
            var isMetadataVirtualIgnoringModifiers = methodKind == MethodKind.ExplicitInterfaceImplementation && (declarationModifiers & DeclarationModifiers.Static) == 0;

            this.MakeFlags(methodKind, declarationModifiers, returnsVoid, isExtensionMethod: isExtensionMethod, isNullableAnalysisEnabled: isNullableAnalysisEnabled, isMetadataVirtualIgnoringModifiers: isMetadataVirtualIgnoringModifiers);

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

        protected abstract ImmutableArray<TypeParameterSymbol> MakeTypeParameters(CSharpSyntaxNode node, BindingDiagnosticBag diagnostics);

#nullable enable
        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(this.MethodKind != MethodKind.UserDefinedOperator, "SourceUserDefinedOperatorSymbolBase overrides this");

            var (returnType, parameters, isVararg, declaredConstraints) = MakeParametersAndBindReturnType(diagnostics);

            MethodSymbol? overriddenOrExplicitlyImplementedMethod = MethodChecks(returnType, parameters, diagnostics);

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

            CheckModifiers(MethodKind == MethodKind.ExplicitInterfaceImplementation, isVararg, HasAnyBody, locations[0], diagnostics);
        }
#nullable disable

        protected abstract (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics);

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

            var diagnostics = BindingDiagnosticBag.GetInstance();
            AsyncMethodChecks(diagnostics);

            CompleteAsyncMethodChecks(diagnostics, cancellationToken);
            diagnostics.Free();
        }

        private void CompleteAsyncMethodChecks(BindingDiagnosticBag diagnosticsOpt, CancellationToken cancellationToken)
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

        public abstract override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken));

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

        private (DeclarationModifiers mods, bool hasExplicitAccessMod) MakeModifiers(MethodKind methodKind, bool isPartial, bool hasBody, Location location, BindingDiagnosticBag diagnostics)
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
            else
            {
                Debug.Assert(isExplicitInterfaceImplementation);

                if (isInterface)
                {
                    allowedModifiers |= DeclarationModifiers.Abstract;
                }
                else
                {
                    allowedModifiers |= DeclarationModifiers.Static;
                }
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

            ModifierUtils.CheckFeatureAvailabilityForStaticAbstractMembersInInterfacesIfNeeded(mods, isExplicitInterfaceImplementation, location, diagnostics);

            this.CheckUnsafeModifier(mods, diagnostics);

            ModifierUtils.ReportDefaultInterfaceImplementationModifiers(hasBody, mods,
                                                                        defaultInterfaceImplementationModifiers,
                                                                        location, diagnostics);

            mods = AddImpliedModifiers(mods, isInterface, methodKind, hasBody);
            return (mods, hasExplicitAccessMod);
        }

        protected abstract DeclarationModifiers MakeDeclarationModifiers(DeclarationModifiers allowedModifiers, BindingDiagnosticBag diagnostics);

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

        private void CheckModifiers(bool isExplicitInterfaceImplementation, bool isVararg, bool hasBody, Location location, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(!IsStatic || (!IsVirtual && !IsOverride)); // Otherwise 'virtual' and 'override' should have been reported and cleared earlier.

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
            else if (IsStatic && IsAbstract && !ContainingType.IsInterface)
            {
                // A static member '{0}' cannot be marked as 'abstract'
                diagnostics.Add(ErrorCode.ERR_StaticNotVirtual, location, ModifierUtils.ConvertSingleModifierToSyntaxText(DeclarationModifiers.Abstract));
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
            else if (isVararg && (IsGenericMethod || ContainingType.IsGenericType || Parameters.Length > 0 && Parameters[Parameters.Length - 1].IsParams))
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
    }
}
