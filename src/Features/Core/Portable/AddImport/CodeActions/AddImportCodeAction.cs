// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        /// <summary>
        /// Code action we use when just adding a using, possibly with a project or
        /// metadata reference.  We don't use the standard code action types because
        /// we want to do things like show a glyph if this will do more than just add
        /// an import.
        /// </summary>
        private abstract class AddImportCodeAction : CodeAction
        {
            public sealed override string Title { get; }
            public sealed override ImmutableArray<string> Tags { get; }
            internal sealed override CodeActionPriority Priority { get; }

            public sealed override string EquivalenceKey => this.Title;

            /// <summary>
            /// The <see cref="Document"/> we started the add-import analysis in.
            /// </summary>
            protected readonly Document OriginalDocument;

            /// <summary>
            /// The changes to make to <see cref="OriginalDocument"/> to add the import.
            /// </summary>
            protected readonly ImmutableArray<TextChange> TextChanges;

            protected AddImportCodeAction(
                Document originalDocument,
                ImmutableArray<TextChange> textChanges,
                string title, ImmutableArray<string> tags,
                CodeActionPriority priority)
            {
                OriginalDocument = originalDocument;
                TextChanges = textChanges;
                Title = title;
                Tags = tags;
                Priority = priority;
            }

            protected async Task<SourceText> GetUpdatedTextAsync(CancellationToken cancellationToken)
            {
                var oldText = await OriginalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newText = oldText.WithChanges(TextChanges);
                return newText;
            }

            protected async Task<Document> GetUpdatedDocumentAsync(CancellationToken cancellationToken)
            {
                var newText = await GetUpdatedTextAsync(cancellationToken).ConfigureAwait(false);
                var newDocument = OriginalDocument.WithText(newText);
                return newDocument;
            }
        }
    }
}