
Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.MakeMethodSynchronous
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.MakeMethodSynchronous
    <ExportCodeFixProvider(LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicMakeMethodSynchronousCodeFixProvider
        Inherits AbstractMakeMethodSynchronousCodeFixProvider

        Private Const BC42356 As String = NameOf(BC42356) ' This async method lacks 'Await' operators and so will run synchronously.

        Private Shared ReadOnly diagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(BC42356)

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return diagnosticIds
            End Get
        End Property

        Protected Overrides Function IsMethodOrAnonymousFunction(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.FunctionBlock) OrElse
                node.IsKind(SyntaxKind.SubBlock) OrElse
                node.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) OrElse
                node.IsKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
                node.IsKind(SyntaxKind.SingleLineFunctionLambdaExpression) OrElse
                node.IsKind(SyntaxKind.SingleLineSubLambdaExpression)
        End Function

        Protected Overrides Function RemoveAsyncTokenAndFixReturnType(methodSymbolOpt As IMethodSymbol, node As SyntaxNode, taskType As ITypeSymbol, taskOfTType As ITypeSymbol) As SyntaxNode
            If node.IsKind(SyntaxKind.SingleLineSubLambdaExpression) OrElse
                node.IsKind(SyntaxKind.SingleLineFunctionLambdaExpression) Then

                Return FixSingleLineLambdaExpression(DirectCast(node, SingleLineLambdaExpressionSyntax))
            ElseIf node.IsKind(SyntaxKind.MultiLineSubLambdaExpression) OrElse
                    node.IsKind(SyntaxKind.MultiLineFunctionLambdaExpression) Then

                Return FixMultiLineLambdaExpression(DirectCast(node, MultiLineLambdaExpressionSyntax))
            ElseIf node.IsKind(SyntaxKind.SubBlock) Then
                Return FixSubBlock(DirectCast(node, MethodBlockSyntax))
            Else
                Return FixFunctionBlock(methodSymbolOpt, DirectCast(node, MethodBlockSyntax), taskType, taskOfTType)
            End If
        End Function

        Private Function FixFunctionBlock(methodSymbol As IMethodSymbol, node As MethodBlockSyntax, taskType As ITypeSymbol, taskOfTType As ITypeSymbol) As SyntaxNode
            Dim functionStatement = node.SubOrFunctionStatement

            ' if this returns Task<T>, then we want to convert this to a T returning function.
            ' if this returns Task, then we want to convert it to a Sub method.
            If methodSymbol.ReturnType.OriginalDefinition.Equals(taskOfTType) Then
                Dim newAsClause = functionStatement.AsClause.WithType(methodSymbol.ReturnType.GetTypeArguments()(0).GenerateTypeSyntax())
                Dim newFunctionStatement = functionStatement.WithAsClause(newAsClause)
                newFunctionStatement = RemoveAsyncKeyword(newFunctionStatement)
                Return node.WithSubOrFunctionStatement(newFunctionStatement)
            ElseIf methodSymbol.ReturnType.OriginalDefinition Is taskType Then
                ' Convert this to a 'Sub' method.
                Dim subStatement = SyntaxFactory.SubStatement(
                    functionStatement.AttributeLists,
                    functionStatement.Modifiers,
                    SyntaxFactory.Token(SyntaxKind.SubKeyword).WithTriviaFrom(functionStatement.SubOrFunctionKeyword),
                    functionStatement.Identifier,
                    functionStatement.TypeParameterList,
                    functionStatement.ParameterList,
                    functionStatement.AsClause,
                    functionStatement.HandlesClause,
                    functionStatement.ImplementsClause)

                subStatement = subStatement.RemoveNode(subStatement.AsClause, SyntaxRemoveOptions.KeepTrailingTrivia)
                subStatement = RemoveAsyncKeyword(subStatement)

                Dim endSubStatement = SyntaxFactory.EndSubStatement(
                    node.EndSubOrFunctionStatement.EndKeyword,
                    SyntaxFactory.Token(SyntaxKind.SubKeyword).WithTriviaFrom(node.EndSubOrFunctionStatement.BlockKeyword))

                Return SyntaxFactory.SubBlock(subStatement, node.Statements, endSubStatement)
            Else
                Dim newFunctionStatement = RemoveAsyncKeyword(functionStatement)
                Return node.WithSubOrFunctionStatement(newFunctionStatement)
            End If
        End Function

        Private Function FixSubBlock(node As MethodBlockSyntax) As SyntaxNode
            Dim newSubStatement = RemoveAsyncKeyword(node.SubOrFunctionStatement)
            Return node.WithSubOrFunctionStatement(newSubStatement)
        End Function

        Private Shared Function RemoveAsyncKeyword(subOrFunctionStatement As MethodStatementSyntax) As MethodStatementSyntax
            Dim modifiers = subOrFunctionStatement.Modifiers
            Dim asyncTokenIndex = modifiers.IndexOf(SyntaxKind.AsyncKeyword)

            Dim newSubKeyword = subOrFunctionStatement.SubOrFunctionKeyword
            Dim newModifiers As SyntaxTokenList
            If asyncTokenIndex = 0 Then
                ' Have to move the trivia on teh async token appropriately.
                Dim asyncLeadingTrivia = modifiers(0).LeadingTrivia

                If modifiers.Count > 1 Then
                    ' Move the trivia to the next modifier;
                    newModifiers = modifiers.Replace(
                        modifiers(1),
                        modifiers(1).WithPrependedLeadingTrivia(asyncLeadingTrivia))
                    newModifiers = newModifiers.RemoveAt(0)
                Else
                    ' move it to the return type.
                    newModifiers = modifiers.RemoveAt(0)
                    newSubKeyword = newSubKeyword.WithPrependedLeadingTrivia(asyncLeadingTrivia)
                End If
            Else
                newModifiers = modifiers.RemoveAt(asyncTokenIndex)
            End If

            Dim newSubStatement = subOrFunctionStatement.WithModifiers(newModifiers).WithSubOrFunctionKeyword(newSubKeyword)
            Return newSubStatement
        End Function

        Private Function FixMultiLineLambdaExpression(node As MultiLineLambdaExpressionSyntax) As SyntaxNode
            Dim header As LambdaHeaderSyntax = GetNewHeader(node)
            Return node.WithSubOrFunctionHeader(header).WithLeadingTrivia(node.GetLeadingTrivia())
        End Function

        Private Function FixSingleLineLambdaExpression(node As SingleLineLambdaExpressionSyntax) As SingleLineLambdaExpressionSyntax
            Dim header As LambdaHeaderSyntax = GetNewHeader(node)
            Return node.WithSubOrFunctionHeader(header).WithLeadingTrivia(node.GetLeadingTrivia())
        End Function

        Private Shared Function GetNewHeader(node As LambdaExpressionSyntax) As LambdaHeaderSyntax
            Dim header = DirectCast(node.SubOrFunctionHeader, LambdaHeaderSyntax)
            Dim asyncKeywordIndex = header.Modifiers.IndexOf(SyntaxKind.AsyncKeyword)
            Dim newHeader = header.WithModifiers(header.Modifiers.RemoveAt(asyncKeywordIndex))
            Return newHeader
        End Function
    End Class
End Namespace