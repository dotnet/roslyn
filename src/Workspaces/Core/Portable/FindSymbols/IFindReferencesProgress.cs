// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// Reports the progress of the FindReferences operation.  Note: these methods may be called on
    /// any thread.
    /// </summary>
    public interface IFindReferencesProgress
    {
        void OnStarted();
        void OnCompleted();

        void OnFindInDocumentStarted(Document document);
        void OnFindInDocumentCompleted(Document document);

        void OnDefinitionFound(ISymbol symbol);
        void OnReferenceFound(ISymbol symbol, ReferenceLocation location);

        void ReportProgress(int current, int maximum);
    }
}
