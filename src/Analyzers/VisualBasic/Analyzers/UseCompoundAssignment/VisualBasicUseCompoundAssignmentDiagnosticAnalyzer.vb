' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.UseCompoundAssignment
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCompoundAssignment
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicUseCompoundAssignmentDiagnosticAnalyzer
        Inherits AbstractUseCompoundAssignmentDiagnosticAnalyzer(Of SyntaxKind, AssignmentStatementSyntax, BinaryExpressionSyntax)

        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance, Kinds)
        End Sub

        Protected Overrides Function GetAnalysisKind() As SyntaxKind
            Return SyntaxKind.SimpleAssignmentStatement
        End Function

        Protected Overrides Function IsSupported(assignmentKind As SyntaxKind, options As ParseOptions) As Boolean
            Return True
        End Function

        Protected Overrides Function TryGetIncrementOrDecrement(opKind As SyntaxKind, constantValue As Object) As Integer
            Return 0
        End Function
    End Class
End Namespace
