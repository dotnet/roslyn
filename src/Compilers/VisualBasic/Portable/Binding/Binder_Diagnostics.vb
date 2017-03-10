' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' Various helpers to report diagnostics are declared in this part.

    Partial Friend Class Binder

        ''' <summary>
        ''' Report a diagnostic, and also produce an error expression with error type.
        ''' </summary>
        Public Shared Function ReportDiagnosticAndProduceBadExpression(diagBag As DiagnosticBag,
                                                                       syntax As VisualBasicSyntaxNode,
                                                                       id As ERRID) As BoundExpression
            Return ReportDiagnosticAndProduceBadExpression(diagBag, syntax, ErrorFactory.ErrorInfo(id))
        End Function

        ''' <summary>
        ''' Report a diagnostic, and also produce an error expression with error type.
        ''' </summary>
        Public Shared Function ReportDiagnosticAndProduceBadExpression(diagBag As DiagnosticBag,
                                                                  syntax As VisualBasicSyntaxNode,
                                                                  id As ERRID,
                                                                  ParamArray args As Object()) As BoundExpression
            Return ReportDiagnosticAndProduceBadExpression(diagBag, syntax, ErrorFactory.ErrorInfo(id, args))
        End Function

        ''' <summary>
        ''' Report a diagnostic, and also produce an error expression with error type.
        ''' </summary>
        Public Shared Function ReportDiagnosticAndProduceBadExpression(diagBag As DiagnosticBag,
                                                                  syntax As VisualBasicSyntaxNode,
                                                                  info As DiagnosticInfo,
                                                                  ParamArray nodes As BoundNode()) As BoundExpression
            Return BadExpression(syntax,
                                 If(nodes.IsEmpty, ImmutableArray(Of BoundNode).Empty, ImmutableArray.Create(Of BoundNode)(nodes)),
                                 ReportDiagnosticAndProduceErrorTypeSymbol(diagBag, syntax, info))
        End Function

        ''' <summary>
        ''' Report a diagnostic, and also produce an error expression with error type.
        ''' </summary>
        Public Shared Function ReportDiagnosticAndProduceErrorTypeSymbol(diagBag As DiagnosticBag,
                                                                  syntax As VisualBasicSyntaxNode,
                                                                  id As ERRID,
                                                                  ParamArray args As Object()) As ErrorTypeSymbol
            Return ReportDiagnosticAndProduceErrorTypeSymbol(diagBag, syntax, ErrorFactory.ErrorInfo(id, args))
        End Function

        ''' <summary>
        ''' Report a diagnostic, and also produce an error expression with error type.
        ''' </summary>
        Public Shared Function ReportDiagnosticAndProduceErrorTypeSymbol(diagBag As DiagnosticBag,
                                                                  syntax As VisualBasicSyntaxNode,
                                                                  info As DiagnosticInfo) As ErrorTypeSymbol
            ReportDiagnostic(diagBag, syntax, info)
            Return ErrorTypeSymbol.UnknownResultType
        End Function

    End Class

End Namespace
