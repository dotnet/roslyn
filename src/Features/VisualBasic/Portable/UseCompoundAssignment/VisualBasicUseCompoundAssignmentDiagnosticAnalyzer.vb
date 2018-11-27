' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UseCompoundAssignment

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseCompoundAssignmentDiagnosticAnalyzer
        Inherits AbstractUseCompoundAssignmentDiagnosticAnalyzer(Of SyntaxKind, AssignmentStatementSyntax, BinaryExpressionSyntax)

        Public Sub New()
            MyBase.New(VisualBasicSyntaxFactsService.Instance, Kinds)
        End Sub

        Protected Overrides Function GetKind(rawKind As Integer) As SyntaxKind
            Return CType(rawKind, SyntaxKind)
        End Function

        Protected Overrides Function GetAnalysisKind() As SyntaxKind
            Return SyntaxKind.SimpleAssignmentStatement
        End Function

        Protected Overrides Function IsSupported(assignmentKind As SyntaxKind, options As ParseOptions) As Boolean
            Return True
        End Function
    End Class
End Namespace
