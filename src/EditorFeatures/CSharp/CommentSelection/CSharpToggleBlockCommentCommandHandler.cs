﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(PredefinedCommandHandlerNames.ToggleBlockComment)]
    internal class CSharpToggleBlockCommentCommandHandler :
        ToggleBlockCommentCommandHandler
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        internal CSharpToggleBlockCommentCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            ITextStructureNavigatorSelectorService navigatorSelectorService)
            : base(undoHistoryRegistry, editorOperationsFactoryService, navigatorSelectorService)
        {
        }

        /// <summary>
        /// Retrieves block comments near the selection in the document.
        /// Uses the CSharp syntax tree to find the commented spans.
        /// </summary>
        protected override async Task<ImmutableArray<TextSpan>> GetBlockCommentsInDocumentAsync(Document document, ITextSnapshot snapshot,
            TextSpan linesContainingSelections, CommentSelectionInfo commentInfo, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            // Only search for block comments intersecting the lines in the selections.
            return root.DescendantTrivia(linesContainingSelections)
                .Where(trivia => trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                .SelectAsArray(blockCommentTrivia => blockCommentTrivia.Span);
        }
    }
}
