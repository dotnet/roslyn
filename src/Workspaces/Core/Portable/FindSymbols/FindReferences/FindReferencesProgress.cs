// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.FindSymbols
{
    /// <summary>
    /// A class that reports the current progress made when finding references to symbols.  
    /// </summary>
    internal class FindReferencesProgress : IFindReferencesProgress
    {
        public static readonly IFindReferencesProgress Instance = new FindReferencesProgress();

        private FindReferencesProgress()
        {
        }

        public void ReportProgress(int current, int maximum)
        {
        }

        public void OnCompleted()
        {
        }

        public void OnStarted()
        {
        }

        public void OnDefinitionFound(ISymbol symbol)
        {
        }

        public void OnReferenceFound(ISymbol symbol, ReferenceLocation location)
        {
        }

        public void OnFindInDocumentStarted(Document document)
        {
        }

        public void OnFindInDocumentCompleted(Document document)
        {
        }
    }
}
