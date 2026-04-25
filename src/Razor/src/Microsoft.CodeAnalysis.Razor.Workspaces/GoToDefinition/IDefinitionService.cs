// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Razor.DocumentMapping;
using Microsoft.CodeAnalysis.Razor.ProjectSystem;

namespace Microsoft.CodeAnalysis.Razor.GoToDefinition;

/// <summary>
///  Go to Definition support for Razor tag helpers (Mvc tag helpers and components).
/// </summary>
internal interface IDefinitionService
{
    Task<LspLocation[]?> GetDefinitionAsync(
        IDocumentSnapshot documentSnapshot,
        DocumentPositionInfo positionInfo,
        ISolutionQueryOperations solutionQueryOperations,
        bool includeMvcTagHelpers,
        CancellationToken cancellationToken);

    Task<LspLocation[]?> TryGetDefinitionFromStringLiteralAsync(
        IDocumentSnapshot documentSnapshot,
        Position position,
        CancellationToken cancellationToken);
}
