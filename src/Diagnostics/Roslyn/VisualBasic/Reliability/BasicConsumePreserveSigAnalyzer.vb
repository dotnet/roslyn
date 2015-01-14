' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Roslyn.Diagnostics.Analyzers.VisualBasic
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicConsumePreserveSigAnalyzer
        Inherits ConsumePreserveSigAnalyzer(Of SyntaxKind)

        Protected Overrides ReadOnly Property InvocationExpressionSyntaxKind As SyntaxKind
            Get
                Return SyntaxKind.InvocationExpression
            End Get
        End Property

        Protected Overrides Function IsExpressionStatementSyntaxKind(rawKind As Integer) As Boolean
            Return CType(rawKind, SyntaxKind) = SyntaxKind.ExpressionStatement
        End Function
    End Class
End Namespace
