// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Contracts.EditAndContinue;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api;

internal readonly struct UnitTestingDocumentSpan
{
    internal UnitTestingDocumentSpan(DocumentSpan sourceSpan, FileLinePositionSpan span)
    {
        DocumentSpan = sourceSpan;
        Span = span;
    }

    /// <summary>
    /// The raw <see cref="Document"/> and <see cref="TextSpan"/> that the symbol is located at.
    /// </summary>
    public DocumentSpan DocumentSpan { get; }

    /// <summary>
    /// The line and character the symbol is located.  If this is a mapped location (e.g. affected by a <c>#line</c>
    /// directive), this will be the final location the symbol was mapped to.
    /// </summary>
    public FileLinePositionSpan Span { get; }

    public async Task NavigateToAsync(UnitTestingNavigationOptions options, CancellationToken cancellationToken)
    {
        var location = await this.DocumentSpan.GetNavigableLocationAsync(cancellationToken).ConfigureAwait(false);
        if (location != null)
            await location.NavigateToAsync(new NavigationOptions(options.PreferProvisionalTab, options.ActivateTab), cancellationToken).ConfigureAwait(false);
    }
}
