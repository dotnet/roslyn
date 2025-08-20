// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotChangeAnalysisUtilities
{
    /// <summary>
    /// Analyzes and collects interesting data about an edit made by some copilot feature, and reports that back as
    /// telemetry to help inform what automatic fixing features we should invest in.
    /// </summary>
    /// <param name="document">The document being edited.  The document should represent the contents of hte file
    /// prior to the <paramref name="textChanges"/> being applied.</param>
    /// <param name="accepted">Whether or not the user accepted the copilot suggestion, or rejected it.  Used to
    /// determine if there are interesting issues occurring that might be leading to the user rejecting the change
    /// (for example, excessive syntax errors).</param>
    /// <param name="featureId">The name of the feature making the text change.  For example 'Completion'.  Used
    /// to bucket information by feature area in case certain feature produce different sets of diagnostics or 
    /// fixes commonly.</param>
    /// <param name="proposalId">Copilot proposal id (generally a stringified <see cref="Guid"/>).  Used to be able
    /// to map from one of these proposed edits to any additional telemetry stored in other tables about this copilot
    /// interaction.</param>
    /// <param name="textChanges">The actual text changes to make.  The text changes do not have to be normalized.
    /// Though they should not overlap.  If they overlap, this request will be ignored.  These would be the changes
    /// passed to <see cref="SourceText.WithChanges(IEnumerable{TextChange})"/> for the text snapshot corresponding to
    /// <paramref name="document"/>.</param>
    public static async Task AnalyzeCopilotChangeAsync(
        Document document,
        bool accepted,
        string featureId,
        string proposalId,
        ImmutableArray<TextChange> textChanges,
        CancellationToken cancellationToken)
    {
        // Currently we do not support analyzing languges other than C# and VB.  This is because we only want to do
        // this analsis in our OOP process to avoid perf impact on the VS process.  And we don't have OOP for other
        // languages yet.
        if (!document.SupportsSemanticModel)
            return;

        if (textChanges.IsDefaultOrEmpty)
            return;

        var normalizedEdits = CopilotUtilities.TryNormalizeCopilotTextChanges(textChanges);
        if (normalizedEdits.IsDefaultOrEmpty)
            return;

        var changeAnalysisService = document.Project.Solution.Services.GetRequiredService<ICopilotChangeAnalysisService>();
        var analysisResult = await changeAnalysisService.AnalyzeChangeAsync(
            document, normalizedEdits, cancellationToken).ConfigureAwait(false);

        LogCopilotChangeAnalysis(
            featureId, accepted, proposalId, analysisResult, cancellationToken).Dispose();
    }

    public static IDisposable LogCopilotChangeAnalysis(
        string featureId, bool accepted, string proposalId, CopilotChangeAnalysis analysisResult, CancellationToken cancellationToken)
    {
        return Logger.LogBlock(FunctionId.Copilot_AnalyzeChange, KeyValueLogMessage.Create(static (d, args) =>
        {
            var (featureId, accepted, proposalId, analysisResult) = args;
            d["Accepted"] = accepted;
            d["FeatureId"] = featureId;
            d["ProposalId"] = proposalId;

            d["Succeeded"] = analysisResult.Succeeded;

            d["OldDocumentLength"] = analysisResult.OldDocumentLength;
            d["NewDocumentLength"] = analysisResult.NewDocumentLength;
            d["TextChangeDelta"] = analysisResult.TextChangeDelta;

            d["ProjectDocumentCount"] = analysisResult.ProjectDocumentCount;
            d["ProjectSourceGeneratedDocumentCount"] = analysisResult.ProjectSourceGeneratedDocumentCount;
            d["ProjectConeCount"] = analysisResult.ProjectConeCount;

            foreach (var diagnosticAnalysis in analysisResult.DiagnosticAnalyses)
            {
                var keyPrefix = $"DiagnosticAnalysis_{diagnosticAnalysis.Kind}";

                d[$"{keyPrefix}_ComputationTime"] = Stringify(diagnosticAnalysis.ComputationTime);
                d[$"{keyPrefix}_IdToCount"] = StringifyDictionary(diagnosticAnalysis.IdToCount);
                d[$"{keyPrefix}_CategoryToCount"] = StringifyDictionary(diagnosticAnalysis.CategoryToCount);
                d[$"{keyPrefix}_SeverityToCount"] = StringifyDictionary(diagnosticAnalysis.SeverityToCount);
            }

            d["CodeFixAnalysis_TotalComputationTime"] = Stringify(analysisResult.CodeFixAnalysis.TotalComputationTime);
            d["CodeFixAnalysis_TotalApplicationTime"] = Stringify(analysisResult.CodeFixAnalysis.TotalApplicationTime);
            d["CodeFixAnalysis_DiagnosticIdToCount"] = StringifyDictionary(analysisResult.CodeFixAnalysis.DiagnosticIdToCount);
            d["CodeFixAnalysis_DiagnosticIdToApplicationTime"] = StringifyDictionary(analysisResult.CodeFixAnalysis.DiagnosticIdToApplicationTime);
            d["CodeFixAnalysis_DiagnosticIdToProviderName"] = StringifyDictionary(analysisResult.CodeFixAnalysis.DiagnosticIdToProviderName);
            d["CodeFixAnalysis_ProviderNameToApplicationTime"] = StringifyDictionary(analysisResult.CodeFixAnalysis.ProviderNameToApplicationTime);
            d["CodeFixAnalysis_ProviderNameToHasConflict"] = StringifyDictionary(analysisResult.CodeFixAnalysis.ProviderNameToHasConflict);
            d["CodeFixAnalysis_ProviderNameToTotalCount"] = StringifyDictionary(analysisResult.CodeFixAnalysis.ProviderNameToTotalCount);
            d["CodeFixAnalysis_ProviderNameToSuccessCount"] = StringifyDictionary(analysisResult.CodeFixAnalysis.ProviderNameToSuccessCount);
        }, args: (featureId, accepted, proposalId, analysisResult)),
        cancellationToken);
    }

    private static string StringifyDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary) where TKey : notnull where TValue : notnull
        => string.Join(",", dictionary.Select(kvp => FormattableString.Invariant($"{kvp.Key}_{Stringify(kvp.Value)}")).OrderBy(v => v));

    private static string Stringify<TValue>(TValue value) where TValue : notnull
    {
        if (value is IEnumerable<string> strings)
            return string.Join(":", strings.OrderBy(v => v));

        if (value is TimeSpan timeSpan)
            return timeSpan.TotalMilliseconds.ToString("G17", CultureInfo.InvariantCulture);

        return value.ToString() ?? "";
    }
}
