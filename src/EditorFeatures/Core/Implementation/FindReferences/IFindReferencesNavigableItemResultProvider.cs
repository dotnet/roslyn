// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Navigation;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IFindReferencesResultProvider
    {
        /// <summary>
        /// Compute navigable reference items for the symbol referenced or defined at the given position in the given document.
        /// </summary>
        Task<IEnumerable<INavigableItem>> FindReferencesAsync(Document document, int position, CancellationToken cancellationToken);
    }
}
