// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Compilation;

namespace CompilerBenchmarks
{
    public class EmitBenchmark
    {
        public enum EmitStageSelection
        {
            FullEmit, // Measures Compilation.Emit
            SerializeOnly // Measures just metadata serialization (internal API)
        }

        [Params(EmitStageSelection.FullEmit, EmitStageSelection.SerializeOnly)]
        public EmitStageSelection Selection { get; set; }

        private Compilation _comp;
        private CommonPEModuleBuilder _moduleBeingBuilt;
        private EmitOptions _options;
        private MemoryStream _peStream;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _peStream = new MemoryStream();
            _comp = Helpers.CreateReproCompilation();

            // Call GetDiagnostics to force binding to finish and most semantic analysis to be completed
            _ = _comp.GetDiagnostics();

            if (Selection == EmitStageSelection.SerializeOnly)
            {
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
        }

        [Benchmark]
        public object RunEmit()
        {
            _peStream.Position = 0;
            switch (Selection)
            {
                case EmitStageSelection.FullEmit:
                {
                    return _comp.Emit(_peStream);
                }
                case EmitStageSelection.SerializeOnly:
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

                default:
                    throw ExceptionUtilities.UnexpectedValue(Selection);
            }
        }
    }
}
