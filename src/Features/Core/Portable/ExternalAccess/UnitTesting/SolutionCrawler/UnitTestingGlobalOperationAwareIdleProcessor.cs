// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.SolutionCrawler;

internal abstract class UnitTestingGlobalOperationAwareIdleProcessor : UnitTestingIdleProcessor
{
    /// <summary>
    /// We're not at a layer where we are guaranteed to have an IGlobalOperationNotificationService.  So allow for
    /// it being null.
    /// </summary>
    private readonly IGlobalOperationNotificationService? _globalOperationNotificationService;

    public UnitTestingGlobalOperationAwareIdleProcessor(
        IAsynchronousOperationListener listener,
        IGlobalOperationNotificationService? globalOperationNotificationService,
        TimeSpan backOffTimeSpan,
        CancellationToken shutdownToken)
        : base(listener, backOffTimeSpan, shutdownToken)
    {
        _globalOperationNotificationService = globalOperationNotificationService;

        if (_globalOperationNotificationService != null)
        {
            _globalOperationNotificationService.Started += OnGlobalOperationStarted;
            _globalOperationNotificationService.Stopped += OnGlobalOperationStopped;
        }
    }

    public virtual void Shutdown()
    {
        if (_globalOperationNotificationService != null)
        {
            _globalOperationNotificationService.Started -= OnGlobalOperationStarted;
            _globalOperationNotificationService.Stopped -= OnGlobalOperationStopped;
        }
    }

    private void OnGlobalOperationStarted(object? sender, EventArgs e)
        => this.SetIsPaused(isPaused: true);

    private void OnGlobalOperationStopped(object? sender, EventArgs e)
        => this.SetIsPaused(isPaused: false);
}
