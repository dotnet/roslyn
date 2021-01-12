// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.GoToDefinition
{
    internal interface IFindDefinitionService : ILanguageService
    {
        /// <summary>
        /// Finds the definitions for the symbol at the specific position in the document.
        /// </summary>
        Task<ImmutableArray<INavigableItem>> FindDefinitionsAsync(Document document, int position, CancellationToken cancellationToken);
    }
}
