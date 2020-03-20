// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Host.Mef;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(ITaskSchedulerProvider), ServiceLayer.Default)]
    [Shared]
    internal sealed class TaskSchedulerProvider : ITaskSchedulerProvider
    {
        [ImportingConstructor]
        public TaskSchedulerProvider()
        {
        }

        public TaskScheduler GetCurrentContextScheduler()
            => (SynchronizationContext.Current != null) ? TaskScheduler.FromCurrentSynchronizationContext() : TaskScheduler.Default;
    }
}
