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

namespace Microsoft.CodeAnalysis.CSharp.Copilot;
internal sealed partial class CSharpCopilotNotImplementedMethodFixProvider
{
    internal static class CodeProvider
    {
        public static async Task<string> SuggestCodeBlockAsync(ICopilotCodeAnalysisService copilotService, MethodImplementationProposal analysisRecord, CancellationToken cancellationToken)
        {
            // Get the Copilot service and fetch the method implementation
            var (dictionary, isQuotaExceeded) = await copilotService.GetMethodImplementationAsync(analysisRecord, cancellationToken).ConfigureAwait(false);

            // Quietly fail if the quota has been exceeded.
            if (isQuotaExceeded ||
                dictionary is null ||
                dictionary.Count() == 0 ||
                !dictionary.ContainsKey("Implementation"))
            {
                return string.Empty;
            }

            return dictionary["Implementation"];
        }
    }
}
