// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class OutVarLocalPendingInference
    {
        public BoundLocal SetInferredType(TypeSymbol type, bool success)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(this.Syntax.Kind() == SyntaxKind.DeclarationExpression);

            this.LocalSymbol.SetType(type);
            return new BoundLocal(this.Syntax, this.LocalSymbol, constantValueOpt: null, type: type, hasErrors: this.HasErrors || !success);
        }

        public BoundLocal FailInference(Binder binder, DiagnosticBag diagnosticsOpt)
        {
            if (diagnosticsOpt != null)
            {
                var declaration = (DeclarationExpressionSyntax)this.Syntax;
                Binder.Error(
                    diagnosticsOpt, ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable, declaration.Identifier(),
                    declaration.Identifier().ValueText);
            }

            return this.SetInferredType(binder.CreateErrorType("var"), success: false);
        }
    }
}