' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.AnalyzerPowerPack.Design
Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Design
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicCA1024DiagnosticAnalyzer
        Inherits CA1024DiagnosticAnalyzer(Of SyntaxKind)

        Protected Overrides Function GetCodeBlockEndedAnalyzer() As CA1024CodeBlockEndedAnalyzer
            Return New CodeBlockEndedAnalyzer()
        End Function

        Private Class CodeBlockEndedAnalyzer
            Inherits CA1024CodeBlockEndedAnalyzer

            Public Overrides ReadOnly Property SyntaxKindOfInterest As SyntaxKind
                Get
                    Return SyntaxKind.InvocationExpression
                End Get
            End Property

            Protected Overrides Function GetDiagnosticLocation(node As SyntaxNode) As Location
                Dim methodBlock = TryCast(node, MethodBlockSyntax)
                If methodBlock IsNot Nothing Then
                    Return methodBlock.SubOrFunctionStatement.Identifier.GetLocation()
                End If

                Dim methodStatement = TryCast(node, MethodStatementSyntax)
                If methodStatement IsNot Nothing Then
                    Return methodStatement.Identifier.GetLocation()
                End If

                Return Location.None
            End Function

        End Class
    End Class
End Namespace
