// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceService(typeof(IWorkspaceStatusService), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultWorkspaceStatusService() : IWorkspaceStatusService
{
    event EventHandler IWorkspaceStatusService.StatusChanged
    {
        add { }
        remove { }
    }

    public async Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken)
    {
        // by the default, we are always fully loaded
    }

    public async Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
    {
        // by the default, we are always fully loaded
        return true;
    }
}
