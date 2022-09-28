// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    /// <summary>
    /// Synthesized expression evaluation method.
    /// </summary>
    internal sealed class EEConstructorSymbol : SynthesizedInstanceConstructor
    {
        internal EEConstructorSymbol(NamedTypeSymbol containingType)
            : base(containingType)
        {
        }

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var noLocals = ImmutableArray<LocalSymbol>.Empty;
            var initializerInvocation = MethodCompiler.BindImplicitConstructorInitializer(this, diagnostics, compilationState.Compilation);
            var syntax = initializerInvocation.Syntax;

            compilationState.AddSynthesizedMethod(this,
                new BoundBlock(
                    syntax,
                    noLocals,
                    ImmutableArray.Create<BoundStatement>(
                        new BoundExpressionStatement(syntax, initializerInvocation),
                        new BoundReturnStatement(syntax, RefKind.None, null, @checked: false))));
        }
    }
}
