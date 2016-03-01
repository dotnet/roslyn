' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Partial Friend Class VisualBasicProjectShimWithServices
        Implements IProjectCodeModelProvider

        Private _projectCodeModel As VisualBasicProjectCodeModel

        Public ReadOnly Property ProjectCodeModel As AbstractProjectCodeModel Implements IProjectCodeModelProvider.ProjectCodeModel
            Get
                LazyInitialization.EnsureInitialized(_projectCodeModel, Function() New VisualBasicProjectCodeModel(Me, DirectCast(Me.Workspace, VisualStudioWorkspace), ServiceProvider))
                Return _projectCodeModel
            End Get
        End Property

        Public Overrides Function CreateCodeModel(pProject As EnvDTE.Project, pProjectItem As EnvDTE.ProjectItem, ByRef ppCodeModel As EnvDTE.CodeModel) As Integer
            ppCodeModel = Nothing

            Dim codeModelCache = ProjectCodeModel.GetCodeModelCache()
            If codeModelCache Is Nothing Then
                Return VSConstants.E_FAIL
            End If

            ppCodeModel = codeModelCache.GetOrCreateRootCodeModel(pProject)

            Return VSConstants.S_OK
        End Function

        Public Overrides Function CreateFileCodeModel(pProject As EnvDTE.Project, pProjectItem As EnvDTE.ProjectItem, ByRef ppFileCodeModel As EnvDTE.FileCodeModel) As Integer
            ppFileCodeModel = Nothing

            If pProjectItem IsNot Nothing Then
                Dim fileName = pProjectItem.FileNames(1)

                If Not String.IsNullOrWhiteSpace(fileName) Then
                    Dim codeModelCache = ProjectCodeModel.GetCodeModelCache()
                    If codeModelCache Is Nothing Then
                        Return VSConstants.E_FAIL
                    End If

                    ppFileCodeModel = codeModelCache.GetOrCreateFileCodeModel(fileName, pProjectItem).Handle
                    Return VSConstants.S_OK
                End If
            End If

            Return VSConstants.E_INVALIDARG
        End Function

        Protected Overrides Sub OnDocumentRemoved(filePath As String)
            MyBase.OnDocumentRemoved(filePath)

            ' We may have a code model floating around for it
            Dim codeModelCache = ProjectCodeModel.GetCodeModelCache()
            If codeModelCache IsNot Nothing Then
                codeModelCache.OnSourceFileRemoved(filePath)
            End If
        End Sub

        Public Overrides Sub Disconnect()
            ' Clear code model cache and shutdown instances, if any exists.
            _projectCodeModel?.OnProjectClosed()

            MyBase.Disconnect()
        End Sub
    End Class
End Namespace
