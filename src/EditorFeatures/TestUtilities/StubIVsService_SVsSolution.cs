// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    [Export(typeof(IVsService<SVsSolution, IVsSolution2>))]
    [PartNotDiscoverable]
    internal class StubIVsService_SVsSolution : IVsService<SVsSolution, IVsSolution2>
    {
        //private readonly IVsSolution2 _solution = new MockSolution();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public StubIVsService_SVsSolution()
        {
        }

        public Task<IVsSolution2> GetValueAsync(CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IVsSolution2?> GetValueOrNullAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IVsSolution2?>(null);

        //private class MockSolution : IVsSolution2
        //{
        //    public int GetSolutionInfo(out string? solutionDirectory, out string? solutionFile, out string? userOptsFile)
        //    {
        //        solutionDirectory = solutionFile = userOptsFile = null;
        //        return VSConstants.S_OK;
        //    }

        //    #region Not Implemented IVsSolution2 Members

        //    public int GetProjectEnum(uint grfEnumFlags, ref Guid rguidEnumOnlyThisType, out IEnumHierarchies ppenum)
        //        => throw new NotImplementedException();

        //    public int CreateProject(ref Guid rguidProjectType, string lpszMoniker, string lpszLocation, string lpszName, uint grfCreateFlags, ref Guid iidProject, out IntPtr ppProject)
        //        => throw new NotImplementedException();

        //    public int GenerateUniqueProjectName(string lpszRoot, out string pbstrProjectName)
        //        => throw new NotImplementedException();

        //    public int GetProjectOfGuid(ref Guid rguidProjectID, out IVsHierarchy ppHierarchy)
        //        => throw new NotImplementedException();

        //    public int GetGuidOfProject(IVsHierarchy pHierarchy, out Guid pguidProjectID)
        //        => throw new NotImplementedException();

        //    public int AdviseSolutionEvents(IVsSolutionEvents pSink, out uint pdwCookie)
        //        => throw new NotImplementedException();

        //    public int UnadviseSolutionEvents(uint dwCookie)
        //        => throw new NotImplementedException();

        //    public int SaveSolutionElement(uint grfSaveOpts, IVsHierarchy pHier, uint docCookie)
        //        => throw new NotImplementedException();

        //    public int CloseSolutionElement(uint grfCloseOpts, IVsHierarchy pHier, uint docCookie)
        //        => throw new NotImplementedException();

        //    public int GetProjectOfProjref(string pszProjref, out IVsHierarchy ppHierarchy, out string pbstrUpdatedProjref, VSUPDATEPROJREFREASON[] puprUpdateReason)
        //        => throw new NotImplementedException();

        //    public int GetProjrefOfProject(IVsHierarchy pHierarchy, out string pbstrProjref)
        //        => throw new NotImplementedException();

        //    public int GetProjectInfoOfProjref(string pszProjref, int propid, out object pvar)
        //        => throw new NotImplementedException();

        //    public int AddVirtualProject(IVsHierarchy pHierarchy, uint grfAddVPFlags)
        //        => throw new NotImplementedException();

        //    public int GetItemOfProjref(string pszProjref, out IVsHierarchy ppHierarchy, out uint pitemid, out string pbstrUpdatedProjref, VSUPDATEPROJREFREASON[] puprUpdateReason)
        //        => throw new NotImplementedException();

        //    public int GetProjrefOfItem(IVsHierarchy pHierarchy, uint itemid, out string pbstrProjref)
        //        => throw new NotImplementedException();

        //    public int GetItemInfoOfProjref(string pszProjref, int propid, out object pvar)
        //        => throw new NotImplementedException();

        //    public int GetProjectOfUniqueName(string pszUniqueName, out IVsHierarchy ppHierarchy)
        //        => throw new NotImplementedException();

        //    public int GetUniqueNameOfProject(IVsHierarchy pHierarchy, out string pbstrUniqueName)
        //        => throw new NotImplementedException();

        //    public int GetProperty(int propid, out object pvar)
        //        => throw new NotImplementedException();

        //    public int SetProperty(int propid, object var)
        //        => throw new NotImplementedException();

        //    public int OpenSolutionFile(uint grfOpenOpts, string pszFilename)
        //        => throw new NotImplementedException();

        //    public int QueryEditSolutionFile(out uint pdwEditResult)
        //        => throw new NotImplementedException();

        //    public int CreateSolution(string lpszLocation, string lpszName, uint grfCreateFlags)
        //        => throw new NotImplementedException();

        //    public int GetProjectFactory(uint dwReserved, ref Guid pguidProjectType, string pszMkProject, out IVsProjectFactory ppProjectFactory)
        //        => throw new NotImplementedException();

        //    public int GetProjectTypeGuid(uint dwReserved, string pszMkProject, out Guid pguidProjectType)
        //        => throw new NotImplementedException();

        //    public int OpenSolutionViaDlg(string pszStartDirectory, int fDefaultToAllProjectsFilter)
        //        => throw new NotImplementedException();

        //    public int AddVirtualProjectEx(IVsHierarchy pHierarchy, uint grfAddVPFlags, ref Guid rguidProjectID)
        //        => throw new NotImplementedException();

        //    public int QueryRenameProject(IVsProject pProject, string pszMkOldName, string pszMkNewName, uint dwReserved, out int pfRenameCanContinue)
        //        => throw new NotImplementedException();

        //    public int OnAfterRenameProject(IVsProject pProject, string pszMkOldName, string pszMkNewName, uint dwReserved)
        //        => throw new NotImplementedException();

        //    public int RemoveVirtualProject(IVsHierarchy pHierarchy, uint grfRemoveVPFlags)
        //        => throw new NotImplementedException();

        //    public int CreateNewProjectViaDlg(string pszExpand, string pszSelect, uint dwReserved)
        //        => throw new NotImplementedException();

        //    public int GetVirtualProjectFlags(IVsHierarchy pHierarchy, out uint pgrfAddVPFlags)
        //        => throw new NotImplementedException();

        //    public int GenerateNextDefaultProjectName(string pszBaseName, string pszLocation, out string pbstrProjectName)
        //        => throw new NotImplementedException();

        //    public int GetProjectFilesInSolution(uint grfGetOpts, uint cProjects, string[] rgbstrProjectNames, out uint pcProjectsFetched)
        //        => throw new NotImplementedException();

        //    public int CanCreateNewProjectAtLocation(int fCreateNewSolution, string pszFullProjectFilePath, out int pfCanCreate)
        //        => throw new NotImplementedException();

        //    public int UpdateProjectFileLocation(IVsHierarchy pHierarchy)
        //        => throw new NotImplementedException();

        //    public int GetProjectFactory(uint dwReserved, Guid[] pguidProjectType, string pszMkProject, out IVsProjectFactory ppProjectFactory)
        //        => throw new NotImplementedException();

        //    #endregion Not Implemented IVsSolution2 Members
        //}
    }
}
