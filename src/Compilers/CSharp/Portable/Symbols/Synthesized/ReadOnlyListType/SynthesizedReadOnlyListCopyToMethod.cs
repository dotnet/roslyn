// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListCopyToMethod : SynthesizedImplementationMethod
    {
        internal SynthesizedReadOnlyListCopyToMethod(SynthesizedReadOnlyListTypeSymbol containingType, MethodSymbol interfaceMethod) : base(interfaceMethod, containingType)
        {
        }

        internal sealed override bool SynthesizesLoweredBoundBody => true;

        internal sealed override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory f = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            f.CurrentFunction = this;

            try
            {
                var exceptionType = DeclaringCompilation.GetWellKnownType(WellKnownType.System_NotSupportedException);
                var constructor = exceptionType.InstanceConstructors.Single(c => c.ParameterCount == 0);
                // throw new System.NotSupportedException();
                var statement = f.Throw(f.New(constructor));
                f.CloseMethod(statement);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                f.CloseMethod(f.ThrowNull());
            }
        }
    }
}
