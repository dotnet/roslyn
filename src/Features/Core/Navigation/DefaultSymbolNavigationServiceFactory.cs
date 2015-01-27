// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Navigation
{
    [ExportWorkspaceServiceFactory(typeof(ISymbolNavigationService), ServiceLayer.Default), Shared]
    internal class DefaultSymbolNavigationServiceFactory : IWorkspaceServiceFactory
    {
        private ISymbolNavigationService singleton;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (this.singleton == null)
            {
                this.singleton = new DefaultSymbolNavigationService();
            }

            return singleton;
        }
    }
}
