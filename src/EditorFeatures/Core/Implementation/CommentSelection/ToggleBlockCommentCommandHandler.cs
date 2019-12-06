// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    [Export(typeof(ICommandHandler))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
    [VisualStudio.Utilities.Name(PredefinedCommandHandlerNames.ToggleBlockComment)]
    internal class ToggleBlockCommentCommandHandler : AbstractToggleBlockCommentBase
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        internal ToggleBlockCommentCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            ITextStructureNavigatorSelectorService navigatorSelectorService)
            : base(undoHistoryRegistry, editorOperationsFactoryService, navigatorSelectorService)
        {
        }

        /// <summary>
        /// Gets block comments by parsing the text for comment markers.
        /// </summary>
        protected override Task<ImmutableArray<TextSpan>> GetBlockCommentsInDocumentAsync(Document document, ITextSnapshot snapshot,
            TextSpan linesContainingSelections, CommentSelectionInfo commentInfo, CancellationToken cancellationToken)
        {
            var allText = snapshot.AsText();
            var commentedSpans = ArrayBuilder<TextSpan>.GetInstance();

            var openIdx = 0;
            while ((openIdx = allText.IndexOf(commentInfo.BlockCommentStartString, openIdx, caseSensitive: true)) >= 0)
            {
                // Retrieve the first closing marker located after the open index.
                var closeIdx = allText.IndexOf(commentInfo.BlockCommentEndString, openIdx + commentInfo.BlockCommentStartString.Length, caseSensitive: true);
                // If an open marker is found without a close marker, it's an unclosed comment.
                if (closeIdx < 0)
                {
                    closeIdx = allText.Length - commentInfo.BlockCommentEndString.Length;
                }

                var blockCommentSpan = new TextSpan(openIdx, closeIdx + commentInfo.BlockCommentEndString.Length - openIdx);
                commentedSpans.Add(blockCommentSpan);
                openIdx = closeIdx;
            }

            return Task.FromResult(commentedSpans.ToImmutableAndFree());
        }
    }
}
