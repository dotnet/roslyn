// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class ITextSnapshotExtensions
    {
        /// <summary>
        /// format given snapshot and apply text changes to buffer
        /// </summary>
        public static void FormatAndApplyToBuffer(
            this ITextBuffer textBuffer,
            TextSpan span,
            EditorOptionsService editorOptionsService,
            CancellationToken cancellationToken)
        {
            var document = textBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var documentSyntax = ParsedDocument.CreateSynchronously(document, cancellationToken);
            var rules = FormattingRuleUtilities.GetFormattingRules(documentSyntax, span, additionalRules: null);

            var formatter = document.GetRequiredLanguageService<ISyntaxFormattingService>();

            var options = textBuffer.GetSyntaxFormattingOptions(editorOptionsService, document.Project.Services, explicitFormat: false);
            var result = formatter.GetFormattingResult(documentSyntax.Root, SpecializedCollections.SingletonEnumerable(span), options, rules, cancellationToken);
            var changes = result.GetTextChanges(cancellationToken);

            using (Logger.LogBlock(FunctionId.Formatting_ApplyResultToBuffer, cancellationToken))
            {
                textBuffer.ApplyChanges(changes);
            }
        }

        /// <summary>
        /// Get <see cref="Document"/> from <see cref="Text.Extensions.GetOpenDocumentInCurrentContextWithChanges(ITextSnapshot)"/>
        /// once <see cref="IWorkspaceStatusService.WaitUntilFullyLoadedAsync(CancellationToken)"/> returns
        /// 
        /// for synchronous code path, make sure to use synchronous version 
        /// <see cref="GetFullyLoadedOpenDocumentInCurrentContextWithChanges(ITextSnapshot, IUIThreadOperationContext, IThreadingContext)"/>.
        /// otherwise, one can get into a deadlock
        /// </summary>
        public static async Task<Document?> GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(
            this ITextSnapshot snapshot, IUIThreadOperationContext operationContext)
        {
            // just get a document from whatever we have
            var document = snapshot.TextBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
            if (document == null)
            {
                // we don't know about this buffer yet
                return null;
            }

            // partial mode is always cancellable
            using (operationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Waiting_for_background_work_to_finish))
            {
                var service = document.Project.Solution.Services.GetService<IWorkspaceStatusService>();
                if (service != null)
                {
                    // TODO: decide for prototype, we don't do anything complex and just ask workspace whether it is fully loaded
                    // later we might need to go and change all these with more specific info such as document/project/solution
                    await service.WaitUntilFullyLoadedAsync(operationContext.UserCancellationToken).ConfigureAwait(false);
                }

                // get proper document
                return snapshot.GetOpenDocumentInCurrentContextWithChanges();
            }
        }

        /// <summary>
        /// Get <see cref="Document"/> from <see cref="Text.Extensions.GetOpenDocumentInCurrentContextWithChanges(ITextSnapshot)"/>
        /// once <see cref="IWorkspaceStatusService.WaitUntilFullyLoadedAsync(CancellationToken)"/> returns
        /// </summary>
        public static Document? GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
            this ITextSnapshot snapshot, IUIThreadOperationContext operationContext, IThreadingContext threadingContext)
        {
            // make sure this is only called from UI thread
            threadingContext.ThrowIfNotOnUIThread();

            return threadingContext.JoinableTaskFactory.Run(() =>
                snapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(operationContext));
        }
    }
}
