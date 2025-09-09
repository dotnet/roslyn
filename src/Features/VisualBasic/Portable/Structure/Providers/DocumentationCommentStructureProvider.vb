' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class DocumentationCommentStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of DocumentationCommentTriviaSyntax)

        Protected Overrides Sub CollectBlockSpans(previousToken As SyntaxToken,
                                                  documentationComment As DocumentationCommentTriviaSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As BlockStructureOptions,
                                                  cancellationToken As CancellationToken)
            Dim firstCommentToken = documentationComment.ChildNodesAndTokens().FirstOrNull()
            Dim lastCommentToken = documentationComment.ChildNodesAndTokens().LastOrNull()
            If firstCommentToken Is Nothing Then
                Return
            End If

            ' TODO: Need to redo this when DocumentationCommentTrivia.SpanStart points to the start of the exterior trivia.
            Dim startPos = firstCommentToken.Value.FullSpan.Start

            ' The trailing newline is included in DocumentationCommentTrivia, so we need to strip it.
            Dim endPos = lastCommentToken.Value.SpanStart + lastCommentToken.Value.ToString().TrimEnd().Length

            Dim fullSpan = TextSpan.FromBounds(startPos, endPos)

            Dim maxBannerLength = options.MaximumBannerLength
            Dim bannerText = VisualBasicFileBannerFacts.Instance.GetBannerText(
                documentationComment, maxBannerLength, cancellationToken)

            spans.AddIfNotNull(CreateBlockSpan(
                fullSpan, fullSpan, bannerText,
                autoCollapse:=True, type:=BlockTypes.Comment,
                isCollapsible:=True, isDefaultCollapsed:=False))
        End Sub
    End Class
End Namespace
