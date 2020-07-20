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

        internal CSharpCompilerServer(Func<string, MetadataReferenceProperties, PortableExecutableReference> metadataProvider, string[] args, BuildPaths buildPaths, string libDirectory, IAnalyzerAssemblyLoader analyzerLoader)
            : base(CSharpCommandLineParser.Default, buildPaths.ClientDirectory != null ? Path.Combine(buildPaths.ClientDirectory, ResponseFileName) : null, args, buildPaths, libDirectory, analyzerLoader)
        {
            _metadataProvider = metadataProvider;
        }

        internal override Func<string, MetadataReferenceProperties, PortableExecutableReference> GetMetadataProvider()
        {
            return _metadataProvider;
        }
    }
}
