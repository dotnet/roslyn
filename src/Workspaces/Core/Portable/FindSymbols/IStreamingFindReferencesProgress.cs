// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Reports the progress of the FindReferences operation.  Note: these methods may be called on
    /// any thread.
    /// </summary>
    internal interface IStreamingFindReferencesProgress
    {
        Task OnStartedAsync(CancellationToken cancellationToken);
        Task OnCompletedAsync(CancellationToken cancellationToken);

        Task OnFindInDocumentStartedAsync(Document document, CancellationToken cancellationToken);
        Task OnFindInDocumentCompletedAsync(Document document, CancellationToken cancellationToken);

        Task OnDefinitionFoundAsync(SymbolAndProjectId symbolAndProjectId, CancellationToken cancellationToken);
        Task OnReferenceFoundAsync(SymbolAndProjectId symbolAndProjectId, ReferenceLocation location, CancellationToken cancellationToken);

        Task ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken);
    }

    internal interface IStreamingFindLiteralReferencesProgress
    {
        Task OnReferenceFoundAsync(Document document, TextSpan span, CancellationToken cancellationToken);
        Task ReportProgressAsync(int current, int maximum, CancellationToken cancellationToken);
    }
}