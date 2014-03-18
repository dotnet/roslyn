// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This service allows you to access temporary storage.
    /// </summary>
    internal interface ITemporaryStorageService : IWorkspaceService
    {
        ITemporaryStorage CreateTemporaryStorage(CancellationToken cancellationToken);
    }
}