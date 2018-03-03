' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertLinq
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Threading
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.Operations
Imports System.Collections.Immutable

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
                Dim expression As ExpressionSyntax = Nothing
                Dim symbolInfo As SymbolInfo = _semanticModel.GetSymbolInfo(source, _cancellationToken)
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
                        Return CreateInvocationExpression(expression, NameOf(Enumerable.Where), CreateLambdaExpressionWithReplacedIdentifiers(DirectCast(queryClause, WhereClauseSyntax).Condition))
                    Case SyntaxKind.SkipClause
                        Return CreateInvocationExpression(expression, NameOf(Enumerable.Skip), DirectCast(queryClause, PartitionClauseSyntax).Count)
                    Case SyntaxKind.TakeClause
                        Return CreateInvocationExpression(expression, NameOf(Enumerable.Take), DirectCast(queryClause, PartitionClauseSyntax).Count)
                    Case SyntaxKind.OrderByClause
                        Return ProcessOrderByClause(expression, DirectCast(queryClause, OrderByClauseSyntax))
                    Case SyntaxKind.SelectClause
                        Return ProcessSelectClause(expression, DirectCast(queryClause, SelectClauseSyntax))
                    Case SyntaxKind.GroupJoinClause
                        Return Nothing ' TODO: https://github.com/dotnet/roslyn/issues/25112
                    Case SyntaxKind.SimpleJoinClause
                        Return Nothing ' TODO: https://github.com/dotnet/roslyn/issues/25112
                    Case SyntaxKind.LetClause
                        Return Nothing ' TODO: https://github.com/dotnet/roslyn/issues/25112
                    Case SyntaxKind.AggregateClause
                        Return Nothing ' TODO: https://github.com/dotnet/roslyn/issues/25112
                    Case SyntaxKind.GroupByClause
                        Return Nothing ' TODO: https://github.com/dotnet/roslyn/issues/25112
                    Case Else
                        Return Nothing
                End Select
            End Function

            Private Function CreateLambdaExpressionWithReplacedIdentifiers(body As VisualBasicSyntaxNode) As LambdaExpressionSyntax
                Dim anonymousFunction As IAnonymousFunctionOperation = FindParentAnonymousFunction(body)
                If anonymousFunction Is Nothing Then
                    Return Nothing
                End If

                Return CreateLambdaExpression(ReplaceIdentifierNames(body), anonymousFunction.Symbol)
            End Function

            Private Function ProcessFromClause(parentExpression As ExpressionSyntax, fromClause As FromClauseSyntax) As ExpressionSyntax
                Dim expression As ExpressionSyntax = parentExpression
                For Each variable As CollectionRangeVariableSyntax In fromClause.Variables
                    If parentExpression Is Nothing Then
                        expression = variable.Expression
                    End If

                    If variable.AsClause IsNot Nothing Then
                        Dim identifier As IdentifierNameSyntax = SyntaxFactory.IdentifierName(NameOf(Enumerable.Cast))
                        Dim generic As GenericNameSyntax = SyntaxFactory.GenericName(
                            identifier.Identifier,
                            SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(variable.AsClause.Type)))
                        expression = CreateInvocationExpression(expression, generic)
                    End If

                    If parentExpression Is Nothing Then
                        Return expression
                    End If

                    Dim bodyExpression As ExpressionSyntax = variable.Expression
                    Dim argumentAnonymousFunctions As ImmutableArray(Of IAnonymousFunctionOperation) = FindAnonymousFunctionsFromParentInvocationOperation(bodyExpression)
                    Debug.Assert(argumentAnonymousFunctions.Length = 2)
                    Dim secondArgumentAnonymousFunction As IAnonymousFunctionOperation = argumentAnonymousFunctions.Last()

                    Debug.Assert(secondArgumentAnonymousFunction.Body.Operations.Length = 1)
                    Dim returnOperation As IReturnOperation = DirectCast(secondArgumentAnonymousFunction.Body.Operations.First(), IReturnOperation)
                    Dim returnedValue As IOperation = returnOperation.ReturnedValue

                    Dim tupleExpression As VisualBasicSyntaxNode
                    If returnedValue.Kind = OperationKind.AnonymousObjectCreation Then
                        Dim objectCreation As IAnonymousObjectCreationOperation = DirectCast(returnedValue, IAnonymousObjectCreationOperation)
                        Dim fieldInitializers As IEnumerable(Of InferredFieldInitializerSyntax) =
                            objectCreation.
                            Initializers.
                            Select(Function(initializer) SyntaxFactory.InferredFieldInitializer(
                            SyntaxFactory.IdentifierName(BeautifyName(DirectCast(initializer, IParameterReferenceOperation).Parameter.Name))))
                        tupleExpression = SyntaxFactory.AnonymousObjectCreationExpression(
                            SyntaxFactory.ObjectMemberInitializer(SyntaxFactory.SeparatedList(Of FieldInitializerSyntax)(fieldInitializers)))
                    Else
                        tupleExpression = ReplaceIdentifierNames(returnOperation.Syntax)
                    End If

                    Dim secondLambda As LambdaExpressionSyntax = CreateLambdaExpression(tupleExpression, secondArgumentAnonymousFunction.Symbol)
                    expression = CreateInvocationExpression(expression, NameOf(Enumerable.SelectMany), {CreateLambdaExpressionWithReplacedIdentifiers(bodyExpression), secondLambda})
                Next

                Return expression
            End Function

            Private Function CreateLambdaExpression(body As VisualBasicSyntaxNode, methodSymbol As IMethodSymbol) As LambdaExpressionSyntax
                Dim parameters As IEnumerable(Of ParameterSyntax) = methodSymbol.Parameters.Select(Function(p) SyntaxFactory.Parameter(SyntaxFactory.ModifiedIdentifier(BeautifyName(p.Name))))
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
                For Each variable As ExpressionRangeVariableSyntax In selectClause.Variables
                    expression = CreateInvocationExpression(expression, NameOf(Enumerable.Select), CreateLambdaExpressionWithReplacedIdentifiers(variable.Expression))
                Next
                Return expression
            End Function

            Private Shared Function CreateInvocationExpression(parentExpression As ExpressionSyntax, keyword As String, argumentExpression As ExpressionSyntax) As InvocationExpressionSyntax
                Return CreateInvocationExpression(parentExpression, keyword, {argumentExpression})
            End Function

            Private Shared Function CreateInvocationExpression(parentExpression As ExpressionSyntax, simpleName As SimpleNameSyntax, Optional argumentExpressions As IEnumerable(Of ExpressionSyntax) = Nothing) As InvocationExpressionSyntax
                Dim argumentList As ArgumentListSyntax = If(argumentExpressions Is Nothing,
                    SyntaxFactory.ArgumentList(),
                    SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(Of ArgumentSyntax)(
                    argumentExpressions.Select(Function(expression) SyntaxFactory.SimpleArgument(expression)))))
                Dim memberAccessExpression = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression, parentExpression,
                    SyntaxFactory.Token(SyntaxKind.DotToken),
                    simpleName)
                Return SyntaxFactory.InvocationExpression(memberAccessExpression, argumentList)
            End Function

            Private Shared Function CreateInvocationExpression(parentExpression As ExpressionSyntax, keyword As String, Optional argumentExpressions As IEnumerable(Of ExpressionSyntax) = Nothing) As InvocationExpressionSyntax
                Return CreateInvocationExpression(parentExpression, SyntaxFactory.IdentifierName(keyword), argumentExpressions)
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
                    expression = CreateInvocationExpression(expression, keyword, CreateLambdaExpressionWithReplacedIdentifiers(ordering.Expression))
                    isFirst = False
                Next

                Return expression
            End Function

            Private Shared Function BeautifyName(name As String) As String
                Return name.Replace("$", "")
            End Function

            Private Function ReplaceIdentifierNames(node As SyntaxNode) As VisualBasicSyntaxNode
                Return DirectCast(New LambdaRewriter(Function(n) GetIdentifierName(n)).Visit(node), VisualBasicSyntaxNode)
            End Function

            Private Function GetIdentifierName(node As IdentifierNameSyntax) As String
                Dim names As ImmutableArray(Of String) = GetIdentifierNames(node)
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
                    If Not String.IsNullOrEmpty(name) Then
                        node = node.WithIdentifier(SyntaxFactory.Identifier(name))
                    End If

                    Return MyBase.VisitIdentifierName(node)
                End Function
            End Class
        End Class
    End Class
End Namespace
