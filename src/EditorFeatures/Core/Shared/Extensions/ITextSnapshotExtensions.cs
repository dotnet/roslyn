// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    internal static partial class ITextSnapshotExtensions
    {
#if false
        /// <summary>
        /// get Document corresponding to the snapshot
        /// </summary>
        public static bool TryGetDocument(this ITextSnapshot snapshot, out Document document)
        {
            document = snapshot.AsText().GetRelatedDocumentsWithChanges().FirstOrDefault();
            return document != null;
        }
#endif

        /// <summary>
        /// format given snapshot and apply text changes to buffer
        /// </summary>
        public static void FormatAndApplyToBuffer(this ITextSnapshot snapshot, CancellationToken cancellationToken)
        {
            snapshot.FormatAndApplyToBuffer(new TextSpan(0, snapshot.Length), rules: null, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// format given snapshot and apply text changes to buffer
        /// </summary>
        public static void FormatAndApplyToBuffer(this ITextSnapshot snapshot, IEnumerable<IFormattingRule> rules, CancellationToken cancellationToken)
        {
            snapshot.FormatAndApplyToBuffer(new TextSpan(0, snapshot.Length), rules, cancellationToken);
        }

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
        public static void FormatAndApplyToBuffer(this ITextSnapshot snapshot, TextSpan span, IEnumerable<IFormattingRule> rules, CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            rules = GetFormattingRules(document, rules, span);

            var root = document.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var changes = Formatter.GetFormattedTextChanges(root, SpecializedCollections.SingletonEnumerable(span), document.Project.Solution.Workspace, options: null, rules: rules, cancellationToken: cancellationToken);

            using (Logger.LogBlock(FunctionId.Formatting_ApplyResultToBuffer, cancellationToken))
            {
                document.Project.Solution.Workspace.ApplyTextChanges(document.Id, changes, cancellationToken);
            }
        }

        private static IEnumerable<IFormattingRule> GetFormattingRules(Document document, IEnumerable<IFormattingRule> rules, TextSpan span)
        {
            var workspace = document.Project.Solution.Workspace;
            var formattingRuleFactory = workspace.Services.GetService<IHostDependentFormattingRuleFactoryService>();
            var position = (span.Start + span.End) / 2;

            return SpecializedCollections.SingletonEnumerable(formattingRuleFactory.CreateRule(document, position)).Concat(rules ?? Formatter.GetDefaultFormattingRules(document));
        }
    }
}
