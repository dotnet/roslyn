// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.NavigateTo
{
    internal interface INavigateToSearchService : ILanguageService
    {
        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
        Task<ImmutableArray<INavigateToSearchResult>> SearchProjectAsync(Project project, string searchPattern, CancellationToken cancellationToken);

        /// <remarks>
        /// <para>In the event cancellation is requested, this operation may complete in the
        /// <see cref="TaskStatus.Canceled"/> or <see cref="TaskStatus.RanToCompletion"/> state. In the latter case,
        /// the result of this method is not guaranteed to be complete or valid; callers are expected to check the
        /// status of the <paramref name="cancellationToken"/> prior to using the results.</para>
        /// </remarks>
        Task<ImmutableArray<INavigateToSearchResult>> SearchDocumentAsync(Document document, string searchPattern, CancellationToken cancellationToken);
    }
}
