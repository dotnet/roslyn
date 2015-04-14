// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using System.Composition;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(IAnalyzerService), ServiceLayer.Default), Shared]
    internal sealed class AnalyzerServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service();
        }

        private sealed class Service : IAnalyzerService
        {
            public Assembly GetAnalyzer(string fullPath)
            {
                return Assembly.LoadFrom(fullPath);
            }
        }
    }
}
