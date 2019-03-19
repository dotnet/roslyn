// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection
{
    /* TODO - Modify these once the toggle block comment handler is added.*/
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.CommentSelection)]
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

        protected override async Task<IToggleBlockCommentDocumentDataProvider> GetBlockCommentDocumentData(Document document, ITextSnapshot snapshot,
            CommentSelectionInfo commentInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            return new CSharpToggleBlockCommentDocumentDataProvider(root);
        }

        private class CSharpToggleBlockCommentDocumentDataProvider : IToggleBlockCommentDocumentDataProvider
        {
            private readonly SyntaxNode _root;

            public CSharpToggleBlockCommentDocumentDataProvider(SyntaxNode root)
            {
                _root = root;
            }

            /// <summary>
            /// Get a location of itself or the end of the token it is located in.
            /// </summary>
            public int GetLocationAfterToken(int location)
            {
                var token = _root.FindToken(location);
                if (token.Span.Contains(location))
                {
                    return token.Span.End;
                }
                return location;
            }

            /// <summary>
            /// Get the location of the comments from the syntax tree.
            /// </summary>
            /// <returns></returns>
            public IEnumerable<TextSpan> GetBlockCommentsInDocument()
            {
                return _root.DescendantTrivia()
                    .Where(trivia => trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                    .Select(blockCommentTrivia => blockCommentTrivia.Span);
            }
        }
    }
}
