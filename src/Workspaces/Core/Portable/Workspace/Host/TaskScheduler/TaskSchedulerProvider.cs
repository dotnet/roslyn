// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceService(typeof(ITaskSchedulerProvider), ServiceLayer.Default)]
[Shared]
internal sealed class TaskSchedulerProvider : ITaskSchedulerProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public TaskSchedulerProvider()
    {
    }

    public TaskScheduler CurrentContextScheduler
        => (SynchronizationContext.Current != null) ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;
}
