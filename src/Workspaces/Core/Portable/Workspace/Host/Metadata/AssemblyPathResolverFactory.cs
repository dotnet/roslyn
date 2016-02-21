// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IAssemblyPathResolver), ServiceLayer.Default), Shared]
    internal sealed class AssemblyPathResolverFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service();
        }

        private sealed class Service : IAssemblyPathResolver
        {
            public Service()
            {
            }

            public string ResolveAssemblyPath(ProjectId projectId, string assemblyName)
            {
                // Assembly path resolution not supported at the default workspace level.
                return null;
            }
        }
    }
}
