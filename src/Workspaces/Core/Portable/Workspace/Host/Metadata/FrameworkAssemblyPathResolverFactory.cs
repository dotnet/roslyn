// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host;

[ExportWorkspaceServiceFactory(typeof(IFrameworkAssemblyPathResolver), ServiceLayer.Default), Shared]
internal sealed class FrameworkAssemblyPathResolverFactory : IWorkspaceServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FrameworkAssemblyPathResolverFactory()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new Service();

    private sealed class Service : IFrameworkAssemblyPathResolver
    {
        public Service()
        {
        }

        //public bool CanResolveType(ProjectId projectId, string assemblyName, string fullyQualifiedTypeName)
        //{
        //    return false;
        //}

        public string? ResolveAssemblyPath(ProjectId projectId, string assemblyName, string? fullyQualifiedTypeName)
        {
            // Assembly path resolution not supported at the default workspace level.
            return null;
        }
    }
}
