Imports Microsoft.VisualStudio.Shell.Interop
Imports Roslyn.Test.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
    Public Class MockVsSolution
        Implements IVsSolution

        Public GetProjectOfGuidImpl As Func(Of Guid, IVsHierarchy)

        Sub New()
        End Sub

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
            If GetProjectOfGuidImpl Is Nothing Then
                Throw New NotImplementedException()
            End If

            ppHierarchy = GetProjectOfGuidImpl.Invoke(rguidProjectID)
            Return If(ppHierarchy Is Nothing, HResult.S_FALSE, HResult.S_OK)
        End Function

        Public Function GetGuidOfProject(pHierarchy As IVsHierarchy, ByRef pguidProjectID As Guid) As Integer Implements IVsSolution.GetGuidOfProject
            Throw New NotImplementedException()
        End Function

        Public Function GetSolutionInfo(ByRef pbstrSolutionDirectory As String, ByRef pbstrSolutionFile As String, ByRef pbstrUserOptsFile As String) As Integer Implements IVsSolution.GetSolutionInfo
            Throw New NotImplementedException()
        End Function

        Public Function AdviseSolutionEvents(pSink As IVsSolutionEvents, ByRef pdwCookie As UInteger) As Integer Implements IVsSolution.AdviseSolutionEvents
            Return 0
        End Function

        Public Function UnadviseSolutionEvents(dwCookie As UInteger) As Integer Implements IVsSolution.UnadviseSolutionEvents
            Return 0
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
    End Class
End Namespace
