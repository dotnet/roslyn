' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

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
            Dim culture = New CultureInfo("tr-TR", useUserOverride:=False)

            Dim workspace = New TestWorkspace
            Dim helper = CompletionHelper.GetHelper(workspace, LanguageNames.CSharp)
            For Each word In wordsToMatch
                Dim item = CompletionItem.Create(word)
                Assert.True(helper.MatchesFilterText(item, v, culture), $"Expected item {word} does not match {v}")
            Next
        End Sub

        Private Sub TestNotMatches(v As String, wordsToNotMatch() As String)
            Dim culture = New CultureInfo("tr-TR", useUserOverride:=False)
            Dim workspace = New TestWorkspace
            Dim helper = CompletionHelper.GetHelper(workspace, LanguageNames.CSharp)
            For Each word In wordsToNotMatch
                Dim item = CompletionItem.Create(word)
                Assert.False(helper.MatchesFilterText(item, v, culture), $"Unexpected item {word} matches {v}")
            Next
        End Sub
    End Class
End Namespace