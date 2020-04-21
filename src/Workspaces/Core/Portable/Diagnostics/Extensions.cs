// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal static partial class Extensions
    {
        public static readonly CultureInfo USCultureInfo = new CultureInfo("en-US");

        public static string? GetBingHelpMessage(this Diagnostic diagnostic, OptionSet options)
        {
            // We use the ENU version of the message for bing search.
            return options.GetOption(InternalDiagnosticsOptions.PutCustomTypeInBingSearch) ?
                diagnostic.GetMessage(USCultureInfo) : diagnostic.Descriptor.GetBingHelpMessage();
        }

        public static string? GetBingHelpMessage(this DiagnosticDescriptor descriptor)
        {
            // We use the ENU version of the message for bing search.
            return descriptor.MessageFormat.ToString(USCultureInfo);
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
            if (locations.Count == 0)
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
            this DiagnosticDataLocation? dataLocation, Project project, CancellationToken cancellationToken)
        {
            if (dataLocation?.DocumentId == null)
            {
                return Location.None;
            }

            var document = project.GetDocument(dataLocation.DocumentId);
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
            this DiagnosticDataLocation dataLocation, SyntacticDocument? document = null)
        {
            if (dataLocation?.DocumentId == null)
            {
                return Location.None;
            }

            if (document == null)
            {
                if (dataLocation.OriginalFilePath == null || dataLocation.SourceSpan == null)
                {
                    return Location.None;
                }

                var span = dataLocation.SourceSpan.Value;
                return Location.Create(dataLocation.OriginalFilePath, span, new LinePositionSpan(
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
            return type.AssemblyQualifiedName ?? throw ExceptionUtilities.UnexpectedValue(type);
        }

        public static ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder> ToResultBuilderMap(
            this AnalysisResult analysisResult,
            Project project, VersionStamp version, Compilation compilation, IEnumerable<DiagnosticAnalyzer> analyzers,
            SkippedHostAnalyzersInfo skippedAnalyzersInfo,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, DiagnosticAnalysisResultBuilder>();

            ImmutableArray<Diagnostic> diagnostics;

            foreach (var analyzer in analyzers)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (skippedAnalyzersInfo.SkippedAnalyzers.Contains(analyzer))
                {
                    continue;
                }

                var result = new DiagnosticAnalysisResultBuilder(project, version);
                var diagnosticIdsToFilter = skippedAnalyzersInfo.FilteredDiagnosticIdsForAnalyzers.GetValueOrDefault(
                    analyzer,
                    ImmutableArray<string>.Empty);

                foreach (var (tree, diagnosticsByAnalyzerMap) in analysisResult.SyntaxDiagnostics)
                {
                    if (diagnosticsByAnalyzerMap.TryGetValue(analyzer, out diagnostics))
                    {
                        diagnostics = diagnostics.Filter(diagnosticIdsToFilter);
                        Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation).Count());
                        result.AddSyntaxDiagnostics(tree, diagnostics);
                    }
                }

                foreach (var (tree, diagnosticsByAnalyzerMap) in analysisResult.SemanticDiagnostics)
                {
                    if (diagnosticsByAnalyzerMap.TryGetValue(analyzer, out diagnostics))
                    {
                        diagnostics = diagnostics.Filter(diagnosticIdsToFilter);
                        Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation).Count());
                        result.AddSemanticDiagnostics(tree, diagnostics);
                    }
                }

                if (analysisResult.CompilationDiagnostics.TryGetValue(analyzer, out diagnostics))
                {
                    diagnostics = diagnostics.Filter(diagnosticIdsToFilter);
                    Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, compilation).Count());
                    result.AddCompilationDiagnostics(diagnostics);
                }

                builder.Add(analyzer, result);
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Filters out the diagnostics with the specified <paramref name="diagnosticIdsToFilter"/>.
        /// </summary>
        public static ImmutableArray<Diagnostic> Filter(this ImmutableArray<Diagnostic> diagnostics, ImmutableArray<string> diagnosticIdsToFilter)
        {
            if (diagnosticIdsToFilter.IsEmpty)
            {
                return diagnostics;
            }

            return diagnostics.RemoveAll(diagnostic => diagnosticIdsToFilter.Contains(diagnostic.Id));
        }
    }
}
