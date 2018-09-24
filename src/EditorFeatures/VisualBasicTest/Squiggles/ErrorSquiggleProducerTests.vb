' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports
Imports Microsoft.VisualStudio.Text.Adornments
Imports Microsoft.VisualStudio.Text.Tagging

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Squiggles
    <[UseExportProvider]>
    Public Class ErrorSquiggleProducerTests

        Private _producer As New DiagnosticTagProducer(Of DiagnosticsSquiggleTaggerProvider)

        Private Async Function ProduceSquiggles(content As String) As Task(Of ImmutableArray(Of ITagSpan(Of IErrorTag)))
            Using workspace = TestWorkspace.CreateVisualBasic(content)
                Return (Await _producer.GetDiagnosticsAndErrorSpans(workspace)).Item2
            End Using
        End Function

        Private Async Function ProduceSquiggles(analyzerMap As Dictionary(Of String, DiagnosticAnalyzer()), content As String) As Task(Of ImmutableArray(Of ITagSpan(Of IErrorTag)))
            Using workspace = TestWorkspace.CreateVisualBasic(content)
                Return (Await _producer.GetDiagnosticsAndErrorSpans(workspace, analyzerMap)).Item2
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Function ErrorTagGeneratedForSimpleError() As Task
            ' Make sure we have errors from the tree
            Dim spans = Await ProduceSquiggles("^")
            Assert.Equal(1, spans.Count())

            Dim firstSpan = spans.First()
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType)
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Function ArgOutOfRangeExceptionBug_904382() As Task
            Dim spans = Await ProduceSquiggles(
"Class C1
Sub Goo(
End Class")

            'If the following line does not throw an exception then the test passes.
            Dim count = spans.Count
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Function ErrorDoesNotCrashPastEOF() As Task
            Dim spans = Await ProduceSquiggles(
"Class C1
    Sub Goo()
        Dim x = <xml>
    End Sub
End Class")
            Assert.Equal(5, spans.Count())
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Function SemanticError() As Task
            Dim spans = Await ProduceSquiggles(
"Class C1
    Sub Goo(b as Bar)
    End Sub
End Class")
            Assert.Equal(1, spans.Count())

            Dim firstSpan = spans.First()
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType)
            Assert.Contains("Bar", DirectCast(firstSpan.Tag.ToolTipContent, String), StringComparison.Ordinal)
        End Function

        <WpfFact(), Trait(Traits.Feature, Traits.Features.ErrorSquiggles)>
        Public Async Function CustomizableTagsForUnnecessaryCode() As Task

            Dim content = "
' System.Diagnostics is used - rest are unused.
Imports System.Diagnostics
Imports System.Collections
Imports System.Collections.Generic
Imports System.Linq

Class C1
    Sub Goo()
        Process.Start(GetType(Int32).ToString()) 'Int32 can be simplified.
    End Sub
End Class"

            Dim analyzerMap = New Dictionary(Of String, DiagnosticAnalyzer())
            analyzerMap.Add(LanguageNames.VisualBasic,
                    {
                        New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer(),
                        New VisualBasicRemoveUnnecessaryImportsDiagnosticAnalyzer()
                    })

            Using workspace = TestWorkspace.CreateVisualBasic(content)
                Dim options As New Dictionary(Of OptionKey, Object)
                Dim language = workspace.Projects.Single().Language
                Dim preferIntrinsicPredefinedTypeOption = New OptionKey(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration, language)
                Dim preferIntrinsicPredefinedTypeOptionValue = New CodeStyleOption(Of Boolean)(value:=True, notification:=NotificationOption.Error)
                options.Add(preferIntrinsicPredefinedTypeOption, preferIntrinsicPredefinedTypeOptionValue)
                workspace.ApplyOptions(options)

                Dim spans = (Await _producer.GetDiagnosticsAndErrorSpans(workspace, analyzerMap)).Item2.OrderBy(Function(s) s.Span.Span.Start).ToImmutableArray()

                Assert.Equal(2, spans.Length)
                Dim first = spans(0)
                Dim second = spans(1)

                Assert.Equal(PredefinedErrorTypeNames.Suggestion, first.Tag.ErrorType)
                Assert.Equal(VBFeaturesResources.Imports_statement_is_unnecessary, CType(first.Tag.ToolTipContent, String))
                Assert.Equal(Of Integer)(79, first.Span.Start)
                Assert.Equal(83, first.Span.Length)

                Assert.Equal(PredefinedErrorTypeNames.SyntaxError, second.Tag.ErrorType)
                Assert.Equal(WorkspacesResources.Name_can_be_simplified, CType(second.Tag.ToolTipContent, String))
                Assert.Equal(Of Integer)(221, second.Span.Start)
                Assert.Equal(5, second.Span.Length)
            End Using

        End Function
    End Class
End Namespace
