// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceService(typeof(IAnalyzerService), WorkspaceKind.MSBuild), Shared]
    internal sealed class MSBuildWorkspaceAnalyzerService : IAnalyzerService
    {
        private readonly DefaultAnalyzerAssemblyLoader _loader = new DefaultAnalyzerAssemblyLoader();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public MSBuildWorkspaceAnalyzerService()
        {
        }

        public IAnalyzerAssemblyLoader GetLoader()
        {
            return _loader;
        }
    }
}
