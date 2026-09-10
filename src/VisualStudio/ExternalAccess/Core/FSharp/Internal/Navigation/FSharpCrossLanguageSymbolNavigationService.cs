// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation;

/// <summary>
/// Internal F# implementation of the <see cref="ICrossLanguageSymbolNavigationService"/>.  Will defer to the EA
/// layer api (<see cref="IFSharpCrossLanguageSymbolNavigationService"/>) that they will actually export to do the
/// work here.
/// </summary>
[Export(typeof(ICrossLanguageSymbolNavigationService)), Shared]
internal sealed class FSharpCrossLanguageSymbolNavigationService : ICrossLanguageSymbolNavigationService
{
    private readonly IFSharpCrossLanguageSymbolNavigationService? _underlyingService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpCrossLanguageSymbolNavigationService(
        [Import(AllowDefault = true)] IFSharpCrossLanguageSymbolNavigationService? underlyingService)
    {
        _underlyingService = underlyingService;
    }

    public async Task<INavigableLocation?> TryGetNavigableLocationAsync(
        string assemblyName, string documentationCommentId, CancellationToken cancellationToken)
    {
        // Only defer to actual F# service if it exists.
        if (_underlyingService is null)
            return null;

        var location = await _underlyingService.TryGetNavigableLocationAsync(
            assemblyName, documentationCommentId, cancellationToken).ConfigureAwait(false);
        if (location == null)
            return null;

        return new NavigableLocation((options, cancellationToken) =>
            location.NavigateToAsync(new FSharpNavigationOptions2(options.PreferProvisionalTab, options.ActivateTab), cancellationToken));
    }

    public async Task<(string filePath, LinePosition linePosition)?> TryGetNavigableFileLocationAsync(
        string assemblyName, string documentationCommentId, CancellationToken cancellationToken)
    {
        // Only defer to an F# service that can name a file; one that can only navigate has nothing to show in place.
        if (_underlyingService is not IFSharpCrossLanguageSymbolNavigationService2 fileLocationService)
            return null;

        return await fileLocationService.TryGetNavigableFileLocationAsync(
            assemblyName, documentationCommentId, cancellationToken).ConfigureAwait(false);
    }
}
