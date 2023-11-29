// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Navigation;

/// <summary>
/// Service used by "go to definition" and "ctrl-click on symbol" to find the symbol definition location and navigate to it.
/// </summary>
internal interface IDefinitionLocationService : ILanguageService
{
    /// <summary>
    /// If the supplied <paramref name="position"/> is on a code construct with a navigable location, then this
    /// returns that <see cref="INavigableLocation"/>.  The <see cref="TextSpan"/> returned in the span of the
    /// symbol in the code that references that navigable location.  e.g. the full identifier token that the
    /// position is within.
    /// </summary>
    Task<DefinitionLocation?> GetDefinitionLocationAsync(
        Document document, int position, CancellationToken cancellationToken);
}

/// <summary>
/// The result of a <see cref="IDefinitionLocationService.GetDefinitionLocationAsync"/> call.
/// </summary>
/// <param name="Location">The location where the symbol is actually defined at.  Can be used to then navigate to that
/// symbol.
/// </param>
/// <param name="Span">The <see cref="TextSpan"/> returned in the span of the symbol in the code that references that
/// navigable location.  e.g. the full identifier token that the position is within.  Can be used to highlight/underline
/// that text in the document in some fashion.</param>
internal sealed record DefinitionLocation(INavigableLocation Location, DocumentSpan Span);
