// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class CSharpCompilerServer : CSharpCompiler
    {
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _metadataProvider;

        internal CSharpCompilerServer(Func<string, MetadataReferenceProperties, PortableExecutableReference> metadataProvider, string[] args, BuildPaths buildPaths, string? libDirectory, IAnalyzerAssemblyLoader analyzerLoader, GeneratorDriverCache driverCache)
            : this(metadataProvider, Path.Combine(buildPaths.ClientDirectory, ResponseFileName), args, buildPaths, libDirectory, analyzerLoader, driverCache)
        {
        }

        internal CSharpCompilerServer(Func<string, MetadataReferenceProperties, PortableExecutableReference> metadataProvider, string? responseFile, string[] args, BuildPaths buildPaths, string? libDirectory, IAnalyzerAssemblyLoader analyzerLoader, GeneratorDriverCache driverCache)
            : base(CSharpCommandLineParser.Default, responseFile, args, buildPaths, libDirectory, analyzerLoader, driverCache)
        {
            _metadataProvider = metadataProvider;
        }

        internal override Func<string, MetadataReferenceProperties, PortableExecutableReference> GetMetadataProvider()
        {
            return _metadataProvider;
        }
    }
}
