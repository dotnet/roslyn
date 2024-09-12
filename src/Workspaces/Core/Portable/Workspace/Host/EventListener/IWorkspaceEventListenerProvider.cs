// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// Ensure <see cref="IEventListener{TService}.StartListening(Workspace, TService)"/> is called for the workspace
    /// </summary>
    internal interface IWorkspaceEventListenerService : IWorkspaceService
    {
        void EnsureListeners();
        void Stop();
    }

    [ExportWorkspaceServiceFactory(typeof(IWorkspaceEventListenerService), layer: ServiceLayer.Default), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal class DefaultWorkspaceEventListenerServiceFactory(
        [ImportMany] IEnumerable<Lazy<IEventListener, EventListenerMetadata>> eventListeners) : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var workspace = workspaceServices.Workspace;
            return new Service(workspace, EventListenerTracker<object>.GetListeners(workspace.Kind, eventListeners));
        }

        private class Service(Workspace workspace, IEnumerable<IEventListener<object>> eventListeners) : IWorkspaceEventListenerService
        {
            private readonly object _gate = new();
            private bool _initialized = false;
            private readonly ImmutableArray<IEventListener<object>> _eventListeners = eventListeners.ToImmutableArray();

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
                {
                    listener.StartListening(workspace, serviceOpt: null);
                }
            }

            public void Stop()
            {
                lock (_gate)
                {
                    if (!_initialized)
                    {
                        // never initialized. nothing to do
                        return;
                    }

                    foreach (var listener in _eventListeners.OfType<IEventListenerStoppable>())
                    {
                        listener.StopListening(workspace);
                    }
                }
            }
        }
    }
}
