// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class AnalyzerHelper
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
            => analyzer is DocumentDiagnosticAnalyzer || analyzer is ProjectDiagnosticAnalyzer || analyzer == FileContentLoadAnalyzer.Instance;

        public static bool IsBuiltInAnalyzer(this DiagnosticAnalyzer analyzer)
            => analyzer is IBuiltInAnalyzer || analyzer.IsWorkspaceDiagnosticAnalyzer() || analyzer.IsCompilerAnalyzer();

        public static bool IsOpenFileOnly(this DiagnosticAnalyzer analyzer, OptionSet options)
            => analyzer is IBuiltInAnalyzer builtInAnalyzer && builtInAnalyzer.OpenFileOnly(options);

        public static ReportDiagnostic GetEffectiveSeverity(this DiagnosticDescriptor descriptor, CompilationOptions options)
        {
            return options == null
                ? descriptor.DefaultSeverity.ToReportDiagnostic()
                : descriptor.GetEffectiveSeverity(options);
        }

        public static bool IsCompilerAnalyzer(this DiagnosticAnalyzer analyzer)
        {
            // TODO: find better way.
            var typeString = analyzer.GetType().FullName;
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
            => analyzer.GetType().Assembly.GetName().Name ?? throw ExceptionUtilities.Unreachable;

        public static OptionSet GetAnalyzerOptionSet(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var optionSetAsync = GetAnalyzerOptionSetAsync(analyzerOptions, syntaxTree, cancellationToken);
            if (optionSetAsync.IsCompleted)
                return optionSetAsync.Result;

            return optionSetAsync.AsTask().GetAwaiter().GetResult();
        }

        public static async ValueTask<OptionSet> GetAnalyzerOptionSetAsync(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var configOptions = analyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(syntaxTree);
#pragma warning disable CS0612 // Type or member is obsolete
            var optionSet = await GetDocumentOptionSetAsync(analyzerOptions, syntaxTree, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS0612 // Type or member is obsolete

            return new AnalyzerConfigOptionSet(configOptions, optionSet);
        }

        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, ILanguageSpecificOption<T> option, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var optionAsync = GetOptionAsync<T>(analyzerOptions, option, language: null, syntaxTree, cancellationToken);
            if (optionAsync.IsCompleted)
                return optionAsync.Result;

            return optionAsync.AsTask().GetAwaiter().GetResult();
        }

        public static T GetOption<T>(this AnalyzerOptions analyzerOptions, IPerLanguageOption<T> option, string? language, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            var optionAsync = GetOptionAsync<T>(analyzerOptions, option, language, syntaxTree, cancellationToken);
            if (optionAsync.IsCompleted)
                return optionAsync.Result;

            return optionAsync.AsTask().GetAwaiter().GetResult();
        }

        public static async ValueTask<T> GetOptionAsync<T>(this AnalyzerOptions analyzerOptions, IOption option, string? language, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            if (analyzerOptions.TryGetEditorConfigOption<T>(option, syntaxTree, out var value))
            {
                return value;
            }

#pragma warning disable CS0612 // Type or member is obsolete
            var optionSet = await analyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS0612 // Type or member is obsolete
            return (T)optionSet?.GetOption(new OptionKey(option, language)) ?? (T)option.DefaultValue!;
        }

        [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/23582", OftenCompletesSynchronously = true)]
        public static ValueTask<OptionSet?> GetDocumentOptionSetAsync(this AnalyzerOptions analyzerOptions, SyntaxTree syntaxTree, CancellationToken cancellationToken)
        {
            if (!(analyzerOptions is WorkspaceAnalyzerOptions workspaceAnalyzerOptions))
            {
                return new ValueTask<OptionSet?>((OptionSet?)null);
            }

            return workspaceAnalyzerOptions.GetDocumentOptionSetAsync(syntaxTree, cancellationToken);
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

        public static DiagnosticData CreateAnalyzerLoadFailureDiagnostic(AnalyzerLoadFailureEventArgs e, string fullPath, ProjectId? projectId, string? language)
        {
            static string GetLanguageSpecificId(string? language, string noLanguageId, string csharpId, string vbId)
                => language == null ? noLanguageId : (language == LanguageNames.CSharp) ? csharpId : vbId;

            string id, messageFormat, message;

            switch (e.ErrorCode)
            {
                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToLoadAnalyzer:
                    id = GetLanguageSpecificId(language, WRN_UnableToLoadAnalyzerId, WRN_UnableToLoadAnalyzerIdCS, WRN_UnableToLoadAnalyzerIdVB);
                    messageFormat = FeaturesResources.Unable_to_load_Analyzer_assembly_0_colon_1;
                    message = string.Format(FeaturesResources.Unable_to_load_Analyzer_assembly_0_colon_1, fullPath, e.Message);
                    break;

                case AnalyzerLoadFailureEventArgs.FailureErrorCode.UnableToCreateAnalyzer:
                    id = GetLanguageSpecificId(language, WRN_AnalyzerCannotBeCreatedId, WRN_AnalyzerCannotBeCreatedIdCS, WRN_AnalyzerCannotBeCreatedIdVB);
                    messageFormat = FeaturesResources.An_instance_of_analyzer_0_cannot_be_created_from_1_colon_2;
                    message = string.Format(FeaturesResources.An_instance_of_analyzer_0_cannot_be_created_from_1_colon_2, e.TypeName, fullPath, e.Message);
                    break;

                case AnalyzerLoadFailureEventArgs.FailureErrorCode.NoAnalyzers:
                    id = GetLanguageSpecificId(language, WRN_NoAnalyzerInAssemblyId, WRN_NoAnalyzerInAssemblyIdCS, WRN_NoAnalyzerInAssemblyIdVB);
                    messageFormat = FeaturesResources.The_assembly_0_does_not_contain_any_analyzers;
                    message = string.Format(FeaturesResources.The_assembly_0_does_not_contain_any_analyzers, fullPath);
                    break;

                default:
                    throw ExceptionUtilities.UnexpectedValue(e.ErrorCode);
            }

            var description = e.Exception.CreateDiagnosticDescription();

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
                properties: ImmutableDictionary<string, string>.Empty,
                language: language);
        }

        public static void AppendAnalyzerMap(this Dictionary<string, DiagnosticAnalyzer> analyzerMap, IEnumerable<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                // user might have included exact same analyzer twice as project analyzers explicitly. we consider them as one
                analyzerMap[analyzer.GetAnalyzerId()] = analyzer;
            }
        }

        public static IEnumerable<AnalyzerPerformanceInfo> ToAnalyzerPerformanceInfo(this IDictionary<DiagnosticAnalyzer, AnalyzerTelemetryInfo> analysisResult, DiagnosticAnalyzerInfoCache analyzerInfo)
            => analysisResult.Select(kv => new AnalyzerPerformanceInfo(kv.Key.GetAnalyzerId(), analyzerInfo.IsTelemetryCollectionAllowed(kv.Key), kv.Value.ExecutionTime));

        public static async Task<CompilationWithAnalyzers?> CreateCompilationWithAnalyzersAsync(
            Project project,
            IEnumerable<DiagnosticAnalyzer> analyzers,
            bool includeSuppressedDiagnostics,
            CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation == null)
            {
                // project doesn't support compilation
                return null;
            }

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

            // in IDE, we always set concurrentAnalysis == false otherwise, we can get into thread starvation due to
            // async being used with synchronous blocking concurrency.
            var analyzerOptions = new CompilationWithAnalyzersOptions(
                options: new WorkspaceAnalyzerOptions(project.AnalyzerOptions, project.Solution),
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
            DiagnosticAnalyzer analyzer,
            Document document,
            AnalysisKind kind,
            CompilationWithAnalyzers? compilationWithAnalyzers,
            Func<Project, ISkippedAnalyzersInfo>? getSkippedAnalyzersInfo,
            TextSpan? span,
            CancellationToken cancellationToken)
        {
            var loadDiagnostic = await document.State.GetLoadDiagnosticAsync(cancellationToken).ConfigureAwait(false);

            if (analyzer == FileContentLoadAnalyzer.Instance)
            {
                return loadDiagnostic != null ?
                    SpecializedCollections.SingletonEnumerable(DiagnosticData.Create(loadDiagnostic, document)) :
                    SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            if (loadDiagnostic != null)
            {
                return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            if (analyzer is DocumentDiagnosticAnalyzer documentAnalyzer)
            {
                var diagnostics = await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                    documentAnalyzer, document, kind, compilationWithAnalyzers?.Compilation, cancellationToken).ConfigureAwait(false);

                return diagnostics.ConvertToLocalDiagnostics(document);
            }

            // quick optimization to reduce allocations.
            if (compilationWithAnalyzers == null || !analyzer.SupportAnalysisKind(kind))
            {
                if (kind == AnalysisKind.Syntax)
                {
                    Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic,
                        (r, d, a, k) => $"Driver: {r != null}, {d.Id}, {d.Project.Id}, {a}, {k}", compilationWithAnalyzers, document, analyzer, kind);
                }

                return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            // if project is not loaded successfully then, we disable semantic errors for compiler analyzers
            if (kind != AnalysisKind.Syntax && analyzer.IsCompilerAnalyzer())
            {
                var isEnabled = await document.Project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                Logger.Log(FunctionId.Diagnostics_SemanticDiagnostic, (a, d, e) => $"{a}, ({d.Id}, {d.Project.Id}), Enabled:{e}", analyzer, document, isEnabled);

                if (!isEnabled)
                {
                    return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                }
            }

            // REVIEW: more unnecessary allocations just to get diagnostics per analyzer
            var singleAnalyzer = ImmutableArray.Create(analyzer);
            var skippedAnalyzerInfo = getSkippedAnalyzersInfo?.Invoke(document.Project) ?? SkippedHostAnalyzersInfo.Default;
            ImmutableArray<string> filteredIds;

            switch (kind)
            {
                case AnalysisKind.Syntax:
                    var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    if (tree == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                    }

                    var diagnostics = await compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(tree, singleAnalyzer, cancellationToken).ConfigureAwait(false);

                    if (diagnostics.IsDefaultOrEmpty)
                    {
                        Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic, (d, a, t) => $"{d.Id}, {d.Project.Id}, {a}, {t.Length}", document, analyzer, tree);
                    }
                    else if (skippedAnalyzerInfo.FilteredDiagnosticIdsForAnalyzers.TryGetValue(analyzer, out filteredIds))
                    {
                        diagnostics = diagnostics.Filter(filteredIds);
                    }

                    Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationWithAnalyzers.Compilation).Count());
                    return diagnostics.ConvertToLocalDiagnostics(document);

                case AnalysisKind.Semantic:
                    var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    if (model == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                    }

                    diagnostics = await compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(model, span, singleAnalyzer, cancellationToken).ConfigureAwait(false);

                    if (skippedAnalyzerInfo.FilteredDiagnosticIdsForAnalyzers.TryGetValue(analyzer, out filteredIds))
                    {
                        diagnostics = diagnostics.Filter(filteredIds);
                    }

                    Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilationWithAnalyzers.Compilation).Count());
                    return diagnostics.ConvertToLocalDiagnostics(document);

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
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
                diagnostics = ImmutableArray.Create(CreateAnalyzerExceptionDiagnostic(analyzer, e));
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
                diagnostics = ImmutableArray.Create(CreateAnalyzerExceptionDiagnostic(analyzer, e));
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

                    yield return DiagnosticData.Create(diagnostic, targetDocument);
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

                    yield return DiagnosticData.Create(diagnostic, document);
                }
            }
        }

        public static bool? IsCompilationEndAnalyzer(this DiagnosticAnalyzer analyzer, Project project, Compilation? compilation)
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
