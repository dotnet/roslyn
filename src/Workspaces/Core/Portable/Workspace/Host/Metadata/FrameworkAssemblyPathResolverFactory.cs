// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IFrameworkAssemblyPathResolver), ServiceLayer.Default), Shared]
    internal sealed class FrameworkAssemblyPathResolverFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service();
        }

        private sealed class Service : IFrameworkAssemblyPathResolver
        {
            public Service()
            {
            }

            //public bool CanResolveType(ProjectId projectId, string assemblyName, string fullyQualifiedTypeName)
            //{
            //    return false;
            //}

            public string ResolveAssemblyPath(ProjectId projectId, string assemblyName, string fullyQualifiedTypeName = null)
            {
                // Assembly path resolution not supported at the default workspace level.
                return null;
            }
        }
    }
}
