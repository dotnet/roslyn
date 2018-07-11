// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static class Extensions
    {
        public static readonly CultureInfo s_USCultureInfo = new CultureInfo("en-US");

        public static string GetBingHelpMessage(this Diagnostic diagnostic, Workspace workspace)
        {
            var option = GetCustomTypeInBingSearchOption(workspace);

            // We use the ENU version of the message for bing search.
            return option ? diagnostic.GetMessage(s_USCultureInfo) : diagnostic.Descriptor.GetBingHelpMessage();
        }

        public static string GetBingHelpMessage(this DiagnosticDescriptor descriptor)
        {
            // We use the ENU version of the message for bing search.
            return descriptor.MessageFormat.ToString(s_USCultureInfo);
        }

        private static bool GetCustomTypeInBingSearchOption(Workspace workspace)
        {
            if (workspace == null)
            {
                return false;
            }

            return workspace.Options.GetOption(InternalDiagnosticsOptions.PutCustomTypeInBingSearch);
        }

        public static DiagnosticData GetPrimaryDiagnosticData(this CodeFix fix)
        {
            return fix.PrimaryDiagnostic.ToDiagnosticData(fix.Project);
        }

        public static ImmutableArray<DiagnosticData> GetDiagnosticData(this CodeFix fix)
        {
            return fix.Diagnostics.SelectAsArray(d => d.ToDiagnosticData(fix.Project));
        }

        public static DiagnosticData ToDiagnosticData(this Diagnostic diagnostic, Project project)
        {
            if (diagnostic.Location.IsInSource)
            {
                return DiagnosticData.Create(project.GetDocument(diagnostic.Location.SourceTree), diagnostic);
            }

            if (diagnostic.Location.Kind == LocationKind.ExternalFile)
            {
                var document = project.Documents.FirstOrDefault(d => d.FilePath == diagnostic.Location.GetLineSpan().Path);
                if (document != null)
                {
                    return DiagnosticData.Create(document, diagnostic);
                }
            }

            return DiagnosticData.Create(project, diagnostic);
        }

        public static async Task<ImmutableArray<Diagnostic>> ToDiagnosticsAsync(this IEnumerable<DiagnosticData> diagnostics, Project project, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<Diagnostic>.GetInstance();
            foreach (var diagnostic in diagnostics)
            {
                result.Add(await diagnostic.ToDiagnosticAsync(project, cancellationToken).ConfigureAwait(false));
            }

            return result.ToImmutableAndFree();
        }

        public static async Task<IList<Location>> ConvertLocationsAsync(
            this IReadOnlyCollection<DiagnosticDataLocation> locations, Project project, CancellationToken cancellationToken)
        {
            if (locations == null || locations.Count == 0)
            {
                return SpecializedCollections.EmptyList<Location>();
            }

            var result = new List<Location>();
            foreach (var data in locations)
            {
                var location = await data.ConvertLocationAsync(project, cancellationToken).ConfigureAwait(false);
                result.Add(location);
            }

            return result;
        }

        public static async Task<Location> ConvertLocationAsync(
            this DiagnosticDataLocation dataLocation, Project project, CancellationToken cancellationToken)
        {
            if (dataLocation?.DocumentId == null)
            {
                return Location.None;
            }

            var document = project.GetDocument(dataLocation?.DocumentId);
            if (document == null)
            {
                return Location.None;
            }


            if (document.SupportsSyntaxTree)
            {
                var syntacticDocument = await SyntacticDocument.CreateAsync(document, cancellationToken).ConfigureAwait(false);
                return dataLocation.ConvertLocation(syntacticDocument);
            }

            return dataLocation.ConvertLocation();
        }

        public static Location ConvertLocation(
            this DiagnosticDataLocation dataLocation, SyntacticDocument document = null)
        {
            if (dataLocation?.DocumentId == null)
            {
                return Location.None;
            }

            if (document == null)
            {
                if (dataLocation?.OriginalFilePath == null || dataLocation.SourceSpan == null)
                {
                    return Location.None;
                }

                var span = dataLocation.SourceSpan.Value;
                return Location.Create(dataLocation?.OriginalFilePath, span, new LinePositionSpan(
                    new LinePosition(dataLocation.OriginalStartLine, dataLocation.OriginalStartColumn),
                    new LinePosition(dataLocation.OriginalEndLine, dataLocation.OriginalEndColumn)));
            }

            Contract.ThrowIfFalse(dataLocation.DocumentId == document.Document.Id);

            var syntaxTree = document.SyntaxTree;
            return syntaxTree.GetLocation(dataLocation.SourceSpan ?? DiagnosticData.GetTextSpan(dataLocation, document.Text));
        }

        public static string GetAnalyzerId(this DiagnosticAnalyzer analyzer)
        {
            // Get the unique ID for given diagnostic analyzer.
            var type = analyzer.GetType();
            return GetAssemblyQualifiedName(type);
        }

        private static string GetAssemblyQualifiedName(Type type)
        {
            // AnalyzerFileReference now includes things like versions, public key as part of its identity. 
            // so we need to consider them.
            return type.AssemblyQualifiedName;
        }

        public static ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder> ToResultBuilderMap(
            this AnalysisResult analysisResult,
            Project project, VersionStamp version, Compilation compilation, IEnumerable<DiagnosticAnalyzer> analyzers,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder>();

            ImmutableArray<Diagnostic> diagnostics;

            foreach (var analyzer in analyzers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = new DiagnosticAnalysisResultBuilder(project, version);

                foreach (var (tree, diagnosticsByAnalyzerMap) in analysisResult.SyntaxDiagnostics)
                {
                    if (diagnosticsByAnalyzerMap.TryGetValue(analyzer, out diagnostics))
                    {
                        Contract.Requires(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation).Count());
                        result.AddSyntaxDiagnostics(tree, diagnostics);
                    }
                }

                foreach (var (tree, diagnosticsByAnalyzerMap) in analysisResult.SemanticDiagnostics)
                {
                    if (diagnosticsByAnalyzerMap.TryGetValue(analyzer, out diagnostics))
                    {
                        Contract.Requires(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation).Count());
                        result.AddSemanticDiagnostics(tree, diagnostics);
                    }
                }

                if (analysisResult.CompilationDiagnostics.TryGetValue(analyzer, out diagnostics))
                {
                    Contract.Requires(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation).Count());
                    result.AddCompilationDiagnostics(diagnostics);
                }

                builder.Add(analyzer, result);
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Returns the equivalent <see cref="DiagnosticSeverity"/> for a <see cref="ReportDiagnostic"/> value.
        /// </summary>
        /// <param name="reportDiagnostic">The <see cref="ReportDiagnostic"/> value.</param>
        /// <returns>
        /// The equivalent <see cref="DiagnosticSeverity"/> for a <see cref="ReportDiagnostic"/> value; otherwise,
        /// <see langword="null"/> if <see cref="DiagnosticSeverity"/> does not contain a direct equivalent for
        /// <paramref name="reportDiagnostic"/>.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// If <paramref name="reportDiagnostic"/> is not one of the expected values.
        /// </exception>
        public static DiagnosticSeverity? ToDiagnosticSeverity(this ReportDiagnostic reportDiagnostic)
        {
            switch (reportDiagnostic)
            {
            case ReportDiagnostic.Error:
                return DiagnosticSeverity.Error;

            case ReportDiagnostic.Warn:
                return DiagnosticSeverity.Warning;

            case ReportDiagnostic.Info:
                return DiagnosticSeverity.Info;

            case ReportDiagnostic.Hidden:
                return DiagnosticSeverity.Hidden;

            case ReportDiagnostic.Suppress:
            case ReportDiagnostic.Default:
                return null;

            default:
                throw ExceptionUtilities.UnexpectedValue(reportDiagnostic);
            }
        }

        /// <summary>
        /// Applies a default severity to a <see cref="ReportDiagnostic"/> value.
        /// </summary>
        /// <param name="reportDiagnostic">The <see cref="ReportDiagnostic"/> value.</param>
        /// <param name="defaultSeverity">The default severity.</param>
        /// <returns>
        /// <para>If <paramref name="reportDiagnostic"/> is <see cref="ReportDiagnostic.Default"/>, returns
        /// <paramref name="defaultSeverity"/>.</para>
        /// <para>-or-</para>
        /// <para>Otherwise, returns <paramref name="reportDiagnostic"/> if it has a non-default value.</para>
        /// </returns>
        public static ReportDiagnostic WithDefaultSeverity(this ReportDiagnostic reportDiagnostic, DiagnosticSeverity defaultSeverity)
        {
            if (reportDiagnostic != ReportDiagnostic.Default)
            {
                return reportDiagnostic;
            }

            return defaultSeverity.ToReportDiagnostic();
        }

        /// <summary>
        /// Returns the equivalent <see cref="ReportDiagnostic"/> for a <see cref="DiagnosticSeverity"/> value.
        /// </summary>
        /// <param name="diagnosticSeverity">The <see cref="DiagnosticSeverity"/> value.</param>
        /// <returns>
        /// The equivalent <see cref="ReportDiagnostic"/> for the <see cref="DiagnosticSeverity"/> value.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// If <paramref name="diagnosticSeverity"/> is not one of the expected values.
        /// </exception>
        public static ReportDiagnostic ToReportDiagnostic(this DiagnosticSeverity diagnosticSeverity)
        {
            switch (diagnosticSeverity)
            {
            case DiagnosticSeverity.Hidden:
                return ReportDiagnostic.Hidden;

            case DiagnosticSeverity.Info:
                return ReportDiagnostic.Info;

            case DiagnosticSeverity.Warning:
                return ReportDiagnostic.Warn;

            case DiagnosticSeverity.Error:
                return ReportDiagnostic.Error;

            default:
                throw ExceptionUtilities.UnexpectedValue(diagnosticSeverity);
            }
        }
    }
}
