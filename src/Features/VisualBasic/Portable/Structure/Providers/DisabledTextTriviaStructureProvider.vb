' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Structure
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.VisualBasic.Structure
    Friend Class DisabledTextTriviaStructureProvider
        Inherits AbstractSyntaxTriviaStructureProvider

        Public Overrides Sub CollectBlockSpans(document As Document, trivia As SyntaxTrivia,
                                               spans As ImmutableArray(Of BlockSpan).Builder,
                                               cancellationToken As CancellationToken)
            If trivia.Kind = SyntaxKind.DisabledTextTrivia Then
                ' Don't include trailing line breaks in spanToCollapse
                Dim nodeSpan = trivia.Span
                Dim startPos = nodeSpan.Start
                Dim endPos = startPos + trivia.ToString().TrimEnd().Length

                spans.Add(
                    CreateRegion(
                        span:=TextSpan.FromBounds(startPos, endPos),
                        bannerText:=Ellipsis,
                        autoCollapse:=True))
            End If
        End Sub
    End Class
End Namespace