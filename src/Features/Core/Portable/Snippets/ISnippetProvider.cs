// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Snippets
{
    internal interface ISnippetProvider
    {
        /// What we use to identify each SnippetProvider for easier retrieval
        string SnippetIdentifier { get; }

        /// What is being displayed for the Snippet on the Completion list
        string SnippetDisplayName { get; }

        /// Determines if a snippet can exist at a particular location.
        Task<SnippetData?> GetSnippetDataAsync(Document document, int position, CancellationToken cancellationToken);

        /// Gets the Snippet from the corresponding snippet provider.
        Task<Snippet> GetSnippetAsync(Document document, int position, CancellationToken cancellationToken);
    }
}
