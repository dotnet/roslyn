// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

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
    }
}
