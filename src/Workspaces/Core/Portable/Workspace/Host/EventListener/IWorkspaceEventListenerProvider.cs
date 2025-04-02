// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// Ensure <see cref="IEventListener.StartListening"/> is called for the workspace
/// </summary>
internal interface IWorkspaceEventListenerService : IWorkspaceService
{
    void EnsureListeners();
    void Stop();
}

[ExportWorkspaceServiceFactory(typeof(IWorkspaceEventListenerService), layer: ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class DefaultWorkspaceEventListenerServiceFactory(
    [ImportMany] IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners) : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
    {
        var workspace = workspaceServices.Workspace;
        return new Service(workspace, EventListenerTracker.GetListeners(workspace.Kind, eventListeners));
    }

    internal sealed class Service(Workspace workspace, IEnumerable<IEventListener> eventListeners) : IWorkspaceEventListenerService
    {
        private readonly object _gate = new();
        private bool _initialized = false;
        private readonly ImmutableArray<IEventListener> _eventListeners = [.. eventListeners];

        public void EnsureListeners()
        {
            lock (_gate)
            {
                if (_initialized)
                {
                    // already initialized
                    return;
                }

                _initialized = true;
            }

            foreach (var listener in _eventListeners)
                listener.StartListening(workspace);
        }

        public void Stop()
        {
            lock (_gate)
            {
                // If we were never initialized, then there's nothing to do
                if (_initialized)
                {
                    foreach (var listener in _eventListeners)
                        listener.StopListening(workspace);
                }
            }
        }
    }
}
