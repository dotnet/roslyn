// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This service allows you to persist information relative to solution, projects and documents.
    /// </summary>
    internal interface IPersistentStorageService : IWorkspaceService
    {
        IPersistentStorage GetStorage(Solution solution);
    }
}