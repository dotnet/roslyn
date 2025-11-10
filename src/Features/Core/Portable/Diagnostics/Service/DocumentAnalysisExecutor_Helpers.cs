// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    private sealed partial class DocumentAnalysisExecutor
    {
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
            TextDocument document,
            AnalysisKind kind,
            Compilation? compilation,
            SyntaxTree? tree,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ImmutableArray<Diagnostic> diagnostics;
            try
            {
                diagnostics = kind switch
                {
                    AnalysisKind.Syntax => await analyzer.AnalyzeSyntaxAsync(document, tree, cancellationToken).ConfigureAwait(false),
                    AnalysisKind.Semantic => await analyzer.AnalyzeSemanticsAsync(document, tree, cancellationToken).ConfigureAwait(false),
                    _ => throw ExceptionUtilities.UnexpectedValue(kind),
                };

                diagnostics = diagnostics.NullToEmpty();

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
