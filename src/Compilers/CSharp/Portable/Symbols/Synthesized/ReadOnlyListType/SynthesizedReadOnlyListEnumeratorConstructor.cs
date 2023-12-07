// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListEnumeratorConstructor : SynthesizedInstanceConstructor
    {
        internal SynthesizedReadOnlyListEnumeratorConstructor(SynthesizedReadOnlyListEnumeratorTypeSymbol containingType, TypeSymbol parameterType) : base(containingType)
        {
            Parameters = ImmutableArray.Create(
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(parameterType), ordinal: 0, RefKind.None, "item"));
        }

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory f = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            f.CurrentFunction = this;

            try
            {
                var baseConstructor = ContainingType.BaseTypeNoUseSiteDiagnostics.InstanceConstructors.Single();
                var field = ContainingType.GetFieldsToEmit().First();
                var parameter = Parameters.Single();

                var block = f.Block(
                    // object..ctor();
                    f.ExpressionStatement(f.Call(f.This(), baseConstructor)),
                    // _item = item;
                    f.Assignment(f.Field(f.This(), field), f.Parameter(parameter)),
                    // return;
                    f.Return());
                f.CloseMethod(block);
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                f.CloseMethod(f.ThrowNull());
            }
        }
    }
}
