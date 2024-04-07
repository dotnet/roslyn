// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Snippets.SnippetProviders;

namespace Microsoft.CodeAnalysis.Snippets;

internal interface ISnippetService : ILanguageService
{
    /// <summary>
    /// Retrieves all possible types of snippets for a particular position
    /// </summary>
    Task<ImmutableArray<SnippetData>> GetSnippetsAsync(SnippetContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the corresponding provider from a snippet identifier.
    /// Called upon by the AbstractSnippetCompletionProvider
    /// </summary>
    ISnippetProvider GetSnippetProvider(string snippetIdentifier);
}
