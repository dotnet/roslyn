' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Shared.Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class XmlExpressionStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of XmlNodeSyntax)

        Protected Overrides Sub CollectBlockSpans(previousToken As SyntaxToken,
                                                  xmlExpression As XmlNodeSyntax,
                                                  ByRef spans As TemporaryArray(Of BlockSpan),
                                                  options As BlockStructureOptions,
                                                  cancellationToken As CancellationToken)
            ' If this XML expression is inside structured trivia (i.e. an XML doc comment), don't outline.
            If xmlExpression.HasAncestor(Of DocumentationCommentTriviaSyntax)() Then
                Return
            End If

            Dim span = xmlExpression.Span
            Dim syntaxTree = xmlExpression.SyntaxTree
            Dim line = syntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(span.Start)
            Dim lineText = line.ToString().Substring(span.Start - line.Start)
            Dim bannerText = lineText & SpaceEllipsis

            spans.AddIfNotNull(CreateBlockSpan(
                span, span, bannerText, autoCollapse:=False,
                type:=BlockTypes.Expression,
                isCollapsible:=True, isDefaultCollapsed:=False))
        End Sub
    End Class
End Namespace
