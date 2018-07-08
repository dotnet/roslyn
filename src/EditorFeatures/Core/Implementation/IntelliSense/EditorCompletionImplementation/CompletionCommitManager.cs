// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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
using EditorCompletion = Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.EditorImplementation
{
    internal sealed class CompletionCommitManager : IAsyncCompletionCommitManager
    {
        private ImmutableArray<char> CommitChars => ImmutableArray.Create(
            ' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
            ';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
            '~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\');

        IEnumerable<char> IAsyncCompletionCommitManager.PotentialCommitCharacters => CommitChars;

        public bool ShouldCommitCompletion(char typedChar, SnapshotPoint location, CancellationToken token)
        {
            return CommitChars.Contains(typedChar);
        }

        public EditorCompletion.CommitResult TryCommit(ITextView view, ITextBuffer buffer, EditorCompletion.CompletionItem item, ITrackingSpan applicableSpan, char typeChar, CancellationToken token)
        {
            var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return new EditorCompletion.CommitResult(isHandled: false, EditorCompletion.CommitBehavior.None);
            }

            var completionService = document.GetLanguageService<CompletionService>();
            if (!item.Properties.TryGetProperty<RoslynCompletionItem>(CompletionItemSource.RoslynItem, out var roslynItem))
            {
                // This isn't an item we provided (e.g. Razor). Let the editor handle it normally.
                return new EditorCompletion.CommitResult(isHandled: false, EditorCompletion.CommitBehavior.None);
            }

            var needsCustomCommit = ((CompletionServiceWithProviders)completionService).GetProvider(roslynItem) is IFeaturesCustomCommitCompletionProvider;
            if (needsCustomCommit)
            {
                CustomCommit(view, buffer, roslynItem, applicableSpan, typeChar, token);
                return new EditorCompletion.CommitResult(isHandled: true, EditorCompletion.CommitBehavior.SuppressFurtherTypeCharCommandHandlers);
            }

            if (document.Project.Language == LanguageNames.VisualBasic && typeChar == '\n')
            {
                return new EditorCompletion.CommitResult(isHandled: false, EditorCompletion.CommitBehavior.SuppressFurtherTypeCharCommandHandlers);
            }

            if (item.InsertText.EndsWith(":") && typeChar == ':')
            {
                return new EditorCompletion.CommitResult(isHandled: false, EditorCompletion.CommitBehavior.SuppressFurtherTypeCharCommandHandlers);
            }

            return new EditorCompletion.CommitResult(isHandled: false, EditorCompletion.CommitBehavior.None);
        }

        private EditorCompletion.CommitBehavior CustomCommit(
            ITextView view,
            ITextBuffer buffer,
            RoslynCompletionItem roslynItem,
            ITrackingSpan applicableSpan,
            char commitCharacter,
            CancellationToken token)
        {
            // TODO: Store the document so we don't have to get it again (and risk changes having happened): https://github.com/dotnet/roslyn/issues/27417
            var document = buffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            var service = (CompletionServiceWithProviders)document.GetLanguageService<CompletionService>();

            // TODO: Better error handling https://github.com/dotnet/roslyn/issues/27412

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

            return EditorCompletion.CommitBehavior.SuppressFurtherTypeCharCommandHandlers;
        }
    }
}
