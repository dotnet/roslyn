// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IAnalyzerService), WorkspaceKind.MSBuild), Shared]
    internal sealed class SimpleAnalyzerAssemblyLoaderService : IAnalyzerService
    {
#if NET472
        private readonly DesktopAnalyzerAssemblyLoader _loader = new DesktopAnalyzerAssemblyLoader();
#elif NETCOREAPP1_1 || NETCOREAPP2_1
        private readonly CoreClrAnalyzerAssemblyLoader _loader = new CoreClrAnalyzerAssemblyLoader();
#endif

        [ImportingConstructor]
        public SimpleAnalyzerAssemblyLoaderService()
        {
        }

        public IAnalyzerAssemblyLoader GetLoader()
        {
            return _loader;
        }
    }
}
