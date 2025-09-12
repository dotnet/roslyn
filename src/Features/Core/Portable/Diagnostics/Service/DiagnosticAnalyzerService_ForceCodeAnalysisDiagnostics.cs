// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    public async ValueTask<ImmutableArray<DiagnosticData>> ForceRunCodeAnalysisDiagnosticsInProcessAsync(
        Project project, CancellationToken cancellationToken)
    {
        // We are being asked to explicitly analyze this project.  As such we do *not* want to use the
        // default rules determining which analyzers to run.  For example, even if compiler diagnostics
        // are set to 'none' for live diagnostics, we still want to run them here.
        //
        // As such, we are very intentionally not calling into this.GetDefaultAnalyzerFilter
        // here.  We want to control the rules entirely when this is called.
        var analyzers = GetProjectAnalyzers_OnlyCallInProcess(project);
        var filteredAnalyzers = analyzers.WhereAsArray(ShouldIncludeAnalyzer);

        // Compute document and project diagnostics in parallel.

        // Compute all the diagnostics for all the documents in the project.
        var documentDiagnosticsTask = GetDiagnosticsForIdsAsync();

        // Then all the non-document diagnostics for that project as well.
        var projectDiagnosticsTask = this.GetProjectDiagnosticsForIdsInProcessAsync(
            project, diagnosticIds: null, filteredAnalyzers, cancellationToken);

        await Task.WhenAll(documentDiagnosticsTask, projectDiagnosticsTask).ConfigureAwait(false);

        return [.. await documentDiagnosticsTask.ConfigureAwait(false), .. await projectDiagnosticsTask.ConfigureAwait(false)];

        async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsForIdsAsync()
        {

            // Note: in this case we want diagnostics for source generated documents as well.  So ensure those are 
            // generated and included in the results.
            var sourceGeneratorDocuments = await project.GetSourceGeneratedDocumentsAsync(cancellationToken).ConfigureAwait(false);

            return await this.GetDiagnosticsForIdsInProcessAsync(
                project, [.. project.DocumentIds, .. project.AdditionalDocumentIds, .. sourceGeneratorDocuments.Select(d => d.Id)],
                diagnosticIds: null, filteredAnalyzers, includeLocalDocumentDiagnostics: true, cancellationToken).ConfigureAwait(false);
        }

        bool ShouldIncludeAnalyzer(DiagnosticAnalyzer analyzer)
        {
            if (analyzer == FileContentLoadAnalyzer.Instance ||
                analyzer == GeneratorDiagnosticsPlaceholderAnalyzer.Instance ||
                analyzer.IsCompilerAnalyzer())
            {
                return true;
            }

            if (analyzer.IsBuiltInAnalyzer())
            {
                // always return true for builtin analyzer. we can't use
                // descriptor check since many builtin analyzer always return 
                // hidden descriptor regardless what descriptor it actually
                // return on runtime. they do this so that they can control
                // severity through option page rather than rule set editor.
                // this is special behavior only ide analyzer can do. we hope
                // once we support editorconfig fully, third party can use this
                // ability as well and we can remove this kind special treatment on builtin
                // analyzer.
                return true;
            }

            if (analyzer is DiagnosticSuppressor)
            {
                // Always execute diagnostic suppressors.
                return true;
            }

            if (project.CompilationOptions is null)
            {
                // Skip compilation options based checks for non-C#/VB projects.
                return true;
            }

            // For most of analyzers, the number of diagnostic descriptors is small, so this should be cheap.
            var descriptors = this._analyzerInfoCache.GetDiagnosticDescriptors(analyzer);
            var analyzerConfigOptions = project.GetAnalyzerConfigOptions();
            return descriptors.Any(static (d, arg) =>
            {
                var severity = d.GetEffectiveSeverity(
                    arg.CompilationOptions,
                    arg.analyzerConfigOptions?.ConfigOptionsWithFallback,
                    arg.analyzerConfigOptions?.TreeOptions);
                return severity != ReportDiagnostic.Hidden;
            },
            (project.CompilationOptions, analyzerConfigOptions));
        }
    }
}
