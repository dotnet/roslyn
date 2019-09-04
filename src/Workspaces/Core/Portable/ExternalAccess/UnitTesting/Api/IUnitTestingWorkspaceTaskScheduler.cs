// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal interface IUnitTestingWorkspaceTaskScheduler
    {
        Task ScheduleTask(Action taskAction, string taskName, CancellationToken cancellationToken = default);
        Task<T> ScheduleTask<T>(Func<T> taskFunc, string taskName, CancellationToken cancellationToken = default);
        Task ScheduleTask(Func<Task> taskFunc, string taskName, CancellationToken cancellationToken = default);
        Task<T> ScheduleTask<T>(Func<Task<T>> taskFunc, string taskName, CancellationToken cancellationToken = default);
    }
}
