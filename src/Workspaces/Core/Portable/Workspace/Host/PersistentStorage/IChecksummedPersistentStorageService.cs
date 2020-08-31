// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PersistentStorage;

namespace Microsoft.CodeAnalysis.Host
{
    internal interface IChecksummedPersistentStorageService : IPersistentStorageService2
    {
        new IChecksummedPersistentStorage GetStorage(Solution solution);
        new IChecksummedPersistentStorage GetStorage(Solution solution, bool checkBranchId);
        new IChecksummedPersistentStorage GetStorage(Workspace workspace, SolutionKey solutionKey, bool checkBranchId);
    }
}
