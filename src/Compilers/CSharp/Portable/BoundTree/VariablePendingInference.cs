// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Diagnostics;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class DeconstructionVariablePendingInference
    {
        protected override ErrorCode InferenceFailedError => ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedDeconstructionVariable;
    }

    internal partial class OutVariablePendingInference
    {
        protected override ErrorCode InferenceFailedError => ErrorCode.ERR_TypeInferenceFailedForImplicitlyTypedOutVariable;
    }

    internal partial class VariablePendingInference : BoundExpression
    {
        internal BoundExpression SetInferredTypeWithAnnotations(TypeWithAnnotations type, DiagnosticBag diagnosticsOpt)
        {
            Debug.Assert(type.HasType);

            return SetInferredTypeWithAnnotations(type, null, diagnosticsOpt);
        }

        internal BoundExpression SetInferredTypeWithAnnotations(TypeWithAnnotations type, Binder binderOpt, DiagnosticBag diagnosticsOpt)
        {
            Debug.Assert(binderOpt != null || type.HasType);
            Debug.Assert(this.Syntax.Kind() == SyntaxKind.SingleVariableDesignation ||
                (this.Syntax.Kind() == SyntaxKind.DeclarationExpression &&
                    ((DeclarationExpressionSyntax)this.Syntax).Designation.Kind() == SyntaxKind.SingleVariableDesignation));

            bool inferenceFailed = !type.HasType;

            if (inferenceFailed)
            {
                type = TypeWithAnnotations.Create(binderOpt.CreateErrorType("var"));
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
                        else
                        {
                            SyntaxNode typeOrDesignationSyntax = this.Syntax.Kind() == SyntaxKind.DeclarationExpression ?
                                ((DeclarationExpressionSyntax)this.Syntax).Type :
                                this.Syntax;

                            Binder.CheckRestrictedTypeInAsync(localSymbol.ContainingSymbol, type.Type, diagnosticsOpt, typeOrDesignationSyntax);
                        }
                    }

                    localSymbol.SetTypeWithAnnotations(type);
                    return new BoundLocal(this.Syntax, localSymbol, BoundLocalDeclarationKind.WithInferredType, constantValueOpt: null, isNullableUnknown: false, type: type.Type, hasErrors: this.HasErrors || inferenceFailed);

                case SymbolKind.Field:
                    var fieldSymbol = (GlobalExpressionVariable)this.VariableSymbol;
                    var inferenceDiagnostics = DiagnosticBag.GetInstance();

                    if (inferenceFailed)
                    {
                        ReportInferenceFailure(inferenceDiagnostics);
                    }

                    type = fieldSymbol.SetTypeWithAnnotations(type, inferenceDiagnostics);
                    inferenceDiagnostics.Free();

                    return new BoundFieldAccess(this.Syntax,
                                                this.ReceiverOpt,
                                                fieldSymbol,
                                                null,
                                                LookupResultKind.Viable,
                                                isDeclaration: true,
                                                type: type.Type,
                                                hasErrors: this.HasErrors || inferenceFailed);

                default:
                    throw ExceptionUtilities.UnexpectedValue(this.VariableSymbol.Kind);
            }
        }

        internal BoundExpression FailInference(Binder binder, DiagnosticBag diagnosticsOpt)
        {
            return this.SetInferredTypeWithAnnotations(default, binder, diagnosticsOpt);
        }

        private void ReportInferenceFailure(DiagnosticBag diagnostics)
        {
            SingleVariableDesignationSyntax designation;
            switch (this.Syntax.Kind())
            {
                case SyntaxKind.DeclarationExpression:
                    designation = (SingleVariableDesignationSyntax)((DeclarationExpressionSyntax)this.Syntax).Designation;
                    break;
                case SyntaxKind.SingleVariableDesignation:
                    designation = (SingleVariableDesignationSyntax)this.Syntax;
                    break;
                default:
                    throw ExceptionUtilities.Unreachable;
            }

            Binder.Error(
                diagnostics, this.InferenceFailedError, designation.Identifier,
                designation.Identifier.ValueText);
        }

        protected abstract ErrorCode InferenceFailedError { get; }
    }
}
