' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class DocumentationCommentStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of DocumentationCommentTriviaSyntax)

        Protected Overrides Sub CollectBlockSpans(documentationComment As DocumentationCommentTriviaSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
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

            Dim maxBannerLength = options.GetOption(BlockStructureOptions.MaximumBannerLength, LanguageNames.VisualBasic)
            Dim bannerText = VisualBasicSyntaxFactsService.Instance.GetBannerText(
                documentationComment, maxBannerLength, cancellationToken)

            spans.AddIfNotNull(CreateBlockSpan(
                fullSpan, fullSpan, bannerText,
                autoCollapse:=True, type:=BlockTypes.Comment,
                isCollapsible:=True, isDefaultCollapsed:=False))
        End Sub
    End Class
End Namespace
