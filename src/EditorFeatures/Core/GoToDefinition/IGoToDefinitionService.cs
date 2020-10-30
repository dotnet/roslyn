// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface IGoToDefinitionService : ILanguageService, IFindDefinitionService
    {
        /// <summary>
        /// Finds the definitions for the symbol at the specific position in the document and then 
        /// navigates to them.
        /// </summary>
        /// <returns>True if navigating to the definition of the symbol at the provided position succeeds.  False, otherwise.</returns>
        bool TryGoToDefinition(Document document, int position, CancellationToken cancellationToken);
    }
}
