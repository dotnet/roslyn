// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DeconstructionVariablePendingInference
    {
        public BoundExpression SetInferredType(TypeSymbol type, Binder binderOpt, DiagnosticBag diagnostics)
        {
            Debug.Assert(binderOpt != null || (object)type != null);

            Debug.Assert(this.Syntax.Kind() == SyntaxKind.SingleVariableDesignation ||
                (this.Syntax.Kind() == SyntaxKind.DeclarationExpression &&
                ((DeclarationExpressionSyntax)this.Syntax).Designation.Kind() == SyntaxKind.SingleVariableDesignation));

            bool inferenceFailed = ((object)type == null);

            if (inferenceFailed)
            {
                type = binderOpt.CreateErrorType("var");
            }

            switch (this.VariableSymbol.Kind)
            {
                case SymbolKind.Local:
                    var local = (SourceLocalSymbol)this.VariableSymbol;
                    if (inferenceFailed)
                    {
                        ReportInferenceFailure(diagnostics);
                    }
                    else
                    {
                        Binder.CheckRestrictedTypeInAsync(local.ContainingSymbol, type, diagnostics, this.Syntax);
                    }

                    local.SetType(type);
                    return new BoundLocal(this.Syntax, local, isDeclaration: true, constantValueOpt: null, type: type, hasErrors: this.HasErrors || inferenceFailed);

                case SymbolKind.Field:
                    var field = (GlobalExpressionVariable)this.VariableSymbol;
                    var inferenceDiagnostics = DiagnosticBag.GetInstance();
                    if (inferenceFailed)
                    {
                        ReportInferenceFailure(inferenceDiagnostics);
                    }
                    field.SetType(type, inferenceDiagnostics);
                    inferenceDiagnostics.Free();
                    return new BoundFieldAccess(this.Syntax, this.ReceiverOpt, field, constantValueOpt: null, hasErrors: this.HasErrors || inferenceFailed);

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private void ReportInferenceFailure(DiagnosticBag diagnostics)
        {
            SingleVariableDesignationSyntax designation;
            switch (this.Syntax.Kind())
            {
                case SyntaxKind.SingleVariableDesignation:
                    designation = (SingleVariableDesignationSyntax)this.Syntax;
                    break;
                case SyntaxKind.DeclarationExpression:
                    designation = (SingleVariableDesignationSyntax)((DeclarationExpressionSyntax)this.Syntax).Designation;
                    break;
                default:
                    throw ExceptionUtilities.Unreachable;
            }

            Binder.Error(
                diagnostics, ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable, designation.Identifier,
                designation.Identifier.ValueText);
        }

        public BoundExpression FailInference(Binder binder, DiagnosticBag diagnosticsOpt)
        {
            return this.SetInferredType(null, binder, diagnosticsOpt);
        }
    }
}