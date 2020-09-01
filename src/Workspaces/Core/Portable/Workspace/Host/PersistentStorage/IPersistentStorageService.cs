// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PersistentStorage;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This service allows you to persist information relative to solution, projects and documents.
    /// </summary>
    public interface IPersistentStorageService : IWorkspaceService
    {
        IPersistentStorage GetStorage(Solution solution);
    }

    internal interface IPersistentStorageService2 : IPersistentStorageService
    {
        IPersistentStorage GetStorage(Solution solution, bool checkBranchId);
        IPersistentStorage GetStorage(Workspace workspace, SolutionKey solutionKey, bool checkBranchId);
    }
}
