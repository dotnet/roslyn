' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports EnvDTE
Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic

    Friend NotInheritable Class VisualBasicCodeModelInstanceFactory
        Implements ICodeModelInstanceFactory

        Private ReadOnly _project As VisualBasicProject

        Public Sub New(project As VisualBasicProject)
            _project = project
        End Sub

        Public Function TryCreateFileCodeModelThroughProjectSystem(filePath As String) As EnvDTE.FileCodeModel Implements ICodeModelInstanceFactory.TryCreateFileCodeModelThroughProjectSystem
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
            Dim document = _project.GetCurrentDocumentFromPath(filePath)
            If document Is Nothing Then
                Throw New ArgumentException(NameOf(filePath))
            End If

            Dim itemId = document.GetItemId()
            If itemId = VSConstants.VSITEMID.Nil Then
                Throw New ArgumentException(NameOf(filePath))
                Return Nothing
            End If

            Dim pvar As Object = Nothing
            If ErrorHandler.Failed(_project.Hierarchy.GetProperty(itemId, __VSHPROPID.VSHPROPID_ExtObject, pvar)) Then
                Throw New ArgumentException(NameOf(filePath))
            End If

            Dim projectItem = TryCast(pvar, EnvDTE.ProjectItem)
            If projectItem Is Nothing Then
                Throw New ArgumentException(NameOf(filePath))
            End If

            Dim fileCodeModel As EnvDTE.FileCodeModel = Nothing
            If ErrorHandler.Failed(_project.CreateFileCodeModel(projectItem.ContainingProject, projectItem, fileCodeModel)) Then
                Throw New ArgumentException(NameOf(filePath))
            End If

            Return fileCodeModel
        End Function
    End Class

End Namespace
