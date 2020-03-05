// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.SemanticModelWorkspaceService
{
    [ExportWorkspaceServiceFactory(typeof(ISemanticModelService), ServiceLayer.Default), Shared]
    internal partial class SemanticModelWorkspaceServiceFactory : IWorkspaceServiceFactory
    {
        [ImportingConstructor]
        public SemanticModelWorkspaceServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new SemanticModelService();
        }
    }
}
