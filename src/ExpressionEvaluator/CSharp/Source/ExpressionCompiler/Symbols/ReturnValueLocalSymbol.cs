// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class ReturnValueLocalSymbol : PlaceholderLocalSymbol
    {
        private readonly int _index;

        internal ReturnValueLocalSymbol(MethodSymbol method, string name, string displayName, TypeSymbol type, int index) :
            base(method, name, displayName, type)
        {
            _index = index;
        }

        internal override bool IsWritableVariable
        {
            get { return false; }
        }

        internal override BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            var method = GetIntrinsicMethod(compilation, ExpressionCompilerConstants.GetReturnValueMethodName);
            var argument = new BoundLiteral(
                syntax,
                Microsoft.CodeAnalysis.ConstantValue.Create(_index),
#nullable disable // can 'method' be null here?
                method.Parameters[0].Type);
#nullable enable
            var call = BoundCall.Synthesized(
                syntax,
                receiverOpt: null,
                method: method,
                arguments: ImmutableArray.Create<BoundExpression>(argument));
            return ConvertToLocalType(compilation, call, this.Type, diagnostics);
        }
    }
}
