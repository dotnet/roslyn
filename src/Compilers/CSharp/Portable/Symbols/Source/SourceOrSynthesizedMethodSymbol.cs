// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceOrSynthesizedMethodSymbol : MethodSymbol
    {
        internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
        {
            Debug.Assert(this is
                SourceMemberMethodSymbol
                or SynthesizedUnionCtor
                or SynthesizedInstanceConstructor { ParameterCount: 0 }
                or SynthesizedRecordCopyCtor
                or SynthesizedInteractiveInitializerMethod
                or SynthesizedSubmissionConstructor);

            base.AfterAddingTypeMembersChecks(conversions, diagnostics);

            bool checkReadonly = false;
            bool checkReturnAttributes = false;
            bool checkReturnConstraints = false;
            bool checkParameterAttributes = false;
            bool checkParameterConstraints = false;
            bool checkNullableContextAttribute = false;
            bool checkCheckEffectiveAccessibilityAndFileTypes = false;

            switch (this)
            {
                case SynthesizedUnionCtor:
                    checkParameterConstraints = true;
                    checkCheckEffectiveAccessibilityAndFileTypes = true;
                    break;

                case SynthesizedInstanceConstructor:
                    Debug.Assert(this is { ParameterCount: 0 } or SynthesizedRecordCopyCtor or SynthesizedInteractiveInitializerMethod or SynthesizedSubmissionConstructor);
                    return;

                case SynthesizedInteractiveInitializerMethod:
                    return;

                case SourceMemberMethodSymbol:
                    {
                        checkReadonly = true;
                        checkNullableContextAttribute = true;

                        switch (this)
                        {
                            case SourceDelegateMethodSymbol.InvokeMethod:
                                checkParameterAttributes = true;
                                checkReturnAttributes = true;
                                break;
                            case SourceConstructorSymbolBase:
                                checkParameterAttributes = true;
                                checkParameterConstraints = true;
                                checkCheckEffectiveAccessibilityAndFileTypes = true;
                                break;
                            case SourceOrdinaryMethodOrUserDefinedOperatorSymbol:
                                checkParameterAttributes = true;
                                checkReturnAttributes = true;
                                checkReturnConstraints = true;
                                checkParameterConstraints = true;
                                checkCheckEffectiveAccessibilityAndFileTypes = true;
                                break;
                        }
                    }
                    break;

                default:
                    ExceptionUtilities.UnexpectedValue(this);
                    break;
            }

            var compilation = this.DeclaringCompilation;

            if (checkReadonly)
            {
                if (IsDeclaredReadOnly && !ContainingType.IsReadOnly)
                {
                    compilation.EnsureIsReadOnlyAttributeExists(diagnostics, GetFirstLocationOrNone(), modifyCompilation: true);
                }
            }

            // Defer computing location to avoid unnecessary allocations in most cases.
            Location? returnTypeLocation = null;

            // Check constraints on return type and parameters. Note: Dev10 uses the
            // method name location for any such errors. We'll do the same for return
            // type errors but for parameter errors, we'll use the parameter location.

            TypeWithAnnotations returnTypeWithAnnotations = this.ReturnTypeWithAnnotations;

            if (checkReturnConstraints)
            {
                returnTypeWithAnnotations.Type.CheckAllConstraints(compilation, conversions, this.GetFirstLocationOrNone(), diagnostics);
            }

            ImmutableArray<ParameterSymbol> parameters = this.Parameters;

            if (checkParameterConstraints)
            {
                foreach (var parameter in parameters)
                {
                    parameter.Type.CheckAllConstraints(compilation, conversions, parameter.GetFirstLocationOrNone(), diagnostics);
                }
            }

            if (checkReturnAttributes)
            {
                if (RefKind == RefKind.RefReadOnly)
                {
                    compilation.EnsureIsReadOnlyAttributeExists(diagnostics, getReturnTypeLocation(), modifyCompilation: true);
                }

                if (compilation.ShouldEmitNativeIntegerAttributes(returnTypeWithAnnotations.Type))
                {
                    compilation.EnsureNativeIntegerAttributeExists(diagnostics, getReturnTypeLocation(), modifyCompilation: true);
                }
            }

            if (checkParameterAttributes)
            {
                ParameterHelpers.EnsureRefKindAttributesExist(compilation, parameters, diagnostics, modifyCompilation: true);
                ParameterHelpers.EnsureParamCollectionAttributeExists(compilation, parameters, diagnostics, modifyCompilation: true);
                ParameterHelpers.EnsureNativeIntegerAttributeExists(compilation, parameters, diagnostics, modifyCompilation: true);
                ParameterHelpers.EnsureScopedRefAttributeExists(compilation, parameters, diagnostics, modifyCompilation: true);
            }

            if ((checkNullableContextAttribute | checkReturnAttributes | checkParameterAttributes) && compilation.ShouldEmitNullableAttributes(this))
            {
                if (checkNullableContextAttribute)
                {
                    if (ShouldEmitNullableContextValue(out _))
                    {
                        compilation.EnsureNullableContextAttributeExists(diagnostics, GetFirstLocationOrNone(), modifyCompilation: true);
                    }
                }

                if (checkReturnAttributes)
                {
                    if (returnTypeWithAnnotations.NeedsNullableAttribute())
                    {
                        compilation.EnsureNullableAttributeExists(diagnostics, getReturnTypeLocation(), modifyCompilation: true);
                    }
                }

                if (checkParameterAttributes)
                {
                    ParameterHelpers.EnsureNullableAttributeExists(compilation, this, parameters, diagnostics, modifyCompilation: true);
                }
            }

            if (checkCheckEffectiveAccessibilityAndFileTypes)
            {
                this.CheckEffectiveAccessibility(returnTypeWithAnnotations, parameters, diagnostics);
                this.CheckFileTypeUsage(returnTypeWithAnnotations, parameters, diagnostics);
            }

            return;

            Location getReturnTypeLocation()
            {
                returnTypeLocation ??= this.ReturnTypeLocation;
                return returnTypeLocation;
            }
        }

        protected abstract Location ReturnTypeLocation { get; }

        private void CheckEffectiveAccessibility(TypeWithAnnotations returnType, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag diagnostics)
        {
            if (this.DeclaredAccessibility <= Accessibility.Private || MethodKind == MethodKind.ExplicitInterfaceImplementation)
            {
                return;
            }

            ErrorCode code = (this.MethodKind == MethodKind.Conversion || this.MethodKind == MethodKind.UserDefinedOperator) ?
                ErrorCode.ERR_BadVisOpReturn :
                ErrorCode.ERR_BadVisReturnType;

            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, ContainingAssembly);
            if (!this.IsNoMoreVisibleThan(returnType, ref useSiteInfo))
            {
                // Inconsistent accessibility: return type '{1}' is less accessible than method '{0}'
                diagnostics.Add(code, GetFirstLocation(), this, returnType.Type);
            }

            code = (this.MethodKind == MethodKind.Conversion || this.MethodKind == MethodKind.UserDefinedOperator) ?
                ErrorCode.ERR_BadVisOpParam :
                ErrorCode.ERR_BadVisParamType;

            foreach (var parameter in parameters)
            {
                if (!parameter.TypeWithAnnotations.IsAtLeastAsVisibleAs(this, ref useSiteInfo))
                {
                    // Inconsistent accessibility: parameter type '{1}' is less accessible than method '{0}'
                    diagnostics.Add(code, GetFirstLocation(), this, parameter.Type);
                }
            }

            if (this.IsExtensionBlockMember() && ContainingType.ExtensionParameter is { } extensionParameter)
            {
                if (!extensionParameter.TypeWithAnnotations.IsAtLeastAsVisibleAs(this, ref useSiteInfo))
                {
                    // Inconsistent accessibility: parameter type '{1}' is less accessible than method '{0}'
                    diagnostics.Add(code, GetFirstLocation(), this, extensionParameter.Type);
                }
            }

            diagnostics.Add(GetFirstLocation(), useSiteInfo);
        }

        private void CheckFileTypeUsage(TypeWithAnnotations returnType, ImmutableArray<ParameterSymbol> parameters, BindingDiagnosticBag diagnostics)
        {
            NamedTypeSymbol containingType = ContainingType;

            if (containingType is { IsExtension: true, ContainingType: { } enclosing })
            {
                containingType = enclosing;
            }

            if (containingType.HasFileLocalTypes())
            {
                return;
            }

            if (returnType.Type.HasFileLocalTypes())
            {
                diagnostics.Add(ErrorCode.ERR_FileTypeDisallowedInSignature, GetFirstLocation(), returnType.Type, containingType);
            }

            foreach (var param in parameters)
            {
                if (param.Type.HasFileLocalTypes())
                {
                    diagnostics.Add(ErrorCode.ERR_FileTypeDisallowedInSignature, GetFirstLocation(), param.Type, containingType);
                }
            }
        }
    }
}
