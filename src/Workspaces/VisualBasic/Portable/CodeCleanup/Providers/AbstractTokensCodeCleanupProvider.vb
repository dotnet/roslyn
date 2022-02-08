' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.CodeCleanup.Providers
    Friend MustInherit Class AbstractTokensCodeCleanupProvider
        Implements ICodeCleanupProvider

        Public MustOverride ReadOnly Property Name As String Implements ICodeCleanupProvider.Name

        Protected MustOverride Function GetRewriterAsync(
            document As Document, root As SyntaxNode, spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken) As Task(Of Rewriter)

        Public Async Function CleanupAsync(document As Document, spans As ImmutableArray(Of TextSpan), options As SyntaxFormattingOptions, cancellationToken As CancellationToken) As Task(Of Document) Implements ICodeCleanupProvider.CleanupAsync
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim rewriter As Rewriter = Await GetRewriterAsync(document, root, spans, cancellationToken).ConfigureAwait(False)
            Dim newRoot = rewriter.Visit(root)

            Return If(root Is newRoot, document, document.WithSyntaxRoot(newRoot))
        End Function

        Public Async Function CleanupAsync(root As SyntaxNode, spans As ImmutableArray(Of TextSpan), options As SyntaxFormattingOptions, services As HostWorkspaceServices, cancellationToken As CancellationToken) As Task(Of SyntaxNode) Implements ICodeCleanupProvider.CleanupAsync
            Dim rewriter As Rewriter = Await GetRewriterAsync(Nothing, root, spans, cancellationToken).ConfigureAwait(False)
            Return rewriter.Visit(root)
        End Function

        Protected MustInherit Class Rewriter
            Inherits VisualBasicSyntaxRewriter

            Protected ReadOnly _spans As SimpleIntervalTree(Of TextSpan, TextSpanIntervalIntrospector)
            Protected ReadOnly _cancellationToken As CancellationToken

            ' a global state indicating whether the visitor is visiting structured trivia or not
            Protected _underStructuredTrivia As Boolean

            Public Sub New(spans As ImmutableArray(Of TextSpan), cancellationToken As CancellationToken)
                ' need to visit structured trivia for cases such as "Region"
                MyBase.New(visitIntoStructuredTrivia:=True)

                _cancellationToken = cancellationToken
                _spans = New SimpleIntervalTree(Of TextSpan, TextSpanIntervalIntrospector)(New TextSpanIntervalIntrospector(), spans)
                _underStructuredTrivia = False
            End Sub

            Protected Function ShouldRewrite(node As SyntaxNode) As Boolean
                ' if there is no overlapping spans, no need to walk down this node
                ' use full span to include structured trivia
                Return node IsNot Nothing AndAlso
                    _spans.GetIntervalsThatOverlapWith(node.FullSpan.Start, node.FullSpan.Length).Any()
            End Function

            Public Overrides Function Visit(node As SyntaxNode) As SyntaxNode
                _cancellationToken.ThrowIfCancellationRequested()

                If Not ShouldRewrite(node) Then
                    Return node
                End If

                ' set structured trivia state before walking down tree
                Dim oldState = _underStructuredTrivia
                Try
                    _underStructuredTrivia = node.IsStructuredTrivia() OrElse oldState

                    Return MyBase.Visit(node)
                Finally
                    _underStructuredTrivia = oldState
                End Try
            End Function

            Protected Shared Function CreateToken(token As SyntaxToken, kind As SyntaxKind) As SyntaxToken
                ' create a new token with valid token text and carries over annotations attached to original token to be a good citizen 
                ' it might be replacing a token that has annotation injected by other code cleanups
                Dim leading = If(token.LeadingTrivia.Count > 0, token.LeadingTrivia, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker))
                Dim trailing = If(token.TrailingTrivia.Count > 0, token.TrailingTrivia, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker))

                Return token.CopyAnnotationsTo(SyntaxFactory.Token(leading, kind, trailing))
            End Function

            Protected Shared Function CreateIdentifierToken(token As SyntaxToken, newValueText As String) As SyntaxToken
                Debug.Assert(token.Kind = SyntaxKind.IdentifierToken)

                ' create a new token with valid token text and carries over annotations attached to original token to be a good citizen 
                ' it might be replacing a token that has annotation injected by other code cleanups
                Dim leading = If(token.LeadingTrivia.Count > 0, token.LeadingTrivia, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker))
                Dim trailing = If(token.TrailingTrivia.Count > 0, token.TrailingTrivia, SyntaxTriviaList.Create(SyntaxFactory.ElasticMarker))

                Return token.CopyAnnotationsTo(SyntaxFactory.Identifier(leading, newValueText, trailing))
            End Function
        End Class
    End Class
End Namespace
