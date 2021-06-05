// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorDocumentOptionSetProviderWrapper : IDocumentOptionSetProvider
    {
        private readonly IRazorDocumentOptionSetProvider _razorDocumentOptionsProvider;

        public RazorDocumentOptionSetProviderWrapper(IRazorDocumentOptionSetProvider razorDocumentOptionsService)
        {
            _razorDocumentOptionsProvider = razorDocumentOptionsService ?? throw new ArgumentNullException(nameof(razorDocumentOptionsService));
        }

        public async Task<OptionSet> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var options = await _razorDocumentOptionsProvider.GetDocumentOptionsAsync(document, cancellationToken).ConfigureAwait(false);
            return options;
        }
    }
}
