// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Razor;

[Method(SemanticRangesMethodName)]
internal class SemanticTokensRangesHandler : ILspServiceRequestHandler<SemanticTokensRangesParams, SemanticTokens>
{
    private const int TokenSize = 5;
    public const string SemanticRangesMethodName = "textDocument/semanticTokens/ranges";
    private readonly IGlobalOptionService _globalOptions;
    private readonly SemanticTokensRefreshQueue _semanticTokenRefreshQueue;

    public bool MutatesSolutionState => false;

    public bool RequiresLSPSolution => true;

    public SemanticTokensRangesHandler(
        IGlobalOptionService globalOptions,
        SemanticTokensRefreshQueue semanticTokensRefreshQueue)
    {
        _globalOptions = globalOptions;
        _semanticTokenRefreshQueue = semanticTokensRefreshQueue;
    }

    public async Task<SemanticTokens> HandleRequestAsync(
            SemanticTokensRangesParams request,
            RequestContext context,
            CancellationToken cancellationToken)
    {
        Contract.ThrowIfNull(request.TextDocument, "TextDocument is null.");
        if (request.Ranges.Length == 0)
        {
            return new SemanticTokens() { Data = Array.Empty<int>() };
        }

        var contextDocument = context.GetRequiredDocument();
        var document = contextDocument.WithFrozenPartialSemantics(cancellationToken);
        var project = document.Project;
        var options = _globalOptions.GetClassificationOptions(project.Language) with { ForceFrozenPartialSemanticsForCrossProcessOperations = true };
        var capabilities = context.GetRequiredClientCapabilities();
        var responseData = new List<int[]>();
        foreach (var range in request.Ranges)
        {
            var semanticTokens = await SemanticTokensHelpers.ComputeSemanticTokensDataAsync(
                capabilities,
                document,
                range,
                options,
                cancellationToken).ConfigureAwait(false);

            if (semanticTokens is not null && semanticTokens.Length != 0)
            {
                responseData.Add(semanticTokens);
            }
        }

        await _semanticTokenRefreshQueue.TryEnqueueRefreshComputationAsync(project, cancellationToken).ConfigureAwait(false);
        return new SemanticTokens() { Data = StitchSemanticTokenResponsesTogether(responseData) };
    }

    private static int[] StitchSemanticTokenResponsesTogether(List<int[]> responseData)
    {
        // Each inner array in `responseData` represents a single C# document that is broken down into a list of tokens.
        // This method stitches these lists of tokens together into a single, coherent list of semantic tokens.
        // The resulting array is a flattened version of the input array, and is in the precise format expected by the Microsoft Language Server Protocol.
        if (responseData.Count == 0)
        {
            return Array.Empty<int>();
        }

        if (responseData.Count == 1)
        {
            return responseData[0];
        }

        var count = responseData.Sum(r => r.Length);
        var data = new int[count];
        var dataIndex = 0;
        var lastTokenLine = 0;

        for (var i = 0; i < responseData.Count; i++)
        {
            var curData = responseData[i];

            if (curData.Length == 0)
            {
                continue;
            }

            Array.Copy(curData, 0, data, dataIndex, curData.Length);
            if (i != 0)
            {
                // The first two items in result.Data will potentially need it's line/col offset modified
                var lineDelta = data[dataIndex] - lastTokenLine;
                Debug.Assert(lineDelta >= 0);

                // Update the first line copied over from curData
                data[dataIndex] = lineDelta;

                // Update the first column copied over from curData if on the same line as the previous token
                if (lineDelta == 0)
                {
                    var lastTokenCol = 0;

                    // Walk back accumulating column deltas until we find a start column (indicated by it's line offset being non-zero)
                    for (var j = dataIndex - TokenSize; j >= 0; j -= TokenSize)
                    {
                        lastTokenCol += data[j + 1];
                        if (data[j] != 0)
                        {
                            break;
                        }
                    }

                    Debug.Assert(lastTokenCol >= 0);
                    data[dataIndex + 1] -= lastTokenCol;
                    Debug.Assert(data[dataIndex + 1] >= 0);
                }
            }

            lastTokenLine = 0;
            for (var j = 0; j < curData.Length; j += TokenSize)
            {
                lastTokenLine += curData[j];
            }

            dataIndex += curData.Length;
        }

        return data;
    }
}
