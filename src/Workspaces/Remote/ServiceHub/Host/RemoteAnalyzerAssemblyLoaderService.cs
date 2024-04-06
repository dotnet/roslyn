// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// Customizes the path where to store shadow-copies of analyzer assemblies.
    /// </summary>
    [ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider), [WorkspaceKind.RemoteWorkspace]), Shared]
    internal sealed class RemoteAnalyzerAssemblyLoaderService : IAnalyzerAssemblyLoaderProvider
    {
        private readonly RemoteAnalyzerAssemblyLoader _loader;
        private readonly ShadowCopyAnalyzerAssemblyLoader _shadowCopyLoader;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteAnalyzerAssemblyLoaderService()
        {
            var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(typeof(RemoteAnalyzerAssemblyLoader).GetTypeInfo().Assembly.Location));
            Debug.Assert(baseDirectory != null);

            _loader = new(baseDirectory);
            _shadowCopyLoader = new(Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"));
        }

        public IAnalyzerAssemblyLoader GetLoader(in AnalyzerAssemblyLoaderOptions options)
            => options.ShadowCopy ? _shadowCopyLoader : _loader;
    }
}
