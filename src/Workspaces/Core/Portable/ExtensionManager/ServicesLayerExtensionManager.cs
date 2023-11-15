// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Extensions;

[ExportWorkspaceServiceFactory(typeof(IExtensionManager), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class ServicesLayerExtensionManager() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new ExtensionManager();

    private sealed class ExtensionManager : AbstractExtensionManager
    {
        protected override void HandleNonCancellationException(object provider, Exception exception)
        {
            Debug.Assert(exception is not OperationCanceledException);
            DisableProvider(provider);
        }
    }
}
