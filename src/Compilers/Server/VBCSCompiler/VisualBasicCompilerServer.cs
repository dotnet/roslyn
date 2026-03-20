// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.CommandLine;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class VisualBasicCompilerServer : VisualBasicCompiler
    {
        private readonly Func<string, MetadataReferenceProperties, PortableExecutableReference> _metadataProvider;
        private readonly CompilationCache? _cache;
        private readonly ICompilerServerLogger _logger;
        // Stored between CheckCache and OnCompilationSucceeded to avoid recomputing.
        private string? _deterministicKey;
        private string? _hashKey;

        internal VisualBasicCompilerServer(Func<string, MetadataReferenceProperties, PortableExecutableReference> metadataProvider, string[] args, BuildPaths buildPaths, string? libDirectory, IAnalyzerAssemblyLoader analyzerLoader, GeneratorDriverCache driverCache, CompilationCache? cache = null, ICompilerServerLogger? logger = null)
            : this(metadataProvider, Path.Combine(buildPaths.ClientDirectory, ResponseFileName), args, buildPaths, libDirectory, analyzerLoader, driverCache, cache, logger)
        {
        }

        internal VisualBasicCompilerServer(Func<string, MetadataReferenceProperties, PortableExecutableReference> metadataProvider, string? responseFile, string[] args, BuildPaths buildPaths, string? libDirectory, IAnalyzerAssemblyLoader analyzerLoader, GeneratorDriverCache driverCache, CompilationCache? cache = null, ICompilerServerLogger? logger = null)
            : base(VisualBasicCommandLineParser.Default, responseFile, args, buildPaths, libDirectory, analyzerLoader, driverCache)
        {
            _metadataProvider = metadataProvider;
            _cache = cache;
            _logger = logger ?? EmptyCompilerServerLogger.Instance;
        }

        internal override Func<string, MetadataReferenceProperties, PortableExecutableReference> GetMetadataProvider()
        {
            return _metadataProvider;
        }

        protected override int? CheckCache(
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<ISourceGenerator> generators,
            ImmutableArray<AdditionalText> additionalTexts,
            CancellationToken cancellationToken)
        {
            return CompilationCacheUtilities.CheckCache(_cache, _logger, Arguments, compilation, analyzers, generators, additionalTexts, cancellationToken, out _deterministicKey, out _hashKey);
        }

        protected override void OnCompilationSucceeded(
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            ImmutableArray<ISourceGenerator> generators,
            ImmutableArray<AdditionalText> additionalTexts,
            CancellationToken cancellationToken)
        {
            CompilationCacheUtilities.OnCompilationSucceeded(_cache, _logger, Arguments, _deterministicKey, _hashKey);
        }
    }
}

