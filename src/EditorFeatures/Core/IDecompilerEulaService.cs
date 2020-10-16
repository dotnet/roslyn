﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
