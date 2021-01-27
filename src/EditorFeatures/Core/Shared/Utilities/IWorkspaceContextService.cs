// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
    internal interface IWorkspaceContextService : IWorkspaceService
    {
        /// <summary>
        /// Determines if LSP is being used as the editor.
        /// Used to disable non-LSP editor feature integration.
        /// </summary>
        bool IsInLspEditorContext();

        /// <summary>
        /// Determines if the VS instance is being as a cloud environment client.
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

        public bool IsInLspEditorContext() => false;

        public bool IsCloudEnvironmentClient() => false;
    }
}
