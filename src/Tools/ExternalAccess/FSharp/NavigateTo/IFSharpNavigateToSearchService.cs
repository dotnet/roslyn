// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.NavigateTo
{
    internal interface IFSharpNavigateToSearchService
    {
        IImmutableSet<string> KindsProvided
        {
            get;
        }

        bool CanFilter
        {
            get;
        }

        Task<ImmutableArray<FSharpNavigateToSearchResult>> SearchProjectAsync(Project project, ImmutableArray<Document> priorityDocuments, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
        Task<ImmutableArray<FSharpNavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, IImmutableSet<string> kinds, CancellationToken cancellationToken);
    }
}
