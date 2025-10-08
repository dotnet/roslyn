// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Notification;

internal abstract partial class AbstractGlobalOperationNotificationService : IGlobalOperationNotificationService
{
    private readonly object _gate = new();

    private readonly HashSet<IDisposable> _registrations = [];
    private readonly HashSet<string> _operations = [];

    private readonly AsyncBatchingWorkQueue<bool> _eventQueue;

    public event EventHandler? Started;
    public event EventHandler? Stopped;

    protected AbstractGlobalOperationNotificationService(
        IAsynchronousOperationListenerProvider listenerProvider,
        CancellationToken disposalToken)
    {
        _eventQueue = new AsyncBatchingWorkQueue<bool>(
            TimeSpan.Zero,
            ProcessEventsAsync,
            listenerProvider.GetListener(FeatureAttribute.GlobalOperation),
            disposalToken);
    }

    ~AbstractGlobalOperationNotificationService()
    {
        if (!Environment.HasShutdownStarted)
        {
            Contract.ThrowIfFalse(_registrations.Count == 0);
            Contract.ThrowIfFalse(_operations.Count == 0, $"Non-disposed operations: {string.Join(", ", _operations)}");
        }
    }

    private ValueTask ProcessEventsAsync(ImmutableSegmentedList<bool> list, CancellationToken cancellationToken)
    {
        foreach (var value in list)
        {
            var eventHandler = value ? Started : Stopped;
            eventHandler?.Invoke(this, EventArgs.Empty);
        }

        return ValueTask.CompletedTask;
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
                _eventQueue.AddWork(true);
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
                _eventQueue.AddWork(false);
            }
        }
    }
}
