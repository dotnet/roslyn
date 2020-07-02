// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SemanticModelReuse
{
    [ExportWorkspaceServiceFactory(typeof(ISemanticModelReuseWorkspaceService), ServiceLayer.Default), Shared]
    internal partial class SemanticModelReuseWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public SemanticModelReuseWorkspaceServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
            => new SemanticModelReuseWorkspaceService(workspaceServices.Workspace);
    }
}
