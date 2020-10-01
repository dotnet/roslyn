// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Implementation.SplitComment;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
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
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        protected override bool LineContainsComment(ITextSnapshotLine line, int caretPosition)
        {
            var snapshot = line.Snapshot;
            var text = snapshot.GetText();

            if (caretPosition > line.End.Position)
            {
                return false;
            }
            else
            {
                return text.Contains(CommentSplitter.CommentCharacter);
            }
        }

        protected override int? SplitComment(
           Document document, DocumentOptionSet options, int position, CancellationToken cancellationToken)
        {
            var useTabs = options.GetOption(FormattingOptions.UseTabs);
            var tabSize = options.GetOption(FormattingOptions.TabSize);
            var indentStyle = options.GetOption(FormattingOptions.SmartIndent, LanguageNames.CSharp);

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var sourceText = root.SyntaxTree.GetText(cancellationToken);

            var splitter = CommentSplitter.TryCreate(
                document, position, root, sourceText,
                useTabs, tabSize, indentStyle, cancellationToken);

            return splitter?.TrySplit();
        }
    }
}
