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
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
    <ExportCodeCleanupProvider(PredefinedCodeCleanupProviderNames.RemoveUnnecessaryLineContinuation, LanguageNames.VisualBasic), [Shared]>
    <ExtensionOrder(After:=PredefinedCodeCleanupProviderNames.NormalizeModifiersOrOperators, Before:=PredefinedCodeCleanupProviderNames.Format)>
    Friend Class RemoveUnnecessaryLineContinuationCodeCleanupProvider
        Implements ICodeCleanupProvider

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="https://github.com/dotnet/roslyn/issues/42820")>
        Public Sub New()
        End Sub

        Public ReadOnly Property Name As String Implements ICodeCleanupProvider.Name
            Get
                Return PredefinedCodeCleanupProviderNames.RemoveUnnecessaryLineContinuation
            End Get
        End Property

        Public Async Function CleanupAsync(document As Document, spans As ImmutableArray(Of TextSpan), options As CodeCleanupOptions, cancellationToken As CancellationToken) As Task(Of Document) Implements ICodeCleanupProvider.CleanupAsync
            ' Is this VB 9? If so, we shouldn't remove line continuations because implicit line continuation was introduced in VB 10.
            Dim parseOptions = TryCast(document.Project.ParseOptions, VisualBasicParseOptions)
            If parseOptions?.LanguageVersion <= LanguageVersion.VisualBasic9 Then
                Return document
            End If

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim newRoot = Await CleanupAsync(root, spans, options.FormattingOptions, document.Project.Solution.Workspace.Services, cancellationToken).ConfigureAwait(False)

            Return If(newRoot Is root, document, document.WithSyntaxRoot(newRoot))
        End Function

        Public Function CleanupAsync(root As SyntaxNode, spans As ImmutableArray(Of TextSpan), options As SyntaxFormattingOptions, services As HostWorkspaceServices, cancellationToken As CancellationToken) As Task(Of SyntaxNode) Implements ICodeCleanupProvider.CleanupAsync
            Return Task.FromResult(Replacer.Process(root, spans, cancellationToken))
        End Function

        Private Class Replacer
            Private ReadOnly _leading As New Dictionary(Of SyntaxToken, SyntaxTriviaList)
            Private ReadOnly _trailing As New Dictionary(Of SyntaxToken, SyntaxTriviaList)
            Private ReadOnly _tokens As New Dictionary(Of SyntaxToken, SyntaxToken)

            Private ReadOnly _root As SyntaxNode
            Private ReadOnly _spans As ImmutableArray(Of TextSpan)

            Public Shared Function Process(root As SyntaxNode, spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken) As SyntaxNode
                Dim replacer = New Replacer(root, spans)
                Return replacer.Do(cancellationToken)
            End Function

            Private Sub New(root As SyntaxNode, spans As ImmutableArray(Of TextSpan))
                _root = root
                _spans = spans
            End Sub

            Private Function [Do](cancellationToken As CancellationToken) As SyntaxNode
                _spans.Do(Sub(s) Cleanup(_root, s, cancellationToken))

                If _leading.Count = 0 AndAlso _trailing.Count = 0 AndAlso _tokens.Count = 0 Then
                    Return _root
                End If

                Dim newRoot = _root.ReplaceTokens(_leading.Keys.Concat(_trailing.Keys).Concat(_tokens.Keys).Distinct(),
                                                 Function(n, m)
                                                     Dim token = n

                                                     ' replace token if needed
                                                     If _tokens.ContainsKey(token) Then
                                                         token = _tokens(token)
                                                     End If

                                                     ' replace leading trivia if needed
                                                     Dim current = token
                                                     If _leading.ContainsKey(token) Then
                                                         Dim leading = _leading(token)
                                                         current = current.WithLeadingTrivia(leading)
                                                     End If

                                                     ' replace trailing trivia if needed
                                                     If _trailing.ContainsKey(token) Then
                                                         Dim trailing = _trailing(token)
                                                         current = current.WithTrailingTrivia(trailing)
                                                     End If

                                                     Return current
                                                 End Function)

                Return newRoot
            End Function

            Private Sub Cleanup(root As SyntaxNode, span As TextSpan, cancellationToken As CancellationToken)
                cancellationToken.ThrowIfCancellationRequested()

                ' go through all token in pair
                Dim token1 As SyntaxToken = Nothing
                For Each token2 In root.DescendantTokens(span)
                    cancellationToken.ThrowIfCancellationRequested()

                    ProcessAroundColon(token1, token2)
                    ProcessExplicitLineContinuation(token1, token2)

                    ' hold on to previous and one before previous token
                    token1 = token2
                Next
            End Sub

            Private Sub ProcessExplicitLineContinuation(token1 As SyntaxToken, token2 As SyntaxToken)
                ' we are at the very beginning
                If token1.Kind = SyntaxKind.None Then
                    Return
                End If

                ' check context
                If token1.IsLastTokenOfStatement() AndAlso Not token1.IsMissing Then
                    ' check trivia
                    If Not GetTrailingTrivia(token1).Any(SyntaxKind.LineContinuationTrivia) AndAlso
                       Not GetLeadingTrivia(token2).Any(SyntaxKind.LineContinuationTrivia) Then
                        Return
                    End If

                    ' Do not remove line continuations within single line constructs.
                    If PartOfSinglelineConstruct(token1) Then
                        Return
                    End If

                    ReplaceLineContinuationToEndOfLine(token1, token2)
                    Return
                End If

                If SyntaxFacts.AllowsTrailingImplicitLineContinuation(token1) OrElse
                   SyntaxFacts.AllowsLeadingImplicitLineContinuation(token2) Then
                    If LineDelta(token1, token2) > 1 Then
                        Return
                    End If

                    ' check trivia
                    If Not GetTrailingTrivia(token1).Any(SyntaxKind.LineContinuationTrivia) AndAlso
                       Not GetLeadingTrivia(token2).Any(SyntaxKind.LineContinuationTrivia) Then
                        Return
                    End If

                    ReplaceLineContinuationToEndOfLine(token1, token2)
                    Return
                End If
            End Sub

            Private Sub ProcessAroundColon(token1 As SyntaxToken, token2 As SyntaxToken)
                ' remove colon trivia that are not needed
                Dim colonInTrailing = GetTrailingTrivia(token1).Any(SyntaxKind.ColonTrivia)
                Dim colonInLeading = GetLeadingTrivia(token2).Any(SyntaxKind.ColonTrivia)

                If Not colonInLeading AndAlso Not colonInTrailing Then
                    Return
                End If

                ' trivia can have multiple consecutive colons, make sure we normalize them to one colon
                ReplaceTrailingTrivia(token1, RemoveTrailingColonTrivia(token1, RemoveConsecutiveColons(GetTrailingTrivia(token1))).ToSyntaxTriviaList())
                ReplaceLeadingTrivia(token2, RemoveConsecutiveColons(GetLeadingTrivia(token2)).ToSyntaxTriviaList())

                ' bug # 12899
                ' never delete colon token that belongs to label
                If IsLabelToken(token1) Then
                    RemoveColonAfterLabel(token1, token2)
                    Return
                End If

                ' check whether token2 and token3 is on same line
                Dim trailingTrivia = GetTrailingTrivia(token1)
                Dim leadingTrivia = GetLeadingTrivia(token2)

                ' colon and next token must not be on same line
                If OnSimpleLine(token1, token2) Then
                    Return
                End If

                ' if colon contains any skipped token, don't do anything
                If leadingTrivia.Any(SyntaxKind.SkippedTokensTrivia) Then
                    Return
                End If

                Dim token2Kind = GetToken(token2).Kind
                Dim dotOrExclamationInWithBlock =
                    (token2Kind = SyntaxKind.DotToken OrElse
                      token2Kind = SyntaxKind.ExclamationToken) AndAlso
                    (token2.GetAncestor(Of WithBlockSyntax)() IsNot Nothing OrElse
                     token2.GetAncestor(Of ObjectMemberInitializerSyntax)() IsNot Nothing)

                ' don't remove colon in these cases:

                ' (1)
                ' With ""
                '   Dim y = From x In "" Distinct :
                '   .ToLower()
                ' End With

                ' (2)
                ' With ""
                '   Dim y = From x In "" Distinct :
                '       !A = !B
                ' End With

                If GetToken(token1).Kind = SyntaxKind.DistinctKeyword AndAlso dotOrExclamationInWithBlock Then
                    Return
                End If

                ' check whether colon is for statements inside of single-line statement
                If (colonInTrailing AndAlso PartOfSinglelineConstruct(token1)) OrElse
                   (colonInLeading AndAlso PartOfSinglelineConstruct(token2)) Then
                    Return
                End If

                ' colon is not on the same line, remove colon trivia
                ReplaceTrailingTrivia(token1, trailingTrivia.Where(Function(t) t.Kind <> SyntaxKind.ColonTrivia).ToSyntaxTriviaList())

                If colonInLeading And dotOrExclamationInWithBlock Then
                    Return
                End If

                ReplaceLeadingTrivia(token2, leadingTrivia.Where(Function(t) t.Kind <> SyntaxKind.ColonTrivia).ToSyntaxTriviaList())
            End Sub

            Private Shared Function RemoveTrailingColonTrivia(token1 As SyntaxToken, trailing As IEnumerable(Of SyntaxTrivia)) As IEnumerable(Of SyntaxTrivia)
                If token1.Kind <> SyntaxKind.ColonToken OrElse trailing.Count = 0 Then
                    Return trailing
                End If

                If trailing(0).Kind = SyntaxKind.ColonTrivia Then
                    Return trailing.SkipWhile(Function(t) t.Kind = SyntaxKind.ColonTrivia)
                End If

                Return trailing
            End Function

            Private Function OnSimpleLine(token1 As SyntaxToken, token2 As SyntaxToken) As Boolean
                ' colon and next token must not be on same line
                Return LineDelta(token1, token2) = 0
            End Function

            Private Function LineDelta(token1 As SyntaxToken, token2 As SyntaxToken) As Integer
                Return GetTrailingTrivia(token1).ToFullString().GetNumberOfLineBreaks() +
                       GetLeadingTrivia(token2).ToFullString().GetNumberOfLineBreaks()
            End Function

            Private Shared Function IsLabelToken(token As SyntaxToken) As Boolean
                Return TypeOf token.Parent Is LabelStatementSyntax
            End Function

            Private Shared Function PartOfSinglelineConstruct(token As SyntaxToken) As Boolean
                Dim node = token.Parent
                While node IsNot Nothing
                    If TypeOf node Is SingleLineIfStatementSyntax OrElse
                       TypeOf node Is SingleLineLambdaExpressionSyntax Then
                        Return True
                    End If

                    node = node.Parent
                End While

                Return False
            End Function

            Private Shared Iterator Function RemoveConsecutiveColons(trivia As SyntaxTriviaList) As IEnumerable(Of SyntaxTrivia)
                Dim last As SyntaxTrivia = Nothing
                For Each t In trivia
                    If t.Kind <> SyntaxKind.ColonTrivia OrElse
                        last.Kind <> SyntaxKind.ColonTrivia Then
                        Yield t
                    End If

                    last = t
                Next
            End Function

            Private Sub RemoveColonAfterLabel(token1 As SyntaxToken, token2 As SyntaxToken)
                Dim colon = False

                Dim trailing = New List(Of SyntaxTrivia)
                Dim leading = New List(Of SyntaxTrivia)

                For Each trivia In GetTrailingTrivia(token1)
                    If trivia.Kind = SyntaxKind.ColonTrivia Then
                        If colon Then
                            Continue For
                        End If

                        colon = True
                    End If

                    trailing.Add(trivia)
                Next

                For Each trivia In GetLeadingTrivia(token2)
                    If trivia.Kind = SyntaxKind.ColonTrivia Then
                        If colon Then
                            Continue For
                        End If

                        colon = True
                    End If

                    leading.Add(trivia)
                Next

                ReplaceTrailingTrivia(token1, trailing.ToSyntaxTriviaList())
                ReplaceLeadingTrivia(token2, leading.ToSyntaxTriviaList())
            End Sub

            Private Sub ReplaceLineContinuationToEndOfLine(token1 As SyntaxToken, token2 As SyntaxToken)
                ' only whitespace and line continuation is valid. otherwise, we don't touch
                Dim trailingTrivia = GetTrailingTrivia(token1)
                Dim leadingTrivia = GetLeadingTrivia(token2)
                If ContainsInapplicableTrivia(trailingTrivia) OrElse ContainsInapplicableTrivia(leadingTrivia) Then
                    Return
                End If

                ReplaceTrailingTrivia(token1, ReplaceLineContinuationToEndOfLine(trailingTrivia).ToSyntaxTriviaList())
                ReplaceLeadingTrivia(token2, ReplaceLineContinuationToEndOfLine(leadingTrivia).ToSyntaxTriviaList())
            End Sub

            Private Shared Function ContainsInapplicableTrivia(trivia As SyntaxTriviaList) As Boolean
                Return trivia.Any(Function(t) t.Kind <> SyntaxKind.WhitespaceTrivia AndAlso
                                              t.Kind <> SyntaxKind.LineContinuationTrivia AndAlso
                                              t.Kind <> SyntaxKind.EndOfLineTrivia)
            End Function

            Private Shared Function ReplaceLineContinuationToEndOfLine(trivia As IEnumerable(Of SyntaxTrivia)) As IEnumerable(Of SyntaxTrivia)
                Return trivia.Where(Function(t) t.Kind <> SyntaxKind.LineContinuationTrivia)
            End Function

            Private Function GetLeadingTrivia(token As SyntaxToken) As SyntaxTriviaList
                Return GetTriviaList(token, _leading, token.LeadingTrivia)
            End Function

            Private Function GetTrailingTrivia(token As SyntaxToken) As SyntaxTriviaList
                Return GetTriviaList(token, _trailing, token.TrailingTrivia)
            End Function

            Private Function GetTriviaList(token As SyntaxToken,
                                           map As Dictionary(Of SyntaxToken, SyntaxTriviaList),
                                           defaultTrivia As SyntaxTriviaList) As SyntaxTriviaList
                Dim value As SyntaxTriviaList = Nothing
                If map.TryGetValue(GetToken(token), value) Then
                    Return value
                End If

                Return defaultTrivia
            End Function

            Private Sub ReplaceTrailingTrivia(token As SyntaxToken, trivia As SyntaxTriviaList)
                ReplaceTrivia(token, _trailing, trivia)
            End Sub

            Private Sub ReplaceLeadingTrivia(token As SyntaxToken, trivia As SyntaxTriviaList)
                ReplaceTrivia(token, _leading, trivia)
            End Sub

            Private Sub ReplaceTrivia(token As SyntaxToken,
                                      map As Dictionary(Of SyntaxToken, SyntaxTriviaList),
                                      trivia As SyntaxTriviaList)
                map(GetToken(token)) = trivia
            End Sub

            Private Function GetToken(token As SyntaxToken) As SyntaxToken
                Dim value As SyntaxToken = Nothing
                If _tokens.TryGetValue(token, value) Then
                    Return value
                End If

                Return token
            End Function
        End Class
    End Class
End Namespace
