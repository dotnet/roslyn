// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;

namespace Microsoft.CodeAnalysis.Remote.Diagnostics
{
    /// <summary>
    /// For analyzers shipped in Roslyn, different set of assemblies might be used when running
    /// in-proc and OOP e.g. in-proc (VS) running on desktop clr and OOP running on ServiceHub .Net6
    /// host. We need to make sure to use the ones from the same location as the remote.
    /// </summary>
    internal sealed class RemoteAnalyzerAssemblyLoader : DefaultAnalyzerAssemblyLoader
    {
        private readonly string _baseDirectory;

        public RemoteAnalyzerAssemblyLoader(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
        }

        protected override string GetPathToLoad(string fullPath)
        {
            var fixedPath = Path.GetFullPath(Path.Combine(_baseDirectory, Path.GetFileName(fullPath)));
            return File.Exists(fixedPath) ? fixedPath : fullPath;
        }

#if NETCOREAPP

        // The following are special assemblies since they contain IDE analyzers and/or their dependencies,
        // but in the meantime, they also contain the host of compiler in remote process. Therefore on coreclr,
        // we must ensure they are only loaded once and in the same ALC compiler asemblies are loaded into.
        // Otherwise these analyzers will fail to interoperate with the host due to mismatch in assembly identity.
        private static readonly ImmutableHashSet<string> s_ideAssemblySimpleNames =
            CompilerAssemblySimpleNames.Union(new[]
            {
                    "Microsoft.CodeAnalysis.Features",
                    "Microsoft.CodeAnalysis.CSharp.Features",
                    "Microsoft.CodeAnalysis.VisualBasic.Features",
                    "Microsoft.CodeAnalysis.Workspaces",
                    "Microsoft.CodeAnalysis.CSharp.Workspaces",
                    "Microsoft.CodeAnalysis.VisualBasic.Workspaces",
            });

        internal override ImmutableHashSet<string> AssemblySimpleNamesToBeLoadedInCompilerContext => s_ideAssemblySimpleNames;
#endif
    }
}
