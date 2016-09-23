// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal abstract partial class AbstractNavigateToSearchService : INavigateToSearchService
    {
        public Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(
            Document document, string searchPattern, CancellationToken cancellationToken)
        {
            var engineService = document.Project.Solution.Workspace.Services.GetService<INavigateToEngineService>();
            return engineService.SearchDocumentAsync(document, searchPattern, cancellationToken);
        }

        public Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(
            Project project, string searchPattern, CancellationToken cancellationToken)
        {
            var engineService = project.Solution.Workspace.Services.GetService<INavigateToEngineService>();
            return engineService.SearchProjectAsync(project, searchPattern, cancellationToken);
        }
    }
}