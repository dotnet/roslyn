// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceStatusService), ServiceLayer.Default), Shared]
    internal sealed class WorkspaceStatusService : IWorkspaceStatusService
    {
        public readonly static WorkspaceStatusService Default = new WorkspaceStatusService();

        [ImportingConstructor]
        public WorkspaceStatusService()
        {
        }

        event EventHandler IWorkspaceStatusService.StatusChanged
        {
            add { }
            remove { }
        }

        public Task WaitUntilFullyLoadedAsync(CancellationToken cancellationToken)
        {
            // by the default, we are always fully loaded
            return Task.CompletedTask;
        }

        public Task<bool> IsFullyLoadedAsync(CancellationToken cancellationToken)
        {
            // by the default, we are always fully loaded
            return SpecializedTasks.True;
        }
    }
}
