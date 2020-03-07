// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = nameof(RenameTrackingCodeRefactoringProvider)), Shared]
    internal class RenameTrackingCodeRefactoringProvider : CodeRefactoringProvider
    {
        public const string DiagnosticId = "RenameTracking";
        public static DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
            DiagnosticId, title: "", messageFormat: "", category: "",
            defaultSeverity: DiagnosticSeverity.Hidden, isEnabledByDefault: true,
            customTags: DiagnosticCustomTags.Microsoft.Append(WellKnownDiagnosticTags.NotConfigurable));

        public const string RenameFromPropertyKey = "RenameFrom";
        public const string RenameToPropertyKey = "RenameTo";

        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

        [ImportingConstructor]
        public RenameTrackingCodeRefactoringProvider(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _refactorNotifyServices = refactorNotifyServices;
        }

        // Internal for testing purposes
        internal async Task<Diagnostic?> TryGetDiagnosticAsync(Document document, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            return RenameTrackingTaggerProvider.TryGetDiagnostic(
                syntaxTree, DiagnosticDescriptor, cancellationToken);
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            // Ensure rename can still be invoked in this document. We reanalyze the document for
            // diagnostics when rename tracking is manually dismissed, but the existence of our
            // diagnostic may still be cached, so we have to double check before actually providing
            // any fixes.
            if (!RenameTrackingTaggerProvider.CanInvokeRename(document))
                return;

            var diagnostic = await TryGetDiagnosticAsync(document, cancellationToken).ConfigureAwait(false);
            if (diagnostic == null)
                return;

            // user needs to be on the same line as the diagnostic location.
            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            if (!text.AreOnSameLine(span.Start, diagnostic.Location.SourceSpan.Start))
                return;

            var action = RenameTrackingTaggerProvider.CreateCodeAction(
                document, diagnostic, _refactorNotifyServices, _undoHistoryRegistry);
            context.RegisterRefactoring(action);
        }
    }
}
