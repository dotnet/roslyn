' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests
    Friend NotInheritable Class MockVsSolution
        Implements IVsSolution
        Implements IVsSolution2
        Implements IVsSolution3
        Implements IVsSolution4
        Implements IVsSolution5
        Implements IVsSolution6
        Implements IVsSolution7
        Implements IVsSolution8

        Private ReadOnly _lock As New Object
        Private ReadOnly _eventHandlers As New Dictionary(Of UInteger, IVsSolutionEvents)
        Private _nextCookie As UInteger

        Public Function GetProjectEnum(grfEnumFlags As UInteger, ByRef rguidEnumOnlyThisType As Guid, ByRef ppenum As IEnumHierarchies) As Integer Implements IVsSolution.GetProjectEnum
            Throw New NotImplementedException()
        End Function

        Public Function CreateProject(ByRef rguidProjectType As Guid, lpszMoniker As String, lpszLocation As String, lpszName As String, grfCreateFlags As UInteger, ByRef iidProject As Guid, ByRef ppProject As IntPtr) As Integer Implements IVsSolution.CreateProject
            Throw New NotImplementedException()
        End Function

        Public Function GenerateUniqueProjectName(lpszRoot As String, ByRef pbstrProjectName As String) As Integer Implements IVsSolution.GenerateUniqueProjectName
            Throw New NotImplementedException()
        End Function

        Public Function GetProjectOfGuid(ByRef rguidProjectID As Guid, ByRef ppHierarchy As IVsHierarchy) As Integer Implements IVsSolution.GetProjectOfGuid
            Throw New NotImplementedException()
        End Function

        Public Function GetGuidOfProject(pHierarchy As IVsHierarchy, ByRef pguidProjectID As Guid) As Integer Implements IVsSolution.GetGuidOfProject
            Throw New NotImplementedException()
        End Function

        Public Function GetSolutionInfo(ByRef pbstrSolutionDirectory As String, ByRef pbstrSolutionFile As String, ByRef pbstrUserOptsFile As String) As Integer Implements IVsSolution.GetSolutionInfo
            Throw New NotImplementedException()
        End Function

        Public Function AdviseSolutionEvents(pSink As IVsSolutionEvents, ByRef pdwCookie As UInteger) As Integer Implements IVsSolution.AdviseSolutionEvents
            SyncLock _lock
                Dim cookie = _nextCookie
                _eventHandlers.Add(cookie, pSink)
                _nextCookie += 1UI

                pdwCookie = cookie
                Return VSConstants.S_OK
            End SyncLock
        End Function

        Public Function UnadviseSolutionEvents(dwCookie As UInteger) As Integer Implements IVsSolution.UnadviseSolutionEvents
            SyncLock _lock
                _eventHandlers.Remove(dwCookie)
                Return VSConstants.S_OK
            End SyncLock
        End Function

        Public Function SaveSolutionElement(grfSaveOpts As UInteger, pHier As IVsHierarchy, docCookie As UInteger) As Integer Implements IVsSolution.SaveSolutionElement
            Throw New NotImplementedException()
        End Function

        Public Function CloseSolutionElement(grfCloseOpts As UInteger, pHier As IVsHierarchy, docCookie As UInteger) As Integer Implements IVsSolution.CloseSolutionElement
            Throw New NotImplementedException()
        End Function

        Public Function GetProjectOfProjref(pszProjref As String, ByRef ppHierarchy As IVsHierarchy, ByRef pbstrUpdatedProjref As String, puprUpdateReason() As VSUPDATEPROJREFREASON) As Integer Implements IVsSolution.GetProjectOfProjref
            Throw New NotImplementedException()
        End Function

        Public Function GetProjrefOfProject(pHierarchy As IVsHierarchy, ByRef pbstrProjref As String) As Integer Implements IVsSolution.GetProjrefOfProject
            Throw New NotImplementedException()
        End Function

        Public Function GetProjectInfoOfProjref(pszProjref As String, propid As Integer, ByRef pvar As Object) As Integer Implements IVsSolution.GetProjectInfoOfProjref
            Throw New NotImplementedException()
        End Function

        Public Function AddVirtualProject(pHierarchy As IVsHierarchy, grfAddVPFlags As UInteger) As Integer Implements IVsSolution.AddVirtualProject
            Throw New NotImplementedException()
        End Function

        Public Function GetItemOfProjref(pszProjref As String, ByRef ppHierarchy As IVsHierarchy, ByRef pitemid As UInteger, ByRef pbstrUpdatedProjref As String, puprUpdateReason() As VSUPDATEPROJREFREASON) As Integer Implements IVsSolution.GetItemOfProjref
            Throw New NotImplementedException()
        End Function

        Public Function GetProjrefOfItem(pHierarchy As IVsHierarchy, itemid As UInteger, ByRef pbstrProjref As String) As Integer Implements IVsSolution.GetProjrefOfItem
            Throw New NotImplementedException()
        End Function

        Public Function GetItemInfoOfProjref(pszProjref As String, propid As Integer, ByRef pvar As Object) As Integer Implements IVsSolution.GetItemInfoOfProjref
            Throw New NotImplementedException()
        End Function

        Public Function GetProjectOfUniqueName(pszUniqueName As String, ByRef ppHierarchy As IVsHierarchy) As Integer Implements IVsSolution.GetProjectOfUniqueName
            Throw New NotImplementedException()
        End Function

        Public Function GetUniqueNameOfProject(pHierarchy As IVsHierarchy, ByRef pbstrUniqueName As String) As Integer Implements IVsSolution.GetUniqueNameOfProject
            Throw New NotImplementedException()
        End Function

        Public Function GetProperty(propid As Integer, ByRef pvar As Object) As Integer Implements IVsSolution.GetProperty
            Throw New NotImplementedException()
        End Function

        Public Function SetProperty(propid As Integer, var As Object) As Integer Implements IVsSolution.SetProperty
            Throw New NotImplementedException()
        End Function

        Public Function OpenSolutionFile(grfOpenOpts As UInteger, pszFilename As String) As Integer Implements IVsSolution.OpenSolutionFile
            Throw New NotImplementedException()
        End Function

        Public Function QueryEditSolutionFile(ByRef pdwEditResult As UInteger) As Integer Implements IVsSolution.QueryEditSolutionFile
            Throw New NotImplementedException()
        End Function

        Public Function CreateSolution(lpszLocation As String, lpszName As String, grfCreateFlags As UInteger) As Integer Implements IVsSolution.CreateSolution
            Throw New NotImplementedException()
        End Function

        Public Function GetProjectFactory(dwReserved As UInteger, pguidProjectType() As Guid, pszMkProject As String, ByRef ppProjectFactory As IVsProjectFactory) As Integer Implements IVsSolution.GetProjectFactory
            Throw New NotImplementedException()
        End Function

        Public Function GetProjectTypeGuid(dwReserved As UInteger, pszMkProject As String, ByRef pguidProjectType As Guid) As Integer Implements IVsSolution.GetProjectTypeGuid
            Throw New NotImplementedException()
        End Function

        Public Function OpenSolutionViaDlg(pszStartDirectory As String, fDefaultToAllProjectsFilter As Integer) As Integer Implements IVsSolution.OpenSolutionViaDlg
            Throw New NotImplementedException()
        End Function

        Public Function AddVirtualProjectEx(pHierarchy As IVsHierarchy, grfAddVPFlags As UInteger, ByRef rguidProjectID As Guid) As Integer Implements IVsSolution.AddVirtualProjectEx
            Throw New NotImplementedException()
        End Function

        Public Function QueryRenameProject(pProject As IVsProject, pszMkOldName As String, pszMkNewName As String, dwReserved As UInteger, ByRef pfRenameCanContinue As Integer) As Integer Implements IVsSolution.QueryRenameProject
            Throw New NotImplementedException()
        End Function

        Public Function OnAfterRenameProject(pProject As IVsProject, pszMkOldName As String, pszMkNewName As String, dwReserved As UInteger) As Integer Implements IVsSolution.OnAfterRenameProject
            Throw New NotImplementedException()
        End Function

        Public Function RemoveVirtualProject(pHierarchy As IVsHierarchy, grfRemoveVPFlags As UInteger) As Integer Implements IVsSolution.RemoveVirtualProject
            Throw New NotImplementedException()
        End Function

        Public Function CreateNewProjectViaDlg(pszExpand As String, pszSelect As String, dwReserved As UInteger) As Integer Implements IVsSolution.CreateNewProjectViaDlg
            Throw New NotImplementedException()
        End Function

        Public Function GetVirtualProjectFlags(pHierarchy As IVsHierarchy, ByRef pgrfAddVPFlags As UInteger) As Integer Implements IVsSolution.GetVirtualProjectFlags
            Throw New NotImplementedException()
        End Function

        Public Function GenerateNextDefaultProjectName(pszBaseName As String, pszLocation As String, ByRef pbstrProjectName As String) As Integer Implements IVsSolution.GenerateNextDefaultProjectName
            Throw New NotImplementedException()
        End Function

        Public Function GetProjectFilesInSolution(grfGetOpts As UInteger, cProjects As UInteger, rgbstrProjectNames() As String, ByRef pcProjectsFetched As UInteger) As Integer Implements IVsSolution.GetProjectFilesInSolution
            Throw New NotImplementedException()
        End Function

        Public Function CanCreateNewProjectAtLocation(fCreateNewSolution As Integer, pszFullProjectFilePath As String, ByRef pfCanCreate As Integer) As Integer Implements IVsSolution.CanCreateNewProjectAtLocation
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProjectEnum(grfEnumFlags As UInteger, ByRef rguidEnumOnlyThisType As Guid, ByRef ppenum As IEnumHierarchies) As Integer Implements IVsSolution2.GetProjectEnum
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_CreateProject(ByRef rguidProjectType As Guid, lpszMoniker As String, lpszLocation As String, lpszName As String, grfCreateFlags As UInteger, ByRef iidProject As Guid, ByRef ppProject As IntPtr) As Integer Implements IVsSolution2.CreateProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GenerateUniqueProjectName(lpszRoot As String, ByRef pbstrProjectName As String) As Integer Implements IVsSolution2.GenerateUniqueProjectName
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProjectOfGuid(ByRef rguidProjectID As Guid, ByRef ppHierarchy As IVsHierarchy) As Integer Implements IVsSolution2.GetProjectOfGuid
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetGuidOfProject(pHierarchy As IVsHierarchy, ByRef pguidProjectID As Guid) As Integer Implements IVsSolution2.GetGuidOfProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetSolutionInfo(ByRef pbstrSolutionDirectory As String, ByRef pbstrSolutionFile As String, ByRef pbstrUserOptsFile As String) As Integer Implements IVsSolution2.GetSolutionInfo
            pbstrSolutionDirectory = Nothing
            pbstrSolutionFile = Nothing
            pbstrUserOptsFile = Nothing
            Return VSConstants.E_NOTIMPL
        End Function

        Private Function IVsSolution2_AdviseSolutionEvents(pSink As IVsSolutionEvents, ByRef pdwCookie As UInteger) As Integer Implements IVsSolution2.AdviseSolutionEvents
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_UnadviseSolutionEvents(dwCookie As UInteger) As Integer Implements IVsSolution2.UnadviseSolutionEvents
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_SaveSolutionElement(grfSaveOpts As UInteger, pHier As IVsHierarchy, docCookie As UInteger) As Integer Implements IVsSolution2.SaveSolutionElement
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_CloseSolutionElement(grfCloseOpts As UInteger, pHier As IVsHierarchy, docCookie As UInteger) As Integer Implements IVsSolution2.CloseSolutionElement
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProjectOfProjref(pszProjref As String, ByRef ppHierarchy As IVsHierarchy, ByRef pbstrUpdatedProjref As String, puprUpdateReason() As VSUPDATEPROJREFREASON) As Integer Implements IVsSolution2.GetProjectOfProjref
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProjrefOfProject(pHierarchy As IVsHierarchy, ByRef pbstrProjref As String) As Integer Implements IVsSolution2.GetProjrefOfProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProjectInfoOfProjref(pszProjref As String, propid As Integer, ByRef pvar As Object) As Integer Implements IVsSolution2.GetProjectInfoOfProjref
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_AddVirtualProject(pHierarchy As IVsHierarchy, grfAddVPFlags As UInteger) As Integer Implements IVsSolution2.AddVirtualProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetItemOfProjref(pszProjref As String, ByRef ppHierarchy As IVsHierarchy, ByRef pitemid As UInteger, ByRef pbstrUpdatedProjref As String, puprUpdateReason() As VSUPDATEPROJREFREASON) As Integer Implements IVsSolution2.GetItemOfProjref
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProjrefOfItem(pHierarchy As IVsHierarchy, itemid As UInteger, ByRef pbstrProjref As String) As Integer Implements IVsSolution2.GetProjrefOfItem
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetItemInfoOfProjref(pszProjref As String, propid As Integer, ByRef pvar As Object) As Integer Implements IVsSolution2.GetItemInfoOfProjref
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProjectOfUniqueName(pszUniqueName As String, ByRef ppHierarchy As IVsHierarchy) As Integer Implements IVsSolution2.GetProjectOfUniqueName
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetUniqueNameOfProject(pHierarchy As IVsHierarchy, ByRef pbstrUniqueName As String) As Integer Implements IVsSolution2.GetUniqueNameOfProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProperty(propid As Integer, ByRef pvar As Object) As Integer Implements IVsSolution2.GetProperty
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_SetProperty(propid As Integer, var As Object) As Integer Implements IVsSolution2.SetProperty
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_OpenSolutionFile(grfOpenOpts As UInteger, pszFilename As String) As Integer Implements IVsSolution2.OpenSolutionFile
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_QueryEditSolutionFile(ByRef pdwEditResult As UInteger) As Integer Implements IVsSolution2.QueryEditSolutionFile
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_CreateSolution(lpszLocation As String, lpszName As String, grfCreateFlags As UInteger) As Integer Implements IVsSolution2.CreateSolution
            Throw New NotImplementedException()
        End Function

        Public Function GetProjectFactory(dwReserved As UInteger, ByRef pguidProjectType As Guid, pszMkProject As String, ByRef ppProjectFactory As IVsProjectFactory) As Integer Implements IVsSolution2.GetProjectFactory
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProjectTypeGuid(dwReserved As UInteger, pszMkProject As String, ByRef pguidProjectType As Guid) As Integer Implements IVsSolution2.GetProjectTypeGuid
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_OpenSolutionViaDlg(pszStartDirectory As String, fDefaultToAllProjectsFilter As Integer) As Integer Implements IVsSolution2.OpenSolutionViaDlg
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_AddVirtualProjectEx(pHierarchy As IVsHierarchy, grfAddVPFlags As UInteger, ByRef rguidProjectID As Guid) As Integer Implements IVsSolution2.AddVirtualProjectEx
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_QueryRenameProject(pProject As IVsProject, pszMkOldName As String, pszMkNewName As String, dwReserved As UInteger, ByRef pfRenameCanContinue As Integer) As Integer Implements IVsSolution2.QueryRenameProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_OnAfterRenameProject(pProject As IVsProject, pszMkOldName As String, pszMkNewName As String, dwReserved As UInteger) As Integer Implements IVsSolution2.OnAfterRenameProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_RemoveVirtualProject(pHierarchy As IVsHierarchy, grfRemoveVPFlags As UInteger) As Integer Implements IVsSolution2.RemoveVirtualProject
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_CreateNewProjectViaDlg(pszExpand As String, pszSelect As String, dwReserved As UInteger) As Integer Implements IVsSolution2.CreateNewProjectViaDlg
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetVirtualProjectFlags(pHierarchy As IVsHierarchy, ByRef pgrfAddVPFlags As UInteger) As Integer Implements IVsSolution2.GetVirtualProjectFlags
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GenerateNextDefaultProjectName(pszBaseName As String, pszLocation As String, ByRef pbstrProjectName As String) As Integer Implements IVsSolution2.GenerateNextDefaultProjectName
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_GetProjectFilesInSolution(grfGetOpts As UInteger, cProjects As UInteger, rgbstrProjectNames() As String, ByRef pcProjectsFetched As UInteger) As Integer Implements IVsSolution2.GetProjectFilesInSolution
            Throw New NotImplementedException()
        End Function

        Private Function IVsSolution2_CanCreateNewProjectAtLocation(fCreateNewSolution As Integer, pszFullProjectFilePath As String, ByRef pfCanCreate As Integer) As Integer Implements IVsSolution2.CanCreateNewProjectAtLocation
            Throw New NotImplementedException()
        End Function

        Public Function UpdateProjectFileLocation(pHierarchy As IVsHierarchy) As Integer Implements IVsSolution2.UpdateProjectFileLocation
            Throw New NotImplementedException()
        End Function

        Public Function CreateNewProjectViaDlgEx(pszDlgTitle As String, pszTemplateDir As String, pszExpand As String, pszSelect As String, pszHelpTopic As String, cnpvdeFlags As UInteger, pBrowse As IVsBrowseProjectLocation) As Integer Implements IVsSolution3.CreateNewProjectViaDlgEx
            Throw New NotImplementedException()
        End Function

        Public Function GetUniqueUINameOfProject(pHierarchy As IVsHierarchy, ByRef pbstrUniqueName As String) As Integer Implements IVsSolution3.GetUniqueUINameOfProject
            Throw New NotImplementedException()
        End Function

        Public Function CheckForAndSaveDeferredSaveSolution(fCloseSolution As Integer, pszMessage As String, pszTitle As String, grfFlags As UInteger) As Integer Implements IVsSolution3.CheckForAndSaveDeferredSaveSolution
            Throw New NotImplementedException()
        End Function

        Public Function UpdateProjectFileLocationForUpgrade(pszCurrentLocation As String, pszUpgradedLocation As String) As Integer Implements IVsSolution3.UpdateProjectFileLocationForUpgrade
            Throw New NotImplementedException()
        End Function

        Public Function WriteUserOptsFile() As Integer Implements IVsSolution4.WriteUserOptsFile
            Throw New NotImplementedException()
        End Function

        <Obsolete>
        Public Function IsBackgroundSolutionLoadEnabled(ByRef pfIsEnabled As Boolean) As Integer Implements IVsSolution4.IsBackgroundSolutionLoadEnabled
            Throw New NotImplementedException()
        End Function

        <Obsolete>
        Public Function EnsureProjectsAreLoaded(cProjects As UInteger, guidProjects() As Guid, grfFlags As UInteger) As Integer Implements IVsSolution4.EnsureProjectsAreLoaded
            Throw New NotImplementedException()
        End Function

        <Obsolete>
        Public Function EnsureProjectIsLoaded(ByRef guidProject As Guid, grfFlags As UInteger) As Integer Implements IVsSolution4.EnsureProjectIsLoaded
            Throw New NotImplementedException()
        End Function

        <Obsolete>
        Public Function EnsureSolutionIsLoaded(grfFlags As UInteger) As Integer Implements IVsSolution4.EnsureSolutionIsLoaded
            Throw New NotImplementedException()
        End Function

        Public Function ReloadProject(ByRef guidProjectID As Guid) As Integer Implements IVsSolution4.ReloadProject
            Throw New NotImplementedException()
        End Function

        Public Function UnloadProject(ByRef guidProjectID As Guid, dwUnloadStatus As UInteger) As Integer Implements IVsSolution4.UnloadProject
            Throw New NotImplementedException()
        End Function

        Public Sub ResolveFaultedProjects(cHierarchies As UInteger, rgHierarchies() As IVsHierarchy, pProjectFaultResolutionContext As IVsPropertyBag, ByRef pcResolved As UInteger, ByRef pcFailed As UInteger) Implements IVsSolution5.ResolveFaultedProjects
            Throw New NotImplementedException()
        End Sub

        Public Function GetGuidOfProjectFile(pszProjectFile As String) As Guid Implements IVsSolution5.GetGuidOfProjectFile
            Throw New NotImplementedException()
        End Function

        Public Function SetProjectParent(pProject As IVsHierarchy, pParent As IVsHierarchy) As Integer Implements IVsSolution6.SetProjectParent
            Throw New NotImplementedException()
        End Function

        Public Function AddNewProjectFromTemplate(szTemplatePath As String, rgCustomParams As Array, szTargetFramework As String, szDestinationFolder As String, szProjectName As String, pParent As IVsHierarchy, ByRef ppNewProj As IVsHierarchy) As Integer Implements IVsSolution6.AddNewProjectFromTemplate
            Throw New NotImplementedException()
        End Function

        Public Function AddExistingProject(szFullPath As String, pParent As IVsHierarchy, ByRef ppNewProj As IVsHierarchy) As Integer Implements IVsSolution6.AddExistingProject
            Throw New NotImplementedException()
        End Function

        Public Function BrowseForExistingProject(szDialogTitle As String, szStartupLocation As String, preferedProjectType As Guid, ByRef pbstrSelected As String) As Integer Implements IVsSolution6.BrowseForExistingProject
            Throw New NotImplementedException()
        End Function

        Public Sub OpenFolder(folderPath As String) Implements IVsSolution7.OpenFolder
            Throw New NotImplementedException()
        End Sub

        Public Sub CloseFolder(folderPath As String) Implements IVsSolution7.CloseFolder
            Throw New NotImplementedException()
        End Sub

        <Obsolete>
        Public Function IsSolutionLoadDeferred() As Boolean Implements IVsSolution7.IsSolutionLoadDeferred
            Throw New NotImplementedException()
        End Function

        <Obsolete>
        Public Function IsDeferredProjectLoadAllowed(projectFullPath As String) As Boolean Implements IVsSolution7.IsDeferredProjectLoadAllowed
            Throw New NotImplementedException()
        End Function

        Public Function AdviseSolutionEventsEx(ByRef guidCallerId As Guid, pSink As Object, ByRef pdwCookie As UInteger) As Integer Implements IVsSolution8.AdviseSolutionEventsEx
            Throw New NotImplementedException()
        End Function

        Public Function BatchProjectAction(action As UInteger, dwFlags As UInteger, dwNumProjects As UInteger, rgProjects() As Guid, ByRef pContext As IVsBatchProjectActionContext) As Integer Implements IVsSolution8.BatchProjectAction
            Throw New NotImplementedException()
        End Function

        Public Function IsBatchProjectActionBlocking(action As UInteger, dwFlags As UInteger, dwNumProjects As UInteger, rgProjects() As Guid, ByRef pfIsBlocking As Integer) As Integer Implements IVsSolution8.IsBatchProjectActionBlocking
            Throw New NotImplementedException()
        End Function

        Public Function GetCurrentBatchProjectAction(ByRef pContext As IVsBatchProjectActionContext) As Integer Implements IVsSolution8.GetCurrentBatchProjectAction
            Throw New NotImplementedException()
        End Function
    End Class
End Namespace
