// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Roslyn.Utilities;
using EditorCompletion = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    internal class CompletionCommitManager : IAsyncCompletionCommitManager
    {
        private ImmutableArray<char> CommitChars => ImmutableArray.Create(
            ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
            ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
            '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\');

        public ImmutableArray<char> GetPotentialCommitCharacters()
        {
            return CommitChars;
        }

        public bool ShouldCommitCompletion(char typedChar, SnapshotPoint location)
        {
            return CommitChars.Contains(typedChar);
        }

        public CommitResult TryCommit(ITextView view, ITextBuffer buffer, EditorCompletion.CompletionItem item, ITrackingSpan applicableSpan, char typeChar, CancellationToken token)
        {
            var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return new CommitResult(handled: false);
            }

            var completionService = document.GetLanguageService<CompletionService>();
            if (!item.Properties.TryGetProperty<RoslynCompletionItem>(CompletionItemSource.RoslynItem, out var roslynItem))
            {
                return new CommitResult(handled: false);
            }

            var needsCustomCommit = ((CompletionServiceWithProviders)completionService).GetProvider(roslynItem) is IFeaturesCustomCommitCompletionProvider;
            if (needsCustomCommit)
            {
                CustomCommit(view, buffer, roslynItem, applicableSpan, typeChar, token);
                return new CommitResult(handled: true, CommitBehavior.SuppressFurtherCommandHandlers);
            }

            if (document.Project.Language == LanguageNames.VisualBasic && typeChar == '\n')
            {
                return new CommitResult(handled: false, CommitBehavior.RaiseFurtherCommandHandlers);
            }

            if (item.InsertText.EndsWith(":") && typeChar == ':')
            {
                return new CommitResult(handled: false, CommitBehavior.SuppressFurtherCommandHandlers);
            }

            return new CommitResult(handled: false);
        }

        private CommitBehavior CustomCommit(
            ITextView view,
            ITextBuffer buffer,
            RoslynCompletionItem roslynItem,
            ITrackingSpan applicableSpan,
            char commitCharacter,
            CancellationToken token)
        {
            var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var service = (CompletionServiceWithProviders)document.GetLanguageService<CompletionService>();

            using (var edit = buffer.CreateEdit())
            {
                var provider = service.GetProvider(roslynItem);

                // TODO: Do we actually want the document from the initial snapshot?
                edit.Delete(applicableSpan.GetSpan(buffer.CurrentSnapshot));

                var change = ((IFeaturesCustomCommitCompletionProvider)provider).GetChangeAsync(
                    document,
                    roslynItem,
                    commitCharacter,
                    token).WaitAndGetResult(token);

                edit.Replace(change.TextChange.Span.ToSpan(), change.TextChange.NewText);
                edit.Apply();

                if (change.NewPosition.HasValue)
                {
                    view.TryMoveCaretToAndEnsureVisible(new SnapshotPoint(buffer.CurrentSnapshot, change.NewPosition.Value));
                }
            }

            return CommitBehavior.SuppressFurtherCommandHandlers;
        }
    }
}
