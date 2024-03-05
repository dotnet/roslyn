// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Snippets.SnippetProviders;

internal interface ISnippetProvider
{
    /// <summary>
    /// What we use to identify each SnippetProvider on the completion list
    /// </summary>
    string Identifier { get; }

    /// <summary>
    /// What is being displayed for the inline-description of the snippet as well as the title on the tool-tip.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Determines if a snippet can exist at a particular location.
    /// </summary>
    ValueTask<SnippetData?> GetSnippetDataAsync(SnippetContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Gets the Snippet from the corresponding snippet provider.
    /// </summary>
    Task<SnippetChange> GetSnippetAsync(Document document, int position, CancellationToken cancellationToken);
}
