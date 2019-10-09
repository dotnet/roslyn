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
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    [Export(typeof(IDocumentOptionsProviderFactory))]
    [Order(After = PredefinedDocumentOptionsProviderNames.EditorConfig)]
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

            public Task<IDocumentOptions?> GetOptionsForDocumentAsync(Document document, CancellationToken cancellationToken)
            {
                return Task.FromResult<IDocumentOptions?>(new DocumentOptions(document.Project.Solution.Workspace, document.Id, _indentationManagerService));
            }

            private sealed class DocumentOptions : IDocumentOptions
            {
                private readonly Workspace _workspace;
                private readonly DocumentId _documentId;
                private readonly IIndentationManagerService _indentationManagerService;

                public DocumentOptions(Workspace workspace, DocumentId id, IIndentationManagerService indentationManagerService)
                {
                    _workspace = workspace;
                    _documentId = id;
                    _indentationManagerService = indentationManagerService;
                }

                public bool TryGetDocumentOption(OptionKey option, out object? value)
                {
                    // We have to go back to the original workspace to see if this document is open, and if so, grab the text container. The API
                    // from the editor is defined on a text buffer, and once a Document is forked it's definitely not holding onto a buffer anymore.
                    if (_workspace.IsDocumentOpen(_documentId))
                    {
                        var currentDocument = _workspace.CurrentSolution.GetDocument(_documentId);
                        if (currentDocument != null && currentDocument.TryGetText(out var text))
                        {
                            var snapshot = text.FindCorrespondingEditorTextSnapshot();
                            return TryGetOptionForBuffer(snapshot.TextBuffer, option, out value);
                        }
                    }

                    value = null;
                    return false;
                }

                private bool TryGetOptionForBuffer(ITextBuffer textBuffer, OptionKey option, out object? value)
                {
                    if (option.Option == FormattingOptions.UseTabs)
                    {
                        value = !_indentationManagerService.UseSpacesForWhitespace(textBuffer, explicitFormat: false);
                        return true;
                    }
                    else if (option.Option == FormattingOptions.TabSize)
                    {
                        value = _indentationManagerService.GetTabSize(textBuffer, explicitFormat: false);
                        return true;
                    }
                    else if (option.Option == FormattingOptions.IndentationSize)
                    {
                        value = _indentationManagerService.GetIndentSize(textBuffer, explicitFormat: false);
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
