// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// Customizes the path where to store shadow-copies of analyzer assemblies.
    /// </summary>
    [ExportWorkspaceService(typeof(IAnalyzerAssemblyLoaderProvider), WorkspaceKind.RemoteWorkspace), Shared]
    internal sealed class RemoteAnalyzerAssemblyLoaderService : IAnalyzerAssemblyLoaderProvider
    {
        private readonly RemoteAnalyzerAssemblyLoader _loader;
        private readonly ShadowCopyAnalyzerAssemblyLoader _shadowCopyLoader;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RemoteAnalyzerAssemblyLoaderService()
        {
            _loader = new(Path.GetDirectoryName(Path.GetFullPath(typeof(RemoteAnalyzerAssemblyLoader).GetTypeInfo().Assembly.Location)));
            _shadowCopyLoader = new(Path.Combine(Path.GetTempPath(), "VS", "AnalyzerAssemblyLoader"));
        }

        public IAnalyzerAssemblyLoader GetLoader(in AnalyzerAssemblyLoaderOptions options)
            => options.ShadowCopy ? _shadowCopyLoader : _loader;

        // For analyzers shipped in Roslyn, different set of assemblies might be used when running
        // in-proc and OOP e.g. in-proc (VS) running on desktop clr and OOP running on ServiceHub .Net6
        // host. We need to make sure to use the right one for OOP.
        private class RemoteAnalyzerAssemblyLoader : DefaultAnalyzerAssemblyLoader
        {
            private readonly string? _baseDirectory;

            protected override Assembly LoadImpl(string fullPath)
                => base.LoadImpl(FixPath(fullPath));

            public RemoteAnalyzerAssemblyLoader(string? baseDirectory)
            {
                _baseDirectory = baseDirectory;
            }

            private string FixPath(string fullPath)
            {
                if (_baseDirectory == null)
                {
                    return fullPath;
                }

                var assemblyName = Path.GetFileName(fullPath);
                var fixedPath = Path.GetFullPath(Path.Combine(_baseDirectory, assemblyName));

                if (!PathUtilities.PathsEqual(fixedPath, Path.GetFullPath(fullPath)) && File.Exists(fixedPath))
                {
                    return fixedPath;
                }

                return fullPath;
            }
        }
    }
}
