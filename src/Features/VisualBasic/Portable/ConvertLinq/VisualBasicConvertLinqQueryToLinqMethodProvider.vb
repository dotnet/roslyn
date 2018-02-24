' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertLinq
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.Operations

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertLinq
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertLinqQueryToLinqMethodProvider)), [Shared]>
    Partial Friend NotInheritable Class VisualBasicConvertLinqQueryToLinqMethodProvider
        Inherits AbstractConvertLinqQueryToLinqMethodProvider

        Protected Overrides Function CreateAnalyzer(semanticModel As SemanticModel, cancellationToken As CancellationToken) As IAnalyzer
            Return New VisualBasicAnalyzer(semanticModel, cancellationToken)
        End Function

        Private NotInheritable Class VisualBasicAnalyzer
            Inherits Analyzer(Of QueryExpressionSyntax, ExpressionSyntax)

            Public Sub New(semanticModel As SemanticModel, cancellationToken As CancellationToken)
                MyBase.New(semanticModel, cancellationToken)
            End Sub

            Protected Overrides ReadOnly Property Title() As String
                Get
                    Return VBFeaturesResources.Convert_linq_query_to_linq_method
                End Get
            End Property

            Protected Overrides Function TryConvert(source As QueryExpressionSyntax) As ExpressionSyntax
                Dim expression As ExpressionSyntax
                expression = Nothing

                Dim symbolInfo = _semanticModel.GetSymbolInfo(source, _cancellationToken)
                For Each clause In source.Clauses
                    expression = ProcessQueryClause(expression, clause)
                    If expression Is Nothing Then
                        Return Nothing
                    End If
                Next

                Return expression
            End Function

            Private Function ProcessQueryClause(expression As ExpressionSyntax, queryClause As QueryClauseSyntax) As ExpressionSyntax
                Select Case (queryClause.Kind())
                    Case SyntaxKind.FromClause
                        Return ProcessFromClause(expression, DirectCast(queryClause, FromClauseSyntax))
                    Case SyntaxKind.DistinctClause
                        Return CreateInvocationExpression(expression, NameOf(Enumerable.Distinct))
                    Case SyntaxKind.WhereClause
                        Return CreateInvocationExpression(expression, NameOf(Enumerable.Where), CreateArgumentList(DirectCast(queryClause, WhereClauseSyntax).Condition))
                    Case SyntaxKind.SkipClause
                        Return ProcessPartitionClause(expression, DirectCast(queryClause, PartitionClauseSyntax).Count, NameOf(Enumerable.Skip))
                    Case SyntaxKind.TakeClause
                        Return ProcessPartitionClause(expression, DirectCast(queryClause, PartitionClauseSyntax).Count, NameOf(Enumerable.Take))
                    Case SyntaxKind.OrderByClause
                        Return ProcessOrderByClause(expression, DirectCast(queryClause, OrderByClauseSyntax))
                    Case SyntaxKind.SelectClause
                        Return ProcessSelectClause(expression, DirectCast(queryClause, SelectClauseSyntax))
                    Case SyntaxKind.GroupJoinClause
                        Return Nothing ' Too complicated for query method syntax.
                    Case SyntaxKind.SimpleJoinClause
                        Return Nothing ' Too complicated for query method syntax.
                    Case SyntaxKind.LetClause
                        Return Nothing ' Too complicated for query method syntax.
                    Case SyntaxKind.AggregateClause
                        Return Nothing ' Too complicated for query method syntax.
                    Case SyntaxKind.GroupByClause
                        Return Nothing ' Group by in query and in method are not 100% equivalent. Consdier support of some group by in the future.
                    Case Else
                        Return Nothing
                End Select
            End Function

            Private Function ProcessFromClause(parentExpression As ExpressionSyntax, fromClause As FromClauseSyntax) As ExpressionSyntax
                Dim expression = parentExpression
                Dim variables = fromClause.Variables
                If variables.Count > 1 Then
                    Return Nothing ' Do not support more than 1 variable. It complicates query methods.
                End If

                Dim variable = variables.First()
                Dim identifier = variable.Identifier.Identifier
                If parentExpression Is Nothing Then
                    expression = variable.Expression

                    If variable.AsClause IsNot Nothing Then
                        Dim tryCastExpression = SyntaxFactory.TryCastExpression(SyntaxFactory.IdentifierName(identifier), variable.AsClause.Type)
                        expression = CreateInvocationExpression(expression, identifier, tryCastExpression, NameOf(Enumerable.Select))
                    End If

                    Return expression
                End If

                Dim expressionOperation = _semanticModel.GetOperation(variable.Expression, _cancellationToken)
                Dim invocationOperation = FindParentInvocationOperation(expressionOperation)

                Dim anonymousFunction = DirectCast(DirectCast(invocationOperation.Arguments.Last().Value, IDelegateCreationOperation).Target, IAnonymousFunctionOperation)
                Dim argumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList({CreateArgument(invocationOperation.Arguments.First()), SyntaxFactory.SimpleArgument(CreateNewWithExpression(anonymousFunction))}))

                Return CreateInvocationExpression(expression, NameOf(Enumerable.SelectMany), argumentList)
            End Function

            Private Function CreateArgumentList(expresison As ExpressionSyntax) As ArgumentListSyntax
                Dim expressionOperation = _semanticModel.GetOperation(expresison, _cancellationToken)
                Dim invocationOperation = FindParentInvocationOperation(expressionOperation)
                Return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(invocationOperation.Arguments.Select(Function(argument) CreateArgument(argument))))
            End Function

            Private Function CreateNewWithExpression(anonymousFunction As IAnonymousFunctionOperation) As LambdaExpressionSyntax
                Dim header = CreateLambdaHeader(anonymousFunction)
                Dim objectMemberInitializer = SyntaxFactory.ObjectMemberInitializer(SyntaxFactory.SeparatedList(Of FieldInitializerSyntax)(anonymousFunction.Symbol.Parameters.Select(Function(p) SyntaxFactory.InferredFieldInitializer(SyntaxFactory.IdentifierName(p.Name.Replace("$", ""))))))
                Dim anonymousObjectCreation = SyntaxFactory.AnonymousObjectCreationExpression(objectMemberInitializer)
                Return SyntaxFactory.SingleLineFunctionLambdaExpression(header, anonymousObjectCreation)
            End Function

            Private Function CreateArgument(argumentOperation As IArgumentOperation) As ArgumentSyntax
                ' TODO do we need to check for cast?
                Dim anonymousFunction = DirectCast(DirectCast(argumentOperation.Value, IDelegateCreationOperation).Target, IAnonymousFunctionOperation)
                Dim syntax = anonymousFunction.Body.Operations.First().Syntax
                Dim lambdaBody = DirectCast(New LambdaRewriter(_semanticModel, _cancellationToken).Visit(syntax), VisualBasicSyntaxNode)
                Dim argument = SyntaxFactory.SingleLineFunctionLambdaExpression(CreateLambdaHeader(anonymousFunction), lambdaBody)
                Return SyntaxFactory.SimpleArgument(argument)
            End Function

            Private Function CreateLambdaHeader(anonymousFunction As IAnonymousFunctionOperation) As LambdaHeaderSyntax
                Dim parameters = SyntaxFactory.SeparatedList(anonymousFunction.Symbol.Parameters.Select(Function(p) SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier(p.Name.Replace("$", "")))))
                Dim parameterList = SyntaxFactory.ParameterList(parameters)
                Return SyntaxFactory.LambdaHeader(SyntaxKind.FunctionLambdaHeader, attributeLists:=Nothing, modifiers:=Nothing, SyntaxFactory.Token(SyntaxKind.FunctionKeyword), parameterList, asClause:=Nothing)
            End Function

            Private Function ProcessSelectClause(expression As ExpressionSyntax, selectClause As SelectClauseSyntax) As ExpressionSyntax
                Dim selectExpression = selectClause.Variables.First().Expression
                ' Avoid trivial Select(Funciton(x) x)
                Dim variables = selectClause.Variables
                Dim variable = variables.First()
                Dim bodyExpression = variable.Expression

                If bodyExpression.Kind() = SyntaxKind.IdentifierName Then
                    Dim identifierName = DirectCast(bodyExpression, IdentifierNameSyntax)
                    Dim identifierNames = GetIdentifierNames(_semanticModel, identifierName, _cancellationToken)
                    If identifierNames.IsDefault OrElse identifierNames.Length = 1 Then
                        Return expression
                    End If
                End If

                Return CreateInvocationExpression(expression, NameOf(Enumerable.Select), CreateArgumentList(selectClause.Variables.First().Expression))
            End Function

            Private Function CreateInvocationExpression(expression As ExpressionSyntax, identifier As SyntaxToken, bodyExpression As ExpressionSyntax, keyword As String) As ExpressionSyntax
                Dim parameter = SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier(identifier))
                Dim lambdaHeader = SyntaxFactory.LambdaHeader(SyntaxKind.FunctionLambdaHeader, attributeLists:=Nothing, modifiers:=Nothing, SyntaxFactory.Token(SyntaxKind.FunctionKeyword), SyntaxFactory.ParameterList().AddParameters(parameter), asClause:=Nothing)
                Dim lambda = SyntaxFactory.SingleLineLambdaExpression(SyntaxKind.SingleLineFunctionLambdaExpression, lambdaHeader, bodyExpression)
                Dim argument = SyntaxFactory.SimpleArgument(lambda)
                Dim arguments = SyntaxFactory.SingletonSeparatedList(Of ArgumentSyntax)(argument)
                Dim argumentList = SyntaxFactory.ArgumentList(arguments)
                Return CreateInvocationExpression(expression, keyword, argumentList)
            End Function

            Private Function ProcessPartitionClause(expression As ExpressionSyntax, bodyExpression As ExpressionSyntax, keyword As String) As ExpressionSyntax
                Dim argument = SyntaxFactory.SimpleArgument(bodyExpression)
                Dim arguments = SyntaxFactory.SingletonSeparatedList(Of ArgumentSyntax)(argument)
                Dim argumentList = SyntaxFactory.ArgumentList(arguments)

                Return CreateInvocationExpression(expression, keyword, argumentList)
            End Function

            Private Function CreateInvocationExpression(expression As ExpressionSyntax, keyword As String, Optional argumentList As ArgumentListSyntax = Nothing) As ExpressionSyntax
                Dim keywordIdentifier = SyntaxFactory.IdentifierName(keyword)
                Dim memberAccessExpression = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, expression, SyntaxFactory.Token(SyntaxKind.DotToken), keywordIdentifier)
                Return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList)
            End Function

            Private Function ProcessOrderByClause(expression As ExpressionSyntax, orderByClause As OrderByClauseSyntax) As ExpressionSyntax
                Dim isFirst = True
                For Each ordering In orderByClause.Orderings
                    Dim keyword As String
                    Select Case (ordering.Kind())
                        Case SyntaxKind.AscendingOrdering
                            keyword = If(isFirst, NameOf(Enumerable.OrderBy), NameOf(Enumerable.ThenBy))
                        Case SyntaxKind.DescendingOrdering
                            keyword = If(isFirst, NameOf(Enumerable.OrderByDescending), NameOf(Enumerable.ThenByDescending))
                        Case Else
                            Return Nothing
                    End Select
                    expression = CreateInvocationExpression(expression, keyword, CreateArgumentList(ordering.Expression))
                    isFirst = False
                Next

                Return expression
            End Function

            Private Class LambdaRewriter
                Inherits VisualBasicSyntaxRewriter
                Private _semanticModel As SemanticModel
                Private _cancellationToken As CancellationToken

                Public Sub New(semanticModel As SemanticModel, cancellationToken As CancellationToken)
                    _semanticModel = semanticModel
                    _cancellationToken = cancellationToken
                End Sub

                Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                    Dim names = GetIdentifierNames(_semanticModel, node, _cancellationToken)
                    If names.IsDefault() Then
                        Return MyBase.VisitIdentifierName(node)
                    Else
                        Return SyntaxFactory.IdentifierName(names.Join(".").Replace("$", ""))
                    End If
                End Function
            End Class
        End Class
    End Class
End Namespace
