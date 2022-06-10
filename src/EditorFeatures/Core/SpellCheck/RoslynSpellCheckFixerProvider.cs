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
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.SpellChecker;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.SpellCheck
{
    [Obsolete]
    [Export(typeof(ISpellCheckFixerProvider))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(nameof(RoslynSpellCheckFixerProvider))]
    internal sealed class RoslynSpellCheckFixerProvider : ISpellCheckFixerProvider
    {
        private readonly IThreadingContext _threadingContext;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RoslynSpellCheckFixerProvider(
            IThreadingContext threadingContext)
        {
            _threadingContext = threadingContext;
        }

        public async Task RenameWordAsync(
            SnapshotSpan span,
            string replacement,
            IUIThreadOperationContext operationContext)
        {
            var cancellationToken = operationContext.UserCancellationToken;

            var success = await TryRenameWordAsync(span, replacement, cancellationToken).ConfigureAwait(false);
            if (success)
                return;

            // failure path, just apply the text change directly.
            await ApplySimpleChangeAsync(span, replacement, cancellationToken).ConfigureAwait(false);
        }

        private async Task ApplySimpleChangeAsync(SnapshotSpan span, string replacement, CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var buffer = span.Snapshot.TextBuffer;
            var edit = buffer.CreateEdit();

            edit.Replace(span.TranslateTo(buffer.CurrentSnapshot, SpanTrackingMode.EdgeInclusive), replacement);
            edit.Apply();
        }

        private async Task<bool> TryRenameWordAsync(SnapshotSpan span, string replacement, CancellationToken cancellationToken)
        {
            // See if we can map this to a roslyn document.
            var snapshot = span.Snapshot;
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document is null)
                return false;

            // If so, see if the language supports smart rename capabilities.
            var renameService = document.GetLanguageService<IEditorInlineRenameService>();
            if (renameService is null)
                return false;

            // Attempt to figure out what the language would rename here given the position of the misspelled word in
            // the full token.
            var info = await renameService.GetRenameInfoAsync(document, span.Span.Start, cancellationToken).ConfigureAwait(false);
            if (!info.CanRename)
                return false;

            // The subspan we're being asked to rename better fall entirely within the span of the token we're renaming.
            var fullTokenSpan = info.TriggerSpan;
            var subSpanBeingRenamed = span.Span.ToTextSpan();
            if (!fullTokenSpan.Contains(subSpanBeingRenamed))
                return false;

            // Now attempt to call into the language to actually perform the rename.
            var options = new SymbolRenameOptions();
            var renameLocations = await info.FindRenameLocationsAsync(options, cancellationToken).ConfigureAwait(false);
            var replacements = await renameLocations.GetReplacementsAsync(replacement, options, cancellationToken).ConfigureAwait(false);
            if (!replacements.ReplacementTextValid)
                return false;

            // Finally, apply the rename to the solution.
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var workspace = document.Project.Solution.Workspace;
            return workspace.TryApplyChanges(replacements.NewSolution);
        }
    }
}
