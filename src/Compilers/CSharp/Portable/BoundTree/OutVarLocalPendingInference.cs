// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class OutVarLocalPendingInference
    {
        public BoundLocal SetInferredType(TypeSymbol type, bool success)
        {
            var syntaxNode = (ArgumentSyntax)this.Syntax;

            Binder.DeclareLocalVariable(
                (SourceLocalSymbol)this.LocalSymbol,
                syntaxNode.Identifier,
                type);

            return new BoundLocal(syntaxNode, this.LocalSymbol, constantValueOpt: null, type: type, hasErrors: this.HasErrors || !success);
        }

        public BoundLocal FailInference(Binder binder, DiagnosticBag diagnosticsOpt)
        {
            if (diagnosticsOpt != null)
            {
                Binder.Error(diagnosticsOpt, ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable, ((ArgumentSyntax)this.Syntax).Identifier);
            }

            return this.SetInferredType(binder.CreateErrorType("var"), success: false);
        }
    }
}