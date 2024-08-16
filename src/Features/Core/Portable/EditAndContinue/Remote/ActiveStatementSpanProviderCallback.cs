// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.EditAndContinue;

internal sealed class ActiveStatementSpanProviderCallback(ActiveStatementSpanProvider provider)
{
    private readonly ActiveStatementSpanProvider _provider = provider;

    /// <summary>
    /// Remote API.
    /// </summary>
    public async ValueTask<ImmutableArray<ActiveStatementSpan>> GetSpansAsync(DocumentId? documentId, string filePath, CancellationToken cancellationToken)
    {
        try
        {
            return await _provider(documentId, filePath, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, cancellationToken))
        {
            return [];
        }
    }
}
