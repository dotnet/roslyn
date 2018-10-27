' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Classification.FormattedClassifications
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.Text.Shared.Extensions
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Venus
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Venus

    <UseExportProvider>
    Public Class DocumentServiceTests
        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestSpanMappingService() As Task
            Dim service = ContainedDocument.DocumentServiceProvider.Instace
            Dim spanMapper = service.GetService(Of ISpanMappingService)

            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C { $$ }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument As TestHostDocument = CreateAndAddProjectionBufferDocument(workspace, subjectDocument)

                Dim position = projectedDocument.CursorPosition.Value
                Dim spans = Await spanMapper.MapSpansAsync(workspace.CurrentSolution.GetDocument(projectedDocument.Id), {New TextSpan(position, length:=0)}, CancellationToken.None)

                Assert.Equal(1, spans.Count)
                Assert.Equal(subjectDocument.CursorPosition.Value, spans(0).Span.Start)
                Assert.Equal(projectedDocument.FilePath, spans(0).FilePath)
            End Using
        End Function

        <WpfFact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestSpanMappingService_InvalidPosition() As Task
            Dim service = ContainedDocument.DocumentServiceProvider.Instace
            Dim spanMapper = service.GetService(Of ISpanMappingService)

            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C$$ { }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument As TestHostDocument = CreateAndAddProjectionBufferDocument(workspace, subjectDocument)

                Dim position = projectedDocument.CursorPosition.Value
                Dim spans = Await spanMapper.MapSpansAsync(workspace.CurrentSolution.GetDocument(projectedDocument.Id), {New TextSpan(position, length:=0), New TextSpan(start:=1, length:=0), New TextSpan(position + 1, length:=0)}, CancellationToken.None)

                ' order of input is maintained
                Assert.Equal(3, spans.Count)

                Assert.Equal(subjectDocument.CursorPosition.Value, spans(0).Span.Start)
                Assert.Equal(projectedDocument.FilePath, spans(0).FilePath)

                ' but return default for invalid span
                Assert.True(spans(1).IsDefault)

                Assert.Equal(subjectDocument.CursorPosition.Value + 1, spans(2).Span.Start)
                Assert.Equal(projectedDocument.FilePath, spans(2).FilePath)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Sub TestDocumentOperation()
            Dim service = ContainedDocument.DocumentServiceProvider.Instace
            Dim documentOperations = service.GetService(Of IDocumentOperationService)

            ' contained document supports both document modification and diagnostics
            ' soon, contained document will be only used to support old venus and razor but not new razor
            ' which will use thier own implementation of these services
            Assert.True(documentOperations.CanApplyChange)
            Assert.True(documentOperations.SupportDiagnostics)
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestExcerptService_SingleLine() As Task
            Dim service = ContainedDocument.DocumentServiceProvider.Instace
            Dim excerptService = service.GetService(Of IDocumentExcerptService)

            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C { }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument As TestHostDocument = CreateAndAddProjectionBufferDocument(workspace, subjectDocument)

                Dim result = Await excerptService.TryExcerptAsync(workspace.CurrentSolution.GetDocument(projectedDocument.Id), GetNamedSpan(projectedDocument), ExcerptMode.SingleLine, CancellationToken.None)
                Assert.True(result.HasValue)

                Dim content = result.Value.Content.ToString()
                Assert.Equal(subjectDocument.TextBuffer.CurrentSnapshot.GetText(), content)

                Dim expcetedFormatted = {FormattedClassifications.Text("class outter { "),
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

                Assert.Equal(GetNamedSpan(subjectDocument), result.Value.MappedSpan)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestExcerptService_Tooltip_Singleline() As Task
            Dim service = ContainedDocument.DocumentServiceProvider.Instace
            Dim excerptService = service.GetService(Of IDocumentExcerptService)

            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter { {|Document:class C { }|} }</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument As TestHostDocument = CreateAndAddProjectionBufferDocument(workspace, subjectDocument)

                ' make sure single line buffer doesn't throw on ExcerptMode.Tooltip
                Dim result = Await excerptService.TryExcerptAsync(workspace.CurrentSolution.GetDocument(projectedDocument.Id), GetNamedSpan(projectedDocument), ExcerptMode.Tooltip, CancellationToken.None)
                Assert.True(result.HasValue)

                Dim content = result.Value.Content.ToString()
                Assert.Equal(subjectDocument.TextBuffer.CurrentSnapshot.GetText(), content)

                Dim expcetedFormatted = {FormattedClassifications.Text("class outter { "),
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

                Assert.Equal(GetNamedSpan(subjectDocument), result.Value.MappedSpan)
            End Using
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Venus)>
        Public Async Function TestExcerptService_Tooltip_MultiLines() As Task
            Dim service = ContainedDocument.DocumentServiceProvider.Instace
            Dim excerptService = service.GetService(Of IDocumentExcerptService)

            Using workspace = TestWorkspace.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>class outter 
{|Content:                        {|FirstText:{ 
                            private void SurfaceMethod() { }

                            |}{|Document:class C { }|}{|LastText:

                            private void SurfaceMethod2() { }
                        }|}|}</Document>
                    </Project>
                </Workspace>, exportProvider:=TestExportProvider.ExportProviderWithCSharpAndVisualBasic)

                Dim subjectDocument = workspace.Documents.Single()
                Dim projectedDocument As TestHostDocument = CreateAndAddProjectionBufferDocument(workspace, subjectDocument)

                ' make sure single line buffer doesn't throw on ExcerptMode.Tooltip
                Dim result = Await excerptService.TryExcerptAsync(workspace.CurrentSolution.GetDocument(projectedDocument.Id), GetNamedSpan(projectedDocument), ExcerptMode.Tooltip, CancellationToken.None)
                Assert.True(result.HasValue)

                Dim content = result.Value.Content.ToString()

                ' calculate expected span
                Dim contentSpan = GetNamedSpan(subjectDocument, "Content")
                Dim expectedContent = subjectDocument.TextBuffer.CurrentSnapshot.GetText(contentSpan.ToSpan())
                Assert.Equal(expectedContent, content)

                Dim firstText = subjectDocument.TextBuffer.CurrentSnapshot.GetText(GetNamedSpan(subjectDocument, "FirstText").ToSpan())
                Dim lastText = subjectDocument.TextBuffer.CurrentSnapshot.GetText(GetNamedSpan(subjectDocument, "LastText").ToSpan())
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
                Dim documentSpan = GetNamedSpan(subjectDocument, "Document")

                Assert.Equal(New TextSpan(documentSpan.Start - contentSpan.Start, documentSpan.Length), result.Value.MappedSpan)
            End Using
        End Function

        Private Shared Function GetNamedSpan(document As TestHostDocument, Optional spanName As String = "Document") As TextSpan
            Return document.AnnotatedSpans(spanName).First()
        End Function

        Private Shared Function CreateAndAddProjectionBufferDocument(workspace As TestWorkspace, subjectDocument As TestHostDocument, Optional projectedMarkup As String = Nothing) As TestHostDocument

            projectedMarkup = If(projectedMarkup Is Nothing, "class projected { {|Document:|} }", projectedMarkup)
            Dim projectedDocument = workspace.CreateProjectionBufferDocument(projectedMarkup, {subjectDocument}, LanguageNames.CSharp)

            ' add projected document to workspace and open the document to simulate how venus work

            ' first, we need to connect the orphan projected document to the project
            projectedDocument.SetProject(subjectDocument.Project)

            ' and add the project document to the TestWorkapce and roslyn solution through workspace
            workspace.Documents.Add(projectedDocument)
            workspace.OnDocumentAdded(projectedDocument.ToDocumentInfo())

            ' and open it so that it is connected to text buffer tracker
            workspace.OpenDocument(projectedDocument.Id)

            Return projectedDocument
        End Function
    End Class
End Namespace
