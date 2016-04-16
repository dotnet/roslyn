// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Runtime.InteropServices;


namespace Microsoft.Internal.VisualStudio.Shell.Interop
{
    internal enum __SolutionWorkingFolder
    {
        SlnWF_All = -1,
        SlnWF_StatePersistence = 1
    }

    [ComImport]
    [Guid("774FAAAD-4311-4E92-8B8B-D2759666D9C6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsSolutionWorkingFolders
    {
        void GetFolder([ComAliasName("Microsoft.Internal.VisualStudio.Shell.Interop.SolutionWorkingFolder")]uint location, Guid guidProject, bool fVersionSpecific, bool fEnsureCreated, out bool pfIsTemporary, out string pszBstrFullPath);
        void GetMigrationFolder([ComAliasName("Microsoft.Internal.VisualStudio.Shell.Interop.SolutionWorkingFolder")]uint location, Guid guidProject, [ComAliasName("OLE.DWORD")]out uint pdwOldMajorVersion, out string pszOldLocation);
        void ClearOldWorkingFolder([ComAliasName("Microsoft.Internal.VisualStudio.Shell.Interop.SolutionWorkingFolder")]uint location);
        void ClearWorkingFolder([ComAliasName("Microsoft.Internal.VisualStudio.Shell.Interop.SolutionWorkingFolder")]uint location, bool fSaveAll, bool fReloadSolution);
    }

    [ComImport]
    [Guid("3584678F-DA35-4F62-AB2A-8092B281C1FA")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IVsSolutionWorkingFoldersEvents
    {
        void OnQueryLocationChange([ComAliasName("Microsoft.Internal.VisualStudio.Shell.Interop.SolutionWorkingFolder")]uint location, out bool pfCanMoveContent);
        void OnAfterLocationChange([ComAliasName("Microsoft.Internal.VisualStudio.Shell.Interop.SolutionWorkingFolder")]uint location, bool contentMoved);
    }
}
