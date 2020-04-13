// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// Customizes the path where to store shadow-copies of analyzer assemblies.
    /// </summary>
    [ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider), WorkspaceKind.RemoteWorkspace), Shared]
    internal sealed class RemoteAnalyzerAssemblyLoaderService : IAnalyzerAssemblyLoaderProvider
    {
        private readonly DefaultAnalyzerAssemblyLoader _loader = new DefaultAnalyzerAssemblyLoader();
        private readonly ShadowCopyAnalyzerAssemblyLoader _shadowCopyLoader = new ShadowCopyAnalyzerAssemblyLoader(Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"));

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteAnalyzerAssemblyLoaderService()
        {
        }

        public IAnalyzerAssemblyLoader GetLoader(in AnalyzerAssemblyLoaderOptions options)
            => options.ShadowCopy ? _shadowCopyLoader : _loader;
    }
}
