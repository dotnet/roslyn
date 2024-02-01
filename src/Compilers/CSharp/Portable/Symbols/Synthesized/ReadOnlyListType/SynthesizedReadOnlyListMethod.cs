// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal delegate BoundStatement GenerateMethodBodyDelegate(SyntheticBoundNodeFactory factory, MethodSymbol method, MethodSymbol interfaceMethod);

    internal sealed class SynthesizedReadOnlyListMethod : SynthesizedImplementationMethod
    {
        private readonly GenerateMethodBodyDelegate _generateMethodBody;

        internal SynthesizedReadOnlyListMethod(NamedTypeSymbol containingType, MethodSymbol interfaceMethod, GenerateMethodBodyDelegate generateMethodBody) :
            base(interfaceMethod, containingType)
        {
            _generateMethodBody = generateMethodBody;
        }

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory f = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            f.CurrentFunction = this;

            try
            {
                var body = _generateMethodBody(f, this, _interfaceMethod);
                f.CloseMethod(body);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                f.CloseMethod(f.ThrowNull());
            }
        }
    }
}
