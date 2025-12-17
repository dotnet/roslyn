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

namespace Microsoft.CodeAnalysis.Editor.Implementation.RenameTracking;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic,
    Name = PredefinedCodeRefactoringProviderNames.RenameTracking), Shared]
internal sealed class RenameTrackingCodeRefactoringProvider : CodeRefactoringProvider
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

        // Backdoor that allows this provider to use the high-priority bucket.
        this.CustomTags = this.CustomTags.Add(CodeAction.CanBeHighPriorityTag);
    }

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, _) = context;

        var (action, renameSpan) = RenameTrackingTaggerProvider.TryGetCodeAction(
            document, span, _refactorNotifyServices, _undoHistoryRegistry);

        if (action != null)
            context.RegisterRefactoring(action, renameSpan);
    }

    /// <summary>
    /// This is a high priority refactoring that we want to run first so that the user can quickly
    /// change the name of something and pop up the lightbulb without having to wait for the rest to
    /// compute.
    /// </summary>
    protected override CodeActionRequestPriority ComputeRequestPriority()
        => CodeActionRequestPriority.High;
}
