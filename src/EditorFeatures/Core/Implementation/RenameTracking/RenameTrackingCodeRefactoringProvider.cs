// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.VisualStudio.Text.Operations;

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
        Name = PredefinedCodeRefactoringProviderNames.RenameTracking), Shared]
    internal class RenameTrackingCodeRefactoringProvider : CodeRefactoringProvider
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEnumerable<IRefactorNotifyService> _refactorNotifyServices;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public RenameTrackingCodeRefactoringProvider(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            [ImportMany] IEnumerable<IRefactorNotifyService> refactorNotifyServices)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _refactorNotifyServices = refactorNotifyServices;
        }

        public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var (action, renameSpan) = RenameTrackingTaggerProvider.TryGetCodeAction(
                document, span, _refactorNotifyServices, _undoHistoryRegistry, cancellationToken);

            if (action != null)
                context.RegisterRefactoring(action, renameSpan);

            return Task.CompletedTask;
        }

        /// <summary>
        /// This is a high priority refactoring that we want to run first so that the user can quickly
        /// change the name of something and pop up the lightbulb without having to wait for the rest to
        /// compute.
        /// </summary>
        private protected override CodeActionRequestPriority ComputeRequestPriority()
            => CodeActionRequestPriority.High;
    }
}
