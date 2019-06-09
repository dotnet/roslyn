// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Compilation;

namespace CompilerBenchmarks
{
    public class StageBenchmarks
    {
        private Compilation _comp;
        private CommonPEModuleBuilder _moduleBeingBuilt;
        private EmitOptions _options;
        private MemoryStream _peStream;

        [Benchmark]
        public object Parsing()
        {
            return Helpers.CreateReproCompilation();
        }

        [GlobalSetup(Target = nameof(CompileMethodsAndEmit))]
        public void LoadCompilation()
        {
            _peStream = new MemoryStream();
            _comp = Helpers.CreateReproCompilation();

            // Call GetDiagnostics to force declaration symbol binding to finish
            _ = _comp.GetDiagnostics();
        }

        [Benchmark]
        public object CompileMethodsAndEmit()
        {
            _peStream.Position = 0;
            return _comp.Emit(_peStream);
        }

        [GlobalSetup(Target = nameof(SerializeMetadata))]
        public void CompileMethods()
        {
            LoadCompilation();

            _options = EmitOptions.Default.WithIncludePrivateMembers(true);

            bool embedPdb = _options.DebugInformationFormat == DebugInformationFormat.Embedded;

            var diagnostics = DiagnosticBag.GetInstance();

            _moduleBeingBuilt = _comp.CheckOptionsAndCreateModuleBuilder(
                diagnostics,
                manifestResources: null,
                _options,
                debugEntryPoint: null,
                sourceLinkStream: null,
                embeddedTexts: null,
                testData: null,
                cancellationToken: default);

            bool success = false;

            success = _comp.CompileMethods(
                _moduleBeingBuilt,
                emittingPdb: embedPdb,
                emitMetadataOnly: _options.EmitMetadataOnly,
                emitTestCoverageData: _options.EmitTestCoverageData,
                diagnostics: diagnostics,
                filterOpt: null,
                cancellationToken: default);

            _comp.GenerateResourcesAndDocumentationComments(
                _moduleBeingBuilt,
                xmlDocumentationStream: null,
                win32ResourcesStream: null,
                _options.OutputNameOverride,
                diagnostics,
                cancellationToken: default);

            _comp.ReportUnusedImports(null, diagnostics, default);
            _moduleBeingBuilt.CompilationFinished();

            diagnostics.Free();
        }

        [Benchmark]
        public object SerializeMetadata()
        {
            var diagnostics = DiagnosticBag.GetInstance();

            _comp.SerializeToPeStream(
                _moduleBeingBuilt,
                new SimpleEmitStreamProvider(_peStream),
                metadataPEStreamProvider: null,
                pdbStreamProvider: null,
                testSymWriterFactory: null,
                diagnostics,
                metadataOnly: _options.EmitMetadataOnly,
                includePrivateMembers: _options.IncludePrivateMembers,
                emitTestCoverageData: _options.EmitTestCoverageData,
                pePdbFilePath: _options.PdbFilePath,
                privateKeyOpt: null,
                cancellationToken: default);

            diagnostics.Free();

            return _peStream;
        }
    }
}
