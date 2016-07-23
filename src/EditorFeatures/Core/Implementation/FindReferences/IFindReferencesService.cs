// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    internal struct FindReferencesResult
    {
        public Solution Solution { get; }
        public ImmutableArray<INavigableItem> Items { get; }

        public FindReferencesResult(Solution solution, ImmutableArray<INavigableItem> items)
        {
            Solution = solution;
            Items = items;
        }
    }

    internal interface IFindReferencesService : ILanguageService
    {
        /// <summary>
        /// Finds the references for the symbol at the specific position in the document and then 
        /// presents them.
        /// </summary>
        /// <returns>True if finding references of the symbol at the provided position succeeds.  False, otherwise.</returns>
        Task<FindReferencesResult?> FindReferencesAsync(Document document, int position, IWaitContext waitContext);
    }
}