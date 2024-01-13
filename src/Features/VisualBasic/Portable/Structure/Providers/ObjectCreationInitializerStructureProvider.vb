' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure

    Friend Class ObjectCreationInitializerStructureProvider
        Inherits AbstractSyntaxNodeStructureProvider(Of ObjectCreationInitializerSyntax)

        Protected Overrides Sub CollectBlockSpans(previousToken As SyntaxToken,
                                                  node As ObjectCreationInitializerSyntax,
                                                  ByRef spans As TemporaryArray(Of BlockSpan),
                                                  options As BlockStructureOptions,
                                                  cancellationToken As CancellationToken)

            ' ObjectCreationInitializerSyntax is either "With { ... }" or "From { ... }"
            ' Parent Is something Like
            '
            '      New Dictionary(Of int, string) From {
            '          ...
            '      }
            '
            ' The collapsed textspan should be from the   )   to the   }
            '
            ' However, the hint span should be the entire object creation.
            spans.Add(New BlockSpan(
                isCollapsible:=True,
                textSpan:=TextSpan.FromBounds(previousToken.Span.End, node.Span.End),
                hintSpan:=node.Parent.Span,
                type:=BlockTypes.Expression))
        End Sub
    End Class
End Namespace
