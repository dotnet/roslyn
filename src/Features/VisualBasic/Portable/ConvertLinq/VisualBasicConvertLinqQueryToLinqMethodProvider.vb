' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertLinq
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.CSharp.ConvertLinq
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertLinqQueryToLinqMethodProvider)), [Shared]>
    Friend NotInheritable Class VisualBasicConvertLinqQueryToLinqMethodProvider
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

                Dim fromClause = source.FromClause

                Dim identifier = fromClause.Identifier
                Dim context = New ConversionContext(identifier)
                Dim expression = fromClause.Expression
                If fromClause.Type <> Nothing Then
                    Dim lambda = SyntaxFactory.BinaryExpression(SyntaxKind.IsExpression, SyntaxFactory.IdentifierName(identifier), SyntaxFactory.Token(SyntaxKind.IsKeyword), fromClause.Type)
                    expression = ProcesQueryClause(context, expression, lambda, "Select")
                End If

                Return ProcessQueryBody(context, expression.WithoutTrailingTrivia(), source.Body)
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
                Throw New NotImplementedException()
            End Function

            Private Function ProcessQueryBody(context As ConversionContext, parentExpression As ExpressionSyntax, queryBody As QueryBodySyntax) As ExpressionSyntax

                Dim expression = parentExpression
                For Each queryClause In queryBody.Clauses
                    expression = ProcessQueryClause(context, expression, queryClause)
                    If (expression Is Nothing) Then
                        Return Nothing
                    End If
                Next

                expression = ProcessSelectOrGroup(context, expression, queryBody.SelectOrGroup)

                If queryBody.Continuation <> Nothing Then

                    Dim newContext = New ConversionContext(queryBody.Continuation.Identifier)
                    Return ProcessQueryBody(newContext, expression, queryBody.Continuation.Body)
                End If

                Return expression
            End Function

            Private Function ProcessSelectOrGroup(context As ConversionContext, parentExpression As ExpressionSyntax, selectOrGroupClause As SelectOrGroupClauseSyntax) As ExpressionSyntax
                Select Case (selectOrGroupClause.Kind())
                    Case SyntaxKind.SelectClause : Return ProcesSelectClause(context, parentExpression, DirectCast(selectOrGroupClause, SelectClauseSyntax))
                    Case SyntaxKind.GroupByClause : Return ProcessGroupClause(context, parentExpression, DirectCast(selectOrGroupClause, GroupByClauseSyntax))
                    Case SyntaxKind.LetClause : Return Nothing ' Skip this because the linq method For the Let will be more complicated than the query.
                    Case Else : Return Nothing
                End Select
            End Function

            Private Function ProcessGroupClause(context As ConversionContext, parentExpression As ExpressionSyntax, groupClause As GroupByClauseSyntax) As ExpressionSyntax

                Dim parameter = SyntaxFactory.Parameter(context.Identifier)
                Dim groupExpressionLambda = SyntaxFactory.SimpleLambdaExpression(parameter, groupClause.GroupExpression.WithoutTrailingTrivia())
                Dim byExpressionLambda = SyntaxFactory.SimpleLambdaExpression(parameter, groupClause.ByExpression.WithoutTrailingTrivia())
                Dim groupExpressionArgument = SyntaxFactory.SimpleArgument(groupExpressionLambda)
                Dim byExpressionArgument = SyntaxFactory.SimpleArgument(byExpressionLambda)
                Dim arguments = New SeparatedSyntaxList(Of ArgumentSyntax)().Add(byExpressionArgument).Add(groupExpressionArgument)
                Dim argumentList = SyntaxFactory.ArgumentList(arguments)

                Dim groupKeyword = SyntaxFactory.IdentifierName("GroupBy")
                Dim memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentExpression, SyntaxFactory.Token(SyntaxKind.DotToken), groupKeyword)
                Return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList)
            End Function

            Private Function ProcesSelectClause(context As ConversionContext, parentExpression As ExpressionSyntax, selectClause As SelectClauseSyntax) As ExpressionSyntax
                Dim expression = selectClause.Expression
                ' Avoid trivial Select(x => x)
                Dim identifierName = TryCast(expression, IdentifierNameSyntax)
                If Not identifierName Is Nothing Then
                    ' TODO consider a better condition to compare
                    If identifierName.Identifier.Text = context.Identifier.Text Then
                        Return parentExpression
                    End If
                End If

                Return ProcesQueryClause(context, parentExpression, selectClause.Expression, "Select")
            End Function

            Private Function ProcesQueryClause(context As ConversionContext, parentExpression As ExpressionSyntax, expression As ExpressionSyntax, keyword As String) As InvocationExpressionSyntax
                Dim parameter = SyntaxFactory.Parameter(context.Identifier)
                Dim lambda = SyntaxFactory.SingleLineLambdaExpression(parameter, expression.WithoutTrailingTrivia())
                Dim argument = SyntaxFactory.SimpleArgument(lambda)
                Dim arguments = New SeparatedSyntaxList(Of ArgumentSyntax)().Add(argument)
                Dim argumentList = SyntaxFactory.ArgumentList(arguments)

                Dim selectKeyword = SyntaxFactory.IdentifierName(keyword)
                Dim memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, parentExpression, SyntaxFactory.Token(SyntaxKind.DotToken), selectKeyword)
                Return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList)
            End Function

            Private Function ProcessQueryClause(context As ConversionContext, parentExpression As ExpressionSyntax, queryClause As QueryClauseSyntax) As ExpressionSyntax

                Select Case (queryClause.Kind())
                    Case SyntaxKind.WhereClause : Return ProcesQueryClause(context, parentExpression, DirectCast(queryClause, WhereClauseSyntax).Condition, "Where")
                    Case SyntaxKind.OrderByClause : Return ProcessOrderByClause(context, parentExpression, DirectCast(queryClause, OrderByClauseSyntax))
                    Case SyntaxKind.SimpleJoinClause                        ' Not supported because linq queries seem to provide essentially simpler syntax for the same than query methods.
                    Case SyntaxKind.GroupJoinClause                         ' Not supported because linq queries seem to provide essentially simpler syntax for the same than query methods.
                    Case SyntaxKind.FromClause                        ' More than one fromClause Is Not supported. The linq method seems to be more complicated for this.
                    Case Else : Return Nothing
                End Select
            End Function

            Private Function ProcessOrderByClause(context As ConversionContext, parentExpression As ExpressionSyntax, orderByClause As OrderByClauseSyntax) As ExpressionSyntax
                Dim isFirst = True
                For Each ordering In orderByClause.Orderings
                    parentExpression = ProcesQueryClause(context, parentExpression, ordering.Expression, GetOrderingKeyword(ordering, isFirst))
                    isFirst = False
                Next

                Return parentExpression
            End Function

            Private Function GetOrderingKeyword(ordering As OrderingSyntax, isFirst As Boolean) As String
                Select Case (ordering.Kind())
                    Case SyntaxKind.AscendingOrdering : Return If(isFirst, "OrderBy", "ThenBy")
                    Case SyntaxKind.DescendingOrdering : Return If(isFirst, "OrderByDescending", "ThenByDescending")
                    Case Else : Return Nothing
                End Select
            End Function
        End Class

        Protected Function FindNodeToRefactor(root As SyntaxNode, context As CodeRefactoringContext) As QueryExpressionSyntax
            Return root.FindNode(context.Span).FirstAncestorOrSelf(Of QueryExpressionSyntax)
        End Function

        Private Structure ConversionContext
            Public ReadOnly Identifier As ModifiedIdentifierSyntax

            Public Sub New(identifier As ModifiedIdentifierSyntax)
                identifier = identifier
            End Sub
        End Structure

    End Class
End Namespace
