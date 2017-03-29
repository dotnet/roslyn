// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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