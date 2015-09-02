' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
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

        Private Function ProduceSquiggles(ParamArray lines As String()) As IEnumerable(Of ITagSpan(Of IErrorTag))
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(lines)
                Return GetErrorSpans(workspace)
            End Using
        End Function

        Private Function ProduceSquiggles(analyzerMap As Dictionary(Of String, DiagnosticAnalyzer()), ParamArray lines As String()) As IEnumerable(Of ITagSpan(Of IErrorTag))
            Using workspace = VisualBasicWorkspaceFactory.CreateWorkspaceFromLines(lines)
                Return GetErrorSpans(workspace, analyzerMap)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub ErrorTagGeneratedForSimpleError()
            ' Make sure we have errors from the tree
            Dim spans = ProduceSquiggles("^")
            Assert.Equal(1, spans.Count())

            Dim firstSpan = spans.First()
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub ArgOutOfRangeExceptionBug_904382()
            Dim spans = ProduceSquiggles("Class C1", "Sub Foo(", "End Class")

            'If the following line does not throw an exception then the test passes.
            Dim count = spans.Count
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub ErrorDoesNotCrashPastEOF()
            Dim spans = ProduceSquiggles("Class C1",
                                         "    Sub Foo()",
                                         "        Dim x = <xml>",
                                         "    End Sub",
                                         "End Class")
            Assert.Equal(5, spans.Count())
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub SemanticError()
            Dim spans = ProduceSquiggles(
"Class C1",
"    Sub Foo(b as Bar)",
"    End Sub",
"End Class")
            Assert.Equal(1, spans.Count())

            Dim firstSpan = spans.First()
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType)
            Assert.Contains("Bar", DirectCast(firstSpan.Tag.ToolTipContent, String), StringComparison.Ordinal)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Sub SuggestionTagsForUnnecessaryCode()

            Dim analyzerMap = New Dictionary(Of String, DiagnosticAnalyzer())
            analyzerMap.Add(LanguageNames.VisualBasic,
                    {
                        New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer(),
                        New VisualBasicRemoveUnnecessaryImportsDiagnosticAnalyzer()
                    })

            Dim spans = ProduceSquiggles(analyzerMap,
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
End Class").OrderBy(Function(s) s.Span.Span.Start).ToImmutableArray()

            Assert.Equal(2, spans.Length)
            Dim first = spans(0)
            Dim second = spans(1)

            Assert.Equal(PredefinedErrorTypeNames.Suggestion, first.Tag.ErrorType)
            Assert.Equal(VBFeaturesResources.RemoveUnnecessaryImportsDiagnosticTitle, first.Tag.ToolTipContent)
            Assert.Equal(79, first.Span.Start)
            Assert.Equal(83, first.Span.Length)

            Assert.Equal(PredefinedErrorTypeNames.Suggestion, second.Tag.ErrorType)
            Assert.Equal(WorkspacesResources.NameCanBeSimplified, second.Tag.ToolTipContent)
            Assert.Equal(221, second.Span.Start)
            Assert.Equal(5, second.Span.Length)
        End Sub
    End Class
End Namespace
