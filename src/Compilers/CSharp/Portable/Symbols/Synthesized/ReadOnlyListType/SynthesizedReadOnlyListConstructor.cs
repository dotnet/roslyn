// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedReadOnlyListConstructor : SynthesizedInstanceConstructor
    {
        internal SynthesizedReadOnlyListConstructor(SynthesizedReadOnlyListTypeSymbol containingType, TypeSymbol parameterType, string parameterName) : base(containingType)
        {
            Parameters = ImmutableArray.Create(
                SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(parameterType), ordinal: 0, RefKind.None, parameterName));
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
                var field = ContainingType.GetFieldsToEmit().Single();
                var parameter = Parameters.Single();

                var block = f.Block(
                    // object..ctor();
                    f.ExpressionStatement(f.Call(f.This(), baseConstructor)),
                    // _items = items;
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
