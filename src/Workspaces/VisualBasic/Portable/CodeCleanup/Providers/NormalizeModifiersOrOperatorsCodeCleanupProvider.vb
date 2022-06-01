' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.CodeStyle
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
    <ExportCodeCleanupProvider(PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators, LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeCleanupProviderNames.AddMissingTokens, Before:=PredefinedCodeCleanupProviderNames.Format)>
    Friend Class NormalizeModifiersOrOperatorsCodeCleanupProvider
        Implements ICodeCleanupProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="https://github.com/dotnet/roslyn/issues/42820")>
        Public Sub New()
        End Sub

        Public ReadOnly Property Name As String Implements ICodeCleanupProvider.Name
            Get
                Return PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators
            End Get
        End Property

        Public Async Function CleanupAsync(document As Document, spans As ImmutableArray(Of TextSpan), options As CodeCleanupOptions, cancellationToken As CancellationToken) As Task(Of Document) Implements ICodeCleanupProvider.CleanupAsync
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim newRoot = Await CleanupAsync(root, spans, options.FormattingOptions, document.Project.Solution.Workspace.Services, cancellationToken).ConfigureAwait(False)

            Return If(root Is newRoot, document, document.WithSyntaxRoot(newRoot))
        End Function

        Public Function CleanupAsync(root As SyntaxNode, spans As ImmutableArray(Of TextSpan), options As SyntaxFormattingOptions, services As HostWorkspaceServices, cancellationToken As CancellationToken) As Task(Of SyntaxNode) Implements ICodeCleanupProvider.CleanupAsync
            Dim rewriter = New Rewriter(spans, cancellationToken)
            Dim newRoot = rewriter.Visit(root)

            Return Task.FromResult(If(root Is newRoot, root, newRoot))
        End Function

        Private Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            ' list of modifier syntax kinds in order
            ' this order will be used when the rewriter re-order modifiers
            ' PERF: Using UShort instead of SyntaxKind as the element type so that the compiler can use array literal initialization
            Private Shared ReadOnly s_modifierKindsInOrder As SyntaxKind() =
                VisualBasicCodeStyleOptions.PreferredModifierOrderDefault.ToArray()

            Private Shared ReadOnly s_removeDimKeywordSet As HashSet(Of SyntaxKind) = New HashSet(Of SyntaxKind)(SyntaxFacts.EqualityComparer) From {
                SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.PublicKeyword, SyntaxKind.FriendKeyword,
                SyntaxKind.SharedKeyword, SyntaxKind.ShadowsKeyword, SyntaxKind.ReadOnlyKeyword}

            Private Shared ReadOnly s_normalizeOperatorsSet As Dictionary(Of SyntaxKind, List(Of SyntaxKind)) = New Dictionary(Of SyntaxKind, List(Of SyntaxKind))(SyntaxFacts.EqualityComparer) From {
                    {SyntaxKind.LessThanGreaterThanToken, New List(Of SyntaxKind) From {SyntaxKind.GreaterThanToken, SyntaxKind.LessThanToken}},
                    {SyntaxKind.GreaterThanEqualsToken, New List(Of SyntaxKind) From {SyntaxKind.EqualsToken, SyntaxKind.GreaterThanToken}},
                    {SyntaxKind.LessThanEqualsToken, New List(Of SyntaxKind) From {SyntaxKind.EqualsToken, SyntaxKind.LessThanToken}}
                }

            Private ReadOnly _spans As SimpleIntervalTree(Of TextSpan, TextSpanIntervalIntrospector)
            Private ReadOnly _cancellationToken As CancellationToken

            Public Sub New(spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken)
                MyBase.New(visitIntoStructuredTrivia:=True)

                _spans = New SimpleIntervalTree(Of TextSpan, TextSpanIntervalIntrospector)(New TextSpanIntervalIntrospector(), spans)
                _cancellationToken = cancellationToken
            End Sub

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                ' if there are no overlapping spans, no need to walk down this node
                If node Is Nothing OrElse
                   Not _spans.HasIntervalThatOverlapsWith(node.FullSpan.Start, node.FullSpan.Length) Then
                    Return node
                End If

                ' walk down this path
                Return MyBase.Visit(node)
            End Function

            Public Overrides Function VisitModuleStatement(node As ModuleStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitModuleStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitStructureStatement(node As StructureStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitStructureStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitInterfaceStatement(node As InterfaceStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitInterfaceStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitClassStatement(node As ClassStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitClassStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitEnumStatement(node As EnumStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitEnumStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitMethodStatement(node As MethodStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitMethodStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitSubNewStatement(node As SubNewStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitSubNewStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitDeclareStatement(node As DeclareStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitDeclareStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitDelegateStatement(node As DelegateStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitDelegateStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitEventStatement(node As EventStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitEventStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitPropertyStatement(node As PropertyStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitPropertyStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitAccessorStatement(node As AccessorStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitAccessorStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitIncompleteMember(node As IncompleteMemberSyntax) As SyntaxNode
                ' don't do anything
                Return MyBase.VisitIncompleteMember(node)
            End Function

            Public Overrides Function VisitFieldDeclaration(node As FieldDeclarationSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitFieldDeclaration(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitLocalDeclarationStatement(node As LocalDeclarationStatementSyntax) As SyntaxNode
                Return NormalizeModifiers(node, MyBase.VisitLocalDeclarationStatement(node), Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))
            End Function

            Public Overrides Function VisitParameterList(node As ParameterListSyntax) As SyntaxNode
                ' whole node must be under the span. otherwise, we just return
                Dim newNode = MyBase.VisitParameterList(node)

                ' bug # 12898
                ' decide not to automatically remove "ByVal"
#If False Then
                Dim span = node.Span
                If Not _spans.GetContainingIntervals(span.Start, span.Length).Any() Then
                    Return newNode
                End If

                ' remove any existing ByVal keyword
                Dim currentNode = DirectCast(newNode, ParameterListSyntax)
                For i = 0 To node.Parameters.Count - 1
                    currentNode = RemoveByValKeyword(currentNode, i)
                Next

                ' no changes
                If newNode Is currentNode Then
                    Return newNode
                End If

                ' replace whole parameter list
                _textChanges.Add(node.FullSpan, currentNode.GetFullText())

                Return currentNode
#End If

                Return newNode
            End Function

            Public Overrides Function VisitLambdaHeader(node As LambdaHeaderSyntax) As SyntaxNode
                ' lambda can have async and iterator modifiers but we currently don't support those
                Return node
            End Function

            Public Overrides Function VisitOperatorStatement(node As OperatorStatementSyntax) As SyntaxNode
                Dim visitedNode = DirectCast(MyBase.VisitOperatorStatement(node), OperatorStatementSyntax)

                Dim span = node.Span
                If Not _spans.HasIntervalThatContains(span.Start, span.Length) Then
                    Return visitedNode
                End If

                ' operator sometimes requires a fix up outside of modifiers
                Dim fixedUpNode = OperatorStatementSpecialFixup(visitedNode)

                ' now, normalize modifiers
                Dim newNode = NormalizeModifiers(node, fixedUpNode, Function(n) n.Modifiers, Function(n, modifiers) n.WithModifiers(modifiers))

                Dim [operator] = NormalizeOperator(
                                    newNode.OperatorToken,
                                    Function(t) t.Kind = SyntaxKind.GreaterThanToken,
                                    Function(t) t.TrailingTrivia,
                                    Function(t) New List(Of SyntaxKind) From {SyntaxKind.LessThanToken},
                                    Function(t, i)
                                        Return t.CopyAnnotationsTo(
                                            SyntaxFactory.Token(
                                                t.LeadingTrivia.Concat(t.TrailingTrivia.Take(i)).ToSyntaxTriviaList(),
                                                SyntaxKind.LessThanGreaterThanToken,
                                                t.TrailingTrivia.Skip(i + 1).ToSyntaxTriviaList()))
                                    End Function)

                If [operator].Kind = SyntaxKind.None Then
                    Return newNode
                End If

                Return newNode.WithOperatorToken([operator])
            End Function

            Public Overrides Function VisitBinaryExpression(node As BinaryExpressionSyntax) As SyntaxNode
                ' normalize binary operators
                Dim binaryOperator = DirectCast(MyBase.VisitBinaryExpression(node), BinaryExpressionSyntax)

                ' quick check. operator must be missing
                If Not binaryOperator.OperatorToken.IsMissing Then
                    Return binaryOperator
                End If

                Dim span = node.Span
                If Not _spans.HasIntervalThatContains(span.Start, span.Length) Then
                    Return binaryOperator
                End If

                ' and the operator must be one of kinds that we are interested in
                Dim [operator] = NormalizeOperator(
                                    binaryOperator.OperatorToken,
                                    Function(t) s_normalizeOperatorsSet.ContainsKey(t.Kind),
                                    Function(t) t.LeadingTrivia,
                                    Function(t) s_normalizeOperatorsSet(t.Kind),
                                    Function(t, i)
                                        Return t.CopyAnnotationsTo(
                                            SyntaxFactory.Token(
                                                t.LeadingTrivia.Take(i).ToSyntaxTriviaList(),
                                                t.Kind,
                                                t.LeadingTrivia.Skip(i + 1).Concat(t.TrailingTrivia).ToSyntaxTriviaList()))
                                    End Function)

                If [operator].Kind = SyntaxKind.None Then
                    Return binaryOperator
                End If

                Return binaryOperator.WithOperatorToken([operator])
            End Function

            Public Overrides Function VisitToken(token As SyntaxToken) As SyntaxToken
                Dim newToken = MyBase.VisitToken(token)

                Dim span = token.Span
                If Not _spans.HasIntervalThatContains(span.Start, span.Length) Then
                    Return newToken
                End If

                If token.IsMissing OrElse Not (SyntaxFacts.IsOperator(token.Kind) OrElse token.IsKind(SyntaxKind.ColonEqualsToken)) Then
                    Return newToken
                End If

                Dim actualText = token.ToString()
                Dim expectedText = SyntaxFacts.GetText(token.Kind)

                If String.IsNullOrWhiteSpace(expectedText) OrElse actualText = expectedText Then
                    Return newToken
                End If

                Return SyntaxFactory.Token(newToken.LeadingTrivia, newToken.Kind, newToken.TrailingTrivia, expectedText)
            End Function

            ''' <summary>
            ''' this will put operator token and modifier tokens in right order
            ''' </summary>
            Private Shared Function OperatorStatementSpecialFixup(node As OperatorStatementSyntax) As OperatorStatementSyntax
                ' first check whether operator is missing
                If Not node.OperatorToken.IsMissing Then
                    Return node
                End If

                ' check whether operator has missing stuff in skipped token list
                Dim skippedTokens = node.OperatorToken.TrailingTrivia _
                                                 .Where(Function(t) t.Kind = SyntaxKind.SkippedTokensTrivia) _
                                                 .Select(Function(t) DirectCast(t.GetStructure(), SkippedTokensTriviaSyntax)) _
                                                 .SelectMany(Function(t) t.Tokens)

                ' there must be 2 skipped tokens
                If skippedTokens.Count <> 2 Then
                    Return node
                End If

                Dim last = skippedTokens.Last()
                If Not SyntaxFacts.IsOperatorStatementOperatorToken(last.Kind) Then
                    Return node
                End If

                ' reorder some tokens
                Dim newNode = node.WithModifiers(node.Modifiers.AddRange(skippedTokens.Take(skippedTokens.Count - 1).ToArray())).WithOperatorToken(last)
                If Not ValidOperatorStatement(newNode) Then
                    Return node
                End If

                Return newNode
            End Function

            ''' <summary>
            ''' check whether given operator statement is valid or not
            ''' </summary>
            Private Shared Function ValidOperatorStatement(node As OperatorStatementSyntax) As Boolean
                Dim parsableStatementText = node.NormalizeWhitespace().ToString()
                Dim parsableCompilationUnit = "Class C" + vbCrLf + parsableStatementText + vbCrLf + "End Operator" + vbCrLf + "End Class"
                Dim parsedNode = SyntaxFactory.ParseCompilationUnit(parsableCompilationUnit)

                Return Not parsedNode.ContainsDiagnostics()
            End Function

            ''' <summary>
            ''' normalize operator
            ''' </summary>
            Private Shared Function NormalizeOperator(
                [operator] As SyntaxToken,
                checker As Func(Of SyntaxToken, Boolean),
                triviaListGetter As Func(Of SyntaxToken, SyntaxTriviaList),
                tokenKindsGetter As Func(Of SyntaxToken, List(Of SyntaxKind)),
                operatorCreator As Func(Of SyntaxToken, Integer, SyntaxToken)) As SyntaxToken

                If Not checker([operator]) Then
                    Return Nothing
                End If

                ' now, it should have skipped token trivia in trivia list
                Dim skippedTokenTrivia = triviaListGetter([operator]).FirstOrDefault(Function(t) t.Kind = SyntaxKind.SkippedTokensTrivia)
                If skippedTokenTrivia.Kind = SyntaxKind.None Then
                    Return Nothing
                End If

                ' token in the skipped token list must match what we are expecting
                Dim skippedTokensList = DirectCast(skippedTokenTrivia.GetStructure(), SkippedTokensTriviaSyntax)

                Dim actual = skippedTokensList.Tokens
                Dim expected = tokenKindsGetter([operator])
                If actual.Count <> expected.Count Then
                    Return Nothing
                End If

                Dim i = -1
                For Each token In actual
                    i = i + 1
                    If token.Kind <> expected(i) Then
                        Return Nothing
                    End If
                Next

                ' okay, looks like it is what we are expecting. let's fix it up
                ' move everything after skippedTokenTrivia to trailing trivia
                Dim index = -1
                Dim list = triviaListGetter([operator])
                For i = 0 To list.Count - 1
                    If list(i) = skippedTokenTrivia Then
                        index = i
                        Exit For
                    End If
                Next

                ' it must exist
                Contract.ThrowIfFalse(index >= 0)

                Return operatorCreator([operator], index)
            End Function

            ''' <summary>
            ''' reorder modifiers in the list
            ''' </summary>
            Private Shared Function ReorderModifiers(modifiers As SyntaxTokenList) As SyntaxTokenList
                ' quick check - if there is only one or less modifier, return as it is
                If modifiers.Count <= 1 Then
                    Return modifiers
                End If

                ' do quick check to see whether modifiers are already in right order
                If AreModifiersInRightOrder(modifiers) Then
                    Return modifiers
                End If

                ' re-create the list with trivia from old modifier token list
                Dim currentModifierIndex = 0
                Dim result = New List(Of SyntaxToken)(modifiers.Count)

                Dim modifierList = modifiers.ToList()
                For Each k In s_modifierKindsInOrder
                    ' we found all modifiers
                    If currentModifierIndex = modifierList.Count Then
                        Exit For
                    End If

                    Dim tokenInRightOrder = modifierList.FirstOrDefault(Function(m) m.Kind = k)

                    ' if we didn't find, move on to next one
                    If tokenInRightOrder.Kind = SyntaxKind.None Then
                        Continue For
                    End If

                    ' we found a modifier, re-create list in right order with right trivia from right original token
                    Dim originalToken = modifierList(currentModifierIndex)
                    result.Add(tokenInRightOrder.With(originalToken.LeadingTrivia, originalToken.TrailingTrivia))

                    currentModifierIndex += 1
                Next

                ' Verify that all unique modifiers were added to the result.
                ' The number added to the result count is the duplicate modifier count in the input modifierList.
                Debug.Assert(modifierList.Count = result.Count +
                             modifierList.GroupBy(Function(token) token.Kind).SelectMany(Function(grp) grp.Skip(1)).Count)
                Return SyntaxFactory.TokenList(result)
            End Function

            ''' <summary>
            ''' normalize modifier list of the node and record changes if there is any change
            ''' </summary>
            Private Function NormalizeModifiers(Of T As SyntaxNode)(originalNode As T, node As SyntaxNode, modifiersGetter As Func(Of T, SyntaxTokenList), withModifiers As Func(Of T, SyntaxTokenList, T)) As T
                Return NormalizeModifiers(originalNode, DirectCast(node, T), modifiersGetter, withModifiers)
            End Function

            ''' <summary>
            ''' normalize modifier list of the node and record changes if there is any change
            ''' </summary>
            Private Function NormalizeModifiers(Of T As SyntaxNode)(originalNode As T, node As T, modifiersGetter As Func(Of T, SyntaxTokenList), withModifiers As Func(Of T, SyntaxTokenList, T)) As T
                Dim modifiers = modifiersGetter(node)

                ' if number of modifiers are less than 1, we don't need to do anything
                If modifiers.Count <= 1 Then
                    Return node
                End If

                ' whole node must be under span, otherwise, we will just return
                Dim span = originalNode.Span
                If Not _spans.HasIntervalThatContains(span.Start, span.Length) Then
                    Return node
                End If

                ' try normalize modifier list
                Dim newNode = withModifiers(node, ReorderModifiers(modifiers))

                ' new modifier list
                Dim newModifiers = modifiersGetter(newNode)

                ' check whether we need to remove "Dim" keyword or not
                If newModifiers.Any(Function(m) s_removeDimKeywordSet.Contains(m.Kind)) Then
                    newNode = RemoveDimKeyword(newNode, modifiersGetter)
                End If

                ' no change
                If newNode Is node Then
                    Return node
                End If

                ' add text change
                Dim originalModifiers = modifiersGetter(originalNode)
                Contract.ThrowIfFalse(originalModifiers.Count > 0)

                Return newNode
            End Function

            ''' <summary>
            ''' remove "Dim" keyword if present
            ''' </summary>
            Private Shared Function RemoveDimKeyword(Of T As SyntaxNode)(node As T, modifiersGetter As Func(Of T, SyntaxTokenList)) As T
                Return RemoveModifierKeyword(node, modifiersGetter, SyntaxKind.DimKeyword)
            End Function

            ''' <summary>
            ''' remove a modifier from the given node
            ''' </summary>
            Private Shared Function RemoveModifierKeyword(Of T As SyntaxNode)(node As T, modifiersGetter As Func(Of T, SyntaxTokenList), modifierKind As SyntaxKind) As T
                Dim modifiers = modifiersGetter(node)

                ' "Dim" doesn't exist
                Dim modifier = modifiers.FirstOrDefault(Function(m) m.Kind = modifierKind)
                If modifier.Kind = SyntaxKind.None Then
                    Return node
                End If

                ' merge trivia belong to the modifier to be deleted
                Dim trivia = modifier.LeadingTrivia.Concat(modifier.TrailingTrivia)

                ' we have node which owns tokens around modifiers. just replace tokens in the node in case we need to
                ' touch tokens outside of the modifier list
                Dim previousToken = modifier.GetPreviousToken(includeZeroWidth:=True)
                Dim newPreviousToken = previousToken.WithAppendedTrailingTrivia(trivia)

                ' replace previous token and remove "Dim"
                Return node.ReplaceTokens(SpecializedCollections.SingletonEnumerable(modifier).Concat(previousToken),
                                   Function(o, n)
                                       If o = modifier Then
                                           Return Nothing
                                       ElseIf o = previousToken Then
                                           Return newPreviousToken
                                       End If

                                       throw ExceptionUtilities.UnexpectedValue(o)
                                   End Function)
            End Function

            ''' <summary>
            ''' check whether given modifiers are in right order (in sync with ModifierKindsInOrder list)
            ''' </summary>
            Private Shared Function AreModifiersInRightOrder(modifiers As SyntaxTokenList) As Boolean
                Dim startIndex = 0
                For Each modifier In modifiers
                    Dim newIndex = s_modifierKindsInOrder.IndexOf(modifier.Kind, startIndex)
                    If newIndex = 0 AndAlso startIndex = 0 Then
                        ' very first search with matching the very first modifier in the modifier orders
                        startIndex = newIndex + 1
                    ElseIf startIndex < newIndex Then
                        ' new one is after the previous one in order
                        startIndex = newIndex + 1
                    Else
                        ' oops, in wrong order
                        Return False
                    End If
                Next

                Return True
            End Function
        End Class
    End Class
End Namespace
