// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Extensions
{
    [ExportWorkspaceServiceFactory(typeof(IExtensionManager), ServiceLayer.Default), Shared]
    internal class ServicesLayerExtensionManager : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ServicesLayerExtensionManager()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new ExtensionManager();

        private class ExtensionManager : AbstractExtensionManager
        {
        }
    }
}
