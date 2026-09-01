// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.UnitTesting;
using Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.UnitTesting;

[Shared]
[ExportLanguageService(typeof(IUnitTestingSearchService), LanguageNames.FSharp)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class FSharpUnitTestingSearchService(
    [Import(AllowDefault = true)] IFSharpUnitTestingSearchService? service) : IUnitTestingSearchService
{
    private readonly IFSharpUnitTestingSearchService? _service = service;

    public async Task<ImmutableArray<UnitTestingDocumentSpan>> GetSourceLocationsAsync(
        Project project, UnitTestingSearchQuery query, CancellationToken cancellationToken)
    {
        if (_service is null)
            return [];

        var fsharpQuery = new FSharpUnitTestingSearchQuery(
            query.FullyQualifiedTypeName, query.MethodName, query.MethodArity, query.MethodParameterCount, query.Strict);

        var locations = await _service.GetSourceLocationsAsync(project, fsharpQuery, cancellationToken).ConfigureAwait(false);

        return locations.SelectAsArray(static location =>
            new UnitTestingDocumentSpan(new DocumentSpan(location.Document, location.SourceSpan), location.MappedSpan));
    }
}
