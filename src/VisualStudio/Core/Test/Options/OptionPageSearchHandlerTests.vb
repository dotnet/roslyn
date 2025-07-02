' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Windows
Imports System.Windows.Controls
Imports System.Windows.Documents
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Options
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Public Class OptionPageSearchHandlerTests
        <WpfFact>
        Public Sub SearchStringNotFound()
            TestSearchString("Show _marshmallows", "frogs")
        End Sub

        <WpfFact>
        Public Sub Middle()
            TestSearchString("Show _marshmallows", "mallow",
                Normal("Show "),
                Underline("m"),
                Normal("arsh"),
                Highlight("mallow"),
                Normal("s"))
        End Sub

        <WpfFact>
        Public Sub OverlapUnderline()
            TestSearchString("Show mar_shmallows", "rsh",
                Normal("Show ma"),
                Highlight("r"),
                Highlight(Underline("s")),
                Highlight("h"),
                Normal("mallows"))
        End Sub

        <WpfFact>
        Public Sub CaseInsensitive()
            TestSearchString("Show mar_shmallows", "show",
                Highlight("Show"),
                Normal(" mar"),
                Underline("s"),
                Normal("hmallows"))
        End Sub

        <WpfFact>
        Public Sub HighlightAtStart()
            TestSearchString("Show mar_shmallows", "Show",
                Highlight("Show"),
                Normal(" mar"),
                Underline("s"),
                Normal("hmallows"))
        End Sub

        <WpfFact>
        Public Sub HighlightAtStart_NoUnderline()
            TestSearchString("Show marshmallows", "Show",
                Highlight("Show"),
                Normal(" marshmallows"))
        End Sub

        <WpfFact>
        Public Sub HighlightAtEnd()
            TestSearchString("Show mar_shmallows", "lows",
                Normal("Show mar"),
                Underline("s"),
                Normal("hmal"),
                Highlight("lows"))
        End Sub

        <WpfFact>
        Public Sub HighlightAtEnd_NoUnderline()
            TestSearchString("Show marshmallows", "lows",
                Normal("Show marshmal"),
                Highlight("lows"))
        End Sub

        <WpfFact>
        Public Sub UnderlineAfterEndOfHighlight()
            TestSearchString("Show marshma_llows", "arshma",
                Normal("Show m"),
                Highlight("arshma"),
                Underline("l"),
                Normal("lows"))
        End Sub

        <WpfFact>
        Public Sub UnderlineAtEndOfHighlight()
            TestSearchString("Show marshma_llows", "arshmal",
                Normal("Show m"),
                Highlight("arshma"),
                Highlight(Underline("l")),
                Normal("lows"))
        End Sub

        <WpfFact>
        Public Sub UnderlineAtStartOfHighlight()
            TestSearchString("Show m_arshmallows", "arshmal",
                Normal("Show m"),
                Highlight(Underline("a")),
                Highlight("rshmal"),
                Normal("lows"))
        End Sub

        <WpfFact>
        Public Sub UnderlineBeforeStartOfHighlight()
            TestSearchString("Show m_arshmallows", "rshmal",
                Normal("Show m"),
                Underline("a"),
                Highlight("rshmal"),
                Normal("lows"))
        End Sub

        <WpfFact>
        Public Sub UnderlineAfterStartOfHighlight()
            TestSearchString("Show ma_rshmallows", "arshmal",
                Normal("Show m"),
                Highlight("a"),
                Highlight(Underline("r")),
                Highlight("shmal"),
                Normal("lows"))
        End Sub

        <WpfFact>
        Public Sub UnderlineAndHighlightFirstChar()
            TestSearchString("_Show marshmallows", "show",
                Highlight(Underline("S")),
                Highlight("how"),
                Normal(" marshmallows"))
        End Sub

        <WpfFact>
        Public Sub UnderlineFirstHighlightSecondChar()
            TestSearchString("_Show marshmallows", "how",
                Underline("S"),
                Highlight("how"),
                Normal(" marshmallows"))
        End Sub

        <WpfFact>
        Public Sub UnderlineLastHighlightSecondLastChar()
            TestSearchString("Show marshmallow_s", "allow",
                Normal("Show marshm"),
                Highlight("allow"),
                Underline("s"))
        End Sub

        <WpfFact>
        Public Sub UnderlineAndHighlightLastChar()
            TestSearchString("Show marshmallow_s", "allows",
                Normal("Show marshm"),
                Highlight("allow"),
                Highlight(Underline("s")))
        End Sub

        <WpfFact>
        Public Sub HighlightEntireContent()
            TestSearchString("Show marshmallows", "Show marshmallows",
                Highlight("Show marshmallows"))
        End Sub

        Private Shared Sub TestSearchString(controlContent As String, searchString As String, ParamArray runs As Run())
            Dim control = New Label With
            {
                .Content = controlContent
            }

            Dim handler = New OptionPageSearchHandler(control, controlContent)

            If runs.Length = 0 Then
                Assert.False(handler.TryHighlightSearchString(searchString))
            Else
                Assert.True(handler.TryHighlightSearchString(searchString))
                Dim textBlock = Assert.IsType(Of TextBlock)(control.Content)
                Dim actualRuns = textBlock.Inlines.OfType(Of Run).ToArray()

                Assert.Equal(runs.Length, actualRuns.Length)

                For i = 0 To runs.Length - 1
                    Assert.Equal(runs(i).Text, actualRuns(i).Text)
                    Assert.Equal(runs(i).TextDecorations, actualRuns(i).TextDecorations)
                    Assert.Equal(runs(i).Background, actualRuns(i).Background)
                Next
            End If
        End Sub

        Private Shared Function Normal(content As String) As Run
            Return New Run(content)
        End Function

        Private Shared Function Underline(content As String) As Run
            Dim run = Normal(content)
            run.TextDecorations.Add(TextDecorations.Underline)
            Return run
        End Function

        Private Shared Function Highlight(content As String) As Run
            Return Highlight(Normal(content))
        End Function

        Private Shared Function Highlight(run As Run) As Run
            run.Background = OptionPageSearchHandler.HighlightBackground
            Return run
        End Function
    End Class
End Namespace
