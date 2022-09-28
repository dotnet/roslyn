// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.EditAndContinue.Contracts;
using Microsoft.CodeAnalysis.Navigation;

namespace Microsoft.CodeAnalysis.ExternalAccess.UnitTesting.Api
{
    internal readonly struct UnitTestingDocumentSpan
    {
        internal UnitTestingDocumentSpan(DocumentSpan sourceSpan, FileLinePositionSpan span)
        {
            DocumentSpan = sourceSpan;
            Span = span;
        }

        public DocumentSpan DocumentSpan { get; }
        public FileLinePositionSpan Span { get; }

        public async Task NavigateToAsync(UnitTestingNavigationOptions options, CancellationToken cancellationToken)
        {
            var location = await this.DocumentSpan.GetNavigableLocationAsync(cancellationToken).ConfigureAwait(false);
            if (location != null)
                await location.NavigateToAsync(new NavigationOptions(options.PreferProvisionalTab, options.ActivateTab), cancellationToken).ConfigureAwait(false);
        }
    }
}
