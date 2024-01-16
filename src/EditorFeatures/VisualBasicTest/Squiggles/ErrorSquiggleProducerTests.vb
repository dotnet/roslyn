' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.CodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Squiggles
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.Test.Utilities.QuickInfo
Imports Microsoft.CodeAnalysis.VisualBasic.CodeFixes.SimplifyTypeNames
Imports Microsoft.CodeAnalysis.VisualBasic.RemoveUnnecessaryImports
Imports Microsoft.VisualStudio.Text.Adornments
Imports Microsoft.VisualStudio.Text.Tagging

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Squiggles
    <[UseExportProvider]>
    <Trait(Traits.Feature, Traits.Features.ErrorSquiggles), Trait(Traits.Feature, Traits.Features.Tagging)>
    Public Class ErrorSquiggleProducerTests

        Private Shared Async Function ProduceSquiggles(content As String) As Task(Of ImmutableArray(Of ITagSpan(Of IErrorTag)))
            Using workspace = EditorTestWorkspace.CreateVisualBasic(content)
                Return (Await TestDiagnosticTagProducer(Of DiagnosticsSquiggleTaggerProvider, IErrorTag).GetDiagnosticsAndErrorSpans(workspace)).Item2
            End Using
        End Function

        <WpfFact>
        Public Async Function ErrorTagGeneratedForSimpleError() As Task
            ' Make sure we have errors from the tree
            Dim spans = Await ProduceSquiggles("^")
            Assert.Equal(1, spans.Count())

            Dim firstSpan = spans.First()
            Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.Tag.ErrorType)
        End Function

        <WpfFact>
        Public Async Function ArgOutOfRangeExceptionBug_904382() As Task
            Dim spans = Await ProduceSquiggles(
"Class C1
Sub Goo(
End Class")

            'If the following line does not throw an exception then the test passes.
            Dim count = spans.Count
        End Function

        <WpfFact>
        Public Async Function ErrorDoesNotCrashPastEOF() As Task
            Dim spans = Await ProduceSquiggles(
"Class C1
    Sub Goo()
        Dim x = <xml>
    End Sub
End Class")
            Assert.Equal(5, spans.Count())
        End Function

        <WpfFact>
        Public Async Function SemanticError() As Task
            Using workspace = EditorTestWorkspace.CreateVisualBasic("Class C1
    Sub Goo(b as Bar)
    End Sub
End Class")

                Dim diagnosticsAndSpans = Await TestDiagnosticTagProducer(Of DiagnosticsSquiggleTaggerProvider, IErrorTag).GetDiagnosticsAndErrorSpans(workspace)
                Dim spans = diagnosticsAndSpans.Item1.Zip(diagnosticsAndSpans.Item2, Function(diagnostic, span) (diagnostic, span)).OrderBy(Function(s) s.span.Span.Span.Start).ToImmutableArray()

                Assert.Equal(1, spans.Count())

                Dim firstSpan = spans.First()
                Assert.Equal(PredefinedErrorTypeNames.SyntaxError, firstSpan.span.Tag.ErrorType)

                Dim expectedToolTip = New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "BC30002", QuickInfoHyperLink.TestAccessor.CreateNavigationAction(New Uri("https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC30002)", UriKind.Absolute)), "https://msdn.microsoft.com/query/roslyn.query?appId=roslyn&k=k(BC30002)"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, firstSpan.diagnostic.Message)))

                ToolTipAssert.EqualContent(expectedToolTip, firstSpan.span.Tag.ToolTipContent)
            End Using
        End Function

        <WpfFact>
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

            Dim analyzerMap = New Dictionary(Of String, ImmutableArray(Of DiagnosticAnalyzer)) From
            {
                {
                    LanguageNames.VisualBasic,
                    ImmutableArray.Create(Of DiagnosticAnalyzer)(
                        New VisualBasicSimplifyTypeNamesDiagnosticAnalyzer(),
                        New VisualBasicRemoveUnnecessaryImportsDiagnosticAnalyzer())
                }
            }

            Using workspace = EditorTestWorkspace.CreateVisualBasic(content, composition:=SquiggleUtilities.CompositionWithSolutionCrawler)
                Dim language = workspace.Projects.Single().Language

                workspace.GlobalOptions.SetGlobalOption(
                    CodeStyleOptions2.PreferIntrinsicPredefinedTypeKeywordInDeclaration, language, New CodeStyleOption2(Of Boolean)(value:=True, notification:=NotificationOption2.Error))

                Dim diagnosticsAndSpans = Await TestDiagnosticTagProducer(Of DiagnosticsSquiggleTaggerProvider, IErrorTag).GetDiagnosticsAndErrorSpans(workspace, analyzerMap)
                Dim spans = diagnosticsAndSpans.Item1.Zip(diagnosticsAndSpans.Item2, Function(diagnostic, span) (diagnostic, span)).OrderBy(Function(s) s.span.Span.Span.Start).ToImmutableArray()

                Assert.Equal(2, spans.Length)
                Dim first = spans(0)
                Dim second = spans(1)

                Dim expectedToolTip = New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "IDE0005", QuickInfoHyperLink.TestAccessor.CreateNavigationAction(New Uri("https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005", UriKind.Absolute)), "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0005"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, VisualBasicAnalyzersResources.Imports_statement_is_unnecessary)))

                Assert.Equal(PredefinedErrorTypeNames.Suggestion, first.span.Tag.ErrorType)
                ToolTipAssert.EqualContent(expectedToolTip, first.span.Tag.ToolTipContent)
                Assert.Equal(Of Integer)(79, first.span.Span.Start)
                Assert.Equal(83, first.span.Span.Length)

                expectedToolTip = New ContainerElement(
                    ContainerElementStyle.Wrapped,
                    New ClassifiedTextElement(
                        New ClassifiedTextRun(ClassificationTypeNames.Text, "IDE0049", QuickInfoHyperLink.TestAccessor.CreateNavigationAction(New Uri("https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0049", UriKind.Absolute)), "https://learn.microsoft.com/dotnet/fundamentals/code-analysis/style-rules/ide0049"),
                        New ClassifiedTextRun(ClassificationTypeNames.Punctuation, ":"),
                        New ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, " "),
                        New ClassifiedTextRun(ClassificationTypeNames.Text, AnalyzersResources.Name_can_be_simplified)))

                Assert.Equal(PredefinedErrorTypeNames.SyntaxError, second.span.Tag.ErrorType)
                ToolTipAssert.EqualContent(expectedToolTip, second.span.Tag.ToolTipContent)
                Assert.Equal(Of Integer)(221, second.span.Span.Start)
                Assert.Equal(5, second.span.Span.Length)
            End Using

        End Function
    End Class
End Namespace
