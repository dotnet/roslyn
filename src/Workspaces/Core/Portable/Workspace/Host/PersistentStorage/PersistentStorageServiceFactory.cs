// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A service that enables storing and retrieving of information associated with solutions,
    /// projects or documents across runtime sessions.
    /// </summary>
    [ExportWorkspaceServiceFactory(typeof(IPersistentStorageService), ServiceLayer.Default), Shared]
    internal class PersistentStorageServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public PersistentStorageServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => NoOpPersistentStorageService.Instance;
    }
}
