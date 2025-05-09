// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;

namespace Microsoft.CodeAnalysis.Copilot;

internal static class CopilotChangeAnalysisUtilities
{
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
            return timeSpan.TotalMilliseconds.ToString("G17");

        return value.ToString() ?? "";
    }
}
