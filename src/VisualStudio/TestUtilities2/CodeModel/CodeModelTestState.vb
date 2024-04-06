' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CodeModel
    Friend Class CodeModelTestState
        Implements IDisposable

        Public ReadOnly Workspace As EditorTestWorkspace
        Private ReadOnly _visualStudioWorkspace As VisualStudioWorkspace
        Private ReadOnly _rootCodeModel As ComHandle(Of EnvDTE.CodeModel, RootCodeModel)
        Private ReadOnly _fileCodeModel As ComHandle(Of EnvDTE80.FileCodeModel2, FileCodeModel)?
        Private ReadOnly _codeModelService As ICodeModelService

        Public Sub New(
            workspace As EditorTestWorkspace,
            visualStudioWorkspace As VisualStudioWorkspace,
            rootCodeModel As ComHandle(Of EnvDTE.CodeModel, RootCodeModel),
            fileCodeModel As ComHandle(Of EnvDTE80.FileCodeModel2, FileCodeModel)?,
            codeModelService As ICodeModelService
        )

            If workspace Is Nothing Then
                Throw New ArgumentNullException(NameOf(workspace))
            End If

            If codeModelService Is Nothing Then
                Throw New ArgumentNullException(NameOf(codeModelService))
            End If

            Me.Workspace = workspace
            _visualStudioWorkspace = visualStudioWorkspace
            _rootCodeModel = rootCodeModel
            _fileCodeModel = fileCodeModel
            _codeModelService = codeModelService
        End Sub

        Public ReadOnly Property VisualStudioWorkspace As VisualStudioWorkspace
            Get
                Return _visualStudioWorkspace
            End Get
        End Property

        Public ReadOnly Property FileCodeModel As EnvDTE80.FileCodeModel2
            Get
                Return _fileCodeModel.Value.Handle
            End Get
        End Property

        Public ReadOnly Property FileCodeModelObject As FileCodeModel
            Get
                Return _fileCodeModel.Value.Object
            End Get
        End Property

        Public ReadOnly Property RootCodeModel As EnvDTE.CodeModel
            Get
                Return _rootCodeModel.Handle
            End Get
        End Property

        Public ReadOnly Property RootCodeModelObject As RootCodeModel
            Get
                Return _rootCodeModel.Object
            End Get
        End Property

        Public ReadOnly Property CodeModelService As ICodeModelService
            Get
                Return _codeModelService
            End Get
        End Property

#Region "IDisposable Support"
        Private _disposedValue As Boolean ' To detect redundant calls

        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposing Then
                FailFast.Fail("TestWorkspaceAndFileModelCodel GC'd without call to Dispose()!")
            End If

            If Not Me._disposedValue Then
                If disposing Then
                    ' Ensure the existing project is removed from the ProjectCodeModelFactory; we otherwise later might try updating any state
                    ' for it.
                    Dim projectId = Workspace.CurrentSolution.ProjectIds.Single()
                    Dim projectCodeModel = Workspace.ExportProvider.GetExportedValue(Of ProjectCodeModelFactory)().GetProjectCodeModel(projectId)
                    projectCodeModel.OnProjectClosed()

                    Workspace.Dispose()
                End If
            End If

            Me._disposedValue = True
        End Sub

        Protected Overrides Sub Finalize()
            Dispose(False)
            MyBase.Finalize()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
            GC.SuppressFinalize(Me)
        End Sub
#End Region

    End Class
End Namespace
