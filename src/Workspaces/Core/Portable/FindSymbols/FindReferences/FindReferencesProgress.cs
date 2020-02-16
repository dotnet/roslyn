// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


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
