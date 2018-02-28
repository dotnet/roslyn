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

                Return expression.WithTrailingTrivia(source.GetTrailingTrivia())
            End Function

            Private Function ProcessQueryClause(expression As ExpressionSyntax, queryClause As QueryClauseSyntax) As ExpressionSyntax
                Select Case (queryClause.Kind())
                    Case SyntaxKind.FromClause
                        Return ProcessFromClause(expression, DirectCast(queryClause, FromClauseSyntax))
                    Case SyntaxKind.DistinctClause
                        Return CreateInvocationExpression(expression, NameOf(Enumerable.Distinct))
                    Case SyntaxKind.WhereClause
                        Return CreateInvocationExpression(expression, NameOf(Enumerable.Where), DirectCast(queryClause, WhereClauseSyntax).Condition)
                    Case SyntaxKind.SkipClause
                        Return CreateInvocationExpression(expression, NameOf(Enumerable.Skip), DirectCast(queryClause, PartitionClauseSyntax).Count)
                    Case SyntaxKind.TakeClause
                        Return CreateInvocationExpression(expression, NameOf(Enumerable.Take), DirectCast(queryClause, PartitionClauseSyntax).Count)
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
                If parentExpression Is Nothing Then
                    expression = variable.Expression

                    If variable.AsClause IsNot Nothing Then
                        Dim identifier = SyntaxFactory.IdentifierName(NameOf(Enumerable.Cast))
                        Dim generic = SyntaxFactory.GenericName(
                            identifier.Identifier,
                            SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(variable.AsClause.Type)))
                        expression = SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            expression,
                            SyntaxFactory.Token(SyntaxKind.DotToken), generic),
                            SyntaxFactory.ArgumentList())
                    End If

                    Return expression
                End If

                Dim anonymousFunctions = FindAnonymousFunctionsFromParentInvocationOperation(variable.Expression)
                Dim firstLambda = CreateLambdaExpression(anonymousFunctions.First())
                Dim secondArgumentAnonymousFunction = anonymousFunctions.Last()
                Dim returnedValue = DirectCast(secondArgumentAnonymousFunction.Body.Operations.First(), IReturnOperation).ReturnedValue
                Dim secondLambda As LambdaExpressionSyntax
                If returnedValue.Kind = OperationKind.AnonymousObjectCreation Then
                    Dim objectCreation = DirectCast(returnedValue, IAnonymousObjectCreationOperation)
                    Dim fieldInitializers = objectCreation.Initializers.
                        Select(Function(initializer) SyntaxFactory.InferredFieldInitializer(
                        SyntaxFactory.IdentifierName(BeautifyName(DirectCast(initializer, IParameterReferenceOperation).Parameter.Name))))
                    Dim anonymousObjectCreation = SyntaxFactory.AnonymousObjectCreationExpression(
                        SyntaxFactory.ObjectMemberInitializer(SyntaxFactory.SeparatedList(Of FieldInitializerSyntax)(fieldInitializers)))
                    secondLambda = CreateLambdaExpression(secondArgumentAnonymousFunction, anonymousObjectCreation)
                Else
                    secondLambda = CreateLambdaExpression(secondArgumentAnonymousFunction)
                End If

                Return CreateInvocationExpression(expression, NameOf(Enumerable.SelectMany), {firstLambda, secondLambda})
            End Function

            Private Function CreateLambdaExpression(anonymousFunction As IAnonymousFunctionOperation, Optional body As VisualBasicSyntaxNode = Nothing) As LambdaExpressionSyntax
                If body Is Nothing Then
                    body = MakeIdentifierNameReplacements(anonymousFunction.Body.Operations.First().Syntax)
                End If
                Dim parameters = anonymousFunction.Symbol.Parameters.
                    Select(Function(p) SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier(BeautifyName(p.Name))))
                Dim parameterList = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters))
                Dim header = SyntaxFactory.LambdaHeader(
                    SyntaxKind.FunctionLambdaHeader,
                    attributeLists:=Nothing,
                    modifiers:=Nothing,
                    SyntaxFactory.Token(SyntaxKind.FunctionKeyword),
                    parameterList,
                    asClause:=Nothing)
                Return SyntaxFactory.SingleLineFunctionLambdaExpression(header, body)
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

                Return CreateInvocationExpression(expression, NameOf(Enumerable.Select), selectClause.Variables.First().Expression)
            End Function

            Private Function CreateInvocationExpression(parentExpression As ExpressionSyntax, keyword As String, expression As ExpressionSyntax) As ExpressionSyntax
                Dim anonymousFunctions = FindAnonymousFunctionsFromParentInvocationOperation(expression)
                Dim expressions = anonymousFunctions.Select(Function(a) CreateLambdaExpression(a))
                Return CreateInvocationExpression(parentExpression, keyword, expressions)
            End Function

            Private Shared Function CreateInvocationExpression(parentExpression As ExpressionSyntax, keyword As String, Optional expressions As IEnumerable(Of ExpressionSyntax) = Nothing) As InvocationExpressionSyntax
                Dim argumentList = If(expressions Is Nothing,
                    Nothing,
                    SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(Of ArgumentSyntax)(
                    expressions.Select(Function(expression) SyntaxFactory.SimpleArgument(expression)))))
                Dim memberAccessExpression = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, parentExpression,
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    SyntaxFactory.IdentifierName(keyword))
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
                    expression = CreateInvocationExpression(expression, keyword, ordering.Expression)
                    isFirst = False
                Next

                Return expression
            End Function

            Private Shared Function BeautifyName(name As String) As String
                Return name.Replace("$", "")
            End Function

            Private Function MakeIdentifierNameReplacements(node As SyntaxNode) As VisualBasicSyntaxNode
                Return DirectCast(New LambdaRewriter(Function(n) GetIdentifierName(n)).Visit(node), VisualBasicSyntaxNode)
            End Function

            Private Function GetIdentifierName(node As IdentifierNameSyntax) As String
                Dim names = GetIdentifierNames(_semanticModel, node, _cancellationToken)
                If (names.IsDefault) Then
                    Return String.Empty
                Else
                    Return BeautifyName(String.Join(".", names))
                End If
            End Function

            Private Class LambdaRewriter
                Inherits VisualBasicSyntaxRewriter
                Private _semanticModel As SemanticModel
                Private _cancellationToken As CancellationToken
                Private _getNameMethod As Func(Of IdentifierNameSyntax, String)

                Public Sub New(getNameMethod As Func(Of IdentifierNameSyntax, String))
                    _getNameMethod = getNameMethod
                End Sub

                Public Overrides Function VisitIdentifierName(node As IdentifierNameSyntax) As SyntaxNode
                    Dim name = _getNameMethod(node)
                    Return If(String.IsNullOrEmpty(name), MyBase.VisitIdentifierName(node), SyntaxFactory.IdentifierName(name))
                End Function
            End Class
        End Class
    End Class
End Namespace
