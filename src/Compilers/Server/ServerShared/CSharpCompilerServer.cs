// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

        internal CSharpCompilerServer(Func<string, MetadataReferenceProperties, PortableExecutableReference> metadataProvider, string[] args, string clientDirectory, string baseDirectory, string sdkDirectory, string libDirectory, IAnalyzerAssemblyLoader analyzerLoader)
            : base(CSharpCommandLineParser.Default, clientDirectory != null ? Path.Combine(clientDirectory, ResponseFileName) : null, args, clientDirectory, baseDirectory, sdkDirectory, libDirectory, analyzerLoader)
        {
            _metadataProvider = metadataProvider;
        }

        internal override Func<string, MetadataReferenceProperties, PortableExecutableReference> GetMetadataProvider()
        {
            return _metadataProvider;
        }
    }
}
