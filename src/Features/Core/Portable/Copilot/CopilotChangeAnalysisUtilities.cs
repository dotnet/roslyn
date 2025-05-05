// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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

                d[$"{keyPrefix}_ComputationTime"] = diagnosticAnalysis.ComputationTime;
                d[$"{keyPrefix}_IdToCount"] = GetOrderedElements(diagnosticAnalysis.IdToCount);
                d[$"{keyPrefix}_CategoryToCount"] = GetOrderedElements(diagnosticAnalysis.CategoryToCount);
                d[$"{keyPrefix}_SeverityToCount"] = GetOrderedElements(diagnosticAnalysis.SeverityToCount);
            }

            d["CodeFixAnalysis_TotalComputationTime"] = analysisResult.CodeFixAnalysis.TotalComputationTime;
            d["CodeFixAnalysis_TotalApplicationTime"] = analysisResult.CodeFixAnalysis.TotalApplicationTime;
            d["CodeFixAnalysis_DiagnosticIdToCount"] = GetOrderedElements(analysisResult.CodeFixAnalysis.DiagnosticIdToCount);
            d["CodeFixAnalysis_DiagnosticIdToApplicationTime"] = GetOrderedElements(analysisResult.CodeFixAnalysis.DiagnosticIdToApplicationTime);
            d["CodeFixAnalysis_DiagnosticIdToProviderName"] = GetOrderedElements(analysisResult.CodeFixAnalysis.DiagnosticIdToProviderName);
            d["CodeFixAnalysis_ProviderNameToApplicationTime"] = GetOrderedElements(analysisResult.CodeFixAnalysis.ProviderNameToApplicationTime);
        }, args: (featureId, accepted, proposalId, analysisResult)),
        cancellationToken);
    }

    private static List<string> GetOrderedElements<TKey, TValue>(Dictionary<TKey, TValue> dictionary) where TKey : notnull
        => [.. dictionary.Select(kvp => $"{kvp.Key}_{GetOrdered(kvp.Value)}").OrderBy(v => v)];

    private static object? GetOrdered<TValue>(TValue value)
    {
        if (value is IEnumerable<string> strings)
            return string.Join(":", strings.OrderBy(v => v));

        return value;
    }
}
