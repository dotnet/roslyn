// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Navigation;

#if Unified_ExternalAccess
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.ExternalAccess.FSharp.Navigation;

namespace Microsoft.VisualStudio.ExternalAccess.FSharp.Internal.Navigation;
#else
using Microsoft.CodeAnalysis.ExternalAccess.FSharp.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.FSharp.Internal.Navigation;
#endif

/// <summary>
/// Internal F# implementation of the <see cref="ICrossLanguageSymbolNavigationService"/>.  Will defer to the EA
/// layer api (<see cref="IFSharpCrossLanguageSymbolNavigationService"/>) that they will actually export to do the
/// work here.
/// </summary>
[Export(typeof(ICrossLanguageSymbolNavigationService)), Shared]
internal sealed class FSharpCrossLanguageSymbolNavigationService : ICrossLanguageSymbolNavigationService
{
    private readonly IFSharpCrossLanguageSymbolNavigationService _underlyingService;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FSharpCrossLanguageSymbolNavigationService(
        [Import(AllowDefault = true)] IFSharpCrossLanguageSymbolNavigationService underlyingService)
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
}
