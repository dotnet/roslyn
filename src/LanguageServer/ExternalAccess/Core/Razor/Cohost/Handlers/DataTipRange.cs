// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Text;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

internal static class DataTipRange
{
    public static Task<VSInternalDataTip?> GetDataTipRangeAsync(Document document, LinePosition linePosition, CancellationToken cancellationToken)
        => DataTipRangeHandler.GetDataTipRangeAsync(document, linePosition, cancellationToken);
}
