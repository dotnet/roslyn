// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed partial class DocumentAnalysisExecutor
    {
        // These are the error codes of the compiler warnings. 
        // Keep the ids the same so that de-duplication against compiler errors
        // works in the error list (after a build).
        internal const string WRN_AnalyzerCannotBeCreatedIdCS = "CS8032";
        internal const string WRN_AnalyzerCannotBeCreatedIdVB = "BC42376";
        internal const string WRN_NoAnalyzerInAssemblyIdCS = "CS8033";
        internal const string WRN_NoAnalyzerInAssemblyIdVB = "BC42377";
        internal const string WRN_UnableToLoadAnalyzerIdCS = "CS8034";
        internal const string WRN_UnableToLoadAnalyzerIdVB = "BC42378";
        internal const string WRN_AnalyzerReferencesNetFrameworkIdCS = "CS8850";
        internal const string WRN_AnalyzerReferencesNetFrameworkIdVB = "BC42503";
        internal const string WRN_AnalyzerReferencesNewerCompilerIdCS = "CS9057";
        internal const string WRN_AnalyzerReferencesNewerCompilerIdVB = "BC42506";

        // Shared with Compiler
        internal const string AnalyzerExceptionDiagnosticId = "AD0001";
        internal const string AnalyzerDriverExceptionDiagnosticId = "AD0002";

        // IDE only errors
        internal const string WRN_AnalyzerCannotBeCreatedId = "AD1000";
        internal const string WRN_NoAnalyzerInAssemblyId = "AD1001";
        internal const string WRN_UnableToLoadAnalyzerId = "AD1002";
        internal const string WRN_AnalyzerReferencesNetFrameworkId = "AD1003";
        internal const string WRN_AnalyzerReferencesNewerCompilerId = "AD1004";

        private const string AnalyzerExceptionDiagnosticCategory = "Intellisense";

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

        public static DiagnosticData CreateAnalyzerLoadFailureDiagnostic(AnalyzerLoadFailureEventArgs e, string fullPath, ProjectId? projectId, string? language)
        {
            static string GetLanguageSpecificId(string? language, string noLanguageId, string csharpId, string vbId)
                => language == null ? noLanguageId : (language == LanguageNames.CSharp) ? csharpId : vbId;

            string id, message;

            switch (e.ErrorCode)
            {
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer:
                    id = GetLanguageSpecificId(language, WRN_UnableToLoadAnalyzerId, WRN_UnableToLoadAnalyzerIdCS, WRN_UnableToLoadAnalyzerIdVB);
                    message = string.Format(FeaturesResources.Unable_to_load_Analyzer_assembly_0_colon_1, fullPath, e.Message);
                    break;

                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer:
                    id = GetLanguageSpecificId(language, WRN_AnalyzerCannotBeCreatedId, WRN_AnalyzerCannotBeCreatedIdCS, WRN_AnalyzerCannotBeCreatedIdVB);
                    message = string.Format(FeaturesResources.An_instance_of_analyzer_0_cannot_be_created_from_1_colon_2, e.TypeName, fullPath, e.Message);
                    break;

                case AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers:
                    id = GetLanguageSpecificId(language, WRN_NoAnalyzerInAssemblyId, WRN_NoAnalyzerInAssemblyIdCS, WRN_NoAnalyzerInAssemblyIdVB);
                    message = string.Format(FeaturesResources.The_assembly_0_does_not_contain_any_analyzers, fullPath);
                    break;

                case AnalyzerLoadFailureEventArgs.FailureErrorCode.ReferencesFramework:
                    id = GetLanguageSpecificId(language, WRN_AnalyzerReferencesNetFrameworkId, WRN_AnalyzerReferencesNetFrameworkIdCS, WRN_AnalyzerReferencesNetFrameworkIdVB);
                    message = string.Format(FeaturesResources.The_assembly_0_containing_type_1_references_NET_Framework, fullPath, e.TypeName);
                    break;

                case AnalyzerLoadFailureEventArgs.FailureErrorCode.ReferencesNewerCompiler:
                    id = GetLanguageSpecificId(language, WRN_AnalyzerReferencesNewerCompilerId, WRN_AnalyzerReferencesNewerCompilerIdCS, WRN_AnalyzerReferencesNewerCompilerIdVB);
                    message = string.Format(FeaturesResources.The_assembly_0_references_compiler_version_1_newer_than_2, fullPath, e.ReferencedCompilerVersion, typeof(AnalyzerLoadFailureEventArgs).Assembly.GetName().Version);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(e.ErrorCode);
            }

            var description = e.Exception.CreateDiagnosticDescription();

            return new DiagnosticData(
                id,
                FeaturesResources.Roslyn_HostError,
                message,
                severity: DiagnosticSeverity.Warning,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                warningLevel: 0,
                customTags: [],
                properties: ImmutableDictionary<string, string?>.Empty,
                projectId: projectId,
                location: new DiagnosticDataLocation(new FileLinePositionSpan(fullPath, span: default)),
                description: description,
                language: language);
        }

        public static async Task<CompilationWithAnalyzers?> CreateCompilationWithAnalyzersAsync(
            Project project,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            bool includeSuppressedDiagnostics,
            bool crashOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation == null)
            {
                // project doesn't support compilation
                return null;
            }

            // Create driver that holds onto compilation and associated analyzers
            var filteredAnalyzers = analyzers.WhereAsArray(a => !a.IsWorkspaceDiagnosticAnalyzer());

            // PERF: there is no analyzers for this compilation.
            //       compilationWithAnalyzer will throw if it is created with no analyzers which is perf optimization.
            if (filteredAnalyzers.IsEmpty)
            {
                return null;
            }

            Contract.ThrowIfFalse(project.SupportsCompilation);
            AssertCompilation(project, compilation);

            // in IDE, we always set concurrentAnalysis == false otherwise, we can get into thread starvation due to
            // async being used with synchronous blocking concurrency.
            var analyzerOptions = new CompilationWithAnalyzersOptions(
                options: project.AnalyzerOptions,
                onAnalyzerException: null,
                analyzerExceptionFilter: GetAnalyzerExceptionFilter(),
                concurrentAnalysis: false,
                logAnalyzerExecutionTime: true,
                reportSuppressedDiagnostics: includeSuppressedDiagnostics);

            // Create driver that holds onto compilation and associated analyzers
            return compilation.WithAnalyzers(filteredAnalyzers, analyzerOptions);

            Func<Exception, bool> GetAnalyzerExceptionFilter()
            {
                return ex =>
                {
                    if (ex is not OperationCanceledException && crashOnAnalyzerException)
                    {
                        // report telemetry
                        FatalError.ReportAndPropagate(ex);

                        // force fail fast (the host might not crash when reporting telemetry):
                        FailFast.OnFatalException(ex);
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
        /// Return true if the given <paramref name="analyzer"/> is not suppressed for the given project.
        /// NOTE: This API is intended to be used only for performance optimization.
        /// </summary>
        public static bool IsAnalyzerEnabledForProject(DiagnosticAnalyzer analyzer, Project project, IGlobalOptionService globalOptions)
        {
            var options = project.CompilationOptions;
            if (options == null || analyzer == FileContentLoadAnalyzer.Instance || analyzer == GeneratorDiagnosticsPlaceholderAnalyzer.Instance)
            {
                return true;
            }

            if (analyzer.IsCompilerAnalyzer())
            {
                return globalOptions.GetBackgroundCompilerAnalysisScope(project.Language) != CompilerDiagnosticsScope.None;
            }

            // Check if user has disabled analyzer execution for this project or via options.
            if (!project.State.RunAnalyzers || globalOptions.GetBackgroundAnalysisScope(project.Language) == BackgroundAnalysisScope.None)
            {
                return false;
            }

            // NOTE: Previously we used to return "CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(options)"
            //       on this code path, which returns true if analyzer is suppressed through compilation options.
            //       However, this check is no longer correct as analyzers can be enabled/disabled for individual
            //       documents through .editorconfig files. So we pessimistically assume analyzer is not suppressed
            //       and let the core analyzer driver in the compiler layer handle skipping redundant analysis callbacks.
            return true;
        }

        public static async Task<ImmutableArray<Diagnostic>> ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
            DocumentDiagnosticAnalyzer analyzer,
            Document document,
            AnalysisKind kind,
            Compilation? compilation,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ImmutableArray<Diagnostic> diagnostics;
            try
            {
                var analyzeAsync = kind switch
                {
                    AnalysisKind.Syntax => analyzer.AnalyzeSyntaxAsync(document, cancellationToken),
                    AnalysisKind.Semantic => analyzer.AnalyzeSemanticsAsync(document, cancellationToken),
                    _ => throw ExceptionUtilities.UnexpectedValue(kind),
                };

                diagnostics = (await analyzeAsync.ConfigureAwait(false)).NullToEmpty();

#if DEBUG
                // since all DocumentDiagnosticAnalyzers are from internal users, we only do debug check. also this can be expensive at runtime
                // since it requires await. if we find any offender through NFW, we should be able to fix those since all those should
                // from intern teams.
                await VerifyDiagnosticLocationsAsync(diagnostics, document.Project, cancellationToken).ConfigureAwait(false);
#endif
            }
            catch (Exception e) when (!IsCanceled(e, cancellationToken))
            {
                diagnostics = [CreateAnalyzerExceptionDiagnostic(analyzer, e)];
            }

            if (compilation != null)
            {
                diagnostics = CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation).ToImmutableArrayOrEmpty();
            }

            return diagnostics;
        }

        public static async Task<ImmutableArray<Diagnostic>> ComputeProjectDiagnosticAnalyzerDiagnosticsAsync(
            ProjectDiagnosticAnalyzer analyzer,
            Project project,
            Compilation? compilation,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ImmutableArray<Diagnostic> diagnostics;
            try
            {
                diagnostics = (await analyzer.AnalyzeProjectAsync(project, cancellationToken).ConfigureAwait(false)).NullToEmpty();
#if DEBUG
                // since all ProjectDiagnosticAnalyzers are from internal users, we only do debug check. also this can be expensive at runtime
                // since it requires await. if we find any offender through NFW, we should be able to fix those since all those should
                // from intern teams.
                await VerifyDiagnosticLocationsAsync(diagnostics, project, cancellationToken).ConfigureAwait(false);
#endif
            }
            catch (Exception e) when (!IsCanceled(e, cancellationToken))
            {
                diagnostics = [CreateAnalyzerExceptionDiagnostic(analyzer, e)];
            }

            // Apply filtering from compilation options (source suppressions, ruleset, etc.)
            if (compilation != null)
            {
                diagnostics = CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation).ToImmutableArrayOrEmpty();
            }

            return diagnostics;
        }

        private static bool IsCanceled(Exception ex, CancellationToken cancellationToken)
            => (ex as OperationCanceledException)?.CancellationToken == cancellationToken;

#if DEBUG
        private static async Task VerifyDiagnosticLocationsAsync(ImmutableArray<Diagnostic> diagnostics, Project project, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                await VerifyDiagnosticLocationAsync(diagnostic.Id, diagnostic.Location).ConfigureAwait(false);

                if (diagnostic.AdditionalLocations != null)
                {
                    foreach (var location in diagnostic.AdditionalLocations)
                    {
                        await VerifyDiagnosticLocationAsync(diagnostic.Id, location).ConfigureAwait(false);
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
                            RoslynDebug.Assert(location.SourceTree != null);
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
                            var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);
                            if (location.SourceSpan.End > text.Length)
                            {
                                // Disallow diagnostics with locations outside this project.
                                throw new ArgumentException(string.Format(FeaturesResources.Reported_diagnostic_0_has_a_source_location_1_in_file_2_which_is_outside_of_the_given_file, id, location.SourceSpan, filePath), "diagnostic");
                            }
                        }

                        break;
                    default:
                        throw ExceptionUtilities.Unreachable();
                }
            }

            Document? TryGetDocumentWithFilePath(string path)
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
#endif

        public static IEnumerable<DiagnosticData> ConvertToLocalDiagnostics(IEnumerable<Diagnostic> diagnostics, TextDocument targetTextDocument, TextSpan? span = null)
        {
            foreach (var diagnostic in diagnostics)
            {
                if (!IsReportedInDocument(diagnostic, targetTextDocument))
                {
                    continue;
                }

                if (span.HasValue && !span.Value.IntersectsWith(diagnostic.Location.SourceSpan))
                {
                    continue;
                }

                yield return DiagnosticData.Create(diagnostic, targetTextDocument);
            }

            static bool IsReportedInDocument(Diagnostic diagnostic, TextDocument targetTextDocument)
            {
                if (diagnostic.Location.SourceTree != null)
                {
                    return targetTextDocument.Project.GetDocument(diagnostic.Location.SourceTree) == targetTextDocument;
                }
                else if (diagnostic.Location.Kind == LocationKind.ExternalFile)
                {
                    var lineSpan = diagnostic.Location.GetLineSpan();

                    var documentIds = targetTextDocument.Project.Solution.GetDocumentIdsWithFilePath(lineSpan.Path);
                    return documentIds.Any(static (id, targetTextDocument) => id == targetTextDocument.Id, targetTextDocument);
                }

                return false;
            }
        }

#if DEBUG
        internal static bool AreEquivalent(Diagnostic[] diagnosticsA, Diagnostic[] diagnosticsB)
        {
            var set = new HashSet<Diagnostic>(diagnosticsA, DiagnosticComparer.Instance);
            return set.SetEquals(diagnosticsB);
        }

        private sealed class DiagnosticComparer : IEqualityComparer<Diagnostic?>
        {
            internal static readonly DiagnosticComparer Instance = new();

            public bool Equals(Diagnostic? x, Diagnostic? y)
            {
                if (x is null)
                    return y is null;
                else if (y is null)
                    return false;

                return x.Id == y.Id && x.Location == y.Location;
            }

            public int GetHashCode(Diagnostic? obj)
            {
                if (obj is null)
                    return 0;

                return Hash.Combine(obj.Id.GetHashCode(), obj.Location.GetHashCode());
            }
        }
#endif
    }
}
