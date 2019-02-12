// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IDecompilerEulaService : IWorkspaceService
    {
        Task<bool> IsAcceptedAsync(CancellationToken cancellationToken);

        Task MarkAcceptedAsync(CancellationToken cancellationToken);
    }
}
