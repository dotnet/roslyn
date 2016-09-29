// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    /// <summary>
    /// Workspace service that pulls in the engine we use to actually do the searching.
    /// This allows us to have a default engine that can search in the current process,
    /// as well as overriding that with an engine that will call out to a remote process
    /// in the VS host case.
    /// </summary>
    internal interface INavigateToEngineService : IWorkspaceService
    {
        Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(Project project, string searchPattern, CancellationToken cancellationToken);
        Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, CancellationToken cancellationToken);
    }
}