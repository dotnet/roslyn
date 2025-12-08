// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;

interface IWorkDoneProgressReporter : IDisposable, IProgress<WorkDoneProgress>
{
    /// <summary>
    /// Cancellation token that can be monitored to know when work done progress has been cancelled,
    /// either by the client or the server.
    /// </summary>
    CancellationToken CancellationToken { get; }
}