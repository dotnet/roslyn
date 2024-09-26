' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.[Shared].Collections
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class DisabledTextTriviaStructureProvider
        Inherits AbstractSyntaxTriviaStructureProvider

        Public Overrides Sub CollectBlockSpans(trivia As SyntaxTrivia,
                                               spans As ArrayBuilder(Of BlockSpan),
                                               options As BlockStructureOptions,
                                               cancellationToken As CancellationToken)
            If trivia.Kind = SyntaxKind.DisabledTextTrivia Then
                ' Don't include trailing line breaks in spanToCollapse
                Dim nodeSpan = trivia.Span
                Dim startPos = nodeSpan.Start
                Dim endPos = startPos + trivia.ToString().TrimEnd().Length

                Dim span = TextSpan.FromBounds(startPos, endPos)
                spans.AddIfNotNull(CreateBlockSpan(
                    span:=span, hintSpan:=span,
                    bannerText:=Ellipsis, autoCollapse:=True,
                    type:=BlockTypes.PreprocessorRegion,
                    isCollapsible:=True, isDefaultCollapsed:=False))
            End If
        End Sub
    End Class
End Namespace
