' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.Implementation.Outlining
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.Outlining
    Friend Class DisabledTextTriviaOutliner
        Inherits AbstractSyntaxTriviaOutliner

        Public Overrides Sub CollectOutliningSpans(document As Document, trivia As SyntaxTrivia, spans As List(Of OutliningSpan), cancellationToken As CancellationToken)
            If trivia.Kind = SyntaxKind.DisabledTextTrivia Then
                ' Don't include trailing line breaks in spanToCollapse
                Dim nodeSpan = trivia.Span.ToSpan()
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
