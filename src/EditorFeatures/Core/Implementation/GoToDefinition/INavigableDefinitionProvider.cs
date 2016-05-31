// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Navigation;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface INavigableDefinitionProvider
    {
        /// <summary>
        /// Find definitions for the symbol referenced or defined at the given position in the given document.
        /// </summary>
        Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken);

        /// <summary>
        /// Find definitions for the given symbol in the context of the given project.
        /// </summary>
        Task<IEnumerable<INavigableItem>> FindDefinitionsAsync(Project project, ISymbol symbol, CancellationToken cancellationToken);
    }
}
