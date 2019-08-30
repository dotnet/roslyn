// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
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
                        new BoundReturnStatement(syntax, RefKind.None, null))));
        }
    }
}
