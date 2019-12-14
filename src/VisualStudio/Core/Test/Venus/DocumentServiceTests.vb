' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Extensions
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus
Imports Microsoft.VisualStudio.Text
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus

    <UseExportProvider>
    Public Class DocumentServiceTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestSpanMappingService() As Task
            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C { $$ }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument("class projected { {|Document:|} }", {subjectDocument})

                Dim service = New ContainedDocument.DocumentServiceProvider(projectedDocument.GetTextBuffer())
                Dim spanMapper = service.GetService(Of ISpanMappingService)

                Dim position = subjectDocument.CursorPosition.Value
                Dim spans = Await spanMapper.MapSpansAsync(workspace.CurrentSolution.GetDocument(subjectDocument.Id), {New TextSpan(position, length:=0)}, CancellationToken.None)

                Assert.Equal(1, spans.Count)
                Assert.Equal(projectedDocument.CursorPosition.Value, spans(0).Span.Start)
                Assert.Equal(subjectDocument.FilePath, spans(0).FilePath)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestSpanMappingService_InvalidPosition() As Task
            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C$$ { }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument("class projected { {|Document:|} }", {subjectDocument})

                Dim service = New ContainedDocument.DocumentServiceProvider(projectedDocument.GetTextBuffer())
                Dim spanMapper = service.GetService(Of ISpanMappingService)

                Dim position = subjectDocument.CursorPosition.Value
                Dim spans = Await spanMapper.MapSpansAsync(workspace.CurrentSolution.GetDocument(subjectDocument.Id), {New TextSpan(position, length:=0), New TextSpan(start:=1, length:=0), New TextSpan(position + 1, length:=0)}, CancellationToken.None)

                ' order of input is maintained
                Assert.Equal(3, spans.Count)

                Assert.Equal(projectedDocument.CursorPosition.Value, spans(0).Span.Start)
                Assert.Equal(subjectDocument.FilePath, spans(0).FilePath)

                ' but return default for invalid span
                Assert.True(spans(1).IsDefault)

                Assert.Equal(projectedDocument.CursorPosition.Value + 1, spans(2).Span.Start)
                Assert.Equal(subjectDocument.FilePath, spans(2).FilePath)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestDocumentOperation()
            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C$$ { }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument("class projected { {|Document:|} }", {subjectDocument})

                Dim service = New ContainedDocument.DocumentServiceProvider(projectedDocument.GetTextBuffer())
                Dim documentOperations = service.GetService(Of IDocumentOperationService)

                ' contained document supports both document modification and diagnostics
                ' soon, contained document will be only used to support old venus and razor but not new razor
                ' which will use their own implementation of these services
                Assert.True(documentOperations.CanApplyChange)
                Assert.True(documentOperations.SupportDiagnostics)

                Dim documentProperties = service.GetService(Of DocumentPropertiesService)
                Assert.True(documentProperties.DesignTimeOnly)
            End Using
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestExcerptService_SingleLine() As Task
            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C { }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument("class projected { {|Document:|} }", {subjectDocument})

                Dim service = New ContainedDocument.DocumentServiceProvider(projectedDocument.GetTextBuffer())
                Dim excerptService = service.GetService(Of IDocumentExcerptService)

                Dim result = Await excerptService.TryExcerptAsync(workspace.CurrentSolution.GetDocument(subjectDocument.Id), GetNamedSpan(subjectDocument), ExcerptMode.SingleLine, CancellationToken.None)
                Assert.True(result.HasValue)

                Dim content = result.Value.Content.ToString()
                Assert.Equal(projectedDocument.GetTextBuffer().CurrentSnapshot.GetText(), content)

                Dim expcetedFormatted = {FormattedClassifications.Text("class projected { "),
                                         Keyword("class"),
                                         FormattedClassifications.Text(" "),
                                         [Class]("C"),
                                         FormattedClassifications.Text(" "),
                                         Punctuation.OpenCurly,
                                         FormattedClassifications.Text(" "),
                                         Punctuation.CloseCurly,
                                         FormattedClassifications.Text(" }")}

                Dim actualFormatted = result.Value.ClassifiedSpans.Select(Function(a) New FormattedClassification(content.Substring(a.TextSpan.Start, a.TextSpan.Length), a.ClassificationType))
                Assert.Equal(expcetedFormatted, actualFormatted)

                Assert.Equal(GetNamedSpan(projectedDocument), result.Value.MappedSpan)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestExcerptService_Tooltip_Singleline() As Task
            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C { }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument("class projected { {|Document:|} }", {subjectDocument})

                Dim service = New ContainedDocument.DocumentServiceProvider(projectedDocument.GetTextBuffer())
                Dim excerptService = service.GetService(Of IDocumentExcerptService)

                ' make sure single line buffer doesn't throw on ExcerptMode.Tooltip
                Dim result = Await excerptService.TryExcerptAsync(workspace.CurrentSolution.GetDocument(subjectDocument.Id), GetNamedSpan(subjectDocument), ExcerptMode.Tooltip, CancellationToken.None)
                Assert.True(result.HasValue)

                Dim content = result.Value.Content.ToString()
                Assert.Equal(projectedDocument.GetTextBuffer().CurrentSnapshot.GetText(), content)

                Dim expcetedFormatted = {FormattedClassifications.Text("class projected { "),
                                         Keyword("class"),
                                         FormattedClassifications.Text(" "),
                                         [Class]("C"),
                                         FormattedClassifications.Text(" "),
                                         Punctuation.OpenCurly,
                                         FormattedClassifications.Text(" "),
                                         Punctuation.CloseCurly,
                                         FormattedClassifications.Text(" }")}

                Dim actualFormatted = result.Value.ClassifiedSpans.Select(Function(a) New FormattedClassification(content.Substring(a.TextSpan.Start, a.TextSpan.Length), a.ClassificationType))
                Assert.Equal(expcetedFormatted, actualFormatted)

                Assert.Equal(GetNamedSpan(projectedDocument), result.Value.MappedSpan)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestExcerptService_Tooltip_MultiLines() As Task
            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C { }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim projectedContent = <Code>class projected 
{|Content:                        {|WithoutLeadingWhitespace:{ 
                            private void SurfaceMethod() { }

                            |}{|Document:|}{|LastText:

                            private void SurfaceMethod2() { }
                        }|}|}</Code>

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument(projectedContent.NormalizedValue(), {subjectDocument})

                Dim service = New ContainedDocument.DocumentServiceProvider(projectedDocument.GetTextBuffer())
                Dim excerptService = service.GetService(Of IDocumentExcerptService)

                Dim result = Await excerptService.TryExcerptAsync(workspace.CurrentSolution.GetDocument(subjectDocument.Id), GetNamedSpan(subjectDocument), ExcerptMode.Tooltip, CancellationToken.None)
                Assert.True(result.HasValue)

                Dim content = result.Value.Content.ToString()

                ' calculate expected span
                Dim contentSpan = GetNamedSpan(projectedDocument, "Content")
                Dim expectedContent = projectedDocument.GetTextBuffer().CurrentSnapshot.GetText(contentSpan.ToSpan())
                Assert.Equal(expectedContent, content)

                Dim firstText = projectedDocument.GetTextBuffer().CurrentSnapshot.GetText(GetNamedSpan(projectedDocument, "WithoutLeadingWhitespace").ToSpan())
                Dim documentSpan = GetNamedSpan(projectedDocument, "Document").ToSpan()
                Dim lastText = projectedDocument.GetTextBuffer().CurrentSnapshot.GetText(Span.FromBounds(documentSpan.End, projectedDocument.GetTextBuffer().CurrentSnapshot.Length))

                Dim expcetedFormatted = {FormattedClassifications.Text(firstText),
                                         Keyword("class"),
                                         FormattedClassifications.Text(" "),
                                         [Class]("C"),
                                         FormattedClassifications.Text(" "),
                                         Punctuation.OpenCurly,
                                         FormattedClassifications.Text(" "),
                                         Punctuation.CloseCurly,
                                         FormattedClassifications.Text(lastText)}

                Dim actualFormatted = result.Value.ClassifiedSpans.Select(Function(a) New FormattedClassification(content.Substring(a.TextSpan.Start, a.TextSpan.Length), a.ClassificationType))
                Assert.Equal(expcetedFormatted, actualFormatted)

                ' ExcerptResult.MappedSpan is relative to ExcerptResult.Content.
                ' recalculate expected span relative to the content span
                Assert.Equal(New TextSpan(documentSpan.Start - contentSpan.Start, documentSpan.Length), result.Value.MappedSpan)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestExcerptService_LeadingWhiteSpace() As Task
            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:            class C { }         |} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim projectedContent = <Code>class projected 
{ 
    {|Document:|}
}</Code>

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument = workspace.CreateProjectionBufferDocument(projectedContent.NormalizedValue(), {subjectDocument})

                Dim service = New ContainedDocument.DocumentServiceProvider(projectedDocument.GetTextBuffer())
                Dim excerptService = service.GetService(Of IDocumentExcerptService)

                Dim result = Await excerptService.TryExcerptAsync(workspace.CurrentSolution.GetDocument(subjectDocument.Id), GetNamedSpan(subjectDocument), ExcerptMode.SingleLine, CancellationToken.None)
                Assert.True(result.HasValue)

                Dim content = result.Value.Content.ToString()

                ' confirm leading whitespace is removed
                Dim expcetedFormatted = {Keyword("class"),
                                         FormattedClassifications.Text(" "),
                                         [Class]("C"),
                                         FormattedClassifications.Text(" "),
                                         Punctuation.OpenCurly,
                                         FormattedClassifications.Text(" "),
                                         Punctuation.CloseCurly,
                                         FormattedClassifications.Text("         ")}

                Dim actualFormatted = result.Value.ClassifiedSpans.Select(Function(a) New FormattedClassification(content.Substring(a.TextSpan.Start, a.TextSpan.Length), a.ClassificationType))
                Assert.Equal(expcetedFormatted, actualFormatted)
            End Using
        End Function

        Private Shared Function GetNamedSpan(document As TestHostDocument, Optional spanName As String = "Document") As TextSpan
            Return document.AnnotatedSpans(spanName).First()
        End Function
    End Class
End Namespace
