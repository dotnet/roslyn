// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(ISolutionStatusService), ServiceLayer.Default), Shared]
    internal sealed class SolutionStatusService : ISolutionStatusService
    {
        public readonly static SolutionStatusService Default = new SolutionStatusService();

        public Task WaitForAsync(Solution solution, CancellationToken cancellationToken)
        {
            // by the default, we are always fully loaded
            return Task.CompletedTask;
        }

        public Task WaitForAsync(Project project, CancellationToken cancellationToken)
        {
            // by the default, we are always fully loaded
            return Task.CompletedTask;
        }

        public Task<bool> IsFullyLoadedAsync(Solution solution, CancellationToken cancellationToken)
        {
            // by the default, we are always fully loaded
            return SpecializedTasks.True;
        }

        public Task<bool> IsFullyLoadedAsync(Project project, CancellationToken cancellationToken)
        {
            // by the default, we are always fully loaded
            return SpecializedTasks.True;
        }
    }
}
