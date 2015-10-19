' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.VisualBasic.Diagnostics.RemoveUnnecessaryImports
Imports Microsoft.VisualStudio.Text.Adornments
Imports Microsoft.VisualStudio.Text.Tagging

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Squiggles
    Public Class ErrorSquiggleProducerTests
        Inherits AbstractSquiggleProducerTests

        Private Async Function ProduceSquiggles(ParamArray lines As String()) As Task(Of IEnumerable(Of ITagSpan(Of IErrorTag)))
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(lines)
                Return Await GetErrorSpans(workspace).ConfigureAwait(True)
            End Using
        End Function

        Private Async Function ProduceSquiggles(analyzerMap As Dictionary(Of String, DiagnosticAnalyzer()), ParamArray lines As String()) As Task(Of IEnumerable(Of ITagSpan(Of IErrorTag)))
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(lines)
                Return Await GetErrorSpans(workspace, analyzerMap).ConfigureAwait(True)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Sub ErrorTagGeneratedForSimpleError()
            ' Make sure we have errors from the tree
            Dim spans = Await ProduceSquiggles("^").ConfigureAwait(True)
            Assert.Equal(1, spans.Count())

            Dim firstSpan = spans.First()
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Sub ArgOutOfRangeExceptionBug_904382()
            Dim spans = Await ProduceSquiggles("Class C1", "Sub Foo(", "End Class").ConfigureAwait(True)

            'If the following line does not throw an exception then the test passes.
            Dim count = spans.Count
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Sub ErrorDoesNotCrashPastEOF()
            Dim spans = Await ProduceSquiggles("Class C1",
                                         "    Sub Foo()",
                                         "        Dim x = <xml>",
                                         "    End Sub",
                                         "End Class").ConfigureAwait(True)
            Assert.Equal(5, spans.Count())
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Sub SemanticError()
            Dim spans = Await ProduceSquiggles(
"Class C1",
"    Sub Foo(b as Bar)",
"    End Sub",
"End Class").ConfigureAwait(True)
            Assert.Equal(1, spans.Count())

            Dim firstSpan = spans.First()
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType)
            Assert.Contains("Bar", DirectCast(firstSpan.Tag.ToolTipContent, String), StringComparison.Ordinal)
        End Sub

        <WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Sub SuggestionTagsForUnnecessaryCode()

            Dim analyzerMap = New Dictionary(Of String, DiagnosticAnalyzer())
            analyzerMap.Add(LanguageNames.VisualBasic,
                    {
                        New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer(),
                        New VisualBasicRemoveUnnecessaryImportsDiagnosticAnalyzer()
                    })

            Dim spans = (Await ProduceSquiggles(analyzerMap,
"
' System.Diagnostics is used - rest are unused.
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Class C1
    Sub Foo()
        Process.Start(GetType(Int32).ToString()) 'Int32 can be simplified.
    End Sub
End Class").ConfigureAwait(True)).OrderBy(Function(s) s.Span.Span.Start).ToImmutableArray()

            Assert.Equal(2, spans.Length)
            Dim first = spans(0)
            Dim second = spans(1)

            Assert.Equal(PredefinedErrorTypeNames.Suggestion, first.Tag.ErrorType)
            Assert.Equal(VBFeaturesResources.RemoveUnnecessaryImportsDiagnosticTitle, CType(first.Tag.ToolTipContent, String))
            Assert.Equal(Of Integer)(79, first.Span.Start)
            Assert.Equal(83, first.Span.Length)

            Assert.Equal(PredefinedErrorTypeNames.Suggestion, second.Tag.ErrorType)
            Assert.Equal(WorkspacesResources.NameCanBeSimplified, CType(second.Tag.ToolTipContent, String))
            Assert.Equal(Of Integer)(221, second.Span.Start)
            Assert.Equal(5, second.Span.Length)
        End Sub
    End Class
End Namespace
