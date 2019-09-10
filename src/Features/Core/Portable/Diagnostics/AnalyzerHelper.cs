// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class AnalyzerHelper
    {
        private const string CSharpCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.CSharp.CSharpCompilerDiagnosticAnalyzer";
        private const string VisualBasicCompilerAnalyzerTypeName = "Microsoft.CodeAnalysis.Diagnostics.VisualBasic.VisualBasicCompilerDiagnosticAnalyzer";

        // These are the error codes of the compiler warnings. 
        // Keep the ids the same so that de-duplication against compiler errors
        // works in the error list (after a build).
        internal const string WRN_AnalyzerCannotBeCreatedIdCS = "CS8032";
        internal const string WRN_AnalyzerCannotBeCreatedIdVB = "BC42376";
        internal const string WRN_NoAnalyzerInAssemblyIdCS = "CS8033";
        internal const string WRN_NoAnalyzerInAssemblyIdVB = "BC42377";
        internal const string WRN_UnableToLoadAnalyzerIdCS = "CS8034";
        internal const string WRN_UnableToLoadAnalyzerIdVB = "BC42378";

        // Shared with Compiler
        internal const string AnalyzerExceptionDiagnosticId = "AD0001";
        internal const string AnalyzerDriverExceptionDiagnosticId = "AD0002";

        // IDE only errors
        internal const string WRN_AnalyzerCannotBeCreatedId = "AD1000";
        internal const string WRN_NoAnalyzerInAssemblyId = "AD1001";
        internal const string WRN_UnableToLoadAnalyzerId = "AD1002";

        private const string AnalyzerExceptionDiagnosticCategory = "Intellisense";

        public static bool IsWorkspaceDiagnosticAnalyzer(this DiagnosticAnalyzer analyzer)
        {
            return analyzer is DocumentDiagnosticAnalyzer || analyzer is ProjectDiagnosticAnalyzer;
        }

        public static bool IsBuiltInAnalyzer(this DiagnosticAnalyzer analyzer)
        {
            return analyzer is IBuiltInAnalyzer || analyzer.IsWorkspaceDiagnosticAnalyzer() || analyzer.IsCompilerAnalyzer();
        }

        public static bool IsOpenFileOnly(this DiagnosticAnalyzer analyzer, Workspace workspace)
        {
            if (analyzer is IBuiltInAnalyzer builtInAnalyzer)
            {
                return builtInAnalyzer.OpenFileOnly(workspace);
            }

            return false;
        }

        public static bool ContainsOpenFileOnlyAnalyzers(this CompilationWithAnalyzers analyzerDriverOpt, Workspace workspace)
        {
            if (analyzerDriverOpt == null)
            {
                // not Roslyn. no open file only analyzers
                return false;
            }

            foreach (var analyzer in analyzerDriverOpt.Analyzers)
            {
                if (analyzer.IsOpenFileOnly(workspace))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasNonHiddenDescriptor(this DiagnosticAnalyzerService service, DiagnosticAnalyzer analyzer, Project project)
        {
            // most of analyzers, number of descriptor is quite small, so this should be cheap.
            return service.GetDiagnosticDescriptors(analyzer).Any(d => d.GetEffectiveSeverity(project.CompilationOptions) != ReportDiagnostic.Hidden);
        }

        public static ReportDiagnostic GetEffectiveSeverity(this DiagnosticDescriptor descriptor, CompilationOptions options)
        {
            return options == null
                ? descriptor.DefaultSeverity.ToReportDiagnostic()
                : descriptor.GetEffectiveSeverity(options);
        }

        public static bool IsCompilerAnalyzer(this DiagnosticAnalyzer analyzer)
        {
            // TODO: find better way.
            var typeString = analyzer.GetType().ToString();
            if (typeString == CSharpCompilerAnalyzerTypeName)
            {
                return true;
            }

            if (typeString == VisualBasicCompilerAnalyzerTypeName)
            {
                return true;
            }

            return false;
        }

        public static (string analyzerId, VersionStamp version) GetAnalyzerIdAndVersion(this DiagnosticAnalyzer analyzer)
        {
            // Get the unique ID for given diagnostic analyzer.
            // note that we also put version stamp so that we can detect changed analyzer.
            var typeInfo = analyzer.GetType().GetTypeInfo();
            return (analyzer.GetAnalyzerId(), GetAnalyzerVersion(typeInfo.Assembly.Location));
        }

        public static string GetAnalyzerAssemblyName(this DiagnosticAnalyzer analyzer)
        {
            var typeInfo = analyzer.GetType().GetTypeInfo();
            return typeInfo.Assembly.GetName().Name;
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public static ValueTask<OptionSet> GetDocumentOptionSetAsync(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            if (!(analyzerOptions is WorkspaceAnalyzerOptions workspaceAnalyzerOptions))
            {
                return new ValueTask<OptionSet>(default(OptionSet));
            }

            return workspaceAnalyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken);
        }

        internal static void OnAnalyzerException_NoTelemetryLogging(
            Exception ex,
            DiagnosticAnalyzer analyzer,
            Diagnostic diagnostic,
            AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource,
            ProjectId projectIdOpt)
        {
            if (diagnostic != null)
            {
                hostDiagnosticUpdateSource?.ReportAnalyzerDiagnostic(analyzer, diagnostic, hostDiagnosticUpdateSource?.Workspace, projectIdOpt);
            }
        }

        internal static void OnAnalyzerExceptionForSupportedDiagnostics(DiagnosticAnalyzer analyzer, Exception exception, AbstractHostDiagnosticUpdateSource hostDiagnosticUpdateSource)
        {
            if (exception is OperationCanceledException)
            {
                return;
            }

            var diagnostic = CreateAnalyzerExceptionDiagnostic(analyzer, exception);
            OnAnalyzerException_NoTelemetryLogging(exception, analyzer, diagnostic, hostDiagnosticUpdateSource, projectIdOpt: null);
        }

        /// <summary>
        /// Create a diagnostic for exception thrown by the given analyzer.
        /// </summary>
        /// <remarks>
        /// Keep this method in sync with "AnalyzerExecutor.CreateAnalyzerExceptionDiagnostic".
        /// </remarks>
        internal static Diagnostic CreateAnalyzerExceptionDiagnostic(DiagnosticAnalyzer analyzer, Exception e)
        {
            var analyzerName = analyzer.ToString();

            // TODO: It is not ideal to create a new descriptor per analyzer exception diagnostic instance.
            // However, until we add a LongMessage field to the Diagnostic, we are forced to park the instance specific description onto the Descriptor's Description field.
            // This requires us to create a new DiagnosticDescriptor instance per diagnostic instance.
            var descriptor = new DiagnosticDescriptor(AnalyzerExceptionDiagnosticId,
                title: FeaturesResources.User_Diagnostic_Analyzer_Failure,
                messageFormat: FeaturesResources.Analyzer_0_threw_an_exception_of_type_1_with_message_2,
                description: string.Format(FeaturesResources.Analyzer_0_threw_the_following_exception_colon_1, analyzerName, e.CreateDiagnosticDescription()),
                category: AnalyzerExceptionDiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);

            return Diagnostic.Create(descriptor, Location.None, analyzerName, e.GetType(), e.Message);
        }

        private static VersionStamp GetAnalyzerVersion(string path)
        {
            if (path == null || !File.Exists(path))
            {
                return VersionStamp.Default;
            }

            return VersionStamp.Create(File.GetLastWriteTimeUtc(path));
        }

        public static DiagnosticData CreateAnalyzerLoadFailureDiagnostic(string fullPath, AnalyzerLoadFailureEventArgs e)
        {
            return CreateAnalyzerLoadFailureDiagnostic(projectId: null, language: null, fullPath, e);
        }

        public static DiagnosticData CreateAnalyzerLoadFailureDiagnostic(ProjectId projectId, string language, string fullPath, AnalyzerLoadFailureEventArgs e)
        {
            if (!TryGetErrorMessage(language, fullPath, e, out var id, out var message, out var messageFormat, out var description))
            {
                return null;
            }

            return new DiagnosticData(
                id,
                FeaturesResources.Roslyn_HostError,
                message,
                messageFormat,
                severity: DiagnosticSeverity.Warning,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                description: description,
                warningLevel: 0,
                projectId: projectId,
                customTags: ImmutableArray<string>.Empty,
                properties: ImmutableDictionary<string, string>.Empty);
        }

        private static bool TryGetErrorMessage(
            string language, string fullPath, AnalyzerLoadFailureEventArgs e,
            out string id, out string message, out string messageFormat, out string description)
        {
            switch (e.ErrorCode)
            {
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer:
                    id = Choose(language, WRN_UnableToLoadAnalyzerId, WRN_UnableToLoadAnalyzerIdCS, WRN_UnableToLoadAnalyzerIdVB);
                    messageFormat = FeaturesResources.Unable_to_load_Analyzer_assembly_0_colon_1;
                    message = string.Format(FeaturesResources.Unable_to_load_Analyzer_assembly_0_colon_1, fullPath, e.Message);
                    description = e.Exception.CreateDiagnosticDescription();
                    break;
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer:
                    id = Choose(language, WRN_AnalyzerCannotBeCreatedId, WRN_AnalyzerCannotBeCreatedIdCS, WRN_AnalyzerCannotBeCreatedIdVB);
                    messageFormat = FeaturesResources.An_instance_of_analyzer_0_cannot_be_created_from_1_colon_2;
                    message = string.Format(FeaturesResources.An_instance_of_analyzer_0_cannot_be_created_from_1_colon_2, e.TypeName, fullPath, e.Message);
                    description = e.Exception.CreateDiagnosticDescription();
                    break;
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers:
                    id = Choose(language, WRN_NoAnalyzerInAssemblyId, WRN_NoAnalyzerInAssemblyIdCS, WRN_NoAnalyzerInAssemblyIdVB);
                    messageFormat = FeaturesResources.The_assembly_0_does_not_contain_any_analyzers;
                    message = string.Format(FeaturesResources.The_assembly_0_does_not_contain_any_analyzers, fullPath);
                    description = e.Exception.CreateDiagnosticDescription();
                    break;
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.None:
                default:
                    id = string.Empty;
                    message = string.Empty;
                    messageFormat = string.Empty;
                    description = string.Empty;
                    return false;
            }

            return true;
        }

        private static string Choose(string language, string noLanguageMessage, string csharpMessage, string vbMessage)
        {
            if (language == null)
            {
                return noLanguageMessage;
            }

            return language == LanguageNames.CSharp ? csharpMessage : vbMessage;
        }

        public static void AppendAnalyzerMap(this Dictionary<string, DiagnosticAnalyzer> analyzerMap, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                // user might have included exact same analyzer twice as project analyzers explicitly. we consider them as one
                analyzerMap[analyzer.GetAnalyzerId()] = analyzer;
            }
        }

        public static IEnumerable<AnalyzerPerformanceInfo> ToAnalyzerPerformanceInfo(this IDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analysisResult, IDiagnosticAnalyzerService serviceOpt = null)
        {
            return Convert(analysisResult.Select(kv => (kv.Key, kv.Value.ExecutionTime)), serviceOpt);
        }

        private static IEnumerable<AnalyzerPerformanceInfo> Convert(IEnumerable<(DiagnosticAnalyzer analyzer, TimeSpan timeSpan)> analyzerPerf, IDiagnosticAnalyzerService serviceOpt = null)
        {
            return analyzerPerf.Select(kv => new AnalyzerPerformanceInfo(kv.analyzer.GetAnalyzerId(), DiagnosticAnalyzerLogger.AllowsTelemetry(kv.analyzer, serviceOpt), kv.timeSpan));
        }

        /// <summary>
        /// Get diagnostics for the given analyzers for the given document.
        /// 
        /// this is a simple API to get all diagnostics for the given document.
        /// 
        /// the intended audience for this API is for ones that pefer simplicity over performance such as document that belong to misc project.
        /// this doesn't cache nor use cache for anything. it will re-caculate new diagnostics every time for the given document.
        /// it will not persist any data on disk nor use OOP to calcuate the data.
        /// 
        /// this should never be used when performance is a big concern. for such context, use much complex API from IDiagnosticAnalyzerService
        /// that provide all kinds of knobs/cache/persistency/OOP to get better perf over simplicity
        /// </summary>
        public static async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
           this IDiagnosticAnalyzerService service, Document document, IEnumerable<DiagnosticAnalyzer> analyzers, AnalysisKind kind, CancellationToken cancellationToken)
        {
            // given service must be DiagnosticAnalyzerService
            var diagnosticService = (DiagnosticAnalyzerService)service;

            var analyzerDriverOpt = await diagnosticService.CreateAnalyzerDriverAsync(
                document.Project, analyzers, includeSuppressedDiagnostics: false, logAggregatorOpt: null, cancellationToken).ConfigureAwait(false);

            var builder = ArrayBuilder<DiagnosticData>.GetInstance();
            foreach (var analyzer in analyzers)
            {
                builder.AddRange(await diagnosticService.ComputeDiagnosticsAsync(
                    analyzerDriverOpt, document, analyzer, kind, spanOpt: null, logAggregatorOpt: null, cancellationToken).ConfigureAwait(false));
            }

            return builder.ToImmutableAndFree();
        }

        public static async Task<CompilationWithAnalyzers> CreateAnalyzerDriverAsync(
                this DiagnosticAnalyzerService service,
                Project project,
                IEnumerable<DiagnosticAnalyzer> analyzers,
                bool includeSuppressedDiagnostics,
                DiagnosticLogAggregator logAggregatorOpt,
                CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
            {
                return null;
            }

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Create driver that holds onto compilation and associated analyzers
            var filteredAnalyzers = analyzers.Where(a => !a.IsWorkspaceDiagnosticAnalyzer()).ToImmutableArrayOrEmpty();

            // PERF: there is no analyzers for this compilation.
            //       compilationWithAnalyzer will throw if it is created with no analyzers which is perf optimization.
            if (filteredAnalyzers.IsEmpty)
            {
                return null;
            }

            Contract.ThrowIfFalse(project.SupportsCompilation);
            AssertCompilation(project, compilation);

            // Always run diagnostic suppressors.
            analyzers = AppendDiagnosticSuppressors(analyzers, allAnalyzersAndSuppressors: service.GetDiagnosticAnalyzers(project));

            // Create driver that holds onto compilation and associated analyzers
            return compilation.WithAnalyzers(filteredAnalyzers, GetAnalyzerOptions());

            CompilationWithAnalyzersOptions GetAnalyzerOptions()
            {
                // in IDE, we always set concurrentAnalysis == false otherwise, we can get into thread starvation due to
                // async being used with syncronous blocking concurrency.
                return new CompilationWithAnalyzersOptions(
                    options: new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution.Options, project.Solution),
                    onAnalyzerException: service.GetOnAnalyzerException(project.Id, logAggregatorOpt),
                    analyzerExceptionFilter: GetAnalyzerExceptionFilter(),
                    concurrentAnalysis: false,
                    logAnalyzerExecutionTime: true,
                    reportSuppressedDiagnostics: includeSuppressedDiagnostics);
            }

            Func<Exception, bool> GetAnalyzerExceptionFilter()
            {
                return ex =>
                {
                    if (project.Solution.Workspace.Options.GetOption(InternalDiagnosticsOptions.CrashOnAnalyzerException))
                    {
                        // if option is on, crash the host to get crash dump.
                        FatalError.ReportUnlessCanceled(ex);
                    }

                    return true;
                };
            }
        }

        [Conditional("DEBUG")]
        private static void AssertCompilation(Project project, Compilation compilation1)
        {
            // given compilation must be from given project.
            Contract.ThrowIfFalse(project.TryGetCompilation(out var compilation2));
            Contract.ThrowIfFalse(compilation1 == compilation2);
        }

        /// <summary>
        /// Return all local diagnostics (syntax, semantic) that belong to given document for the given StateSet (analyzer) by calculating them
        /// </summary>
        public static async Task<IEnumerable<DiagnosticData>> ComputeDiagnosticsAsync(
            this DiagnosticAnalyzerService service,
            CompilationWithAnalyzers analyzerDriverOpt,
            Document document,
            DiagnosticAnalyzer analyzer,
            AnalysisKind kind,
            TextSpan? spanOpt,
            DiagnosticLogAggregator logAggregatorOpt,
            CancellationToken cancellationToken)
        {
            if (analyzer is DocumentDiagnosticAnalyzer documentAnalyzer)
            {
                var diagnostics = await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                    service, document, documentAnalyzer, kind, analyzerDriverOpt?.Compilation, logAggregatorOpt, cancellationToken).ConfigureAwait(false);
                return diagnostics.ConvertToLocalDiagnostics(document);
            }

            var documentDiagnostics = await ComputeDiagnosticAnalyzerDiagnosticsAsync().ConfigureAwait(false);

            return documentDiagnostics.ConvertToLocalDiagnostics(document);

            async Task<IEnumerable<Diagnostic>> ComputeDiagnosticAnalyzerDiagnosticsAsync()
            {
                // quick optimization to reduce allocations.
                if (analyzerDriverOpt == null || !service.SupportAnalysisKind(analyzer, document.Project.Language, kind))
                {
                    LogSyntaxInfoWithoutDiagnostics();
                    return ImmutableArray<Diagnostic>.Empty;
                }

                if (!await AnalysisEnabled().ConfigureAwait(false))
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                // REVIEW: more unnecessary allocations just to get diagnostics per analyzer
                var oneAnalyzers = ImmutableArray.Create(analyzer);

                switch (kind)
                {
                    case AnalysisKind.Syntax:
                        var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                        var diagnostics = await analyzerDriverOpt.GetAnalyzerSyntaxDiagnosticsAsync(tree, oneAnalyzers, cancellationToken).ConfigureAwait(false);
                        LogSyntaxInfo(diagnostics, tree);

                        Debug.Assert(diagnostics.Count() == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriverOpt.Compilation).Count());
                        return diagnostics.ToImmutableArrayOrEmpty();
                    case AnalysisKind.Semantic:
                        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                        diagnostics = await analyzerDriverOpt.GetAnalyzerSemanticDiagnosticsAsync(model, spanOpt, oneAnalyzers, cancellationToken).ConfigureAwait(false);

                        Debug.Assert(diagnostics.Count() == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, analyzerDriverOpt.Compilation).Count());
                        return diagnostics.ToImmutableArrayOrEmpty();
                    default:
                        return Contract.FailWithReturn<ImmutableArray<Diagnostic>>("shouldn't reach here");
                }
            }

            async Task<bool> AnalysisEnabled()
            {
                // if project is not loaded successfully then, we disable semantic errors for compiler analyzers
                if (kind == AnalysisKind.Syntax || !service.IsCompilerDiagnosticAnalyzer(document.Project.Language, analyzer))
                {
                    return true;
                }

                var enabled = await document.Project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                Logger.Log(FunctionId.Diagnostics_SemanticDiagnostic, (a, d, e) => $"{a.ToString()}, ({d.Id}, {d.Project.Id}), Enabled:{e}", analyzer, document, enabled);

                return enabled;
            }

            void LogSyntaxInfo(ImmutableArray<Diagnostic> diagnostics, SyntaxTree tree)
            {
                if (!diagnostics.IsDefaultOrEmpty)
                {
                    return;
                }

                Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic,
                    (d, a, t) => $"{d.Id}, {d.Project.Id}, {a.ToString()}, {t.Length}", document, analyzer, tree);
            }

            void LogSyntaxInfoWithoutDiagnostics()
            {
                if (kind != AnalysisKind.Syntax)
                {
                    return;
                }

                Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic,
                    (r, d, a, k) => $"Driver: {r != null}, {d.Id}, {d.Project.Id}, {a.ToString()}, {k}", analyzerDriverOpt, document, analyzer, kind);
            }
        }

        public static bool SupportAnalysisKind(this DiagnosticAnalyzerService service, DiagnosticAnalyzer analyzer, string language, AnalysisKind kind)
        {
            // compiler diagnostic analyzer always support all kinds
            if (service.IsCompilerDiagnosticAnalyzer(language, analyzer))
            {
                return true;
            }

            return kind switch
            {
                AnalysisKind.Syntax => analyzer.SupportsSyntaxDiagnosticAnalysis(),
                AnalysisKind.Semantic => analyzer.SupportsSemanticDiagnosticAnalysis(),
                _ => Contract.FailWithReturn<bool>("shouldn't reach here"),
            };
        }

        public static async Task<IEnumerable<Diagnostic>> ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                this DiagnosticAnalyzerService service,
                Document document,
                DocumentDiagnosticAnalyzer analyzer,
                AnalysisKind kind,
                Compilation compilationOpt,
                DiagnosticLogAggregator logAggregatorOpt,
                CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {

                var analyzeAsync = kind switch
                {
                    AnalysisKind.Syntax => analyzer.AnalyzeSyntaxAsync(document, cancellationToken),
                    AnalysisKind.Semantic => analyzer.AnalyzeSemanticsAsync(document, cancellationToken),
                    _ => throw ExceptionUtilities.UnexpectedValue(kind),
                };

                var diagnostics = (await analyzeAsync.ConfigureAwait(false)).NullToEmpty();
                if (compilationOpt != null)
                {
                    return CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationOpt);
                }

#if DEBUG
                // since all DocumentDiagnosticAnalyzers are from internal users, we only do debug check. also this can be expensive at runtime
                // since it requires await. if we find any offender through NFW, we should be able to fix those since all those should
                // from intern teams.
                await VerifyDiagnosticLocationsAsync(diagnostics, document.Project, cancellationToken).ConfigureAwait(false);
#endif

                return diagnostics;
            }
            catch (Exception e) when (!IsCanceled(e, cancellationToken))
            {
                OnAnalyzerException(service, analyzer, document.Project.Id, compilationOpt, e, logAggregatorOpt);
                return ImmutableArray<Diagnostic>.Empty;
            }
        }

        public static async Task<IEnumerable<Diagnostic>> ComputeProjectDiagnosticAnalyzerDiagnosticsAsync(
                this DiagnosticAnalyzerService service,
                Project project,
                ProjectDiagnosticAnalyzer analyzer,
                Compilation compilationOpt,
                DiagnosticLogAggregator logAggregatorOpt,
                CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var diagnostics = (await analyzer.AnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false)).NullToEmpty();

                // Apply filtering from compilation options (source suppressions, ruleset, etc.)
                if (compilationOpt != null)
                {
                    diagnostics = CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationOpt).ToImmutableArrayOrEmpty();
                }

#if DEBUG
                // since all ProjectDiagnosticAnalyzers are from internal users, we only do debug check. also this can be expensive at runtime
                // since it requires await. if we find any offender through NFW, we should be able to fix those since all those should
                // from intern teams.
                await VerifyDiagnosticLocationsAsync(diagnostics, project, cancellationToken).ConfigureAwait(false);
#endif

                return diagnostics;
            }
            catch (Exception e) when (!IsCanceled(e, cancellationToken))
            {
                OnAnalyzerException(service, analyzer, project.Id, compilationOpt, e, logAggregatorOpt);
                return ImmutableArray<Diagnostic>.Empty;
            }
        }

        private static bool IsCanceled(Exception ex, CancellationToken cancellationToken)
        {
            return (ex as OperationCanceledException)?.CancellationToken == cancellationToken;
        }

        private static void OnAnalyzerException(
            DiagnosticAnalyzerService service, DiagnosticAnalyzer analyzer, ProjectId projectId, Compilation compilationOpt, Exception ex, DiagnosticLogAggregator logAggregatorOpt)
        {
            var exceptionDiagnostic = AnalyzerHelper.CreateAnalyzerExceptionDiagnostic(analyzer, ex);

            if (compilationOpt != null)
            {
                exceptionDiagnostic = CompilationWithAnalyzers.GetEffectiveDiagnostics(ImmutableArray.Create(exceptionDiagnostic), compilationOpt).SingleOrDefault();
            }

            var onAnalyzerException = service.GetOnAnalyzerException(projectId, logAggregatorOpt);
            onAnalyzerException?.Invoke(ex, analyzer, exceptionDiagnostic);
        }

        private static async Task VerifyDiagnosticLocationsAsync(ImmutableArray<Diagnostic> diagnostics, Project project, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                await VerifyDiagnosticLocationAsync(diagnostic.Id, diagnostic.Location).ConfigureAwait(false);

                if (diagnostic.AdditionalLocations != null)
                {
                    foreach (var location in diagnostic.AdditionalLocations)
                    {
                        await VerifyDiagnosticLocationAsync(diagnostic.Id, diagnostic.Location).ConfigureAwait(false);
                    }
                }
            }

            async Task VerifyDiagnosticLocationAsync(string id, Location location)
            {
                switch (location.Kind)
                {
                    case LocationKind.None:
                    case LocationKind.MetadataFile:
                    case LocationKind.XmlFile:
                        // ignore these kinds
                        break;
                    case LocationKind.SourceFile:
                        {
                            if (project.GetDocument(location.SourceTree) == null)
                            {
                                // Disallow diagnostics with source locations outside this project.
                                throw new ArgumentException(string.Format(FeaturesResources.Reported_diagnostic_0_has_a_source_location_in_file_1_which_is_not_part_of_the_compilation_being_analyzed, id, location.SourceTree.FilePath), "diagnostic");
                            }

                            if (location.SourceSpan.End > location.SourceTree.Length)
                            {
                                // Disallow diagnostics with source locations outside this project.
                                throw new ArgumentException(string.Format(FeaturesResources.Reported_diagnostic_0_has_a_source_location_1_in_file_2_which_is_outside_of_the_given_file, id, location.SourceSpan, location.SourceTree.FilePath), "diagnostic");
                            }
                        }
                        break;
                    case LocationKind.ExternalFile:
                        {
                            var filePath = location.GetLineSpan().Path;
                            var document = TryGetDocumentWithFilePath(filePath);
                            if (document == null)
                            {
                                // this is not a roslyn file. we don't care about this file.
                                return;
                            }

                            // this can be potentially expensive since it will load text if it is not already loaded.
                            // but, this text is most likely already loaded since producer of this diagnostic (Document/ProjectDiagnosticAnalyzers)
                            // should have loaded it to produce the diagnostic at the first place. once loaded, it should stay in memory until
                            // project cache goes away. when text is already there, await should return right away.
                            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                            if (location.SourceSpan.End > text.Length)
                            {
                                // Disallow diagnostics with locations outside this project.
                                throw new ArgumentException(string.Format(FeaturesResources.Reported_diagnostic_0_has_a_source_location_1_in_file_2_which_is_outside_of_the_given_file, id, location.SourceSpan, filePath), "diagnostic");
                            }
                        }
                        break;
                    default:
                        throw ExceptionUtilities.Unreachable;
                }
            }

            Document TryGetDocumentWithFilePath(string path)
            {
                foreach (var documentId in project.Solution.GetDocumentIdsWithFilePath(path))
                {
                    if (documentId.ProjectId == project.Id)
                    {
                        return project.GetDocument(documentId);
                    }
                }

                return null;
            }
        }

        public static IEnumerable<DiagnosticData> ConvertToLocalDiagnostics(this IEnumerable<Diagnostic> diagnostics, Document targetDocument, TextSpan? span = null)
        {
            var project = targetDocument.Project;

            if (project.SupportsCompilation)
            {
                return ConvertToLocalDiagnosticsWithCompilation();
            }

            return ConvertToLocalDiagnosticsWithoutCompilation();

            IEnumerable<DiagnosticData> ConvertToLocalDiagnosticsWithoutCompilation()
            {
                Contract.ThrowIfTrue(project.SupportsCompilation);

                foreach (var diagnostic in diagnostics)
                {
                    var location = diagnostic.Location;
                    if (location.Kind != LocationKind.ExternalFile)
                    {
                        continue;
                    }

                    var lineSpan = location.GetLineSpan();

                    var documentIds = project.Solution.GetDocumentIdsWithFilePath(lineSpan.Path);
                    if (documentIds.IsEmpty || documentIds.All(id => id != targetDocument.Id))
                    {
                        continue;
                    }

                    yield return DiagnosticData.Create(targetDocument, diagnostic);
                }
            }

            IEnumerable<DiagnosticData> ConvertToLocalDiagnosticsWithCompilation()
            {
                Contract.ThrowIfFalse(project.SupportsCompilation);

                foreach (var diagnostic in diagnostics)
                {
                    var document = project.GetDocument(diagnostic.Location.SourceTree);
                    if (document == null || document != targetDocument)
                    {
                        continue;
                    }

                    if (span.HasValue && !span.Value.IntersectsWith(diagnostic.Location.SourceSpan))
                    {
                        continue;
                    }

                    yield return DiagnosticData.Create(document, diagnostic);
                }
            }
        }

        public static bool? IsCompilationEndAnalyzer(this DiagnosticAnalyzer analyzer, Project project, Compilation compilation)
        {
            if (!project.SupportsCompilation)
            {
                return false;
            }

            Contract.ThrowIfNull(compilation);

            try
            {
                // currently, this is only way to see whether analyzer has compilation end analysis or not.
                // also, analyzer being compilation end analyzer or not is dynamic. so this can return different value based on
                // given compilation or options.
                //
                // but for now, this is what we decided in design meeting until we decide how to deal with compilation end analyzer
                // long term
                var context = new CollectCompilationActionsContext(compilation, project.AnalyzerOptions);
                analyzer.Initialize(context);

                return context.IsCompilationEndAnalyzer;
            }
            catch
            {
                // analyzer.initialize can throw. when that happens, we will try again next time.
                // we are not logging anything here since it will be logged by CompilationWithAnalyzer later
                // in the error list
                return null;
            }
        }

        public static IEnumerable<DiagnosticAnalyzer> AppendDiagnosticSuppressors(IEnumerable<DiagnosticAnalyzer> analyzers, IEnumerable<DiagnosticAnalyzer> allAnalyzersAndSuppressors)
        {
            // Append while ensuring no duplicates are added.
            var diagnosticSuppressors = allAnalyzersAndSuppressors.OfType<DiagnosticSuppressor>();
            return !diagnosticSuppressors.Any()
                ? analyzers
                : analyzers.Concat(diagnosticSuppressors).Distinct();
        }

        /// <summary>
        /// Right now, there is no API compiler will tell us whether DiagnosticAnalyzer has compilation end analysis or not
        /// 
        /// </summary>
        private class CollectCompilationActionsContext : AnalysisContext
        {
            private readonly Compilation _compilation;
            private readonly AnalyzerOptions _analyzerOptions;

            public CollectCompilationActionsContext(Compilation compilation, AnalyzerOptions analyzerOptions)
            {
                _compilation = compilation;
                _analyzerOptions = analyzerOptions;
            }

            public bool IsCompilationEndAnalyzer { get; private set; } = false;

            public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action)
            {
                if (action == null)
                {
                    return;
                }

                IsCompilationEndAnalyzer = true;
            }

            public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
            {
                if (action == null)
                {
                    return;
                }

                var nestedContext = new CollectNestedCompilationContext(_compilation, _analyzerOptions, CancellationToken.None);
                action(nestedContext);

                IsCompilationEndAnalyzer |= nestedContext.IsCompilationEndAnalyzer;
            }

            #region not used
            public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action) { }
            public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) { }
            public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action) { }
            public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds) { }
            public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) { }
            public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action) { }
            public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags analysisMode) { }
            public override void EnableConcurrentExecution() { }
            public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds) { }
            public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action) { }
            public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action) { }
            public override void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind) { }
            #endregion

            private class CollectNestedCompilationContext : CompilationStartAnalysisContext
            {
                public bool IsCompilationEndAnalyzer { get; private set; } = false;

                public CollectNestedCompilationContext(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
                    : base(compilation, options, cancellationToken)
                {
                }

                public override void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action)
                {
                    if (action == null)
                    {
                        return;
                    }

                    IsCompilationEndAnalyzer = true;
                }

                #region not used
                public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action) { }
                public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) { }
                public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action) { }
                public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds) { }
                public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) { }
                public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action) { }
                public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds) { }
                public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action) { }
                public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action) { }
                public override void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind) { }
                #endregion
            }
        }
    }
}
