// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.MethodImplementation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;
internal sealed partial class CSharpCopilotNotImplementedMethodFixProvider
{
    internal static class CodeProvider
    {
        public static async Task<string> SuggestCodeBlockAsync(ICopilotCodeAnalysisService copilotService, Document document, TextSpan? textSpan, MethodImplementationProposal proposal, CancellationToken cancellationToken)
        {
            // Get the Copilot service and fetch the method implementation
            var (dictionary, isQuotaExceeded) = await copilotService.ImplementNotImplementedExceptionAsync(document, textSpan, proposal, cancellationToken).ConfigureAwait(false);

            // Quietly fail if the quota has been exceeded.
            return !isQuotaExceeded && dictionary?.TryGetValue("implementation", out var result) == true
                ? result
                : string.Empty;
        }
    }
}
