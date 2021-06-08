// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorDocumentOptionsServiceWrapper : IDocumentOptionsService
    {
        private readonly IRazorDocumentOptionsService _razorDocumentOptionsService;

        public RazorDocumentOptionsServiceWrapper(IRazorDocumentOptionsService razorDocumentOptionsService)
        {
            if (razorDocumentOptionsService is null)
            {
                throw new ArgumentNullException(nameof(razorDocumentOptionsService));
            }

            _razorDocumentOptionsService = razorDocumentOptionsService;
        }

        public async Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var razorOptions = await _razorDocumentOptionsService.GetOptionsForDocumentAsync(document, cancellationToken).ConfigureAwait(false);
            return new RazorDocumentOptions(razorOptions);
        }

        // Used to convert IRazorDocumentOptions -> IDocumentOptions
        private sealed class RazorDocumentOptions : IDocumentOptions
        {
            private readonly IRazorDocumentOptions _razorOptions;

            public RazorDocumentOptions(IRazorDocumentOptions razorOptions)
            {
                _razorOptions = razorOptions;
            }

            public bool TryGetDocumentOption(OptionKey option, out object? value)
                => _razorOptions.TryGetDocumentOption(option, out value);
        }
    }
}
