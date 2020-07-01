﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis.Classification
Imports Microsoft.CodeAnalysis.Editor.Host
Imports Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
Imports Microsoft.CodeAnalysis.Editor.Shared.Utilities
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.QuickInfo
Imports Microsoft.VisualStudio.Core.Imaging
Imports Microsoft.VisualStudio.Imaging
Imports Microsoft.VisualStudio.Text
Imports Microsoft.VisualStudio.Text.Adornments
Imports Moq

Imports VSQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.IntelliSense
    <UseExportProvider>
    Public MustInherit Class AbstractIntellisenseQuickInfoBuilderTests
        Protected Async Function GetQuickInfoItemAsync(quickInfoItem As QuickInfoItem) As Task(Of VSQuickInfoItem)
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
                Dim streamingPresenter = workspace.ExportProvider.GetExport(Of IStreamingFindUsagesPresenter)()
                Return Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, quickInfoItem, cursorBuffer.CurrentSnapshot, document, threadingContext, streamingPresenter, CancellationToken.None)
            End Using
        End Function

        Protected Async Function GetQuickInfoItemAsync(workspaceDefinition As XElement, language As String) As Task(Of VSQuickInfoItem)
            Using workspace = TestWorkspace.Create(workspaceDefinition)
                Dim solution = workspace.CurrentSolution
                Dim cursorDocument = workspace.Documents.First(Function(d) d.CursorPosition.HasValue)
                Dim cursorPosition = cursorDocument.CursorPosition.Value
                Dim cursorBuffer = cursorDocument.GetTextBuffer()

                Dim document = workspace.CurrentSolution.GetDocument(cursorDocument.Id)

                Dim languageServiceProvider = workspace.Services.GetLanguageServices(language)
                Dim quickInfoService = languageServiceProvider.GetRequiredService(Of QuickInfoService)

                Dim codeAnalysisQuickInfoItem = Await quickInfoService.GetQuickInfoAsync(document, cursorPosition, CancellationToken.None).ConfigureAwait(False)

                Dim trackingSpan = New Mock(Of ITrackingSpan)(MockBehavior.Strict) With {
                    .DefaultValue = DefaultValue.Mock
                }

                Dim threadingContext = workspace.ExportProvider.GetExportedValue(Of IThreadingContext)()
                Dim streamingPresenter = workspace.ExportProvider.GetExport(Of IStreamingFindUsagesPresenter)()
                Return Await IntellisenseQuickInfoBuilder.BuildItemAsync(trackingSpan.Object, codeAnalysisQuickInfoItem, cursorBuffer.CurrentSnapshot, document, threadingContext, streamingPresenter, CancellationToken.None)
            End Using
        End Function
    End Class
End Namespace
