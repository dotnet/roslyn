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

        Task SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, Document? activeDocument, Func<INavigateToSearchResult, Task> onResultFound, CancellationToken cancellationToken);

        /// <summary>
        /// Searches the documents inside <paramref name="project"/> for symbols that matches <paramref name="searchPattern"/>.
        /// <paramref name="priorityDocuments"/> is an optional subset of the documents from <paramref name="project"/> that can
        /// be used to prioritize work.  Generates files should not be searched.  Results should be up to date with the actual
        /// document contents for the requested project.
        /// </summary>
        Task SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, Document? activeDocument, Func<INavigateToSearchResult, Task> onResultFound, CancellationToken cancellationToken);

        /// <summary>
        /// Searches the documents inside <paramref name="project"/> for symbols that matches <paramref name="searchPattern"/>.
        /// Results should be reported from a previous computed cache (even if that cache is out of date) to produce results as
        /// quickly as possible.
        /// </summary>
        Task SearchCachedDocumentsAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, Document? activeDocument, Func<INavigateToSearchResult, Task> onResultFound, CancellationToken cancellationToken);

        /// <summary>
        /// Searches the generated documents inside <paramref name="project"/> for symbols that matches <paramref name="searchPattern"/>.
        /// </summary>
        Task SearchGeneratedDocumentsAsync(Project project, string searchPattern, IImmutableSet<string> kinds, Document? activeDocument, Func<INavigateToSearchResult, Task> onResultFound, CancellationToken cancellationToken);
    }
}
