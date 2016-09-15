' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.Implementation.Classification
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Notification
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Tagging
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Classification
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

            Dim exportProvider = MinimalTestExportProvider.CreateExportProvider(
                TestExportProvider.CreateAssemblyCatalogWithCSharpAndVisualBasic().WithParts(
                    GetType(NoCompilationEditorClassificationService)))

            Using workspace = Await TestWorkspace.CreateAsync(workspaceDefinition, exportProvider:=exportProvider)
                Dim waiter = New AsynchronousOperationListener()
                Dim provider = New SemanticClassificationViewTaggerProvider(
                    workspace.GetService(Of IForegroundNotificationService),
                    workspace.GetService(Of ISemanticChangeNotificationService),
                    workspace.GetService(Of ClassificationTypeMap),
                    SpecializedCollections.SingletonEnumerable(
                        New Lazy(Of IAsynchronousOperationListener, FeatureMetadata)(
                            Function() waiter, New FeatureMetadata(New Dictionary(Of String, Object)() From {{"FeatureName", FeatureAttribute.Classification}}))))

                Dim buffer = workspace.Documents.First().GetTextBuffer()
                Dim tagger = provider.CreateTagger(Of IClassificationTag)(
                    workspace.Documents.First().GetTextView(),
                    buffer)

                Using edit = buffer.CreateEdit()
                    edit.Insert(0, " ")
                    edit.Apply()
                End Using

                Using DirectCast(tagger, IDisposable)
                    Await waiter.CreateWaitTask()

                    ' Note: we don't actually care what results we get back.  We're just
                    ' verifying that we don't crash because the SemanticViewTagger ends up
                    ' calling SyntaxTree/SemanticModel code.
                    tagger.GetTags(New NormalizedSnapshotSpanCollection(
                        New SnapshotSpan(buffer.CurrentSnapshot, New Span(0, 1))))
                End Using
            End Using
        End Function

        <ExportLanguageService(GetType(IEditorClassificationService), "NoCompilation"), [Shared]>
        Private Class NoCompilationEditorClassificationService
            Implements IEditorClassificationService

            Public Sub AddLexicalClassifications(text As SourceText, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken) Implements IEditorClassificationService.AddLexicalClassifications
            End Sub

            Public Function AddSemanticClassificationsAsync(document As Document, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken) As Task Implements IEditorClassificationService.AddSemanticClassificationsAsync
                Return SpecializedTasks.EmptyTask
            End Function

            Public Function AddSyntacticClassificationsAsync(document As Document, textSpan As TextSpan, result As List(Of ClassifiedSpan), cancellationToken As CancellationToken) As Task Implements IEditorClassificationService.AddSyntacticClassificationsAsync
                Return SpecializedTasks.EmptyTask
            End Function

            Public Function AdjustStaleClassification(text As SourceText, classifiedSpan As ClassifiedSpan) As ClassifiedSpan Implements IEditorClassificationService.AdjustStaleClassification
            End Function
        End Class
    End Class
End Namespace