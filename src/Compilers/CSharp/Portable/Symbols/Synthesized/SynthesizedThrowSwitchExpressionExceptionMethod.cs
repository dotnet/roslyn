// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Throws a 'System.Runtime.CompilerServices.SwitchExpressionException' with the given 'unmatchedValue'.
    /// </summary>
    internal sealed class SynthesizedThrowSwitchExpressionExceptionMethod : SynthesizedGlobalMethodSymbol
    {
        internal SynthesizedThrowSwitchExpressionExceptionMethod(SynthesizedPrivateImplementationDetailsType privateImplType, TypeSymbol returnType, TypeSymbol paramType)
            : base(privateImplType, returnType, PrivateImplementationDetails.SynthesizedThrowSwitchExpressionExceptionFunctionName)
        {
            this.SetParameters(ImmutableArray.Create(SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(paramType), 0, RefKind.None, "unmatchedValue")));
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            try
            {
                ParameterSymbol unmatchedValue = this.Parameters[0];

                //throw new SwitchExpressionException(unmatchedValue);

                Debug.Assert(unmatchedValue.Type.SpecialType == SpecialType.System_Object);
                var body = F.Throw(F.New(F.WellKnownMethod(WellKnownMember.System_Runtime_CompilerServices_SwitchExpressionException__ctorObject), ImmutableArray.Create<BoundExpression>(F.Parameter(unmatchedValue))));

                // NOTE: we created this block in its most-lowered form, so analysis is unnecessary
                F.CloseMethod(body);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
