' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InlineMethod
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeRefactorings.InlineTemporary

    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(PredefinedCodeRefactoringProviderNames.InlineMethod)), [Shared]>
    <Export(GetType(VisualBasicInlineMethodRefactoringProvider))>
    Friend Class VisualBasicInlineMethodRefactoringProvider
        Inherits AbstractInlineMethodRefactoringProvider(Of InvocationExpressionSyntax, ExpressionSyntax, MethodBlockSyntax, StatementSyntax, LocalDeclarationStatementSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance, VisualBasicSemanticFactsService.Instance)
        End Sub

        Protected Overrides Function GetInlineExpression(methodBlock As MethodBlockSyntax) As ExpressionSyntax
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
            End If
            Return Nothing
        End Function

        Protected Overrides Function GetEnclosingMethodLikeNode(syntaxNode As SyntaxNode) As SyntaxNode
            While syntaxNode IsNot Nothing
                If TypeOf syntaxNode Is MethodBlockSyntax OrElse TypeOf syntaxNode Is LambdaExpressionSyntax Then
                    Return syntaxNode
                End If
                syntaxNode = syntaxNode.Parent
            End While

            Return Nothing
        End Function

        Protected Overrides Function GenerateTypeSyntax(symbol As ITypeSymbol, allowVar As Boolean) As SyntaxNode
            Return symbol.GenerateTypeSyntax()
        End Function

        Protected Overrides Function GenerateArrayInitializerExpression(arguments As ImmutableArray(Of SyntaxNode)) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function Parenthesize(expressionNode As ExpressionSyntax) As ExpressionSyntax
            Return expressionNode.Parenthesize()
        End Function

        Protected Overrides Function IsVariableInitializerInLocalDeclarationSyntax(expressionSyntax As InvocationExpressionSyntax, statementSyntaxEnclosingCallee As LocalDeclarationStatementSyntax) As Boolean
            Return False
        End Function

        Protected Overrides Function IsUsingInferTypeDeclarator(localDeclarationSyntax As LocalDeclarationStatementSyntax) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function UseExplicitTypeAndReplaceInitializerForDeclarationSyntax(localDeclarationSyntax As LocalDeclarationStatementSyntax, syntaxGenerator As SyntaxGenerator, type As ITypeSymbol, initializer As ExpressionSyntax, replacementInitializer As ExpressionSyntax) As LocalDeclarationStatementSyntax
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function IsValidExpressionUnderStatementExpression(expressionNode As ExpressionSyntax) As Boolean
            Return expressionNode.IsKind(SyntaxKind.AwaitExpression) Or expressionNode.IsKind(SyntaxKind.InvocationExpression)
        End Function
    End Class
End Namespace
