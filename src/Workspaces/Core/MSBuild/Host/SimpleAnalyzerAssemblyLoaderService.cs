// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
