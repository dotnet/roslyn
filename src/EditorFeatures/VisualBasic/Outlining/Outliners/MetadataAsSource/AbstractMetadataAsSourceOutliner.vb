' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining.MetadataAsSource

    Friend MustInherit Class AbstractMetadataAsSourceOutliner(Of TSyntaxNode As SyntaxNode)
        Inherits AbstractSyntaxNodeOutliner(Of TSyntaxNode)

        Protected Overrides Sub CollectOutliningSpans(node As TSyntaxNode, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            Dim startToken = node.GetFirstToken()
            Dim endToken = GetEndToken(node)

            Dim firstComment = startToken.LeadingTrivia.FirstOrNullable(Function(t) t.Kind = SyntaxKind.CommentTrivia)

            Dim startPosition = If(firstComment.HasValue,
                                   firstComment.Value.SpanStart,
                                   startToken.SpanStart)

            Dim endPosition = endToken.SpanStart

            ' TODO (tomescht): Mark the regions to be collapsed by default.
            If startPosition <> endPosition Then
                Dim hintTextEndToken = GetHintTextEndToken(node)

                spans.Add(New OutliningSpan(TextSpan.FromBounds(startPosition, endPosition),
                                            TextSpan.FromBounds(startPosition, hintTextEndToken.Span.End),
                                            Ellipsis,
                                            autoCollapse:=True))
            End If
        End Sub

        Protected Overrides Function SupportedInWorkspaceKind(kind As String) As Boolean
            Return kind = WorkspaceKind.MetadataAsSource
        End Function

        Protected Overridable Function GetHintTextEndToken(node As TSyntaxNode) As SyntaxToken
            Return node.GetLastToken()
        End Function

        Protected MustOverride Function GetEndToken(node As TSyntaxNode) As SyntaxToken

    End Class
End Namespace
