// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Navigation
{
    [ExportWorkspaceServiceFactory(typeof(IDocumentNavigationService), ServiceLayer.Default), Shared]
    internal sealed class DefaultDocumentNavigationServiceFactory : IWorkspaceServiceFactory
    {
        private IDocumentNavigationService _singleton;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public DefaultDocumentNavigationServiceFactory()
        {
        }

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (_singleton == null)
            {
                _singleton = new DefaultDocumentNavigationService();
            }

            return _singleton;
        }
    }
}
