' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
Imports Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
    Partial Friend NotInheritable Class VisualBasicProject
        Private NotInheritable Class VisualBasicCodeModelInstanceFactory
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
                Dim document = _project.Workspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).FirstOrDefault(Function(d) d.ProjectId Is _project.ProjectSystemProject.Id)

                If document Is Nothing Then
                    Throw New ArgumentException(NameOf(filePath))
                End If

                Dim itemId = _project.Hierarchy.TryGetItemId(filePath)
                If itemId = VSConstants.VSITEMID.Nil Then
                    Throw New ArgumentException(NameOf(filePath))
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
    End Class
End Namespace
