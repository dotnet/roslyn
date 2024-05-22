// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Notification;

internal abstract partial class AbstractGlobalOperationNotificationService : IGlobalOperationNotificationService
{
    private readonly object _gate = new();

    private readonly HashSet<IDisposable> _registrations = [];
    private readonly HashSet<string> _operations = [];

    private readonly TaskQueue _eventQueue;

    public event EventHandler? Started;
    public event EventHandler? Stopped;

    protected AbstractGlobalOperationNotificationService(
        IAsynchronousOperationListenerProvider listenerProvider)
    {
        _eventQueue = new TaskQueue(listenerProvider.GetListener(FeatureAttribute.GlobalOperation), TaskScheduler.Default);
    }

    ~AbstractGlobalOperationNotificationService()
    {
        if (!Environment.HasShutdownStarted)
        {
            Contract.ThrowIfFalse(_registrations.Count == 0);
            Contract.ThrowIfFalse(_operations.Count == 0, $"Non-disposed operations: {string.Join(", ", _operations)}");
        }
    }

    private void RaiseGlobalOperationStarted()
    {
        var started = this.Started;
        if (started != null)
            _eventQueue.ScheduleTask(nameof(RaiseGlobalOperationStarted), () => this.Started?.Invoke(this, EventArgs.Empty), CancellationToken.None);
    }

    private void RaiseGlobalOperationStopped()
    {
        var stopped = this.Stopped;
        if (stopped != null)
            _eventQueue.ScheduleTask(nameof(RaiseGlobalOperationStopped), () => this.Stopped?.Invoke(this, EventArgs.Empty), CancellationToken.None);
    }

    public IDisposable Start(string operation)
    {
        lock (_gate)
        {
            // create new registration
            var registration = new GlobalOperationRegistration(this);

            // states
            _registrations.Add(registration);
            _operations.Add(operation);

            // the very first one
            if (_registrations.Count == 1)
            {
                Contract.ThrowIfFalse(_operations.Count == 1);
                RaiseGlobalOperationStarted();
            }

            return registration;
        }
    }

    private void Done(GlobalOperationRegistration registration)
    {
        lock (_gate)
        {
            var result = _registrations.Remove(registration);
            Contract.ThrowIfFalse(result);

            if (_registrations.Count == 0)
            {
                _operations.Clear();
                RaiseGlobalOperationStopped();
            }
        }
    }
}
