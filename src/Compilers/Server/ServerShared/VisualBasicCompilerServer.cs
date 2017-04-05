// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.VisualBasic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class VisualBasicCompilerServer : VisualBasicCompiler
    {
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _metadataProvider;

        internal VisualBasicCompilerServer(Func<string, MetadataReferenceProperties, PortableExecutableReference> metadataProvider, string[] args, BuildPaths buildPaths, string libDirectory, IAnalyzerAssemblyLoader analyzerLoader)
            : base(VisualBasicCommandLineParser.Default, buildPaths.ClientDirectory != null ? Path.Combine(buildPaths.ClientDirectory, ResponseFileName) : null, args, buildPaths, libDirectory, analyzerLoader)
        {
            _metadataProvider = metadataProvider;
        }

        internal override Func<string, MetadataReferenceProperties, PortableExecutableReference> GetMetadataProvider()
        {
            return _metadataProvider;
        }
    }
}
