// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        Task OnStartedAsync();
        Task OnCompletedAsync();

        Task OnFindInDocumentStartedAsync(Document document);
        Task OnFindInDocumentCompletedAsync(Document document);

        Task OnDefinitionFoundAsync(SymbolAndProjectId symbolAndProjectId);
        Task OnReferenceFoundAsync(SymbolAndProjectId symbolAndProjectId, ReferenceLocation location);

        Task ReportProgressAsync(int current, int maximum);
    }

    internal interface IStreamingFindLiteralReferencesProgress
    {
        Task OnReferenceFoundAsync(Document document, TextSpan span);
        Task ReportProgressAsync(int current, int maximum);
    }
}
