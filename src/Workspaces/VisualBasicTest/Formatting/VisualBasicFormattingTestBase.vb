' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.UnitTests.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic.Formatting
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Formatting
    Public Class VisualBasicFormattingTestBase
        Inherits FormattingTestBase

        Private _ws As Workspace

        Protected ReadOnly Property DefaultWorkspace As Workspace
            Get
                If _ws Is Nothing Then
                    _ws = New AdhocWorkspace()
                End If

                Return _ws
            End Get
        End Property

        Protected Overrides Function ParseCompilation(text As String, parseOptions As ParseOptions) As SyntaxNode
            Return SyntaxFactory.ParseCompilationUnit(text, options:=DirectCast(parseOptions, VisualBasicParseOptions))
        End Function

        Protected Shared Function CreateMethod(ParamArray lines() As String) As String
            Dim adjustedLines = New List(Of String)()
            adjustedLines.Add("Class C")
            adjustedLines.Add("    Sub Method()")
            adjustedLines.AddRange(lines)
            adjustedLines.Add("    End Sub")
            adjustedLines.Add("End Class")

            Return StringFromLines(adjustedLines.ToArray())
        End Function

        Private Protected Function AssertFormatLf2CrLfAsync(code As String, expected As String, Optional optionSet As OptionsCollection = Nothing) As Task
            code = code.Replace(vbLf, vbCrLf)
            expected = expected.Replace(vbLf, vbCrLf)

            Return AssertFormatAsync(code, expected, changedOptionSet:=optionSet)
        End Function

        Protected Async Function AssertFormatUsingAllEntryPointsAsync(code As String, expected As String) As Task
            Using workspace = New AdhocWorkspace()

                Dim project = workspace.CurrentSolution.AddProject("Project", "Project.dll", LanguageNames.VisualBasic)
                Dim document = project.AddDocument("Document", SourceText.From(code))
                Dim syntaxTree = Await document.GetSyntaxTreeAsync()
                Dim options = VisualBasicSyntaxFormattingOptions.Default

                ' Test various entry points into the formatter

                Dim spans = New List(Of TextSpan)()
                spans.Add(syntaxTree.GetRoot().FullSpan)

                Dim changes = Formatter.GetFormattedTextChanges(Await syntaxTree.GetRootAsync(), workspace.Services, options, CancellationToken.None)
                AssertResult(expected, Await document.GetTextAsync(), changes)

                changes = Formatter.GetFormattedTextChanges(Await syntaxTree.GetRootAsync(), (Await syntaxTree.GetRootAsync()).FullSpan, workspace.Services, options, CancellationToken.None)
                AssertResult(expected, Await document.GetTextAsync(), changes)

                spans = New List(Of TextSpan)()
                spans.Add(syntaxTree.GetRoot().FullSpan)

                changes = Formatter.GetFormattedTextChanges(Await syntaxTree.GetRootAsync(), spans, workspace.Services, options, CancellationToken.None)
                AssertResult(expected, Await document.GetTextAsync(), changes)

                ' format with node and transform
                AssertFormatWithTransformation(workspace.Services, expected, syntaxTree.GetRoot(), spans, options, False)
            End Using
        End Function

        Protected Function AssertFormatSpanAsync(markupCode As String, expected As String) As Task
            Dim code As String = Nothing
            Dim spans As ImmutableArray(Of TextSpan) = Nothing
            MarkupTestFile.GetSpans(markupCode, code, spans)

            Return AssertFormatAsync(expected, code, spans)
        End Function

        Private Protected Overloads Function AssertFormatAsync(
            code As String,
            expected As String,
            Optional debugMode As Boolean = False,
            Optional changedOptionSet As OptionsCollection = Nothing,
            Optional testWithTransformation As Boolean = False,
            Optional experimental As Boolean = False) As Task
            Return AssertFormatAsync(expected, code, SpecializedCollections.SingletonEnumerable(New TextSpan(0, code.Length)), debugMode, changedOptionSet, testWithTransformation, experimental:=experimental)
        End Function

        Private Protected Overloads Function AssertFormatAsync(
            expected As String,
            code As String,
            spans As IEnumerable(Of TextSpan),
            Optional debugMode As Boolean = False,
            Optional changedOptionSet As OptionsCollection = Nothing,
            Optional testWithTransformation As Boolean = False,
            Optional experimental As Boolean = False) As Task

            Dim parseOptions = New VisualBasicParseOptions()
            If (experimental) Then
                ' There are no experimental features at this time.
                ' parseOptions = parseOptions.WithExperimentalFeatures
            End If

            Return AssertFormatAsync(expected, code, spans, LanguageNames.VisualBasic, debugMode, changedOptionSet, testWithTransformation, parseOptions)
        End Function

        Private Shared Function StringFromLines(ParamArray lines As String()) As String
            Return String.Join(Environment.NewLine, lines)
        End Function
    End Class
End Namespace
