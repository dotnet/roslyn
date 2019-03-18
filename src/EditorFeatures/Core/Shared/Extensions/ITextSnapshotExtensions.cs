// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            var document = snapshot.GetDocument();
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
        /// Get <see cref="Document"/> from <see cref="Text.Extensions.GetDocument(ITextSnapshot)"/>
        /// once <see cref="IWorkspaceStatusService.WaitUntilFullyLoadedAsync(CancellationToken)"/> returns
        /// </summary>
        public static Document GetFullyLoadedDocument(
            this ITextBuffer buffer, IUIThreadOperationContext operationContext)
        {
            return GetFullyLoadedDocument(buffer, operationContext, (_1, _2) => true);
        }

        /// <summary>
        /// Get <see cref="Document"/> from <see cref="Text.Extensions.GetDocument(ITextSnapshot)"/>
        /// once <see cref="IWorkspaceStatusService.WaitUntilFullyLoadedAsync(CancellationToken)"/> returns.
        /// </summary>
        /// <param name="shouldLoad">Whether this function should wait for the solution to fully load.</param>
        public static Document GetFullyLoadedDocument(
            this ITextBuffer buffer, IUIThreadOperationContext operationContext, Func<Document, bool> shouldLoad)
        {
            return GetFullyLoadedDocument(buffer, operationContext, (d, ws) => shouldLoad(d));
        }

        /// <summary>
        /// Get <see cref="Document"/> from <see cref="Text.Extensions.GetDocument(ITextSnapshot)"/>
        /// once <see cref="IWorkspaceStatusService.WaitUntilFullyLoadedAsync(CancellationToken)"/> returns.
        /// </summary>
        /// <param name="shouldLoad">Whether this function should wait for the solution to fully load.</param>
        public static Document GetFullyLoadedDocument(
            this ITextBuffer buffer, IUIThreadOperationContext operationContext, Func<Document, Workspace, bool> shouldLoad)
        {
            // just get a document from whatever we have
            var document = buffer.AsTextContainer().GetOpenDocumentInCurrentContext();
            if (document == null)
            {
                // we don't know about this buffer yet
                return null;
            }

            if (!shouldLoad(document, document.Project.Solution.Workspace))
            {
                return null;
            }

            var description = string.Format(EditorFeaturesResources.Operation_is_not_ready_for_0_yet_see_task_center_for_more_detail, document.Name);

            // partial mode is always cancellable
            using (operationContext.AddScope(allowCancellation: true, description))
            {
                var service = document.Project.Solution.Workspace.Services.GetService<IWorkspaceStatusService>();
                if (service != null)
                {
                    // TODO: decide for prototype, we don't do anything complex and just ask workspace whether it is fully loaded
                    // later we might need to go and change all these with more specific info such as document/project/solution
                    var cancellationToken = operationContext.UserCancellationToken;
                    service.WaitUntilFullyLoadedAsync(cancellationToken).Wait(cancellationToken);
                }

                // get proper document
                return buffer.CurrentSnapshot.GetDocument();
            }
        }
    }
}
