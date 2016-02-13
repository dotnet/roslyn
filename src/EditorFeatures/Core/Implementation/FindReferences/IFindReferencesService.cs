// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IFindReferencesService : ILanguageService
    {
        /// <summary>
        /// Finds the references for the symbol at the specific position in the document.
        /// </summary>
        Task<IEnumerable<INavigableItem>> FindReferencesAsync(Document document, int position, IWaitContext waitContext);

        /// <summary>
        /// Finds the references for the symbol at the specific position in the document and then 
        /// presents them.
        /// </summary>
        /// <returns>True if finding references of the symbol at the provided position succeeds.  False, otherwise.</returns>
        bool TryFindReferences(Document document, int position, IWaitContext waitContext);
    }
}
