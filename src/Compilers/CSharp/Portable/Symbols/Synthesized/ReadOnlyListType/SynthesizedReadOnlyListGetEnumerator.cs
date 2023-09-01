// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListGetEnumerator : SynthesizedImplementationMethod
    {
        internal SynthesizedReadOnlyListGetEnumerator(SynthesizedReadOnlyListTypeSymbol containingType, MethodSymbol interfaceMethod) : base(interfaceMethod, containingType)
        {
        }

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory f = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            f.CurrentFunction = this;

            try
            {
                // PROTOTYPE: Test missing member.
                var getEnumerator = (MethodSymbol)DeclaringCompilation.GetSpecialTypeMember(SpecialMember.System_Collections_IEnumerable__GetEnumerator);
                var field = ContainingType.GetFieldsToEmit().Single();
                // return _items.GetEnumerator();
                var statement = f.Return(
                    f.Call(
                        f.Field(f.This(), field),
                        getEnumerator));
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
