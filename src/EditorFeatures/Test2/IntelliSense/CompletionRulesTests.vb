' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Text

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    ' These tests adapted from David Kean's table at
    ' https://github.com/dotnet/roslyn/issues/5524
    <[UseExportProvider]>
    Public Class CompletionRulesTests
        <Fact>
        Public Sub TestMatchLowerCaseEnglishI()
            Dim wordsToMatch = {"[|i|]ndex", "[|I|]ndex", "[|i|]şte", "[|İ|]şte"}
            Dim wordsToNotMatch = {"ırak"}

            TestMatches("i", wordsToMatch)
            TestNotMatches("i", wordsToNotMatch)
        End Sub

        <Fact>
        Public Sub TestMatchDottedUpperTurkishI()
            Dim wordsToMatch = {"[|i|]ndex", "[|i|]şte", "[|İ|]şte"}
            Dim wordsToNotMatch = {"ırak", "Irak", "Index"}

            TestMatches("İ", wordsToMatch)
            TestNotMatches("İ", wordsToNotMatch)
        End Sub

        <Fact>
        Public Sub TestMatchNonDottedLowerTurkishI()
            Dim wordsToMatch = {"[|ı|]rak", "[|I|]rak"}
            Dim wordsToNotMatch = {"index", "işte", "İşte"}

            TestMatches("ı", wordsToMatch)
            TestNotMatches("ı", wordsToNotMatch)
        End Sub

        <Fact>
        Public Sub TestMatchEnglishUpperI()
            ' In turkish-culture "I" will not match "index".  However, we want to verify that
            ' the underlying completion helper will fallback to doing an en-us check if the
            ' tr-tr check fails, and that it properly also returns the matched spans in this case.

            Dim wordsToMatch = {"[|I|]ndex", "[|i|]ndex", "[|ı|]rak", "[|I|]rak"}
            Dim wordsToNotMatch = {"İşte"}

            TestMatches("I", wordsToMatch)
            TestNotMatches("I", wordsToNotMatch)
        End Sub

        Private Sub TestMatches(pattern As String, wordsToMatch() As String)
            Dim culture = New CultureInfo("tr-TR", useUserOverride:=False)

            Dim workspace = New TestWorkspace
            Dim helper = New CompletionHelper(isCaseSensitive:=False)

            For Each wordMarkup In wordsToMatch
                Dim word As String = Nothing
                Dim wordMatchSpan As TextSpan = Nothing
                MarkupTestFile.GetSpan(wordMarkup, word, wordMatchSpan)

                Dim item = CompletionItem.Create(word)
                Assert.True(helper.MatchesPattern(item.FilterText, pattern, culture), $"Expected item {word} does not match {pattern}")

                Dim highlightedSpans = helper.GetHighlightedSpans(item.FilterText, pattern, culture)
                Assert.NotEmpty(highlightedSpans)
                Assert.Equal(1, highlightedSpans.Length)
                Assert.Equal(wordMatchSpan, highlightedSpans(0))
            Next
        End Sub

        Private Sub TestNotMatches(pattern As String, wordsToNotMatch() As String)
            Dim culture = New CultureInfo("tr-TR", useUserOverride:=False)
            Dim workspace = New TestWorkspace
            Dim helper = New CompletionHelper(isCaseSensitive:=True)
            For Each word In wordsToNotMatch
                Dim item = CompletionItem.Create(word)
                Assert.False(helper.MatchesPattern(item.FilterText, pattern, culture), $"Unexpected item {word} matches {pattern}")

                Dim highlightedSpans = helper.GetHighlightedSpans(item.FilterText, pattern, culture)
                Assert.Empty(highlightedSpans)
            Next
        End Sub
    End Class
End Namespace
