﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Formatting
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Formatting.Rules
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.EditorUtilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Formatting
    <[UseExportProvider]>
    Public Class VisualBasicFormatterTestBase
        Inherits CoreFormatterTestsBase

        Protected Overrides Function GetLanguageName() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function ParseCompilationUnit(expected As String) As SyntaxNode
            Return SyntaxFactory.ParseCompilationUnit(expected)
        End Function

        Protected Async Function AssertFormatSpanAsync(content As String, expected As String, Optional baseIndentation As Integer? = Nothing, Optional span As TextSpan = Nothing) As Tasks.Task
            Using workspace = TestWorkspace.CreateVisualBasic(content)
                Dim hostdoc = workspace.Documents.First()

                ' get original buffer
                Dim buffer = workspace.Documents.First().GetTextBuffer()

                ' create new buffer with cloned content
                Dim clonedBuffer = EditorFactory.CreateBuffer(workspace.ExportProvider, buffer.ContentType, buffer.CurrentSnapshot.GetText())

                Dim document = workspace.CurrentSolution.GetDocument(hostdoc.Id)
                Dim syntaxTree = Await document.GetSyntaxTreeAsync()

                ' Add Base IndentationRule that we had just set up.
                Dim formattingRuleProvider = workspace.Services.GetService(Of IHostDependentFormattingRuleFactoryService)()
                If baseIndentation.HasValue Then
                    Dim factory = TryCast(formattingRuleProvider, TestFormattingRuleFactoryServiceFactory.Factory)
                    factory.BaseIndentation = baseIndentation.Value
                    factory.TextSpan = span
                End If

                Dim rules = formattingRuleProvider.CreateRule(document, 0).Concat(Formatter.GetDefaultFormattingRules(document))

                Dim changes = Formatter.GetFormattedTextChanges(
                    Await syntaxTree.GetRootAsync(),
                    workspace.Documents.First(Function(d) d.SelectedSpans.Any()).SelectedSpans,
                    workspace, Await document.GetOptionsAsync(CancellationToken.None), rules, CancellationToken.None)
                AssertResult(expected, clonedBuffer, changes)
            End Using
        End Function

        Private Shared Sub AssertResult(expected As String, buffer As ITextBuffer, changes As IList(Of TextChange))
            Using edit = buffer.CreateEdit()
                For Each change In changes
                    edit.Replace(change.Span.ToSpan(), change.NewText)
                Next

                edit.Apply()
            End Using

            Dim actual = buffer.CurrentSnapshot.GetText()

            Assert.Equal(expected, actual)
        End Sub
    End Class
End Namespace
