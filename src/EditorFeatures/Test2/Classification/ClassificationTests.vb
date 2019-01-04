' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.Implementation.Classification
Imports Microsoft.CodeAnalysis.Editor.Shared.Extensions
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Classification
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
    <[UseExportProvider]>
    Public Class ClassificationTests
        <WpfFact, WorkItem(13753, "https://github.com/dotnet/roslyn/issues/13753")>
        Public Async Function TestSemanticClassificationWithoutSyntaxTree() As Task
            Dim workspaceDefinition =
            <Workspace>
                <Project Language="NoCompilation" AssemblyName="TestAssembly" CommonReferencesPortable="true">
                    <Document>
                        var x = {}; // e.g., TypeScript code or anything else that doesn't support compilations
                    </Document>
                </Project>
            </Workspace>

            Dim exportProvider = ExportProviderCache _
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic().WithParts(GetType(NoCompilationEditorClassificationService))) _
                .CreateExportProvider()

            Using workspace = TestWorkspace.Create(workspaceDefinition, exportProvider:=exportProvider)
                Dim listenerProvider = exportProvider.GetExportedValue(Of IAsynchronousOperationListenerProvider)

                Dim provider = New SemanticClassificationViewTaggerProvider(
                    workspace.ExportProvider.GetExportedValue(Of IThreadingContext),
                    workspace.GetService(Of IForegroundNotificationService),
                    workspace.GetService(Of ISemanticChangeNotificationService),
                    workspace.GetService(Of ClassificationTypeMap),
                    listenerProvider)

                Dim buffer = workspace.Documents.First().GetTextBuffer()
                Dim tagger = provider.CreateTagger(Of IClassificationTag)(
                    workspace.Documents.First().GetTextView(),
                    buffer)

                Using edit = buffer.CreateEdit()
                    edit.Insert(0, " ")
                    edit.Apply()
                End Using

                Using DirectCast(tagger, IDisposable)
                    Await listenerProvider.GetWaiter(FeatureAttribute.Classification).CreateWaitTask()

                    ' Note: we don't actually care what results we get back.  We're just
                    ' verifying that we don't crash because the SemanticViewTagger ends up
                    ' calling SyntaxTree/SemanticModel code.
                    tagger.GetTags(New NormalizedSnapshotSpanCollection(
                        New SnapshotSpan(buffer.CurrentSnapshot, New Span(0, 1))))
                End Using
            End Using
        End Function

        <WpfFact>
        Public Sub TestFailOverOfMissingClassificationType()
            Dim exportProvider = ExportProviderCache _
                .GetOrCreateExportProviderFactory(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic()) _
                .CreateExportProvider()


            Dim typeMap = exportProvider.GetExportedValue(Of ClassificationTypeMap)
            Dim formatMap = exportProvider.GetExportedValue(Of IClassificationFormatMapService).GetClassificationFormatMap("tooltip")

            Dim classifiedText = New ClassifiedText("UnknownClassificationType", "dummy")
            Dim run = classifiedText.ToRun(formatMap, typeMap)

            Assert.NotNull(run)
        End Sub

#Disable Warning BC40000 ' Type or member is obsolete
        <ExportLanguageService(GetType(IEditorClassificationService), "NoCompilation"), [Shared]>
        Private Class NoCompilationEditorClassificationService
            Implements IEditorClassificationService

            Public Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken) Implements IEditorClassificationService.AddLexicalClassifications
            End Sub

            Public Function AddSemanticClassificationsAsync(document As Document, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken) As Task Implements IEditorClassificationService.AddSemanticClassificationsAsync
                Return Task.CompletedTask
            End Function

            Public Function AddSyntacticClassificationsAsync(document As Document, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken) As Task Implements IEditorClassificationService.AddSyntacticClassificationsAsync
                Return Task.CompletedTask
            End Function

            Public Function AdjustStaleClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan Implements IEditorClassificationService.AdjustStaleClassification
            End Function
        End Class
#Enable Warning BC40008 ' Type or member is obsolete
    End Class
End Namespace
