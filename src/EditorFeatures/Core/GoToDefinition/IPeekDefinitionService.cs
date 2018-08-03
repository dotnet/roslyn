// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IPeekDefinitionService : ILanguageService
    {
        /// <summary>
        /// Gets <see cref="IPeekableItem"/>s for the definitions for the symbol at the specific position in the document.
        /// </summary>
        /// <remarks>
        /// A more direct version of <see cref="IGoToDefinitionService.FindDefinitionsAsync"/> that returns <see cref="IPeekableItem"/>s.
        /// Called from the UI thread.
        /// </remarks>
        IEnumerable<IPeekableItem> GetDefinitionPeekItems(Document document, int position, CancellationToken cancellationToken);
    }
}
