' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.InlineMethod
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.InlineMethod
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeRefactoringProviderNames.InlineMethod), [Shared]>
    <Export(GetType(VisualBasicInlineMethodRefactoringProvider))>
    Friend NotInheritable Class VisualBasicInlineMethodRefactoringProvider
        Inherits AbstractInlineMethodRefactoringProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
            MyBase.New(VisualBasicSyntaxFacts.Instance)
        End Sub

        Protected Overrides Async Function GetInvocationExpressionSyntaxNodeAsync(context As CodeRefactoringContext) As Task(Of SyntaxNode)
            Dim syntaxNode = Await context.TryGetRelevantNodeAsync(Of InvocationExpressionSyntax).ConfigureAwait(False)
            Return syntaxNode
        End Function

        Protected Overrides Function IsMethodContainsOneStatement(calleeMethodDeclarationSyntaxNode As SyntaxNode) As Boolean
            Dim methodStatementSyntaxNode = TryCast(calleeMethodDeclarationSyntaxNode, MethodStatementSyntax)
            If methodStatementSyntaxNode IsNot Nothing Then
                Dim methodBlock = TryCast(methodStatementSyntaxNode.Parent, MethodBlockSyntax)
                If methodBlock IsNot Nothing Then
                    Dim statements = methodBlock.Statements
                    If statements.Count = 1 Then
                        Dim singleStatement = statements(0)

                        Return statements.Count = 1 _
                            AndAlso (TypeOf singleStatement Is ReturnStatementSyntax _
                                OrElse TypeOf singleStatement Is ExpressionStatementSyntax _
                                OrElse TypeOf singleStatement Is ThrowStatementSyntax)
                    End If
                End If
            End If

            Return Nothing
        End Function

        Protected Overrides Function GetParameterSymbol(semanticModel As SemanticModel, argumentSyntaxNode As SyntaxNode, cancellationToken As CancellationToken) As IParameterSymbol
            Throw New NotImplementedException
        End Function

        Protected Overrides Function IsExpressionStatement(syntaxNode As SyntaxNode) As Boolean
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GenerateLiteralExpression(typeSymbol As ITypeSymbol, value As Object) As SyntaxNode
            Throw New NotImplementedException
        End Function

        Protected Overrides Function IsExpressionSyntax(syntaxNode As SyntaxNode) As Boolean
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GetIdentifierTokenTextFromIdentifierNameSyntax(syntaxNode As SyntaxNode) As String
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GetSingleVariableNameFromDeclarationExpression(syntaxNode As SyntaxNode) As String
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GenerateArrayInitializerExpression(arguments As ImmutableArray(Of SyntaxNode)) As SyntaxNode
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GenerateLocalDeclarationStatementWithRightHandExpression(identifierTokenName As String, type As ITypeSymbol, expression As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GenerateLocalDeclarationStatement(identifierTokenName As String, type As ITypeSymbol) As SyntaxNode
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GenerateIdentifierNameSyntaxNode(name As String) As SyntaxNode
            Throw New NotImplementedException
        End Function

        Protected Overrides Function GetInlineStatement(calleeMethodDeclarationSyntaxNode As SyntaxNode) As SyntaxNode
            Dim methodStatementSyntaxNode = TryCast(calleeMethodDeclarationSyntaxNode, MethodStatementSyntax)
            Dim inlineSyntaxNode As SyntaxNode = Nothing
            If methodStatementSyntaxNode IsNot Nothing Then
                Dim methodBlock = TryCast(methodStatementSyntaxNode.Parent, MethodBlockSyntax)
                If methodBlock IsNot Nothing Then
                    Dim statements = methodBlock.Statements
                    If statements.Count = 1 Then
                        Dim singleStatement = statements(0)
                        Dim returnStatement = TryCast(singleStatement, ReturnStatementSyntax)
                        If returnStatement IsNot Nothing Then
                            inlineSyntaxNode = returnStatement.Expression
                        End If

                        Dim expressionStatement = TryCast(singleStatement, ExpressionStatementSyntax)
                        If expressionStatement IsNot Nothing Then
                            inlineSyntaxNode = expressionStatement.Expression
                        End If

                        Dim throwStatement = TryCast(singleStatement, ThrowStatementSyntax)
                        If throwStatement IsNot Nothing Then
                            inlineSyntaxNode = throwStatement.Expression
                        End If
                    End If
                End If
            End If

            If inlineSyntaxNode Is Nothing Then
                inlineSyntaxNode = SyntaxFactory.EmptyStatement()
            End If

            Return inlineSyntaxNode
        End Function

        Protected Overrides Function IsEmbeddedStatementOwner(syntaxNode As SyntaxNode) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GenerateTypeSyntax(symbol As ITypeSymbol) As SyntaxNode
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
