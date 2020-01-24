﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    internal abstract class AbstractRenameTrackingCodeFixProvider : CodeFixProvider
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

        protected AbstractRenameTrackingCodeFixProvider(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEnumerable<IRefactorNotifyService> refactorNotifyServices)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _refactorNotifyServices = refactorNotifyServices;
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(RenameTrackingDiagnosticAnalyzer.DiagnosticId); }
        }

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var diagnostic = context.Diagnostics.Single();

            // Ensure rename can still be invoked in this document. We reanalyze the document for
            // diagnostics when rename tracking is manually dismissed, but the existence of our
            // diagnostic may still be cached, so we have to double check before actually providing
            // any fixes.
            if (RenameTrackingTaggerProvider.CanInvokeRename(document))
            {
                var action = RenameTrackingTaggerProvider.CreateCodeAction(document, diagnostic, _refactorNotifyServices, _undoHistoryRegistry);
                context.RegisterCodeFix(action, diagnostic);
            }

            return Task.CompletedTask;
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return null;
        }
    }
}
