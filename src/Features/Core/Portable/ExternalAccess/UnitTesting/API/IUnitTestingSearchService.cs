// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

/// <summary>
/// Locates a test in source for a language that has no Roslyn compilation to search, and so cannot be served by the
/// declared-symbol indexes <see cref="UnitTestingSearchHelpers"/> uses for C# and Visual Basic.  A language that does
/// produce a compilation must not export this: the index path is both faster and remotable.
/// </summary>
internal interface IUnitTestingSearchService : ILanguageService
{
    Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocationsAsync(
        Project project, UnitTestingSearchQuery query, CancellationToken cancellationToken);
}
