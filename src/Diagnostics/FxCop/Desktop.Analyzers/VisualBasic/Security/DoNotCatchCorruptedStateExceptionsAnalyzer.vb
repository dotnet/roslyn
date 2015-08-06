' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports Desktop.Analyzers.Common

Namespace Desktop.Analyzers

    ''' <summary>
    ''' CA2153: Do not catch corrupted state exceptions in general handlers
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class BasicDoNotCatchCorruptedStateExceptionsAnalyzer
        Inherits DoNotCatchCorruptedStateExceptionsAnalyzer(Of SyntaxKind, CatchBlockSyntax, ThrowStatementSyntax)

        Protected Overrides Function GetAnalyzer(compilationTypes As CompilationSecurityTypes, owningSymbol As ISymbol, codeBlock As SyntaxNode) As Analyzer
            Return New BasicAnalyzer(compilationTypes, owningSymbol, codeBlock)
        End Function

        Private NotInheritable Class BasicAnalyzer
            Inherits Analyzer

            Public Overrides ReadOnly Property CatchClauseKind As SyntaxKind
                Get
                    Return SyntaxKind.CatchBlock
                End Get
            End Property

            Public Overrides ReadOnly Property ThrowStatementKind As SyntaxKind
                Get
                    Return SyntaxKind.ThrowStatement
                End Get
            End Property

            Public Sub New(compilationTypes As CompilationSecurityTypes, owningSymbol As ISymbol, codeBlock As SyntaxNode)
                MyBase.New(compilationTypes, owningSymbol, codeBlock)
            End Sub

            Protected Overrides Function GetExceptionTypeSymbolFromCatchClause(catchNode As CatchBlockSyntax, model As SemanticModel) As ISymbol
                Debug.Assert(catchNode.CatchStatement IsNot Nothing)
                Dim catchDeclaration As SimpleAsClauseSyntax = catchNode.CatchStatement.AsClause
                Dim exceptionTypeSym As ISymbol = Nothing
                If catchDeclaration Is Nothing Then
                    exceptionTypeSym = TypesOfInterest.SystemObject
                Else
                    exceptionTypeSym = SyntaxNodeHelper.GetSymbol(catchDeclaration.Type, model)
                End If
                Return exceptionTypeSym
            End Function

            Protected Overrides Function IsThrowStatementWithNoArgument(throwNode As ThrowStatementSyntax) As Boolean
                Debug.Assert(throwNode IsNot Nothing)
                Return throwNode.Expression Is Nothing
            End Function

            Protected Overrides Function IsCatchClause(node As SyntaxNode) As Boolean
                Debug.Assert(node IsNot Nothing)
                Return node.Kind() = SyntaxKind.CatchBlock
            End Function

            Protected Overrides Function IslambdaExpression(node As SyntaxNode) As Boolean
                Debug.Assert(node IsNot Nothing)
                Dim kind As SyntaxKind = node.Kind()
                Return kind = SyntaxKind.MultiLineFunctionLambdaExpression Or
                       kind = SyntaxKind.MultiLineSubLambdaExpression Or
                       kind = SyntaxKind.SingleLineFunctionLambdaExpression Or
                       kind = SyntaxKind.SingleLineSubLambdaExpression
            End Function
        End Class
    End Class
End Namespace
