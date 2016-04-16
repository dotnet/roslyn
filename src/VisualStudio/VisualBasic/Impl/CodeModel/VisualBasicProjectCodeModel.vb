' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic

    Friend Class VisualBasicProjectCodeModel
        Inherits AbstractProjectCodeModel

        Private ReadOnly _project As VisualBasicProjectShimWithServices

        Public Sub New(project As VisualBasicProjectShimWithServices, visualStudioWorkspace As VisualStudioWorkspace, serviceProvider As IServiceProvider)
            MyBase.New(project, visualStudioWorkspace, serviceProvider)

            Me._project = project
        End Sub

        Friend Overrides Function CanCreateFileCodeModelThroughProject(fileName As String) As Boolean
            Return _project.GetCurrentDocumentFromPath(fileName) IsNot Nothing
        End Function

        Friend Overrides Function CreateFileCodeModelThroughProject(fileName As String) As Object
            ' In the Dev11 VB code base, FileCodeModels were created eagerly when a VB SourceFile was created.
            ' In Roslyn, we'll take the same lazy approach that the Dev11 C# language service does.
            '
            ' Essentially, the C# project system has two methods that the C# language service calls to ask the
            ' project system to create a FileCodeModel for a specific file name:
            '
            '   * HRESULT CCSharpBuildMgrSite::CanCreateFileCodeModel(PCWSTR pszFileName, BOOL *pRetVal);
            '   * HRESULT CCSharpBuildMgrSite::CreateFileCodeModel(PCWSTR pszFileName, REFIID riid, void **ppObj)'
            '
            ' If the project system can create a FileCodeModel it calls back into ICSharpProjectSite::CreateFileCodeModel
            ' with the correct "parent" object.
            '
            ' Because the VB project system lacks these hooks, we simulate the same operations that those hooks perform.
            Dim document = _project.GetCurrentDocumentFromPath(fileName)
            If document Is Nothing Then
                Throw New ArgumentException(NameOf(fileName))
            End If

            Dim itemId = document.GetItemId()
            If itemId = VSConstants.VSITEMID.Nil Then
                Throw New ArgumentException(NameOf(fileName))
                Return Nothing
            End If

            Dim pvar As Object = Nothing
            If ErrorHandler.Failed(_project.Hierarchy.GetProperty(itemId, __VSHPROPID.VSHPROPID_ExtObject, pvar)) Then
                Throw New ArgumentException(NameOf(fileName))
            End If

            Dim projectItem = TryCast(pvar, EnvDTE.ProjectItem)
            If projectItem Is Nothing Then
                Throw New ArgumentException(NameOf(fileName))
            End If

            Dim fileCodeModel As EnvDTE.FileCodeModel = Nothing
            If ErrorHandler.Failed(_project.CreateFileCodeModel(projectItem.ContainingProject, projectItem, fileCodeModel)) Then
                Throw New ArgumentException(NameOf(fileName))
            End If

            Return fileCodeModel
        End Function
    End Class

End Namespace
