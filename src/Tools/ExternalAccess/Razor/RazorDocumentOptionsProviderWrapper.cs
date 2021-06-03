// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal sealed class RazorDocumentOptionsProviderWrapper : IDocumentOptionsProvider
    {
        private readonly IRazorDocumentOptionsProvider _razorDocumentOptionsProvider;

        public RazorDocumentOptionsProviderWrapper(IRazorDocumentOptionsProvider razorDocumentOptionsService)
        {
            _razorDocumentOptionsProvider = razorDocumentOptionsService ?? throw new ArgumentNullException(nameof(razorDocumentOptionsService));
        }

        public async Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
        {
            var options = await _razorDocumentOptionsProvider.GetDocumentOptionsAsync(document, cancellationToken).ConfigureAwait(false);
            var razorDocumentOptions = new RazorDocumentOptions(document, options.UseTabs, options.TabSize);
            return razorDocumentOptions;
        }

        private sealed class RazorDocumentOptions : IDocumentOptions
        {
            private readonly Document _document;
            private readonly bool _useTabs;
            private readonly int _tabSize;

            public RazorDocumentOptions(Document document, bool useTabs, int tabSize)
            {
                _document = document;
                _useTabs = useTabs;
                _tabSize = tabSize;
            }

            public bool TryGetDocumentOption(OptionKey option, out object? value)
            {
                if (option.Equals(new OptionKey(FormattingOptions.UseTabs, _document.Project.Language)))
                {
                    value = _useTabs;
                    return true;
                }

                if (option.Equals(new OptionKey(FormattingOptions.TabSize, _document.Project.Language)))
                {
                    value = _tabSize;
                    return true;
                }

                value = null;
                return false;
            }
        }
    }
}
