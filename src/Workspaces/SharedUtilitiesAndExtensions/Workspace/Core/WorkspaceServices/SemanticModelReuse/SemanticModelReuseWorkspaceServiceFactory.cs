// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SemanticModelReuse;

[ExportWorkspaceServiceFactory(typeof(ISemanticModelReuseWorkspaceService), ServiceLayer.Default), Shared]
internal sealed partial class SemanticModelReuseWorkspaceServiceFactory : IWorkspaceServiceFactory
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SemanticModelReuseWorkspaceServiceFactory()
    {
    }

    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new SemanticModelReuseWorkspaceService(workspaceServices.Workspace);
}
