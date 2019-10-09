// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    [Export(typeof(IDocumentOptionsProviderFactory))]
    internal sealed class InferredIndentationDocumentOptionsProviderFactory : IDocumentOptionsProviderFactory
    {
        private readonly IIndentationManagerService _indentationManagerService;

        [ImportingConstructor]
        public InferredIndentationDocumentOptionsProviderFactory(IIndentationManagerService indentationManagerService)
        {
            _indentationManagerService = indentationManagerService;
        }

        public IDocumentOptionsProvider? TryCreate(Workspace workspace)
        {
            return new DocumentOptionsProvider(_indentationManagerService);
        }

        private class DocumentOptionsProvider : IDocumentOptionsProvider
        {
            private readonly IIndentationManagerService _indentationManagerService;

            public DocumentOptionsProvider(IIndentationManagerService indentationManagerService)
            {
                _indentationManagerService = indentationManagerService;
            }

            public async Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var snapshot = text.FindCorrespondingEditorTextSnapshot();

                if (snapshot != null)
                {
                    return new DocumentOptions(snapshot, _indentationManagerService);
                }
                else
                {
                    return null;
                }
            }

            private sealed class DocumentOptions : IDocumentOptions
            {
                private readonly ITextSnapshot _snapshot;
                private readonly IIndentationManagerService _indentationManagerService;

                public DocumentOptions(ITextSnapshot snapshot, IIndentationManagerService indentationManagerService)
                {
                    _snapshot = snapshot;
                    _indentationManagerService = indentationManagerService;
                }

                public bool TryGetDocumentOption(OptionKey option, out object? value)
                {
                    // The editor API allows us to pass in a snapshot point to request the format for a specific point. Today,
                    // the implementation ignores the location and just uses the result for the entire file so it doesn't matter. But
                    // our Roslyn options abstraction is per-document today, so we don't have a position to hand in here.
                    var snapshotPoint = new SnapshotPoint(_snapshot, 0);

                    if (option.Option == FormattingOptions.UseTabs)
                    {
                        value = !_indentationManagerService.UseSpacesForWhitespace(snapshotPoint, explicitFormat: false);
                        return true;
                    }
                    else if (option.Option == FormattingOptions.TabSize)
                    {
                        value = _indentationManagerService.GetTabSize(snapshotPoint, explicitFormat: false);
                        return true;
                    }
                    else if (option.Option == FormattingOptions.IndentationSize)
                    {
                        value = _indentationManagerService.GetIndentSize(snapshotPoint, explicitFormat: false);
                        return true;
                    }
                    else
                    {
                        value = null;
                        return false;
                    }
                }
            }
        }
    }
}
