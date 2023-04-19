// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Api
{
    [Shared]
    [Export(typeof(IDocumentOptionsProviderFactory))]
    [ExtensionOrder(Before = PredefinedDocumentOptionsProviderNames.EditorConfig)]
    internal sealed class RazorDocumentOptionsProviderFactory : IDocumentOptionsProviderFactory
    {
        private readonly Lazy<IRazorDocumentOptionsService> _innerRazorDocumentOptionsService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorDocumentOptionsProviderFactory(
            Lazy<IRazorDocumentOptionsService> innerRazorDocumentOptionsService)
        {
            if (innerRazorDocumentOptionsService is null)
            {
                throw new ArgumentNullException(nameof(innerRazorDocumentOptionsService));
            }

            _innerRazorDocumentOptionsService = innerRazorDocumentOptionsService;
        }

        public IDocumentOptionsProvider? TryCreate(Workspace workspace)
            => new RazorDocumentOptionsProvider(_innerRazorDocumentOptionsService);

        private sealed class RazorDocumentOptionsProvider : IDocumentOptionsProvider
        {
            public readonly Lazy<IRazorDocumentOptionsService> RazorDocumentOptionsService;

            public RazorDocumentOptionsProvider(Lazy<IRazorDocumentOptionsService> razorDocumentOptionsService)
            {
                RazorDocumentOptionsService = razorDocumentOptionsService;
            }

            public async Task<IDocumentOptions?> GetOptionsForDocumentAsync(
                Document document,
                CancellationToken cancellationToken)
            {
                if (!document.IsRazorDocument())
                {
                    return null;
                }

                var options = await RazorDocumentOptionsService.Value.GetOptionsForDocumentAsync(
                    document, cancellationToken).ConfigureAwait(false);
                return new RazorDocumentOptions(options);
            }
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
