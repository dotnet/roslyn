// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceStatusService), ServiceLayer.Default), Shared]
    internal sealed class WorkspaceStatusService : IWorkspaceStatusService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public WorkspaceStatusService()
        {
        }

        event EventHandler IWorkspaceStatusService.StatusChanged
        {
            add { }
            remove { }
        }

        // by default, we are always fully loaded
        public bool IsFullyLoaded => true;

        // by default, we are always fully loaded
        public Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken)
        {
            // by the default, we are always fully loaded
            return Task.CompletedTask;
        }
    }
}
