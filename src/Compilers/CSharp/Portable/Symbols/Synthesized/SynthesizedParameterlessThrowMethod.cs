// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeGen;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Throws an exception of a given type using a parameterless constructor.
    /// </summary>
    internal sealed class SynthesizedParameterlessThrowMethod : SynthesizedGlobalMethodSymbol
    {
        private readonly MethodSymbol _exceptionConstructor;

        internal SynthesizedParameterlessThrowMethod(SynthesizedPrivateImplementationDetailsType privateImplType, TypeSymbol returnType, string synthesizedMethodName, MethodSymbol exceptionConstructor)
            : base(privateImplType, returnType, synthesizedMethodName)
        {
            _exceptionConstructor = exceptionConstructor;
            this.SetParameters(ImmutableArray<ParameterSymbol>.Empty);
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            try
            {
                //throw new GivenException();

                var body = F.Throw(F.New(_exceptionConstructor, ImmutableArray<BoundExpression>.Empty));

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
