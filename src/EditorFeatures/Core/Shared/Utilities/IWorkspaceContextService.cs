// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal interface IWorkspaceContextService : IWorkspaceService
    {
        /// <summary>
        /// Used to determine if running as a client in a cloud connected environment.
        /// </summary>
        bool IsCloudEnvironmentClient();
    }

    [ExportWorkspaceService(typeof(IWorkspaceContextService), ServiceLayer.Default), Shared]
    internal sealed class DefaultWorkspaceContextService : IWorkspaceContextService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultWorkspaceContextService()
        {
        }

        public bool IsCloudEnvironmentClient() => false;
    }

    [ExportWorkspaceService(typeof(IWorkspaceContextService), WorkspaceKind.CloudEnvironmentClientWorkspace), Shared]
    internal sealed class CloudEnvironmentWorkspaceContextService : IWorkspaceContextService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CloudEnvironmentWorkspaceContextService()
        {
        }

        public bool IsCloudEnvironmentClient() => true;
    }
}
