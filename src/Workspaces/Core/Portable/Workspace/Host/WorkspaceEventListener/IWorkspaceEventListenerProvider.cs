// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// provide <see cref="IWorkspaceEventListener"/> for given workspace
    /// </summary>
    internal interface IWorkspaceEventListenerProvider : IWorkspaceService
    {
        IEnumerable<IWorkspaceEventListener> GetListeners();
    }

    [ExportWorkspaceServiceFactory(typeof(IWorkspaceEventListenerProvider), layer: ServiceLayer.Default), Shared]
    internal class DefaultWorkspaceEventListenerProvider : IWorkspaceServiceFactory
    {
        private readonly IEnumerable<Lazy<IWorkspaceEventListener, WorkspaceEventListenerMetadata>> _eventListeners;

        [ImportingConstructor]
        public DefaultWorkspaceEventListenerProvider(
            [ImportMany]IEnumerable<Lazy<IWorkspaceEventListener, WorkspaceEventListenerMetadata>> eventListeners)
        {
            // we use this indirect abstraction to deliver IWorkspaceEventLister to workspace. 
            // otherwise, each Workspace implementation need to explicitly tell base workspace listener either through
            // constructor or through virtual property. 
            // taking indirect approach since i dont believe all workspaces need to know about this. 
            _eventListeners = eventListeners;
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Provider(_eventListeners.Where(l => l.Metadata.WorkspaceKinds.Contains(workspaceServices.Workspace.Kind)));
        }

        private class Provider : IWorkspaceEventListenerProvider
        {
            private readonly IEnumerable<Lazy<IWorkspaceEventListener, WorkspaceEventListenerMetadata>> _eventListeners;

            public Provider(
                IEnumerable<Lazy<IWorkspaceEventListener, WorkspaceEventListenerMetadata>> eventListeners)
            {
                _eventListeners = eventListeners;
            }

            public IEnumerable<IWorkspaceEventListener> GetListeners()
            {
                return _eventListeners.Select(e => e.Value);
            }
        }
    }
}
