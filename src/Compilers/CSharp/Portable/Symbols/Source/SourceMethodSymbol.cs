// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Base class to represent all source method-like symbols. This includes
    /// things like ordinary methods and constructors, and functions
    /// like lambdas and local functions.
    /// </summary>
    internal abstract partial class SourceMethodSymbol : MethodSymbol
    {
        /// <summary>
        /// If there are no constraints, returns an empty immutable array. Otherwise, returns an immutable
        /// array of types, indexed by the constrained type parameter in <see cref="MethodSymbol.TypeParameters"/>.
        /// </summary>
        public abstract ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes();

        /// <summary>
        /// If there are no constraints, returns an empty immutable array. Otherwise, returns an immutable
        /// array of kinds, indexed by the constrained type parameter in <see cref="MethodSymbol.TypeParameters"/>.
        /// </summary>
        public abstract ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds();

        protected static void ReportBadRefToken(TypeSyntax returnTypeSyntax, BindingDiagnosticBag diagnostics)
        {
            if (!returnTypeSyntax.HasErrors)
            {
                var refKeyword = returnTypeSyntax.GetFirstToken();
                diagnostics.Add(ErrorCode.ERR_UnexpectedToken, refKeyword.GetLocation(), refKeyword.ToString());
            }
        }

        protected bool AreContainingSymbolLocalsZeroed
        {
            get
            {
                if (ContainingSymbol is SourceMethodSymbol method)
                {
                    return method.AreLocalsZeroed;
                }
                else if (ContainingType is SourceMemberContainerTypeSymbol type)
                {
                    return type.AreLocalsZeroed;
                }
                else
                {
                    // Sometimes a source method symbol can be contained in a non-source symbol.
                    // For example in EE. We aren't concerned with respecting SkipLocalsInit in such cases.
                    return true;
                }
            }
        }

        internal void ReportAsyncParameterErrors(BindingDiagnosticBag diagnostics, Location location)
        {
            var parameters = this.GetParametersIncludingExtensionParameter(skipExtensionIfStatic: true);

            foreach (var parameter in parameters)
            {
                bool isExtensionParameter = parameter.IsExtensionParameter();
                if (parameter.RefKind != RefKind.None)
                {
                    diagnostics.Add(ErrorCode.ERR_BadAsyncArgType, getLocation(parameter, location, isExtensionParameter));
                }
                else if (parameter.Type.IsPointerOrFunctionPointer() && !isExtensionParameter)
                {
                    // We already reported an error elsewhere if the receiver parameter of an extension is a pointer type.
                    diagnostics.Add(ErrorCode.ERR_UnsafeAsyncArgType, getLocation(parameter, location, isExtensionParameter));
                }
                else if (parameter.Type.IsRestrictedType())
                {
                    diagnostics.Add(ErrorCode.ERR_BadSpecialByRefParameter, getLocation(parameter, location, isExtensionParameter), parameter.Type);
                }
            }

            static Location getLocation(ParameterSymbol parameter, Location location, bool isReceiverParameter)
            {
                return isReceiverParameter
                    ? location
                    : parameter.TryGetFirstLocation() ?? location;
            }
        }

        protected override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal sealed override bool UseUpdatedEscapeRules => ContainingModule.UseUpdatedEscapeRules;

        /// <summary>
        /// Whether the method has the <see langword="unsafe"/> keyword in its signature.
        /// Do not confuse with <see cref="CallerUnsafeMode"/>.
        /// </summary>
        internal abstract bool IsUnsafe { get; }

        internal bool HasRequiresUnsafeAttribute => GetDecodedWellKnownAttributeData()?.HasRequiresUnsafeAttribute == true;

        internal sealed override CallerUnsafeMode CallerUnsafeMode
        {
            get
            {
                if (ContainingModule.UseUpdatedMemorySafetyRules)
                {
                    Debug.Assert(AssociatedSymbol?.CallerUnsafeMode != CallerUnsafeMode.Implicit);

                    return HasRequiresUnsafeAttribute || IsExtern || AssociatedSymbol?.CallerUnsafeMode == CallerUnsafeMode.Explicit
                        ? CallerUnsafeMode.Explicit
                        : CallerUnsafeMode.None;
                }

                return this.HasParameterContainingPointerType() || ReturnType.ContainsPointerOrFunctionPointer()
                    ? CallerUnsafeMode.Implicit : CallerUnsafeMode.None;
            }
        }

        protected bool NeedsSynthesizedRequiresUnsafeAttribute
        {
            get
            {
                return ContainingModule.UseUpdatedMemorySafetyRules &&
                    !HasRequiresUnsafeAttribute &&
                    (IsExtern || AssociatedSymbol?.IsExtern == true);
            }
        }

        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            return SourceMemberContainerTypeSymbol.HasAsyncMethodBuilderAttribute(this, out builderArgument);
        }

        internal sealed override void AddSynthesizedAttributes(PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(moduleBuilder, ref attributes);
            AddSynthesizedAttributes(this, moduleBuilder, ref attributes);
        }

        internal static void AddSynthesizedAttributes(MethodSymbol target, PEModuleBuilder moduleBuilder, ref ArrayBuilder<CSharpAttributeData> attributes)
        {
            Debug.Assert(target is not (LambdaSymbol or LocalFunctionSymbol));

            if (target.IsDeclaredReadOnly && !target.ContainingType.IsReadOnly)
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeIsReadOnlyAttribute(target));
            }

            var compilation = target.DeclaringCompilation;

            if (target is SourceMethodSymbol { NeedsSynthesizedRequiresUnsafeAttribute: true })
            {
                Debug.Assert(target.CallerUnsafeMode == CallerUnsafeMode.Explicit);
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_RequiresUnsafeAttribute__ctor));
            }

            if (compilation.ShouldEmitNullableAttributes(target) &&
                target.ShouldEmitNullableContextValue(out byte nullableContextValue))
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeNullableContextAttribute(target, nullableContextValue));
            }

            if (target.RequiresExplicitOverride(out _))
            {
                // On platforms where it is present, add PreserveBaseOverridesAttribute when a methodimpl is used to override a class method.
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizePreserveBaseOverridesAttribute());
            }

            bool isAsync = target.IsAsync;
            bool isIterator = target.IsIterator;

            if ((isAsync || isIterator) && !target.IsExtensionBlockMember())
            {
                // The async state machine type is not synthesized until the async method body is rewritten. If we are
                // only emitting metadata the method body will not have been rewritten, and the async state machine
                // type will not have been created. In this case, omit the attribute.

                if (moduleBuilder.CompilationState.TryGetStateMachineType(target, out NamedTypeSymbol? stateMachineType))
                {
                    var arg = new TypedConstant(compilation.GetWellKnownType(WellKnownType.System_Type),
                        TypedConstantKind.Type, stateMachineType.GetUnboundGenericTypeOrSelf());

                    if (isAsync && isIterator)
                    {
                        AddSynthesizedAttribute(ref attributes,
                            compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_AsyncIteratorStateMachineAttribute__ctor,
                                ImmutableArray.Create(arg)));
                    }
                    else if (isAsync)
                    {
                        AddSynthesizedAttribute(ref attributes,
                            compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_AsyncStateMachineAttribute__ctor,
                                ImmutableArray.Create(arg)));
                    }
                    else if (isIterator)
                    {
                        AddSynthesizedAttribute(ref attributes,
                            compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_IteratorStateMachineAttribute__ctor,
                                ImmutableArray.Create(arg)));
                    }
                }

                if (isAsync && !isIterator)
                {
                    // Regular async (not async-iterator) kick-off method calls MoveNext, which contains user code.
                    // This means we need to emit DebuggerStepThroughAttribute in order
                    // to have correct stepping behavior during debugging.
                    // However, when runtime async is enabled, no state machine is generated and the kickoff method
                    // directly contains the async logic, so the attribute should not be added.
                    if (!compilation.IsRuntimeAsyncEnabledIn(target))
                    {
                        AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDebuggerStepThroughAttribute());
                    }
                }
            }

            // Do not generate CompilerGeneratedAttribute for members of compiler-generated types:
            if (((target.IsImplicitlyDeclared && target is not SourceFieldLikeEventSymbol.SourceEventDefinitionAccessorSymbol { PartialImplementationPart.IsImplicitlyDeclared: false }) ||
                 target is SourcePropertyAccessorSymbol { IsAutoPropertyAccessor: true }) &&
                !target.ContainingType.IsImplicitlyDeclared &&
                target is SynthesizedMethodBaseSymbol or
                          SourcePropertyAccessorSymbol or
                          SynthesizedSourceOrdinaryMethodSymbol or
                          SynthesizedRecordEqualityOperatorBase or
                          SynthesizedEventAccessorSymbol or
                          SourceFieldLikeEventSymbol.SourceEventDefinitionAccessorSymbol)
            {
                Debug.Assert(WellKnownMembers.IsSynthesizedAttributeOptional(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
                AddSynthesizedAttribute(ref attributes,
                    compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerGeneratedAttribute__ctor));
            }

            if (target is SourceConstructorSymbolBase)
            {
                AddRequiredMembersMarkerAttributes(ref attributes, target);
            }

            if (target.IsExtensionMethod)
            {
                // No need to check if [Extension] attribute was explicitly set since
                // we'll issue CS1112 error in those cases and won't generate IL.
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(
                    WellKnownMember.System_Runtime_CompilerServices_ExtensionAttribute__ctor));
            }

            if (target is SourcePropertyAccessorSymbol { AssociatedSymbol: SourcePropertySymbolBase property })
            {
                if (!target.NotNullMembers.IsEmpty)
                {
                    foreach (var attributeData in property.MemberNotNullAttributeIfExists)
                    {
                        AddSynthesizedAttribute(ref attributes, SynthesizedAttributeData.Create(attributeData));
                    }
                }

                if (!target.NotNullWhenTrueMembers.IsEmpty || !target.NotNullWhenFalseMembers.IsEmpty)
                {
                    foreach (var attributeData in property.MemberNotNullWhenAttributeIfExists)
                    {
                        AddSynthesizedAttribute(ref attributes, SynthesizedAttributeData.Create(attributeData));
                    }
                }
            }

            if (target is MethodToClassRewriter.BaseMethodWrapperSymbol)
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor));
            }

            if (IsInstanceIncrementDecrementOrCompoundAssignmentOperator(target))
            {
                AddSynthesizedAttribute(ref attributes, compilation.TrySynthesizeAttribute(WellKnownMember.System_Runtime_CompilerServices_CompilerFeatureRequiredAttribute__ctor,
                    ImmutableArray.Create(new TypedConstant(compilation.GetSpecialType(SpecialType.System_String), TypedConstantKind.Primitive, nameof(CompilerFeatureRequiredFeatures.UserDefinedCompoundAssignmentOperators)))
                    ));
            }

            if (target.IsExtensionBlockMember())
            {
                AddSynthesizedAttribute(ref attributes, moduleBuilder.SynthesizeExtensionMarkerAttribute(target, ((SourceNamedTypeSymbol)target.ContainingType).ExtensionMarkerName));
            }
        }

        internal static bool IsInstanceIncrementDecrementOrCompoundAssignmentOperator(MethodSymbol target)
        {
            if (target.MethodKind == MethodKind.UserDefinedOperator && !target.IsStatic)
            {
                SyntaxKind syntaxKind = SyntaxFacts.GetOperatorKind(target.Name);

                return syntaxKind is (SyntaxKind.PlusPlusToken or SyntaxKind.MinusMinusToken) ||
                       SyntaxFacts.IsOverloadableCompoundAssignmentOperator(syntaxKind);
            }

            return false;
        }
    }
}
