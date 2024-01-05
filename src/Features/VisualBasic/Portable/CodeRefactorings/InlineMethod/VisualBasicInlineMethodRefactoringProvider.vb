' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InlineMethod
Imports Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary

    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InlineMethod), [Shared]>
    <Export(GetType(VisualBasicInlineMethodRefactoringProvider))>
    Friend Class VisualBasicInlineMethodRefactoringProvider
        Inherits AbstractInlineMethodRefactoringProvider(Of MethodBlockSyntax, ExecutableStatementSyntax, ExpressionSyntax, InvocationExpressionSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance, VisualBasicSemanticFactsService.Instance)
        End Sub

        Protected Overrides Function GetRawInlineExpression(methodBlock As MethodBlockSyntax) As ExpressionSyntax
            Dim statements = methodBlock.Statements
            If statements.Count = 1 Then
                Dim singleStatement = statements(0)
                Dim returnStatement = TryCast(singleStatement, ReturnStatementSyntax)
                If returnStatement IsNot Nothing Then
                    Return returnStatement.Expression
                End If

                Dim expressionStatement = TryCast(singleStatement, ExpressionStatementSyntax)
                If expressionStatement IsNot Nothing Then
                    Return expressionStatement.Expression
                End If

                Dim throwStatement = TryCast(singleStatement, ThrowStatementSyntax)
                If throwStatement IsNot Nothing Then
                    Return throwStatement.Expression
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GenerateTypeSyntax(symbol As ITypeSymbol, allowVar As Boolean) As SyntaxNode
            Return symbol.GenerateTypeSyntax()
        End Function

        Protected Overrides Function GenerateLiteralExpression(typeSymbol As ITypeSymbol, value As Object) As ExpressionSyntax
            Return GenerateExpression(VisualBasicSyntaxGenerator.Instance, typeSymbol, value, canUseFieldReference:=True)
        End Function

        Protected Overrides Function IsFieldDeclarationSyntax(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.FieldDeclaration)
        End Function

        Protected Overrides Function IsValidExpressionUnderExpressionStatement(expressionNode As ExpressionSyntax) As Boolean
            Return expressionNode.IsKind(SyntaxKind.AwaitExpression) OrElse expressionNode.IsKind(SyntaxKind.InvocationExpression)
        End Function

        Protected Overrides Function CanBeReplacedByThrowExpression(syntaxNode As SyntaxNode) As Boolean
            ' Throw Expression doesn't exist in VB
            Return False
        End Function
    End Class
End Namespace
