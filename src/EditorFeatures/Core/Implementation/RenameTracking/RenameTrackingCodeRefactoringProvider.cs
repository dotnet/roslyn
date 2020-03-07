// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Composition;
using System.Linq;
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

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            // Ensure rename can still be invoked in this document. We reanalyze the document for
            // diagnostics when rename tracking is manually dismissed, but the existence of our
            // diagnostic may still be cached, so we have to double check before actually providing
            // any fixes.
            if (RenameTrackingTaggerProvider.CanInvokeRename(document))
            {
                var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var diagnostic = RenameTrackingTaggerProvider.TryGetDiagnostic(
                    syntaxTree, DiagnosticDescriptor, cancellationToken);

                var action = RenameTrackingTaggerProvider.CreateCodeAction(
                    document, diagnostic, _refactorNotifyServices, _undoHistoryRegistry);
                context.RegisterRefactoring(action);
            }
        }
    }
}
