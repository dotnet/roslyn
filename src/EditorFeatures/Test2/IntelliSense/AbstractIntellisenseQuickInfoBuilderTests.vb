' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.CodeAnalysis.Shared.TestHooks
Imports Microsoft.VisualStudio.Core.Imaging
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Adornments
Imports Microsoft.VisualStudio.Utilities
Imports Moq

Imports VSQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <UseExportProvider>
    Public MustInherit Class AbstractIntellisenseQuickInfoBuilderTests
        Protected Shared Async Function GetQuickInfoItemAsync(quickInfoItem As QuickInfoItem) As Task(Of VSQuickInfoItem)
            Dim workspaceDefinition =
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            $$
                        </Document>
                    </Project>
                </Workspace>

            Using workspace = TestWorkspace.Create(workspaceDefinition)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorBuffer = cursorDocument.GetTextBuffer()

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim trackingSpan = New Mock(Of ITrackingSpan)(MockBehavior.Strict) With {
                    .DefaultValue = DefaultValue.Mock
                }

                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()
                Dim operationExecutor = workspace.ExportProvider.GetExportedValue(Of IUIThreadOperationExecutor)()
                Dim streamingPresenter = workspace.ExportProvider.GetExport(Of IStreamingFindUsagesPresenter)()
                Return Await IntellisenseQuickInfoBuilder.BuildItemAsync(
                    trackingSpan.Object, quickInfoItem, document,
                    ClassificationOptions.Default, threadingContext, operationExecutor,
                    AsynchronousOperationListenerProvider.NullListener,
                    streamingPresenter, CancellationToken.None)
            End Using
        End Function

        Protected Shared Async Function GetQuickInfoItemAsync(workspaceDefinition As XElement, language As String) As Task(Of VSQuickInfoItem)
            Using workspace = TestWorkspace.Create(workspaceDefinition)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value
                Dim cursorBuffer = cursorDocument.GetTextBuffer()

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim languageServiceProvider = workspace.Services.GetLanguageServices(language)
                Dim quickInfoService = languageServiceProvider.GetRequiredService(Of QuickInfoService)

                Dim codeAnalysisQuickInfoItem = Await quickInfoService.GetQuickInfoAsync(document, cursorPosition, SymbolDescriptionOptions.Default, CancellationToken.None).ConfigureAwait(False)

                Dim trackingSpan = New Mock(Of ITrackingSpan)(MockBehavior.Strict) With {
                    .DefaultValue = DefaultValue.Mock
                }

                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()
                Dim operationExecutor = workspace.ExportProvider.GetExportedValue(Of IUIThreadOperationExecutor)()
                Dim streamingPresenter = workspace.ExportProvider.GetExport(Of IStreamingFindUsagesPresenter)()
                Dim classificationOptions = workspace.GlobalOptions.GetClassificationOptions(document.Project.Language)
                Return Await IntellisenseQuickInfoBuilder.BuildItemAsync(
                    trackingSpan.Object, codeAnalysisQuickInfoItem, document,
                    classificationOptions, threadingContext, operationExecutor,
                    AsynchronousOperationListenerProvider.NullListener,
                    streamingPresenter, CancellationToken.None)
            End Using
        End Function
    End Class
End Namespace
