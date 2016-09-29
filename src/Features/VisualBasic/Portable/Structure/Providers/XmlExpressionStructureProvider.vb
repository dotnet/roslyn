' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class XmlExpressionStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of XmlNodeSyntax)

        Protected Overrides Sub CollectBlockSpans(xmlExpression As XmlNodeSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
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

            spans.Add(CreateRegion(
                span, bannerText, autoCollapse:=False,
                type:=BlockTypes.Xml, isCollapsible:=True))
        End Sub
    End Class
End Namespace