// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    interface IPersistentStorageLocationService : IWorkspaceService
    {
        bool IsSupported(Workspace workspace);
        string GetStorageLocation(Solution solution);
    }

    [ExportWorkspaceService(typeof(IPersistentStorageLocationService)), Shared]
    internal class DefaultPersistentStorageLocationService : IPersistentStorageLocationService
    {
        public bool IsSupported(Workspace workspace) => false;

        public string GetStorageLocation(Solution solution) => null;
    }
}