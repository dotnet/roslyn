// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.MethodImplementation;
using static Microsoft.CodeAnalysis.CSharp.Copilot.DocumentAnalyzer;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

internal static class CopilotCodeProvider
{
    public static async Task<string> GetCopilotSuggestedCodeBlockAsync(ICopilotCodeAnalysisService copilotService, AnalysisRecord analysisRecord, CancellationToken cancellationToken)
    {
        // Get the Copilot service and fetch the method implementation
        var proposal = GenerateProposalFromAnalysisRecord(analysisRecord);
        var (dictionary, isQuotaExceeded) = await copilotService.GetMethodImplementationAsync(proposal, cancellationToken).ConfigureAwait(false);

        // Quietly fail if the quota has been exceeded.
        if (isQuotaExceeded || dictionary is null || dictionary.Count() == 0)
        {
            return string.Empty;
        }

        var fullMethodImplementation = dictionary.First().Value;
        return ExtractMethodBodyContent(fullMethodImplementation);
    }

    private static MethodImplementationProposal GenerateProposalFromAnalysisRecord(AnalysisRecord analysisRecord)
    {
        return new MethodImplementationProposal(
            analysisRecord.MethodName,
            analysisRecord.ReturnType,
            analysisRecord.ContainingType,
            analysisRecord.Accessibility.ToString().ToLower(),
            analysisRecord.Modifiers,
            analysisRecord.Parameters.Select(p => new MethodImplementationParameterInfo(
                p.Identifier.Text,
                p.Type?.ToString() ?? string.Empty,
                p.Modifiers.Select(m => m.Text).ToImmutableArray())).ToImmutableArray(),
            analysisRecord.PreviousToken.Text,
            analysisRecord.NextToken.Text,
            // evaluate if needed at all
            ImmutableArray<MethodImplementationProposedEdit>.Empty
            );
    }

    private static string ExtractMethodBodyContent(string fullMethodImplementation)
    {
        // Handle null or empty input
        if (string.IsNullOrEmpty(fullMethodImplementation))
        {
            return string.Empty;
        }

        // Find the opening brace position
        var openBraceIndex = fullMethodImplementation.IndexOf('{');
        if (openBraceIndex == -1)
        {
            return string.Empty;
        }

        // Find the last closing brace position
        var closeBraceIndex = fullMethodImplementation.LastIndexOf('}');
        if (closeBraceIndex == -1 || closeBraceIndex <= openBraceIndex)
        {
            return string.Empty;
        }

        // Extract the content between braces
        var content = fullMethodImplementation.Substring(
            openBraceIndex + 1,
            closeBraceIndex - openBraceIndex - 1);

        // Split into lines, trim each line, and remove empty lines at start/end
        var lines = content.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .SkipWhile(string.IsNullOrWhiteSpace)
            .Reverse()
            .SkipWhile(string.IsNullOrWhiteSpace)
            .Reverse();

        // Rejoin the lines with newlines
        return string.Join("\n", lines);
    }
}
