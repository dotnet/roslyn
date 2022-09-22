// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Throws a System.ArgumentNullException with the given 'paramName'.
    /// </summary>
    internal sealed class SynthesizedThrowMethod : SynthesizedGlobalMethodSymbol
    {
        internal SynthesizedThrowMethod(SourceModuleSymbol containingModule, PrivateImplementationDetails privateImplType, TypeSymbol returnType, TypeSymbol paramType)
            : base(containingModule, privateImplType, returnType, PrivateImplementationDetails.SynthesizedThrowFunctionName)
        {
            this.SetParameters(ImmutableArray.Create<ParameterSymbol>(SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(paramType), 0, RefKind.None, "paramName")));
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            try
            {
                ParameterSymbol paramName = this.Parameters[0];

                //throw new ArgumentNullException(paramName);

                var body = F.Throw(F.New(F.WellKnownMethod(WellKnownMember.System_ArgumentNullException__ctorString), ImmutableArray.Create<BoundExpression>(F.Parameter(paramName))));

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
