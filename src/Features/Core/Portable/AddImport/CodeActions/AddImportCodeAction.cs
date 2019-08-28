// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.AddImport
{
    internal abstract partial class AbstractAddImportFeatureService<TSimpleNameSyntax>
    {
        /// <summary>
        /// Code action we use when just adding a using, possibly with a project or
        /// metadata reference.  We don't use the standard code action types because
        /// we want to do things like show a glyph if this will do more than just add
        /// an import.
        /// </summary>
        private abstract class AddImportCodeAction : CodeAction
        {
            protected readonly AddImportFixData FixData;

            public override string Title { get; }
            public sealed override ImmutableArray<string> Tags { get; }
            internal sealed override CodeActionPriority Priority { get; }

            public sealed override string EquivalenceKey => Title;

            /// <summary>
            /// The <see cref="Document"/> we started the add-import analysis in.
            /// </summary>
            protected readonly Document OriginalDocument;

            /// <summary>
            /// The changes to make to <see cref="OriginalDocument"/> to add the import.
            /// </summary>
            private readonly ImmutableArray<TextChange> _textChanges;

            protected AddImportCodeAction(
                Document originalDocument,
                AddImportFixData fixData)
            {
                OriginalDocument = originalDocument;
                FixData = fixData;

                Title = fixData.Title;
                Tags = fixData.Tags.ToImmutableArrayOrEmpty();
                Priority = fixData.Priority;
                _textChanges = fixData.TextChanges.ToImmutableArrayOrEmpty();
            }

            protected async Task<Document> GetUpdatedDocumentAsync(CancellationToken cancellationToken)
            {
                var oldText = await OriginalDocument.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var newText = oldText.WithChanges(_textChanges);
                var newDocument = OriginalDocument.WithText(newText);

                return newDocument;
            }
        }
    }
}
