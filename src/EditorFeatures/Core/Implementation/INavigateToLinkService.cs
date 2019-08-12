// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface INavigateToLinkService : IWorkspaceService
    {
        Task<bool> TryNavigateToLinkAsync(Uri uri, CancellationToken cancellationToken);
    }
}
