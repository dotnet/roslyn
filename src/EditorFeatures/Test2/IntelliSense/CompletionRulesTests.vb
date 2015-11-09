Imports System.Globalization
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.CSharp.Completion

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    ' These tests adapted from David Kean's table at
    ' https://github.com/dotnet/roslyn/issues/5524
    Public Class CompletionRulesTests
        <Fact>
        Public Sub TestMatchLowerCaseEnglishI()
            Dim wordsToMatch = {"index", "Index", "işte", "İşte"}
            Dim wordsToNotMatch = {"ırak"}

            TestMatches("i", wordsToMatch)
            TestNotMatches("i", wordsToNotMatch)
        End Sub

        <Fact>
        Public Sub TestMatchDottedUpperTurkishI()
            Dim wordsToMatch = {"index", "işte", "İşte"}
            Dim wordsToNotMatch = {"ırak", "Irak", "Index"}

            TestMatches("İ", wordsToMatch)
            TestNotMatches("İ", wordsToNotMatch)
        End Sub

        <Fact>
        Public Sub TestMatchNonDottedLowerTurkishI()
            Dim wordsToMatch = {"ırak", "Irak"}
            Dim wordsToNotMatch = {"index", "işte", "İşte"}

            TestMatches("ı", wordsToMatch)
            TestNotMatches("ı", wordsToNotMatch)
        End Sub

        <Fact>
        Public Sub TestMatchEnglishUpperI()
            Dim wordsToMatch = {"Index", "index", "ırak", "Irak"}
            Dim wordsToNotMatch = {"İşte"}

            TestMatches("I", wordsToMatch)
            TestNotMatches("I", wordsToNotMatch)
        End Sub

        Private Sub TestMatches(v As String, wordsToMatch() As String)
            Using New CultureContext("tr-TR")
                Dim rules = New CompletionRules(New CSharpCompletionService())
                For Each word In wordsToMatch
                    Dim item = New CompletionItem(Nothing, word, Nothing)
                    Assert.True(rules.MatchesFilterText(item, v, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(), CompletionFilterReason.TypeChar), $"Expected item {word} does not match {v}")
                Next
            End Using
        End Sub

        Private Sub TestNotMatches(v As String, wordsToNotMatch() As String)
            Using New CultureContext("tr-TR")
                Dim rules = New CompletionRules(New CSharpCompletionService())
                For Each word In wordsToNotMatch
                    Dim item = New CompletionItem(Nothing, word, Nothing)
                    Assert.False(rules.MatchesFilterText(item, v, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(), CompletionFilterReason.TypeChar), $"Unexpected item {word} matches {v}")
                Next
            End Using

        End Sub
    End Class
End Namespace
