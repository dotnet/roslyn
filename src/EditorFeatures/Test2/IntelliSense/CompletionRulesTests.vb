Imports System.Globalization
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.CSharp.Completion

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
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
            Dim wordsToNotMatch = {"Index", "index", "işte", "İşte"}

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
            Dim rules = New CompletionRules(New CSharpCompletionService())
            Dim currentCulture = CultureInfo.CurrentCulture
            Dim trCulture = New CultureInfo("tr-TR")
            System.Threading.Thread.CurrentThread.CurrentCulture = trCulture

            Try
                For Each word In wordsToMatch
                    Dim item = New CompletionItem(Nothing, word, Nothing)
                    Assert.True(rules.MatchesFilterText(item, v, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(), CompletionFilterReason.TypeChar))
                Next
            Finally
                System.Threading.Thread.CurrentThread.CurrentCulture = currentCulture
            End Try

        End Sub

        Private Sub TestNotMatches(v As String, wordsToNotMatch() As String)
            Dim rules = New CompletionRules(New CSharpCompletionService())
            Dim currentCulture = CultureInfo.CurrentCulture
            Dim trCulture = New CultureInfo("tr-TR")
            System.Threading.Thread.CurrentThread.CurrentCulture = trCulture

            Try
                For Each word In wordsToNotMatch
                    Dim item = New CompletionItem(Nothing, word, Nothing)
                    Assert.False(rules.MatchesFilterText(item, v, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo(), CompletionFilterReason.TypeChar))
                Next
            Finally
                System.Threading.Thread.CurrentThread.CurrentCulture = currentCulture
            End Try

        End Sub
    End Class
End Namespace
