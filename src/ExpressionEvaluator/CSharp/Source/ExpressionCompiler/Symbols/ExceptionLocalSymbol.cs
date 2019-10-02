// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed class ExceptionLocalSymbol : PlaceholderLocalSymbol
    {
        private readonly string _getExceptionMethodName;

        internal ExceptionLocalSymbol(MethodSymbol method, string name, string displayName, TypeSymbol type, string getExceptionMethodName) :
            base(method, name, displayName, type)
        {
            _getExceptionMethodName = getExceptionMethodName;
        }

        internal override bool IsWritableVariable
        {
            get { return false; }
        }

        internal override BoundExpression RewriteLocal(CSharpCompilation compilation, EENamedTypeSymbol container, SyntaxNode syntax, DiagnosticBag diagnostics)
        {
            var method = GetIntrinsicMethod(compilation, _getExceptionMethodName);
            var call = BoundCall.Synthesized(syntax, receiverOpt: null, method: method);
            return ConvertToLocalType(compilation, call, this.Type, diagnostics);
        }
    }
}
