﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure

    Friend Class CollectionInitializerStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of CollectionInitializerSyntax)

        Protected Overrides Sub CollectBlockSpans(node As CollectionInitializerSyntax,
                                                  spans As ArrayBuilder(Of BlockSpan),
                                                  options As OptionSet,
                                                  CancellationToken As CancellationToken)

            ' We don't want to make a span for the "{ ... }" in "From { ... }".  The latter
            ' is already handled by ObjectCreationInitializerStructureProvider
            If TypeOf node.Parent IsNot ObjectCollectionInitializerSyntax Then
                ' We have something Like:
                '
                '      New Dictionary(Of int, string) From  {
                '          ...
                '          {
                '              ...
                '          },
                '          ...
                '      }
                '
                '  In this case, we want to collapse the "{ ... }," (including the comma).

                Dim nextToken = node.CloseBraceToken.GetNextToken()
                Dim endPos = If(nextToken.Kind() = SyntaxKind.CommaToken,
                                nextToken.Span.End,
                                node.Span.End)

                spans.Add(New BlockSpan(
                    isCollapsible:=True,
                    textSpan:=TextSpan.FromBounds(node.SpanStart, endPos),
                    hintSpan:=TextSpan.FromBounds(node.SpanStart, endPos),
                    type:=BlockTypes.Expression))
            End If
        End Sub
    End Class
End Namespace