// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class OutVariablePendingInference
    {
        public BoundExpression SetInferredType(TypeSymbol type, DiagnosticBag diagnosticsOpt)
        {
            Debug.Assert((object)type != null);

            return SetInferredType(type, null, diagnosticsOpt);
        }

        private BoundExpression SetInferredType(TypeSymbol type, Binder binderOpt, DiagnosticBag diagnosticsOpt)
        {
            Debug.Assert(binderOpt != null || (object)type != null);
            Debug.Assert(this.Syntax.Kind() == SyntaxKind.DeclarationExpression);

            bool inferenceFailed = ((object)type == null);

            if (inferenceFailed)
            {
                type = binderOpt.CreateErrorType("var");
            }

            switch (this.VariableSymbol.Kind)
            {
                case SymbolKind.Local:
                    var localSymbol = (SourceLocalSymbol)this.VariableSymbol;

                    if (diagnosticsOpt != null)
                    {
                        if (inferenceFailed)
                        {
                            ReportInferenceFailure(diagnosticsOpt);
                        }
                        else if (localSymbol.ContainingSymbol.Kind == SymbolKind.Method &&
                                 ((MethodSymbol)localSymbol.ContainingSymbol).IsAsync &&
                                 type.IsRestrictedType())
                        {
                            var declaration = (TypedVariableComponentSyntax)((DeclarationExpressionSyntax)this.Syntax).VariableComponent;
                            Binder.Error(diagnosticsOpt, ErrorCode.ERR_BadSpecialByRefLocal, declaration.Type, type);
                        }
                    }

                    localSymbol.SetType(type);
                    return new BoundLocal(this.Syntax, localSymbol, constantValueOpt: null, type: type, hasErrors: this.HasErrors || inferenceFailed);

                case SymbolKind.Field:
                    var fieldSymbol = (GlobalExpressionVariable)this.VariableSymbol;
                    var inferenceDiagnostics = DiagnosticBag.GetInstance();

                    if (inferenceFailed)
                    {
                        ReportInferenceFailure(inferenceDiagnostics);
                    }

                    fieldSymbol.SetType(type, inferenceDiagnostics);
                    inferenceDiagnostics.Free();

                    return new BoundFieldAccess(this.Syntax,
                                                this.ReceiverOpt,
                                                fieldSymbol, null, LookupResultKind.Viable, type, 
                                                this.HasErrors || inferenceFailed);

                default:
                    throw ExceptionUtilities.Unreachable;
            }

        }

        private void ReportInferenceFailure(DiagnosticBag diagnostics)
        {
            var declaration = (DeclarationExpressionSyntax)this.Syntax;
            Binder.Error(
                diagnostics, ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable, declaration.Identifier(),
                declaration.Identifier().ValueText);
        }

        public BoundExpression FailInference(Binder binder, DiagnosticBag diagnosticsOpt)
        {
            return this.SetInferredType(null, binder, diagnosticsOpt);
        }
    }
}