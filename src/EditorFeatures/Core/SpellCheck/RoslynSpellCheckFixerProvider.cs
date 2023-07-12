// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.SpellChecker;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.SpellCheck
{
    [Obsolete]
    [Export(typeof(ISpellCheckFixerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(RoslynSpellCheckFixerProvider))]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class RoslynSpellCheckFixerProvider(
        IThreadingContext threadingContext) : ISpellCheckFixerProvider
    {
        private readonly IThreadingContext _threadingContext = threadingContext;

        public Task RenameWordAsync(
            SnapshotSpan span,
            string replacement,
            IUIThreadOperationContext operationContext)
        {
            var cancellationToken = operationContext.UserCancellationToken;
            return RenameWordAsync(span, replacement, cancellationToken);
        }

        private async Task<(FunctionId functionId, string? message)?> RenameWordAsync(
            SnapshotSpan span,
            string replacement,
            CancellationToken cancellationToken)
        {
            var result = await TryRenameAsync(span, replacement, cancellationToken).ConfigureAwait(false);

            // If we succeeded at renaming then nothing more to do.
            if (result != null)
            {
                // Record why we failed so we can determine what issues may be arising in the wild.
                var (functionId, message) = result.Value;
                Logger.Log(functionId, message);

                // Then just apply the text change directly.
                await ApplySimpleChangeAsync(span, replacement, cancellationToken).ConfigureAwait(false);
            }

            return result;
        }

        private async Task ApplySimpleChangeAsync(SnapshotSpan span, string replacement, CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var buffer = span.Snapshot.TextBuffer;
            var edit = buffer.CreateEdit();

            edit.Replace(span.TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive), replacement);
            edit.Apply();
        }

        private async Task<(FunctionId functionId, string? message)?> TryRenameAsync(SnapshotSpan span, string replacement, CancellationToken cancellationToken)
        {
            // See if we can map this to a roslyn document.
            var snapshot = span.Snapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document is null)
                return (FunctionId.SpellCheckFixer_CouldNotFindDocument, null);

            // If so, see if the language supports smart rename capabilities.
            var renameService = document.GetLanguageService<IEditorInlineRenameService>();
            if (renameService is null)
                return (FunctionId.SpellCheckFixer_LanguageDoesNotSupportRename, null);

            // Attempt to figure out what the language would rename here given the position of the misspelled word in
            // the full token.
            var info = await renameService.GetRenameInfoAsync(document, span.Span.Start, cancellationToken).ConfigureAwait(false);
            if (!info.CanRename)
                return (FunctionId.SpellCheckFixer_LanguageCouldNotGetRenameInfo, null);

            // The subspan we're being asked to rename better fall entirely within the span of the token we're renaming.
            var fullTokenSpan = info.TriggerSpan;
            var subSpanBeingRenamed = span.Span.ToTextSpan();
            if (!fullTokenSpan.Contains(subSpanBeingRenamed))
                return (FunctionId.SpellCheckFixer_RenameSpanNotWithinTokenSpan, null);

            // Now attempt to call into the language to actually perform the rename.
            var options = new SymbolRenameOptions();
            var renameLocations = await info.FindRenameLocationsAsync(options, cancellationToken).ConfigureAwait(false);
            var replacements = await renameLocations.GetReplacementsAsync(replacement, options, cancellationToken).ConfigureAwait(false);
            if (!replacements.ReplacementTextValid)
                return (FunctionId.SpellCheckFixer_ReplacementTextInvalid, $"Renaming: '{span.GetText()}' to '{replacement}'");

            // Finally, apply the rename to the solution.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var workspace = document.Project.Solution.Workspace;
            if (!workspace.TryApplyChanges(replacements.NewSolution))
                return (FunctionId.SpellCheckFixer_TryApplyChangesFailure, null);

            return null;
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public readonly struct TestAccessor(RoslynSpellCheckFixerProvider provider)
        {
            private readonly RoslynSpellCheckFixerProvider _provider = provider;

            public Task<(FunctionId functionId, string? message)?> TryRenameAsync(SnapshotSpan span, string replacement, CancellationToken cancellationToken)
                => _provider.RenameWordAsync(span, replacement, cancellationToken);
        }
    }
}
