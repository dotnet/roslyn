// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Storage
{
    [ExportWorkspaceService(typeof(IPersistentStorageLocationService), ServiceLayer.Host), Shared]
    internal class VisualStudioPersistentStorageLocationService : IPersistentStorageLocationService
    {
        public bool IsSupported(Workspace workspace)
            => workspace is VisualStudioWorkspaceImpl;

        public string GetStorageLocation(Solution solution)
        {
            var vsWorkspace = solution.Workspace as VisualStudioWorkspaceImpl;
            return vsWorkspace?.ProjectTracker.GetWorkingFolderPath(solution);
        }
    }
}