// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This service allows you to persist information relative to solution, projects and documents.
    /// </summary>
    public interface IPersistentStorageService : IWorkspaceService
    {
        IPersistentStorage GetStorage(Solution solution);
    }
}