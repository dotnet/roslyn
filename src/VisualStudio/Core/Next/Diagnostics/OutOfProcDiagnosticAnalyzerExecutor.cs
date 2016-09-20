// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.EngineV2;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Remote.Diagnostics;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Microsoft.VisualStudio.LanguageServices.Implementation.Extensions;
using Microsoft.VisualStudio.LanguageServices.Remote;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Diagnostics
{
    [ExportWorkspaceService(typeof(IRemoteHostDiagnosticAnalyzerExecutor), layer: ServiceLayer.Host), Shared]
    internal class OutOfProcDiagnosticAnalyzerExecutor : IRemoteHostDiagnosticAnalyzerExecutor
    {
        private readonly IDiagnosticAnalyzerService _analyzerService;
        private readonly AbstractHostDiagnosticUpdateSource _hostDiagnosticUpdateSource;

        // TODO: this should be removed once we move options down to compiler layer
        private readonly ConcurrentDictionary<string, ValueTuple<OptionSet, Asset>> _lastOptionSetPerLanguage;

        [ImportingConstructor]
        public OutOfProcDiagnosticAnalyzerExecutor(
            IDiagnosticAnalyzerService analyzerService,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            _analyzerService = analyzerService;
            _hostDiagnosticUpdateSource = hostDiagnosticUpdateSource;

            // currently option is a bit wierd since it is not part of snapshot and 
            // we can't load all options without loading all language specific dlls.
            // we have tracking issue for this.
            // https://github.com/dotnet/roslyn/issues/13643
            _lastOptionSetPerLanguage = new ConcurrentDictionary<string, ValueTuple<OptionSet, Asset>>();
        }

        public async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeAsync(CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var remoteHostClient = await project.Solution.Workspace.Services.GetService<IRemoteHostClientService>().GetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (remoteHostClient == null)
            {
                // remote host is not running. this can happen if remote host is disabled.
                return await InProcCodeAnalysisDiagnosticAnalyzerExecutor.Instance.AnalyzeAsync(analyzerDriver, project, cancellationToken).ConfigureAwait(false);
            }

            var outOfProcResult = await AnalyzeOutOfProcAsync(remoteHostClient, analyzerDriver, project, cancellationToken).ConfigureAwait(false);

            // make sure things are not cancelled
            cancellationToken.ThrowIfCancellationRequested();

            return DiagnosticAnalysisResultMap.Create(outOfProcResult.AnalysisResult, outOfProcResult.TelemetryInfo);
        }

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> AnalyzeOutOfProcAsync(
            RemoteHostClient client, CompilationWithAnalyzers analyzerDriver, Project project, CancellationToken cancellationToken)
        {
            var solution = project.Solution;
            var snapshotService = solution.Workspace.Services.GetService<ISolutionChecksumService>();

            // TODO: this should be moved out
            var analyzerMap = CreateAnalyzerMap(analyzerDriver.Analyzers);
            if (analyzerMap.Count == 0)
            {
                return DiagnosticAnalysisResultMap.Create(ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>.Empty, ImmutableDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo>.Empty);
            }

            var optionAsset = GetOptionsAsset(solution, project.Language, cancellationToken);
            var hostChecksums = GetHostAnalyzerReferences(snapshotService, _analyzerService.GetHostAnalyzerReferences(), cancellationToken);

            var argument = new DiagnosticArguments(
                analyzerDriver.AnalysisOptions.ReportSuppressedDiagnostics,
                analyzerDriver.AnalysisOptions.LogAnalyzerExecutionTime,
                project.Id, optionAsset.Checksum.ToArray(), hostChecksums, analyzerMap.Keys.ToArray());

            // TODO: send telemetry on session
            using (var session = await client.CreateCodeAnalysisServiceSessionAsync(solution, cancellationToken).ConfigureAwait(false))
            {
                session.AddAdditionalAssets(optionAsset);

                var result = await session.InvokeAsync(
                    WellKnownServiceHubServices.CodeAnalysisService_CalculateDiagnosticsAsync,
                    new object[] { argument },
                    (s, c) => GetCompilerAnalysisResultAsync(s, analyzerMap, project, c)).ConfigureAwait(false);

                ReportAnalyzerExceptions(project, result.Exceptions);

                return result;
            }
        }

        private Asset GetOptionsAsset(Solution solution, string language, CancellationToken cancellationToken)
        {
            // TODO: we need better way to deal with options. optionSet itself is green node but
            //       it is not part of snapshot and can't save option to solution since we can't use language
            //       specific option without loading related language specific dlls
            var options = solution.Options;

            // we have cached options
            ValueTuple<OptionSet, Asset> value;
            if (_lastOptionSetPerLanguage.TryGetValue(language, out value) && value.Item1 == options)
            {
                return value.Item2;
            }

            // otherwise, we need to build one.
            var assetBuilder = new AssetBuilder(solution);
            var asset = assetBuilder.Build(options, language, cancellationToken);

            _lastOptionSetPerLanguage[language] = ValueTuple.Create(options, asset);
            return asset;
        }

        private CompilationWithAnalyzers CreateAnalyzerDriver(CompilationWithAnalyzers analyzerDriver, Func<DiagnosticAnalyzer, bool> predicate)
        {
            var analyzers = analyzerDriver.Analyzers.Where(predicate).ToImmutableArray();
            if (analyzers.Length == 0)
            {
                // we can't create analyzer driver with 0 analyzers
                return null;
            }

            return analyzerDriver.Compilation.WithAnalyzers(analyzers, analyzerDriver.AnalysisOptions);
        }

        private async Task<DiagnosticAnalysisResultMap<DiagnosticAnalyzer, DiagnosticAnalysisResult>> GetCompilerAnalysisResultAsync(Stream stream, Dictionary<string, DiagnosticAnalyzer> analyzerMap, Project project, CancellationToken cancellationToken)
        {
            // handling of cancellation and exception
            var version = await DiagnosticIncrementalAnalyzer.GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);

            using (var reader = new ObjectReader(stream))
            {
                return DiagnosticResultSerializer.Deserialize(reader, analyzerMap, project, version, cancellationToken);
            }
        }

        private void ReportAnalyzerExceptions(Project project, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<DiagnosticData>> exceptions)
        {
            foreach (var kv in exceptions)
            {
                var analyzer = kv.Key;
                foreach (var diagnostic in kv.Value)
                {
                    _hostDiagnosticUpdateSource.ReportAnalyzerDiagnostic(analyzer, diagnostic, project);
                }
            }
        }

        private ImmutableArray<byte[]> GetHostAnalyzerReferences(ISolutionChecksumService snapshotService, IEnumerable<AnalyzerReference> references, CancellationToken cancellationToken)
        {
            // TODO: cache this to somewhere
            var builder = ImmutableArray.CreateBuilder<byte[]>();
            foreach (var reference in references)
            {
                var asset = snapshotService.GetGlobalAsset(reference, cancellationToken);
                builder.Add(asset.Checksum.ToArray());
            }

            return builder.ToImmutable();
        }

        private Dictionary<string, DiagnosticAnalyzer> CreateAnalyzerMap(IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            // TODO: this needs to be cached. we can have 300+ analyzers
            return analyzers.ToDictionary(a => a.GetAnalyzerIdAndVersion().Item1, a => a);
        }
    }
}
