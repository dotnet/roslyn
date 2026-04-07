// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// This method encodes the information needed to round-trip extension types
    /// through metadata.
    ///
    /// It encodes receiver parameter - the only parameter of the method
    /// </summary>
    internal sealed class SynthesizedExtensionMarker : SynthesizedSourceOrdinaryMethodSymbol
    {
        internal SynthesizedExtensionMarker(SourceMemberContainerTypeSymbol extensionType, ParameterListSyntax parameterList)
            : base(extensionType, WellKnownMemberNames.ExtensionMarkerMethodName, parameterList.OpenParenToken.GetLocation(), parameterList,
                   (GetDeclarationModifiers(), MakeFlags(
                                                    MethodKind.Ordinary, RefKind.None, GetDeclarationModifiers(), returnsVoid: false, returnsVoidIsSet: false,
                                                    isExpressionBodied: false, isExtensionMethod: false, isNullableAnalysisEnabled: false, isVarArg: false,
                                                    isExplicitInterfaceImplementation: false, hasThisInitializer: false)))
        {
            Debug.Assert(extensionType.IsDefinition);
            Debug.Assert(extensionType.IsExtension);
            Debug.Assert(parameterList is not null);

            return;
        }

        private static DeclarationModifiers GetDeclarationModifiers() => DeclarationModifiers.Private | DeclarationModifiers.Static;

        public override TypeMemberVisibility MetadataVisibility
        {
            get
            {
                return ((SourceMemberContainerTypeSymbol)ContainingType.ContainingType).GetExtensionGroupingInfo().GetCorrespondingMarkerMethodVisibility(this);
            }
        }

        internal override bool HasSpecialName => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CloseMethod(F.Return());
        }

        protected override int GetParameterCountFromSyntax()
        {
            ParameterListSyntax parameterList = (ParameterListSyntax)syntaxReferenceOpt.GetSyntax();
            return parameterList.Parameters is [{ IsArgList: false }, ..] ? 1 : 0;
        }

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters) MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            return (TypeWithAnnotations.Create(Binder.GetSpecialType(DeclaringCompilation, SpecialType.System_Void, GetFirstLocation(), diagnostics)),
                    makeExtensionParameter(diagnostics) is { } parameter ? [parameter] : []);

            ParameterSymbol? makeExtensionParameter(BindingDiagnosticBag diagnostics)
            {
                ParameterListSyntax parameterList = (ParameterListSyntax)syntaxReferenceOpt.GetSyntax();
                int count = parameterList.Parameters.Count;

                if (count == 0)
                {
                    return null;
                }

                BinderFactory binderFactory = this.DeclaringCompilation.GetBinderFactory(parameterList.SyntaxTree);
                var withTypeParamsBinder = binderFactory.GetBinder(parameterList);

                // Constraints are checked later
                var signatureBinder = withTypeParamsBinder.WithAdditionalFlagsAndContainingMemberOrLambda(BinderFlags.SuppressConstraintChecks, this);

                for (int parameterIndex = 1; parameterIndex < count; parameterIndex++)
                {
                    diagnostics.Add(ErrorCode.ERR_ReceiverParameterOnlyOne, parameterList.Parameters[parameterIndex].GetLocation());
                }

                ParameterSymbol? parameter = ParameterHelpers.MakeExtensionReceiverParameter(withTypeParametersBinder: signatureBinder, owner: this, parameterList, diagnostics);

                if (parameter is { })
                {
                    TypeSymbol parameterType = parameter.TypeWithAnnotations.Type;
                    RefKind parameterRefKind = parameter.RefKind;
                    SyntaxNode? parameterTypeSyntax = parameterList.Parameters[0].Type;
                    Debug.Assert(parameterTypeSyntax is not null);

                    // Note: SourceOrdinaryMethodSymbol.ExtensionMethodChecks has similar checks, which should be kept in sync.
                    if (!parameterType.IsValidExtensionParameterType())
                    {
                        diagnostics.Add(ErrorCode.ERR_BadTypeforThis, parameterTypeSyntax, parameterType);
                    }
                    else if (parameterRefKind == RefKind.Ref && !parameterType.IsValueType)
                    {
                        diagnostics.Add(ErrorCode.ERR_RefExtensionParameterMustBeValueTypeOrConstrainedToOne, parameterTypeSyntax);
                    }
                    else if (parameterRefKind is RefKind.In or RefKind.RefReadOnlyParameter
                        && !parameterType.IsValidInOrRefReadonlyExtensionParameterType())
                    {
                        diagnostics.Add(ErrorCode.ERR_InExtensionParameterMustBeValueType, parameterTypeSyntax);
                    }

                    if (parameter.Name is "" && parameterRefKind != RefKind.None)
                    {
                        diagnostics.Add(ErrorCode.ERR_ModifierOnUnnamedReceiverParameter, parameterTypeSyntax);
                    }
                }

                if (parameter is { Name: var name } && name != "" &&
                    ContainingType.TypeParameters.Any(static (p, name) => p.Name == name, name))
                {
                    diagnostics.Add(ErrorCode.ERR_ReceiverParameterSameNameAsTypeParameter, parameter.GetFirstLocation(), name);
                }

                return parameter;
            }
        }
    }
}

