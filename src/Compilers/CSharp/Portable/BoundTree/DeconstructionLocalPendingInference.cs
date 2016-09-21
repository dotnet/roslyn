// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    // TODO: rename file
    internal partial class DeconstructionVariablePendingInference
    {
        public BoundExpression SetInferredType(TypeSymbol type, bool success)
        {
            Debug.Assert((object)type != null);
            Debug.Assert(this.Syntax.Kind() == SyntaxKind.SingleVariableDesignation);
            switch (this.VariableSymbol.Kind)
            {
                case SymbolKind.Local:
                    var local = (SourceLocalSymbol)this.VariableSymbol;
                    local.SetType(type);
                    return new BoundLocal(this.Syntax, local, constantValueOpt: null, type: type, hasErrors: this.HasErrors || !success);

                case SymbolKind.Field:
                    var field = (SourceMemberFieldSymbolFromDesignation)this.VariableSymbol;
                    var inferenceDiagnostics = DiagnosticBag.GetInstance();
                    field.SetType(type, inferenceDiagnostics);
                    inferenceDiagnostics.Free();
                    return new BoundFieldAccess(this.Syntax, this.ReceiverOpt, field, constantValueOpt: null);

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        public BoundExpression FailInference(Binder binder)
        {
            return this.SetInferredType(binder.CreateErrorType("var"), success: false);
        }
    }
}