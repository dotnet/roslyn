// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;

/// <inheritdoc cref="ICrossLanguageSymbolNavigationService"/>
internal interface IFSharpCrossLanguageSymbolNavigationService
{
    /// <inheritdoc cref="ICrossLanguageSymbolNavigationService.TryGetNavigableLocationAsync"/>
    Task<IFSharpNavigableLocation?> TryGetNavigableLocationAsync(
        string assemblyName, string documentationCommentId, CancellationToken cancellationToken);
}

/// <summary>
/// The part of <see cref="ICrossLanguageSymbolNavigationService"/> added after <see
/// cref="IFSharpCrossLanguageSymbolNavigationService"/> shipped.  Kept apart so that an implementation compiled
/// against a version without it still loads.
/// </summary>
internal interface IFSharpCrossLanguageSymbolNavigationService2 : IFSharpCrossLanguageSymbolNavigationService
{
    /// <inheritdoc cref="ICrossLanguageSymbolNavigationService.TryGetNavigableFileLocationAsync"/>
    Task<(string filePath, LinePosition linePosition)?> TryGetNavigableFileLocationAsync(
        string assemblyName, string documentationCommentId, CancellationToken cancellationToken);
}

/// <inheritdoc cref="NavigationOptions"/>
internal sealed record class FSharpNavigationOptions2(
    bool PreferProvisionalTab,
    bool ActivateTab);

/// <inheritdoc cref="INavigableLocation"/>
internal interface IFSharpNavigableLocation
{
    Task<bool> NavigateToAsync(FSharpNavigationOptions2 options, CancellationToken cancellationToken);
}
