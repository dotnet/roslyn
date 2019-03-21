// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    /* TODO - Modify these once the toggle block comment handler is added.
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.CommentSelection)]*/
    internal class ToggleBlockCommentCommandHandler : AbstractToggleBlockCommentBase
    {
        [ImportingConstructor]
        internal ToggleBlockCommentCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        /// <summary>
        /// Gets the default text based document data provider for block comments.
        /// </summary>
        protected override Task<IToggleBlockCommentDocumentDataProvider> GetBlockCommentDocumentDataProvider(Document document, ITextSnapshot snapshot,
            CommentSelectionInfo commentInfo, CancellationToken cancellationToken)
        {
            IToggleBlockCommentDocumentDataProvider provider = new ToggleBlockCommentDocumentDataProvider(snapshot, commentInfo);
            return Task.FromResult(provider);
        }
    }
}
