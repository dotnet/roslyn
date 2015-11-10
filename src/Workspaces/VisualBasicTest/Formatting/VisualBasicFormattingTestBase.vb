' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests.Formatting
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Formatting
    Public Class VisualBasicFormattingTestBase
        Inherits FormattingTestBase

        Protected Shared ReadOnly DefaultWorkspace As Workspace = New AdhocWorkspace()

        Protected Overrides Function ParseCompilation(text As String, parseOptions As ParseOptions) As SyntaxNode
            Return SyntaxFactory.ParseCompilationUnit(text, options:=DirectCast(parseOptions, VisualBasicParseOptions))
        End Function

        Protected Function CreateMethod(ParamArray lines() As String) As String
            Dim adjustedLines = New List(Of String)()
            adjustedLines.Add("Class C")
            adjustedLines.Add("    Sub Method()")
            adjustedLines.AddRange(lines)
            adjustedLines.Add("    End Sub")
            adjustedLines.Add("End Class")

            Return StringFromLines(adjustedLines.ToArray())
        End Function

        Protected Function AssertFormatLf2CrLfAsync(code As String, expected As String, Optional optionSet As Dictionary(Of OptionKey, Object) = Nothing) As Task
            code = code.Replace(vbLf, vbCrLf)
            expected = expected.Replace(vbLf, vbCrLf)

            Return AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        Protected Async Function AssertFormatUsingAllEntryPointsAsync(code As String, expected As String) As Task
            Using workspace = New AdhocWorkspace()

                Dim project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.VisualBasic)
                Dim document = project.AddDocument("Document", SourceText.From(code))
                Dim syntaxTree = document.GetSyntaxTreeAsync().Result

                ' Test various entry points into the formatter

                Dim spans = New List(Of TextSpan)()
                spans.Add(syntaxTree.GetRoot().FullSpan)

                Dim changes = Await Formatter.GetFormattedTextChangesAsync(syntaxTree.GetRoot(CancellationToken.None), workspace, cancellationToken:=CancellationToken.None).ConfigureAwait(True)
                AssertResult(expected, document.GetTextAsync().Result, changes)

                changes = Await Formatter.GetFormattedTextChangesAsync(syntaxTree.GetRoot(), syntaxTree.GetRoot(CancellationToken.None).FullSpan, workspace, cancellationToken:=CancellationToken.None).ConfigureAwait(True)
                AssertResult(expected, document.GetTextAsync().Result, changes)

                spans = New List(Of TextSpan)()
                spans.Add(syntaxTree.GetRoot().FullSpan)

                changes = Await Formatter.GetFormattedTextChangesAsync(syntaxTree.GetRoot(CancellationToken.None), spans, workspace, cancellationToken:=CancellationToken.None).ConfigureAwait(True)
                AssertResult(expected, document.GetTextAsync().Result, changes)

                ' format with node and transform
                AssertFormatWithTransformation(workspace, expected, syntaxTree.GetRoot(), spans, Nothing, False)
            End Using
        End Function

        Protected Function AssertFormatSpanAsync(markupCode As String, expected As String) As Task
            Dim code As String = Nothing
            Dim cursorPosition As Integer? = Nothing
            Dim spans As IList(Of TextSpan) = Nothing
            MarkupTestFile.GetSpans(markupCode, code, spans)

            Return AssertFormatAsync(expected, code, spans)
        End Function

        Protected Overloads Function AssertFormatAsync(
            code As String,
            expected As String,
            Optional debugMode As Boolean = False,
            Optional changedOptionSet As Dictionary(Of OptionKey, Object) = Nothing,
            Optional testWithTransformation As Boolean = False,
            Optional experimental As Boolean = False) As Task
            Return AssertFormatAsync(expected, code, SpecializedCollections.SingletonEnumerable(New TextSpan(0, code.Length)), debugMode, changedOptionSet, testWithTransformation, experimental:=experimental)
        End Function

        Protected Overloads Function AssertFormatAsync(
            expected As String,
            code As String,
            spans As IEnumerable(Of TextSpan),
            Optional debugMode As Boolean = False,
            Optional changedOptionSet As Dictionary(Of OptionKey, Object) = Nothing,
            Optional testWithTransformation As Boolean = False,
            Optional experimental As Boolean = False) As Task

            Dim parseOptions = New VisualBasicParseOptions()
            If (experimental) Then
                ' There are no experimental features at this time.
                ' parseOptions = parseOptions.WithExperimentalFeatures
            End If

            Return AssertFormatAsync(expected, code, spans, LanguageNames.VisualBasic, debugMode, changedOptionSet, testWithTransformation, parseOptions)
        End Function

        Private Function StringFromLines(ParamArray lines As String()) As String
            Return String.Join(Environment.NewLine, lines)
        End Function
    End Class
End Namespace
