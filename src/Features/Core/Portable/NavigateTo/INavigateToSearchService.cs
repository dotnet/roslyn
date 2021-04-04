// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface INavigateToSearchService : ILanguageService
    {
        IImmutableSet<string> KindsProvided { get; }
        bool CanFilter { get; }

        /// <summary>
        /// Searches the documents inside <paramref name="project"/> for symbols that matches
        /// <paramref name="searchPattern"/>. <paramref name="priorityDocuments"/> is an optional
        /// subset of the documents from <paramref name="project"/> that can be used to prioritize
        /// work.
        /// </summary>
        Task SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, Func<INavigateToSearchResult, Task> onResultFound, bool isFullyLoaded, CancellationToken cancellationToken);
        Task SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, Func<INavigateToSearchResult, Task> onResultFound, bool isFullyLoaded, CancellationToken cancellationToken);
    }
}
