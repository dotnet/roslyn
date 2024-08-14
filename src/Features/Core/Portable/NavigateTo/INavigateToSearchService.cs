// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.GeneratedCodeRecognition;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.NavigateTo;

internal interface INavigateToSearchService : ILanguageService
{
    IImmutableSet<string> KindsProvided { get; }
    bool CanFilter { get; }

    Task SearchDocumentAsync(
        Document document,
        string searchPattern,
        IImmutableSet<string> kinds,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        CancellationToken cancellationToken);

    /// <summary>
    /// Searches the documents inside <paramref name="projects"/> for symbols that matches <paramref
    /// name="searchPattern"/>. <paramref name="priorityDocuments"/> is an optional subset of the documents from
    /// <paramref name="projects"/> that can be used to prioritize work.  Generates files should not be searched.
    /// Results should be up to date with the actual document contents for the requested project.
    /// </summary>
    /// <param name="searchGeneratedCode">Whether documents that match <see
    /// cref="IGeneratedCodeRecognitionService.IsGeneratedCode"/> should be searched.</param> 
    /// <remarks>
    /// All the projects passed are guaranteed to be for the language this <see cref="INavigateToSearchService"/>
    /// belongs to.  Similarly, all the <paramref name="priorityDocuments"/> belong to these projects.
    /// </remarks>
    Task SearchProjectsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        bool searchGeneratedCode,
        Document? activeDocument,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken);
}

/// <summary>
/// Optional expanded API for Navigate-To.  Allows languages to just implement a simpler set of methods if they don't
/// offer this extra functionality.
/// </summary>
internal interface IAdvancedNavigateToSearchService : INavigateToSearchService
{
    /// <summary>
    /// Searches the documents inside <paramref name="projects"/> for symbols that matches <paramref
    /// name="searchPattern"/>. Results should be reported from a previous computed cache (even if that cache is out of
    /// date) to produce results as quickly as possible.
    /// </summary>
    /// <remarks>
    /// All the projects passed are guaranteed to be for the language this <see cref="INavigateToSearchService"/>
    /// belongs to.  Similarly, all the <paramref name="priorityDocuments"/> belong to these projects.
    /// </remarks>
    Task SearchCachedDocumentsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        ImmutableArray<Document> priorityDocuments,
        string searchPattern,
        IImmutableSet<string> kinds,
        Document? activeDocument,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken);

    /// <summary>
    /// Searches the <see cref="SourceGeneratedDocument"/>s inside <paramref name="projects"/> for symbols that matches
    /// <paramref name="searchPattern"/>.
    /// </summary>
    /// <remarks>
    /// All the projects passed are guaranteed to be for the language this <see
    /// cref="INavigateToSearchService"/> belongs to.
    /// </remarks>
    Task SearchSourceGeneratedDocumentsAsync(
        Solution solution,
        ImmutableArray<Project> projects,
        string searchPattern,
        IImmutableSet<string> kinds,
        Document? activeDocument,
        Func<ImmutableArray<INavigateToSearchResult>, Task> onResultsFound,
        Func<Task> onProjectCompleted,
        CancellationToken cancellationToken);
}
