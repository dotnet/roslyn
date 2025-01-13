// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class ValidateBreakableRange
{
    public static async Task<LinePositionSpan?> GetBreakableRangeAsync(Document document, LinePositionSpan span, CancellationToken cancellationToken)
    {
        var range = ProtocolConversions.LinePositionToRange(span);
        var result = await ValidateBreakableRangeHandler.GetBreakableRangeAsync(document, range, cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return null;
        }

        return ProtocolConversions.RangeToLinePositionSpan(result);
    }
}
