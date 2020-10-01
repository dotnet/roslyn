// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitComment
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(CSharpSplitCommentCommandHandler))]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal partial class CSharpSplitCommentCommandHandler : AbstractSplitCommentCommandHandler
    {
        [ImportingConstructor]
        public CSharpSplitCommentCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        protected override string CommentStart => "//";

        //protected override AbstractCommentSplitter CreateCommentSplitter(Document document, SyntaxNode root, DocumentOptionSet options, int position, CancellationToken cancellationToken)
        //    => new CommentSplitter(document, root, options, position, cancellationToken);
    }
}
