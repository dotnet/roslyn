// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Reports the progress of the FindReferences operation.  Note: these methods may be called on
    /// any thread.
    /// </summary>
    internal interface IStreamingFindReferencesProgress
    {
        Task OnStartedAsync();
        Task OnCompletedAsync();

        Task OnFindInDocumentStartedAsync(Document document);
        Task OnFindInDocumentCompletedAsync(Document document);

        Task OnDefinitionFoundAsync(SymbolAndProjectId symbolAndProjectId);
        Task OnReferenceFoundAsync(SymbolAndProjectId symbolAndProjectId, ReferenceLocation location);

        Task ReportProgressAsync(int current, int maximum);
    }
}