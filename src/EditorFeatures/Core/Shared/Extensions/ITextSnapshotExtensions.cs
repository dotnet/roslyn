// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class ITextSnapshotExtensions
    {
        /// <summary>
        /// format given snapshot and apply text changes to buffer
        /// </summary>
        public static void FormatAndApplyToBuffer(this ITextSnapshot snapshot, TextSpan span, CancellationToken cancellationToken)
        {
            snapshot.FormatAndApplyToBuffer(span, rules: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// format given snapshot and apply text changes to buffer
        /// </summary>
        public static void FormatAndApplyToBuffer(this ITextSnapshot snapshot, TextSpan span, IEnumerable<AbstractFormattingRule> rules, CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            rules = GetFormattingRules(document, rules, span);

            var root = document.GetSyntaxRootSynchronously(cancellationToken);
            var documentOptions = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var changes = Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(span), document.Project.Solution.Workspace, documentOptions, rules, cancellationToken);

            using (Logger.LogBlock(FunctionId.Formatting_ApplyResultToBuffer, cancellationToken))
            {
                document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, cancellationToken);
            }
        }

        private static IEnumerable<AbstractFormattingRule> GetFormattingRules(Document document, IEnumerable<AbstractFormattingRule> rules, TextSpan span)
        {
            var workspace = document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            var position = (span.Start + span.End) / 2;

            return SpecializedCollections.SingletonEnumerable(formattingRuleFactory.CreateRule(document, position)).Concat(rules ?? Formatter.GetDefaultFormattingRules(document));
        }

        /// <summary>
        /// Get <see cref="Document"/> from <see cref="Text.Extensions.GetOpenDocumentInCurrentContextWithChanges(ITextSnapshot)"/>
        /// once <see cref="IWorkspaceStatusService.WaitUntilFullyLoadedAsync(CancellationToken)"/> returns
        /// 
        /// for synchronous code path, make sure to use synchronous version 
        /// <see cref="GetFullyLoadedOpenDocumentInCurrentContextWithChanges(ITextSnapshot, IUIThreadOperationContext, IThreadingContext)"/>.
        /// otherwise, one can get into a deadlock
        /// </summary>
        public static async Task<Document> GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(
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
                var service = document.Project.Solution.Workspace.Services.GetService<IWorkspaceStatusService>();
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
        public static Document GetFullyLoadedOpenDocumentInCurrentContextWithChanges(
            this ITextSnapshot snapshot, IUIThreadOperationContext operationContext, IThreadingContext threadingContext)
        {
            // make sure this is only called from UI thread
            threadingContext.ThrowIfNotOnUIThread();

            return threadingContext.JoinableTaskFactory.Run(() =>
                snapshot.GetFullyLoadedOpenDocumentInCurrentContextWithChangesAsync(operationContext));
        }
    }
}
