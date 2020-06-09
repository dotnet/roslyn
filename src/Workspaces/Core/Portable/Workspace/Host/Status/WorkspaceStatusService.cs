// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IWorkspaceStatusService), ServiceLayer.Default), Shared]
    internal sealed class WorkspaceStatusService : IWorkspaceStatusService
    {
        public static readonly WorkspaceStatusService Default = new WorkspaceStatusService();

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")]
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
