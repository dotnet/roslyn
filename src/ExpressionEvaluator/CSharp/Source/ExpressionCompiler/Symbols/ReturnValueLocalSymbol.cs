// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#nullable disable // can 'method' be null here? https://github.com/dotnet/roslyn/issues/39166
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
