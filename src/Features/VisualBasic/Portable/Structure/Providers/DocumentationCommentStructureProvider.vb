' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class DocumentationCommentStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of DocumentationCommentTriviaSyntax)

        Private Shared Function GetBannerText(documentationComment As DocumentationCommentTriviaSyntax, cancellationToken As CancellationToken) As String
            Return VisualBasicSyntaxFactsService.Instance.GetBannerText(documentationComment, cancellationToken)
        End Function

        Protected Overrides Sub CollectBlockSpans(documentationComment As DocumentationCommentTriviaSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  cancellationToken As CancellationToken)
            Dim firstCommentToken = documentationComment.ChildNodesAndTokens().FirstOrNullable()
            Dim lastCommentToken = documentationComment.ChildNodesAndTokens().LastOrNullable()
            If firstCommentToken Is Nothing Then
                Return
            End If

            ' TODO: Need to redo this when DocumentationCommentTrivia.SpanStart points to the start of the exterior trivia.
            Dim startPos = firstCommentToken.Value.FullSpan.Start

            ' The trailing newline is included in DocumentationCommentTrivia, so we need to strip it.
            Dim endPos = lastCommentToken.Value.SpanStart + lastCommentToken.Value.ToString().TrimEnd().Length

            Dim fullSpan = TextSpan.FromBounds(startPos, endPos)

            spans.AddIfNotNull(CreateBlockSpan(
                fullSpan, fullSpan, GetBannerText(documentationComment, cancellationToken),
                autoCollapse:=True, type:=BlockTypes.Comment,
                isCollapsible:=True, isDefaultCollapsed:=False))
        End Sub
    End Class
End Namespace