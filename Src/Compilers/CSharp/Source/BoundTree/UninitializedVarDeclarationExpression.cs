// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class UninitializedVarDeclarationExpression
    {
        public BoundDeclarationExpression SetInferredType(TypeSymbol type, bool success)
        {
            var syntaxNode = (DeclarationExpressionSyntax)this.Syntax;

            Binder.DeclareLocalVariable(
                (SourceLocalSymbol)this.LocalSymbol,
                syntaxNode.Variable.Identifier,
                type);

            return new BoundDeclarationExpression(syntaxNode, this.LocalSymbol,
                                                  new BoundTypeExpression(syntaxNode.Type, null, inferredType: true, type: type),
                                                  null,
                                                  this.ArgumentsOpt,
                                                  type,
                                                  hasErrors: this.HasErrors || !success);
        }

        public BoundDeclarationExpression FailInference(Binder binder, DiagnosticBag diagnosticsOpt)
        {
            if (diagnosticsOpt != null)
            {
                Binder.Error(diagnosticsOpt, ErrorCode.ERR_ImplicitlyTypedVariableWithNoInitializer, ((DeclarationExpressionSyntax)this.Syntax).Variable.Identifier);
            }

            return this.SetInferredType(binder.CreateErrorType("var"), success: false);
        }
    }
}