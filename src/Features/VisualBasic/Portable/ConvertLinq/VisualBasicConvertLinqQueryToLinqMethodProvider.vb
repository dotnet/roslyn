' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertLinq
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertLinq
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertLinqQueryToLinqMethodProvider)), [Shared]>
    Partial Friend NotInheritable Class VisualBasicConvertLinqQueryToLinqMethodProvider
        Inherits AbstractConvertLinqProvider

        Protected Overrides Function CreateAnalyzer(syntaxFacts As ISyntaxFactsService, semanticModel As SemanticModel) As IAnalyzer
            Return New VisualBasicAnalyzer(syntaxFacts, semanticModel)
        End Function


        Private NotInheritable Class VisualBasicAnalyzer
            Inherits Analyzer(Of QueryExpressionSyntax, ExpressionSyntax)
            Public Sub New(syntaxFacts As ISyntaxFactsService, semanticModel As SemanticModel)
                MyBase.New(syntaxFacts, semanticModel)
            End Sub

            Protected Overrides ReadOnly Property Title() As String
                Get
                    Return VBFeaturesResources.Convert_linq_query_to_linq_method
                End Get
            End Property

            Protected Overrides Function Convert(source As QueryExpressionSyntax) As ExpressionSyntax
                Dim context = New ConversionContext()
                Dim expression As ExpressionSyntax
                expression = Nothing
                For Each clause In source.Clauses
                    expression = ProcessQueryClause(context, expression, clause)
                    If expression Is Nothing Then
                        Return Nothing
                    End If
                Next
                Return expression.WithTrailingTrivia(source.GetTrailingTrivia())
            End Function

            Private Function ProcessQueryClause(context As ConversionContext, expression As ExpressionSyntax, queryClause As QueryClauseSyntax) As ExpressionSyntax
                Dim identifier = context.Identifier
                Select Case (queryClause.Kind())
                    Case SyntaxKind.FromClause : Return ProcessFromClause(context, expression, DirectCast(queryClause, FromClauseSyntax))
                    Case SyntaxKind.DistinctClause : Return CreateInvocationExpression(expression, "Distinct")
                    Case SyntaxKind.WhereClause : Return CreateInvocationExpression(expression, identifier, DirectCast(queryClause, WhereClauseSyntax).Condition, "Where")
                    Case SyntaxKind.SkipClause : Return ProcessPartitionClause(expression, DirectCast(queryClause, PartitionClauseSyntax).Count, "Skip")
                    Case SyntaxKind.TakeClause : Return ProcessPartitionClause(expression, DirectCast(queryClause, PartitionClauseSyntax).Count, "Take")
                    Case SyntaxKind.OrderByClause : Return ProcessOrderByClause(expression, identifier, DirectCast(queryClause, OrderByClauseSyntax))
                    Case SyntaxKind.SelectClause : Return ProcessSelectClause(expression, identifier, DirectCast(queryClause, SelectClauseSyntax))
                    Case SyntaxKind.GroupJoinClause : Return Nothing ' Too complicated for query method syntax.
                    Case SyntaxKind.SimpleJoinClause : Return Nothing ' Too complicated for query method syntax.
                    Case SyntaxKind.LetClause : Return Nothing ' Too complicated for query method syntax.
                    Case SyntaxKind.AggregateClause : Return Nothing ' Too complicated for query method syntax.
                    Case SyntaxKind.GroupByClause : Return Nothing ' Group by in query and in method are not 100% equivalent. Consdier support of some group by in the future.
                    Case Else : Return Nothing
                End Select
            End Function

            Private Function ProcessFromClause(context As ConversionContext, expression As ExpressionSyntax, fromClause As FromClauseSyntax) As ExpressionSyntax
                If Not (expression Is Nothing) Then
                    Return Nothing ' Support a single from for now. More than 1 from clause seems to be complicatedfor query methods.
                End If

                Dim variables = fromClause.Variables
                If variables.Count > 1 Then
                    Return Nothing ' Do not support more than 1 variable. It complicates query methods.
                End If

                Dim variable = variables.First()
                Dim identifier = variable.Identifier.Identifier

                context.UpdateIdentifier(identifier)
                expression = variable.Expression.WithoutTrailingTrivia()

                If Not (variable.AsClause Is Nothing) Then
                    Dim tryCastExpression = SyntaxFactory.TryCastExpression(SyntaxFactory.IdentifierName(identifier), variable.AsClause.Type)
                    expression = CreateInvocationExpression(expression, identifier, tryCastExpression, "Select")
                End If

                Return expression
            End Function

            Protected Overrides Function Validate(source As QueryExpressionSyntax, destination As ExpressionSyntax, cancellationToken As CancellationToken) As Boolean
                Dim speculationAnalyzer = New SpeculationAnalyzer(source, destination, _semanticModel, cancellationToken)
                If speculationAnalyzer.ReplacementChangesSemantics() Then
                    Return False
                End If

                ' TODO add more checks
                Return True
            End Function

            Protected Overrides Function FindNodeToRefactor(root As SyntaxNode, context As CodeRefactoringContext) As QueryExpressionSyntax
                Return root.FindNode(context.Span).FirstAncestorOrSelf(Of QueryExpressionSyntax)
            End Function

            Private Function ProcessSelectClause(expression As ExpressionSyntax, identifier As SyntaxToken, selectClause As SelectClauseSyntax) As ExpressionSyntax
                Dim variables = selectClause.Variables

                If variables.Count > 1 Then
                    Return Nothing ' Do not support more than 1 variable. It complicates query methods.
                End If

                Dim variable = variables.First()
                Dim bodyExpression = variable.Expression
                ' Avoid trivial Select(x => x)
                Dim identifierName = TryCast(bodyExpression, IdentifierNameSyntax)
                If Not identifierName Is Nothing Then
                    ' TODO consider a better condition to compare
                    If identifierName.Identifier.Text = identifier.Text Then
                        Return expression
                    End If
                End If

                Return CreateInvocationExpression(expression, identifier, bodyExpression, "Select")
            End Function

            Private Function CreateInvocationExpression(expression As ExpressionSyntax, identifier As SyntaxToken, bodyExpression As ExpressionSyntax, keyword As String) As ExpressionSyntax
                Dim parameter = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier(identifier))
                Dim lambdaHeader = SyntaxFactory.LambdaHeader(SyntaxKind.FunctionLambdaHeader, Nothing, Nothing, SyntaxFactory.Token(SyntaxKind.FunctionKeyword), SyntaxFactory.ParameterList().AddParameters(parameter), asClause:=Nothing)
                Dim lambda = SyntaxFactory.SingleLineLambdaExpression(SyntaxKind.SingleLineFunctionLambdaExpression, lambdaHeader, bodyExpression.WithoutTrailingTrivia())
                Dim argument = SyntaxFactory.SimpleArgument(lambda)
                Dim arguments = New SeparatedSyntaxList(Of ArgumentSyntax)().Add(argument)
                Dim argumentList = SyntaxFactory.ArgumentList(arguments)
                Return CreateInvocationExpression(expression, keyword, argumentList)
            End Function

            Private Function ProcessPartitionClause(expression As ExpressionSyntax, bodyExpression As ExpressionSyntax, keyword As String) As ExpressionSyntax
                Dim argument = SyntaxFactory.SimpleArgument(bodyExpression.WithoutTrailingTrivia())
                Dim arguments = New SeparatedSyntaxList(Of ArgumentSyntax)().Add(argument)
                Dim argumentList = SyntaxFactory.ArgumentList(arguments)

                Return CreateInvocationExpression(expression, keyword, argumentList)
            End Function

            Private Function CreateInvocationExpression(expression As ExpressionSyntax, keyword As String, Optional argumentList As ArgumentListSyntax = Nothing) As ExpressionSyntax
                Dim keywordIdentifier = SyntaxFactory.IdentifierName(keyword)
                Dim memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, SyntaxFactory.Token(SyntaxKind.DotToken), keywordIdentifier)
                Return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList)
            End Function

            Private Function ProcessOrderByClause(expression As ExpressionSyntax, identifier As SyntaxToken, orderByClause As OrderByClauseSyntax) As ExpressionSyntax
                Dim isFirst = True
                For Each ordering In orderByClause.Orderings
                    Dim keyword As String
                    Select Case (ordering.Kind())
                        Case SyntaxKind.AscendingOrdering : keyword = If(isFirst, "OrderBy", "ThenBy")
                        Case SyntaxKind.DescendingOrdering : keyword = If(isFirst, "OrderByDescending", "ThenByDescending")
                        Case Else : Return Nothing
                    End Select
                    expression = CreateInvocationExpression(expression, identifier, ordering.Expression, keyword)
                    isFirst = False
                Next

                Return expression
            End Function
        End Class

        ' VB QueryExpressionSyntax consists of a collection of QueryClause. Each of them can change the context for all descendants. 
        Private Class ConversionContext
            Public Identifier As SyntaxToken

            Public Sub UpdateIdentifier(identifier As SyntaxToken)
                Me.Identifier = identifier
            End Sub
        End Class
    End Class
End Namespace
