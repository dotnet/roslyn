// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Composition;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [ExportWorkspaceService(typeof(IAnalyzerService), ServiceLayer.Host), Shared]
    internal sealed class VsAnalyzerAssemblyLoaderService : IAnalyzerService
    {
        public Assembly LoadFromPath(string fullPath)
        {
            return InMemoryAssemblyProvider.GetAssembly(fullPath);
        }

        public void AddDependencyLocation(string fullPath)
        {
        }
    }
}
