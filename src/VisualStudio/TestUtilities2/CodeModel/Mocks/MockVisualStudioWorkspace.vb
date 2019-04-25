' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel.Mocks
    Friend Class MockVisualStudioWorkspace
        Inherits VisualStudioWorkspace

        Private ReadOnly _workspace As TestWorkspace
        Private ReadOnly _fileCodeModels As New Dictionary(Of DocumentId, ComHandle(Of EnvDTE80.FileCodeModel2, FileCodeModel))

        Public Sub New(workspace As TestWorkspace)
            MyBase.New(workspace.Services.HostServices)

            _workspace = workspace
            SetCurrentSolution(workspace.CurrentSolution)
        End Sub

        Public Overrides Function CanApplyChange(feature As ApplyChangesKind) As Boolean
            Return _workspace.CanApplyChange(feature)
        End Function

        Protected Overrides Sub OnDocumentTextChanged(document As Document)
            Assert.True(_workspace.TryApplyChanges(_workspace.CurrentSolution.WithDocumentText(document.Id, document.GetTextAsync().Result)))
            SetCurrentSolution(_workspace.CurrentSolution)
        End Sub

        Public Overrides Sub CloseDocument(documentId As DocumentId)
            _workspace.CloseDocument(documentId)
            SetCurrentSolution(_workspace.CurrentSolution)
        End Sub

        Protected Overrides Sub ApplyDocumentRemoved(documentId As DocumentId)
            Assert.True(_workspace.TryApplyChanges(_workspace.CurrentSolution.RemoveDocument(documentId)))
            SetCurrentSolution(_workspace.CurrentSolution)
        End Sub

        Public Overrides Function GetHierarchy(projectId As ProjectId) As Microsoft.VisualStudio.Shell.Interop.IVsHierarchy
            Return Nothing
        End Function

        Friend Overrides Function GetProjectGuid(projectId As ProjectId) As Guid
            Return Guid.Empty
        End Function

        Friend Overrides Function OpenInvisibleEditor(documentId As DocumentId) As IInvisibleEditor
            Return New MockInvisibleEditor(documentId, _workspace)
        End Function

        Public Overrides Function GetFileCodeModel(documentId As DocumentId) As EnvDTE.FileCodeModel
            Return _fileCodeModels(documentId).Handle
        End Function

        Public Overrides Function TryGoToDefinition(symbol As ISymbol, project As Project, cancellationToken As CancellationToken) As Boolean
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

        Friend Sub SetFileCodeModel(id As DocumentId, fileCodeModel As ComHandle(Of EnvDTE80.FileCodeModel2, FileCodeModel))
            _fileCodeModels.Add(id, fileCodeModel)
        End Sub

        Friend Function GetFileCodeModelComHandle(id As DocumentId) As ComHandle(Of EnvDTE80.FileCodeModel2, FileCodeModel)
            Return _fileCodeModels(id)
        End Function

        Friend Overrides Function TryGetRuleSetPathForProject(projectId As ProjectId) As String
            Throw New NotImplementedException()
        End Function
    End Class

    Public Class MockInvisibleEditor
        Implements IInvisibleEditor

        Private ReadOnly _documentId As DocumentId
        Private ReadOnly _workspace As TestWorkspace
        Private ReadOnly _needsClose As Boolean = False

        Public Sub New(documentId As DocumentId, workspace As TestWorkspace)
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
