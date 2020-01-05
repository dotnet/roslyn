// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
