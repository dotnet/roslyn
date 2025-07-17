' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
    <ExportCodeCleanupProvider(PredefinedCodeCleanupProviderNames.AddMissingTokens, LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeCleanupProviderNames.CaseCorrection, Before:=PredefinedCodeCleanupProviderNames.Format)>
    Friend Class AddMissingTokensCodeCleanupProvider
        Inherits AbstractTokensCodeCleanupProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="https://github.com/dotnet/roslyn/issues/42820")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property Name As String
            Get
                Return PredefinedCodeCleanupProviderNames.AddMissingTokens
            End Get
        End Property

        Protected Overrides Async Function GetRewriterAsync(document As Document, root As SyntaxNode, spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken) As Task(Of Rewriter)
            Return Await AddMissingTokensRewriter.CreateAsync(document, spans, cancellationToken).ConfigureAwait(False)
        End Function

        Private Class AddMissingTokensRewriter
            Inherits AbstractTokensCodeCleanupProvider.Rewriter

            Private ReadOnly _model As SemanticModel

            Private Sub New(semanticModel As SemanticModel, spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken)
                MyBase.New(spans, cancellationToken)

                Me._model = semanticModel
            End Sub

            Public Shared Async Function CreateAsync(document As Document, spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken) As Task(Of AddMissingTokensRewriter)
                Dim modifiedSpan = spans.Collapse()
                Dim semanticModel = If(document Is Nothing, Nothing,
                    Await document.ReuseExistingSpeculativeModelAsync(modifiedSpan, cancellationToken).ConfigureAwait(False))

                Return New AddMissingTokensRewriter(semanticModel, spans, cancellationToken)
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                If TypeOf node Is ExpressionSyntax Then
                    Return VisitExpression(DirectCast(node, ExpressionSyntax))
                Else
                    Return MyBase.Visit(node)
                End If
            End Function

            Private Function VisitExpression(node As ExpressionSyntax) As SyntaxNode
                If Not ShouldRewrite(node) Then
                    Return node
                End If

                Return AddParenthesesTransform(node, MyBase.Visit(node),
                                                 Function()
                                                     ' we only care whole name not part of dotted names
                                                     Dim name As NameSyntax = TryCast(node, NameSyntax)
                                                     If name Is Nothing OrElse TypeOf name.Parent Is NameSyntax Then
                                                         Return False
                                                     End If

                                                     Return CheckName(name)
                                                 End Function,
                                                 Function(n) DirectCast(n, InvocationExpressionSyntax).ArgumentList,
                                                 Function(n) SyntaxFactory.InvocationExpression(n, SyntaxFactory.ArgumentList()),
                                                 Function(n) IsMethodSymbol(DirectCast(n, ExpressionSyntax)))
            End Function

            Private Function CheckName(name As NameSyntax) As Boolean
                If _underStructuredTrivia OrElse name.IsStructuredTrivia() OrElse name.IsMissing Then
                    Return False
                End If

                ' can't/don't try to transform member access to invocation
                If TypeOf name.Parent Is MemberAccessExpressionSyntax OrElse
                   TypeOf name.Parent Is TupleElementSyntax OrElse
                   name.CheckParent(Of AttributeSyntax)(Function(p) p.Name Is name) OrElse
                   name.CheckParent(Of ImplementsClauseSyntax)(Function(p) p.InterfaceMembers.Any(Function(i) i Is name)) OrElse
                   name.CheckParent(Of UnaryExpressionSyntax)(Function(p) p.Kind = SyntaxKind.AddressOfExpression AndAlso p.Operand Is name) OrElse
                   name.CheckParent(Of InvocationExpressionSyntax)(Function(p) p.Expression Is name) OrElse
                   name.CheckParent(Of NamedFieldInitializerSyntax)(Function(p) p.Name Is name) OrElse
                   name.CheckParent(Of ImplementsStatementSyntax)(Function(p) p.Types.Any(Function(t) t Is name)) OrElse
                   name.CheckParent(Of HandlesClauseItemSyntax)(Function(p) p.EventMember Is name) OrElse
                   name.CheckParent(Of ObjectCreationExpressionSyntax)(Function(p) p.Type Is name) OrElse
                   name.CheckParent(Of ArrayCreationExpressionSyntax)(Function(p) p.Type Is name) OrElse
                   name.CheckParent(Of ArrayTypeSyntax)(Function(p) p.ElementType Is name) OrElse
                   name.CheckParent(Of SimpleAsClauseSyntax)(Function(p) p.Type Is name) OrElse
                   name.CheckParent(Of TypeConstraintSyntax)(Function(p) p.Type Is name) OrElse
                   name.CheckParent(Of GetTypeExpressionSyntax)(Function(p) p.Type Is name) OrElse
                   name.CheckParent(Of TypeOfExpressionSyntax)(Function(p) p.Type Is name) OrElse
                   name.CheckParent(Of CastExpressionSyntax)(Function(p) p.Type Is name) OrElse
                   name.CheckParent(Of ForEachStatementSyntax)(Function(p) p.ControlVariable Is name) OrElse
                   name.CheckParent(Of ForStatementSyntax)(Function(p) p.ControlVariable Is name) OrElse
                   name.CheckParent(Of AssignmentStatementSyntax)(Function(p) p.Left Is name) OrElse
                   name.CheckParent(Of TypeArgumentListSyntax)(Function(p) p.Arguments.Any(Function(i) i Is name)) OrElse
                   name.CheckParent(Of SimpleArgumentSyntax)(Function(p) p.IsNamed AndAlso p.NameColonEquals.Name Is name) OrElse
                   name.CheckParent(Of CastExpressionSyntax)(Function(p) p.Type Is name) OrElse
                   name.CheckParent(Of SimpleArgumentSyntax)(Function(p) p.Expression Is name) OrElse
                   name.CheckParent(Of NameOfExpressionSyntax)(Function(p) p.Argument Is name) Then
                    Return False
                End If

                Return True
            End Function

            Private Function IsMethodSymbol(expression As ExpressionSyntax) As Boolean
                If Me._model Is Nothing Then
                    Return False
                End If

                Dim symbols = Me._model.GetSymbolInfo(expression, _cancellationToken).GetAllSymbols()
                Return symbols.Any() AndAlso symbols.All(
                    Function(s) If(TryCast(s, IMethodSymbol)?.MethodKind = MethodKind.Ordinary, False))
            End Function

            Private Function IsDelegateType(expression As ExpressionSyntax) As Boolean
                If Me._model Is Nothing Then
                    Return False
                End If

                Dim type = Me._model.GetTypeInfo(expression, _cancellationToken).Type
                Return type.IsDelegateType
            End Function

            Public Overrides Function VisitInvocationExpression(node As InvocationExpressionSyntax) As SyntaxNode
                Dim newNode = MyBase.VisitInvocationExpression(node)

                ' make sure we are not under structured trivia
                If _underStructuredTrivia Then
                    Return newNode
                End If

                If TypeOf node.Expression IsNot NameSyntax AndAlso
                   TypeOf node.Expression IsNot ParenthesizedExpressionSyntax AndAlso
                   TypeOf node.Expression IsNot MemberAccessExpressionSyntax Then
                    Return newNode
                End If

                Dim semanticChecker As Func(Of InvocationExpressionSyntax, Boolean) =
                    Function(n) IsMethodSymbol(n.Expression) OrElse IsDelegateType(n.Expression)

                Return AddParenthesesTransform(
                        node, newNode, Function(n) n.Expression.Span.Length > 0, Function(n) n.ArgumentList, Function(n) n.WithArgumentList(SyntaxFactory.ArgumentList()), semanticChecker)
            End Function

            Public Overrides Function VisitRaiseEventStatement(node As RaiseEventStatementSyntax) As SyntaxNode
                Return AddParenthesesTransform(
                    node, MyBase.VisitRaiseEventStatement(node), Function(n) Not n.Name.IsMissing, Function(n) n.ArgumentList, Function(n) n.WithArgumentList(SyntaxFactory.ArgumentList()))
            End Function

            Public Overrides Function VisitMethodStatement(node As MethodStatementSyntax) As SyntaxNode
                Dim rewrittenMethod = DirectCast(AddParameterListTransform(node, MyBase.VisitMethodStatement(node), Function(n) Not n.Identifier.IsMissing), MethodStatementSyntax)
                Return AsyncOrIteratorFunctionReturnTypeFixer.RewriteMethodStatement(rewrittenMethod, Me._model, node, Me._cancellationToken)
            End Function

            Public Overrides Function VisitSubNewStatement(node As SubNewStatementSyntax) As SyntaxNode
                Return AddParameterListTransform(node, MyBase.VisitSubNewStatement(node), Function(n) Not n.NewKeyword.IsMissing)
            End Function

            Public Overrides Function VisitDeclareStatement(node As DeclareStatementSyntax) As SyntaxNode
                Return AddParameterListTransform(node, MyBase.VisitDeclareStatement(node), Function(n) Not n.Identifier.IsMissing)
            End Function

            Public Overrides Function VisitDelegateStatement(node As DelegateStatementSyntax) As SyntaxNode
                Return AddParameterListTransform(node, MyBase.VisitDelegateStatement(node), Function(n) Not n.Identifier.IsMissing)
            End Function

            Public Overrides Function VisitEventStatement(node As EventStatementSyntax) As SyntaxNode
                If node.AsClause IsNot Nothing Then
                    Return MyBase.VisitEventStatement(node)
                End If

                Return AddParameterListTransform(node, MyBase.VisitEventStatement(node), Function(n) Not n.Identifier.IsMissing)
            End Function

            Public Overrides Function VisitAccessorStatement(node As AccessorStatementSyntax) As SyntaxNode
                Dim newNode = MyBase.VisitAccessorStatement(node)
                If node.DeclarationKeyword.Kind <> SyntaxKind.AddHandlerKeyword AndAlso
                   node.DeclarationKeyword.Kind <> SyntaxKind.RemoveHandlerKeyword AndAlso
                   node.DeclarationKeyword.Kind <> SyntaxKind.RaiseEventKeyword Then
                    Return newNode
                End If

                Return AddParameterListTransform(node, newNode, Function(n) Not n.DeclarationKeyword.IsMissing)
            End Function

            Public Overrides Function VisitAttribute(node As AttributeSyntax) As SyntaxNode
                ' we decide not to auto insert parentheses for attribute
                Return MyBase.VisitAttribute(node)
            End Function

            Public Overrides Function VisitOperatorStatement(node As OperatorStatementSyntax) As SyntaxNode
                ' don't auto insert parentheses
                ' these methods are okay to be removed. but it is here to show other cases where parse tree node can have parentheses
                Return MyBase.VisitOperatorStatement(node)
            End Function

            Public Overrides Function VisitPropertyStatement(node As PropertyStatementSyntax) As SyntaxNode
                ' don't auto insert parentheses
                ' these methods are okay to be removed. but it is here to show other cases where parse tree node can have parentheses
                Return MyBase.VisitPropertyStatement(node)
            End Function

            Public Overrides Function VisitLambdaHeader(node As LambdaHeaderSyntax) As SyntaxNode
                Dim rewrittenLambdaHeader = DirectCast(MyBase.VisitLambdaHeader(node), LambdaHeaderSyntax)
                rewrittenLambdaHeader = AsyncOrIteratorFunctionReturnTypeFixer.RewriteLambdaHeader(rewrittenLambdaHeader, Me._model, node, Me._cancellationToken)
                Return AddParameterListTransform(node, rewrittenLambdaHeader, Function(n) True)
            End Function

            Private Shared Function TryFixupTrivia(Of T As SyntaxNode)(node As T, previousToken As SyntaxToken, lastToken As SyntaxToken, ByRef newNode As T) As Boolean
                ' initialize to initial value
                newNode = Nothing

                ' hold onto the trivia
                Dim prevTrailingTrivia = previousToken.TrailingTrivia

                ' if previous token is not part of node and if it has any trivia, don't do anything
                If Not node.DescendantTokens().Any(Function(token) token = previousToken) AndAlso prevTrailingTrivia.Count > 0 Then
                    Return False
                End If

                ' remove the trivia from the token
                Dim previousTokenWithoutTrailingTrivia = previousToken.WithTrailingTrivia(SyntaxFactory.ElasticMarker)

                ' If previousToken has trailing WhitespaceTrivia, strip off the trailing WhitespaceTrivia from the lastToken.
                Dim lastTrailingTrivia = lastToken.TrailingTrivia
                If prevTrailingTrivia.Any(SyntaxKind.WhitespaceTrivia) Then
                    lastTrailingTrivia = lastTrailingTrivia.WithoutLeadingWhitespaceOrEndOfLine()
                End If

                ' get the trivia and attach it to the last token
                Dim lastTokenWithTrailingTrivia = lastToken.WithTrailingTrivia(prevTrailingTrivia.Concat(lastTrailingTrivia))

                ' replace tokens
                newNode = node.ReplaceTokens(SpecializedCollections.SingletonEnumerable(previousToken).Concat(lastToken),
                                              Function(o, m)
                                                  If o = previousToken Then
                                                      Return previousTokenWithoutTrailingTrivia
                                                  ElseIf o = lastToken Then
                                                      Return lastTokenWithTrailingTrivia
                                                  End If

                                                  Throw ExceptionUtilities.UnexpectedValue(o)
                                              End Function)

                Return True
            End Function

            Private Function AddParameterListTransform(Of T As MethodBaseSyntax)(node As T, newNode As SyntaxNode, nameChecker As Func(Of T, Boolean)) As T
                Dim transform As Func(Of T, T) = Function(n As T)
                                                     Dim newParamList = SyntaxFactory.ParameterList()
                                                     If n.ParameterList IsNot Nothing Then
                                                         If n.ParameterList.HasLeadingTrivia Then
                                                             newParamList = newParamList.WithLeadingTrivia(n.ParameterList.GetLeadingTrivia)
                                                         End If

                                                         If n.ParameterList.HasTrailingTrivia Then
                                                             newParamList = newParamList.WithTrailingTrivia(n.ParameterList.GetTrailingTrivia)
                                                         End If
                                                     End If

                                                     Dim nodeWithParams = DirectCast(n.WithParameterList(newParamList), T)
                                                     If n.HasTrailingTrivia AndAlso nodeWithParams.GetLastToken() = nodeWithParams.ParameterList.CloseParenToken Then
                                                         Dim trailing = n.GetTrailingTrivia
                                                         nodeWithParams = DirectCast(n _
                                                             .WithoutTrailingTrivia() _
                                                             .WithParameterList(newParamList) _
                                                             .WithTrailingTrivia(trailing), T)
                                                     End If

                                                     Return nodeWithParams
                                                 End Function

                Return AddParenthesesTransform(node, newNode, nameChecker, Function(n) n.ParameterList, transform)
            End Function

            Private Function AddParenthesesTransform(Of T As SyntaxNode)(
                originalNode As T,
                node As SyntaxNode,
                nameChecker As Func(Of T, Boolean),
                listGetter As Func(Of T, SyntaxNode),
                withTransform As Func(Of T, T),
                Optional semanticPredicate As Func(Of T, Boolean) = Nothing
            ) As T
                Dim newNode = DirectCast(node, T)
                If Not nameChecker(newNode) Then
                    Return newNode
                End If

                Dim syntaxPredicate As Func(Of Boolean) = Function()
                                                              Dim list = listGetter(originalNode)
                                                              If list Is Nothing Then
                                                                  Return True
                                                              End If

                                                              Dim paramList = TryCast(list, ParameterListSyntax)
                                                              If paramList IsNot Nothing Then
                                                                  Return paramList.Parameters = Nothing AndAlso
                                                                         paramList.OpenParenToken.IsMissing AndAlso
                                                                         paramList.CloseParenToken.IsMissing
                                                              End If

                                                              Dim argsList = TryCast(list, ArgumentListSyntax)
                                                              Return argsList IsNot Nothing AndAlso
                                                                     argsList.Arguments = Nothing AndAlso
                                                                     argsList.OpenParenToken.IsMissing AndAlso
                                                                     argsList.CloseParenToken.IsMissing
                                                          End Function

                Return AddParenthesesTransform(originalNode, node, syntaxPredicate, listGetter, withTransform, semanticPredicate)
            End Function

            Private Function AddParenthesesTransform(Of T As SyntaxNode)(
                originalNode As T,
                node As SyntaxNode,
                syntaxPredicate As Func(Of Boolean),
                listGetter As Func(Of T, SyntaxNode),
                transform As Func(Of T, T),
                Optional semanticPredicate As Func(Of T, Boolean) = Nothing
            ) As T
                Dim span = originalNode.Span

                If syntaxPredicate() AndAlso
                   _spans.HasIntervalThatContains(span.Start, span.Length) AndAlso
                   CheckSkippedTriviaForMissingToken(originalNode, SyntaxKind.OpenParenToken, SyntaxKind.CloseParenToken) Then

                    Dim transformedNode = transform(DirectCast(node, T))

                    ' previous token can be different per different node types. 
                    ' it could be name or close paren of type parameter list and etc. also can be different based on
                    ' what token is omitted
                    ' get one that actually exist and get trailing trivia of that token
                    Dim fixedUpNode As T = Nothing

                    Dim list = listGetter(transformedNode)
                    Dim previousToken = list.GetFirstToken(includeZeroWidth:=True).GetPreviousToken(includeZeroWidth:=True)
                    Dim lastToken = list.GetLastToken(includeZeroWidth:=True)

                    If Not TryFixupTrivia(transformedNode, previousToken, lastToken, fixedUpNode) Then
                        Return DirectCast(node, T)
                    End If

                    ' semanticPredicate is invoked at the last step as it is the most expensive operation which requires building the compilation for semantic validations.
                    If semanticPredicate Is Nothing OrElse semanticPredicate(originalNode) Then
                        Return DirectCast(fixedUpNode, T)
                    End If
                End If

                Return DirectCast(node, T)
            End Function

            Private Shared Function CheckSkippedTriviaForMissingToken(node As SyntaxNode, ParamArray kinds As SyntaxKind()) As Boolean
                Dim lastToken = node.GetLastToken(includeZeroWidth:=True)
                If lastToken.TrailingTrivia.Count = 0 Then
                    Return True
                End If

                Return Not lastToken _
                           .TrailingTrivia _
                           .Where(Function(t) t.Kind = SyntaxKind.SkippedTokensTrivia) _
                           .SelectMany(Function(t) DirectCast(t.GetStructure(), SkippedTokensTriviaSyntax).Tokens) _
                           .Any(Function(t) kinds.Contains(t.Kind))
            End Function

            Public Overrides Function VisitIfStatement(node As IfStatementSyntax) As SyntaxNode
                Return AddMissingOrOmittedTokenTransform(node, MyBase.VisitIfStatement(node), Function(n) n.ThenKeyword, SyntaxKind.ThenKeyword)
            End Function

            Public Overrides Function VisitIfDirectiveTrivia(node As IfDirectiveTriviaSyntax) As SyntaxNode
                Return AddMissingOrOmittedTokenTransform(node, MyBase.VisitIfDirectiveTrivia(node), Function(n) n.ThenKeyword, SyntaxKind.ThenKeyword)
            End Function

            Public Overrides Function VisitElseIfStatement(node As ElseIfStatementSyntax) As SyntaxNode
                Return AddMissingOrOmittedTokenTransform(node, MyBase.VisitElseIfStatement(node), Function(n) n.ThenKeyword, SyntaxKind.ThenKeyword)
            End Function

            Public Overrides Function VisitTypeArgumentList(node As TypeArgumentListSyntax) As SyntaxNode
                Return AddMissingOrOmittedTokenTransform(node, MyBase.VisitTypeArgumentList(node), Function(n) n.OfKeyword, SyntaxKind.OfKeyword)
            End Function

            Public Overrides Function VisitTypeParameterList(node As TypeParameterListSyntax) As SyntaxNode
                Return AddMissingOrOmittedTokenTransform(node, MyBase.VisitTypeParameterList(node), Function(n) n.OfKeyword, SyntaxKind.OfKeyword)
            End Function

            Public Overrides Function VisitContinueStatement(node As ContinueStatementSyntax) As SyntaxNode
                Return AddMissingOrOmittedTokenTransform(node, MyBase.VisitContinueStatement(node), Function(n) n.BlockKeyword, SyntaxKind.DoKeyword, SyntaxKind.ForKeyword, SyntaxKind.WhileKeyword)
            End Function

            Public Overrides Function VisitOptionStatement(node As OptionStatementSyntax) As SyntaxNode
                Select Case node.NameKeyword.Kind
                    Case SyntaxKind.ExplicitKeyword,
                        SyntaxKind.InferKeyword,
                        SyntaxKind.StrictKeyword
                        Return AddMissingOrOmittedTokenTransform(node, node, Function(n) n.ValueKeyword, SyntaxKind.OnKeyword, SyntaxKind.OffKeyword)
                    Case Else
                        Return node
                End Select
            End Function

            Public Overrides Function VisitSelectStatement(node As SelectStatementSyntax) As SyntaxNode
                Dim newNode = DirectCast(MyBase.VisitSelectStatement(node), SelectStatementSyntax)
                Return If(newNode.CaseKeyword.Kind = SyntaxKind.None,
                           newNode.WithCaseKeyword(SyntaxFactory.Token(SyntaxKind.CaseKeyword)),
                           newNode)
            End Function

            Private Function AddMissingOrOmittedTokenTransform(Of T As SyntaxNode)(
                originalNode As T, node As SyntaxNode, tokenGetter As Func(Of T, SyntaxToken), ParamArray kinds As SyntaxKind()) As T

                Dim newNode = DirectCast(node, T)
                If Not CheckSkippedTriviaForMissingToken(originalNode, kinds) Then
                    Return newNode
                End If

                Dim newToken = tokenGetter(newNode)
                Dim processedToken = ProcessToken(tokenGetter(originalNode), newToken, newNode)
                If processedToken <> newToken Then
                    Dim replacedNode = ReplaceOrSetToken(newNode, newToken, processedToken)

                    Dim replacedToken = tokenGetter(replacedNode)
                    Dim previousToken = replacedToken.GetPreviousToken(includeZeroWidth:=True)

                    Dim fixedupNode As T = Nothing
                    If Not TryFixupTrivia(replacedNode, previousToken, replacedToken, fixedupNode) Then
                        Return newNode
                    End If

                    Return fixedupNode
                End If

                Return newNode
            End Function

            Private Function ProcessToken(originalToken As SyntaxToken, token As SyntaxToken, parent As SyntaxNode) As SyntaxToken
                ' special case omitted token case
                If IsOmitted(originalToken) Then
                    Return ProcessOmittedToken(originalToken, token, parent)
                End If

                Dim span = originalToken.Span
                If Not _spans.HasIntervalThatContains(span.Start, span.Length) Then
                    ' token is outside of the provided span
                    Return token
                End If

                ' token is not missing or if missing token is identifier there is not much we can do
                If Not originalToken.IsMissing OrElse
                   originalToken.Kind = SyntaxKind.None OrElse
                   originalToken.Kind = SyntaxKind.IdentifierToken Then
                    Return token
                End If

                Return ProcessMissingToken(originalToken, token)
            End Function

            Private Shared Function ReplaceOrSetToken(Of T As SyntaxNode)(originalParent As T, tokenToFix As SyntaxToken, replacementToken As SyntaxToken) As T
                If Not IsOmitted(tokenToFix) Then
                    Return originalParent.ReplaceToken(tokenToFix, replacementToken)
                Else
                    Return DirectCast(SetOmittedToken(originalParent, replacementToken), T)
                End If
            End Function

            Private Shared Function SetOmittedToken(originalParent As SyntaxNode, newToken As SyntaxToken) As SyntaxNode
                Select Case newToken.Kind
                    Case SyntaxKind.ThenKeyword
                        ' this can be regular If, an If directive, or an ElseIf
                        Dim regularIf = TryCast(originalParent, IfStatementSyntax)
                        If regularIf IsNot Nothing Then
                            Dim previousToken = regularIf.Condition.GetLastToken(includeZeroWidth:=True)
                            Dim nextToken = regularIf.GetLastToken.GetNextToken

                            If Not InvalidOmittedToken(previousToken, nextToken) Then
                                Return regularIf.WithThenKeyword(newToken)
                            End If

                        Else
                            Dim regularElseIf = TryCast(originalParent, ElseIfStatementSyntax)
                            If regularElseIf IsNot Nothing Then
                                Dim previousToken = regularElseIf.Condition.GetLastToken(includeZeroWidth:=True)
                                Dim nextToken = regularElseIf.GetLastToken.GetNextToken

                                If Not InvalidOmittedToken(previousToken, nextToken) Then
                                    Return regularElseIf.WithThenKeyword(newToken)
                                End If

                            Else
                                Dim ifDirective = TryCast(originalParent, IfDirectiveTriviaSyntax)
                                If ifDirective IsNot Nothing Then
                                    Dim previousToken = ifDirective.Condition.GetLastToken(includeZeroWidth:=True)
                                    Dim nextToken = ifDirective.GetLastToken.GetNextToken

                                    If Not InvalidOmittedToken(previousToken, nextToken) Then
                                        Return ifDirective.WithThenKeyword(newToken)
                                    End If
                                End If
                            End If
                        End If

                    Case SyntaxKind.OnKeyword
                        Dim optionStatement = TryCast(originalParent, OptionStatementSyntax)
                        If optionStatement IsNot Nothing Then
                            Return optionStatement.WithValueKeyword(newToken)
                        End If
                End Select

                Return originalParent
            End Function

            Private Shared Function IsOmitted(token As SyntaxToken) As Boolean
                Return token.Kind = SyntaxKind.None
            End Function

            Private Shared Function ProcessOmittedToken(originalToken As SyntaxToken, token As SyntaxToken, parent As SyntaxNode) As SyntaxToken
                ' multiline if statement with missing then keyword case
                If TypeOf parent Is IfStatementSyntax Then
                    Dim ifStatement = DirectCast(parent, IfStatementSyntax)
                    If Exist(ifStatement.Condition) AndAlso ifStatement.ThenKeyword = originalToken Then
                        Return If(parent.GetAncestor(Of MultiLineIfBlockSyntax)() IsNot Nothing, CreateOmittedToken(token, SyntaxKind.ThenKeyword), token)
                    End If
                End If

                If TryCast(parent, IfDirectiveTriviaSyntax)?.ThenKeyword = originalToken Then
                    Return CreateOmittedToken(token, SyntaxKind.ThenKeyword)
                ElseIf TryCast(parent, ElseIfStatementSyntax)?.ThenKeyword = originalToken Then
                    Return If(parent.GetAncestor(Of ElseIfBlockSyntax)() IsNot Nothing, CreateOmittedToken(token, SyntaxKind.ThenKeyword), token)
                ElseIf TryCast(parent, OptionStatementSyntax)?.ValueKeyword = originalToken Then
                    Return CreateOmittedToken(token, SyntaxKind.OnKeyword)
                End If

                Return token
            End Function

            Private Shared Function InvalidOmittedToken(previousToken As SyntaxToken, nextToken As SyntaxToken) As Boolean
                ' if previous token has a problem, don't bother
                If previousToken.IsMissing OrElse previousToken.IsSkipped OrElse previousToken.Kind = 0 Then
                    Return True
                End If

                ' if next token has a problem, do little bit more check
                ' if there is no next token, it is okay to insert the missing token
                If nextToken.Kind = 0 Then
                    Return False
                End If

                ' if next token is missing or skipped, check whether it has EOL
                If nextToken.IsMissing OrElse nextToken.IsSkipped Then
                    Return Not previousToken.TrailingTrivia.Any(SyntaxKind.EndOfLineTrivia) And
                           Not nextToken.LeadingTrivia.Any(SyntaxKind.EndOfLineTrivia)
                End If

                Return False
            End Function

            Private Shared Function Exist(node As SyntaxNode) As Boolean
                Return node IsNot Nothing AndAlso node.Span.Length > 0
            End Function

            Private Shared Function ProcessMissingToken(originalToken As SyntaxToken, token As SyntaxToken) As SyntaxToken
                ' auto insert missing "Of" keyword in type argument list
                If TryCast(originalToken.Parent, TypeArgumentListSyntax)?.OfKeyword = originalToken Then
                    Return CreateMissingToken(token)
                ElseIf TryCast(originalToken.Parent, TypeParameterListSyntax)?.OfKeyword = originalToken Then
                    Return CreateMissingToken(token)
                ElseIf TryCast(originalToken.Parent, ContinueStatementSyntax)?.BlockKeyword = originalToken Then
                    Return CreateMissingToken(token)
                End If

                Return token
            End Function

            Private Shared Function CreateMissingToken(token As SyntaxToken) As SyntaxToken
                Return CreateToken(token, token.Kind)
            End Function

            Private Shared Function CreateOmittedToken(token As SyntaxToken, kind As SyntaxKind) As SyntaxToken
                Return CreateToken(token, kind)
            End Function
        End Class
    End Class
End Namespace
