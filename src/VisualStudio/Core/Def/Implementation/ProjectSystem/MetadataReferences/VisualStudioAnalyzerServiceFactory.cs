// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Reflection;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [ExportWorkspaceServiceFactory(typeof(IAnalyzerService), ServiceLayer.Host), Shared]
    internal sealed class VsAnalyzerServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new Service();
        }

        private sealed class Service : IAnalyzerService
        {
            public Assembly GetAnalyzer(string fullPath)
            {
                return InMemoryAssemblyProvider.GetAssembly(fullPath);
            }
        }
    }
}
