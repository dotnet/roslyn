' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.ComponentModel.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Host
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.VisualStudio.Composition
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks
    <PartNotDiscoverable>
    <Export(GetType(VisualStudioWorkspace))>
    <Export(GetType(VisualStudioWorkspaceImpl))>
    <Export(GetType(MockVisualStudioWorkspace))>
    Friend Class MockVisualStudioWorkspace
        Inherits VisualStudioWorkspaceImpl

        Private _workspace As EditorTestWorkspace

        <ImportingConstructor>
        <System.Diagnostics.CodeAnalysis.SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be marked with 'ObsoleteAttribute'", Justification:="Used in test code: https://github.com/dotnet/roslyn/issues/42814")>
        Public Sub New(exportProvider As ExportProvider)
            MyBase.New(exportProvider, exportProvider.GetExportedValue(Of MockServiceProvider))

        End Sub

        Public Sub SetWorkspace(testWorkspace As EditorTestWorkspace)
            _workspace = testWorkspace
            SetCurrentSolutionEx(testWorkspace.CurrentSolution)

            ' HACK: ensure this service is created so it can be used during disposal
            Me.Services.GetService(Of IWorkspaceEventListenerService)()
        End Sub

        Public Overrides Function CanApplyChange(feature As ApplyChangesKind) As Boolean
            Return _workspace.CanApplyChange(feature)
        End Function

        Protected Overrides Sub ApplyDocumentTextChanged(documentId As DocumentId, newText As SourceText)
            Assert.True(_workspace.TryApplyChanges(_workspace.CurrentSolution.WithDocumentText(documentId, newText)))
            SetCurrentSolutionEx(_workspace.CurrentSolution)
        End Sub

        Public Overrides Sub CloseDocument(documentId As DocumentId)
            _workspace.CloseDocument(documentId)
            SetCurrentSolutionEx(_workspace.CurrentSolution)
        End Sub

        Protected Overrides Sub ApplyDocumentRemoved(documentId As DocumentId)
            Assert.True(_workspace.TryApplyChanges(_workspace.CurrentSolution.RemoveDocument(documentId)))
            SetCurrentSolutionEx(_workspace.CurrentSolution)
        End Sub

        Friend Overrides Function OpenInvisibleEditor(documentId As DocumentId) As IInvisibleEditor
            Return New MockInvisibleEditor(documentId, _workspace)
        End Function

        Public Overrides Function TryGoToDefinition(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Boolean
            Throw New NotImplementedException()
        End Function

        Public Overrides Function TryGoToDefinitionAsync(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Task(Of Boolean)
            Throw New NotImplementedException()
        End Function

        Public Overrides Function TryFindAllReferences(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Boolean
            Throw New NotImplementedException()
        End Function

        Public Overrides Sub DisplayReferencedSymbols(solution As Solution, referencedSymbols As IEnumerable(Of ReferencedSymbol))
            Throw New NotImplementedException()
        End Sub

        Friend Overrides Function GetBrowseObject(symbolListItem As SymbolListItem) As Object
            Throw New NotImplementedException()
        End Function

        Public Overrides Sub EnsureEditableDocuments(documents As IEnumerable(Of DocumentId))
            ' Nothing to do here
        End Sub
    End Class

    Public Class MockInvisibleEditor
        Implements IInvisibleEditor

        Private ReadOnly _documentId As DocumentId
        Private ReadOnly _workspace As EditorTestWorkspace
        Private ReadOnly _needsClose As Boolean

        Public Sub New(documentId As DocumentId, workspace As EditorTestWorkspace)
            Me._documentId = documentId
            Me._workspace = workspace

            If Not workspace.IsDocumentOpen(documentId) Then
                _workspace.OpenDocument(documentId)
                _needsClose = True
            End If
        End Sub

        Public ReadOnly Property TextBuffer As Global.Microsoft.VisualStudio.Text.ITextBuffer Implements IInvisibleEditor.TextBuffer
            Get
                Return Me._workspace.GetTestDocument(Me._documentId).GetTextBuffer()
            End Get
        End Property

        Public Sub Dispose() Implements IDisposable.Dispose
            If _needsClose Then
                _workspace.CloseDocument(_documentId)
            End If
        End Sub

    End Class
End Namespace
