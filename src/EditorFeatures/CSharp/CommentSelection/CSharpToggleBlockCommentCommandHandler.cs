// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection
{
    /* TODO - Modify these once the toggle block comment handler is added.
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.CommentSelection)]*/
    internal class CSharpToggleBlockCommentCommandHandler :
        ToggleBlockCommentCommandHandler
    {
        [ImportingConstructor]
        internal CSharpToggleBlockCommentCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        /// <summary>
        /// Retrieves the toggle block comment data provider with additional CSharp functionality.
        /// </summary>
        protected override async Task<IToggleBlockCommentDocumentDataProvider> GetBlockCommentDocumentDataProvider(Document document, ITextSnapshot snapshot,
            CommentSelectionInfo commentInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return new CSharpToggleBlockCommentDocumentDataProvider(root);
        }
    }
}
