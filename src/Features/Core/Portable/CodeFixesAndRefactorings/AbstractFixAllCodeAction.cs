// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixesAndRefactorings;

/// <summary>
/// Fix all code action for a code action registered by
/// a <see cref="CodeFixes.CodeFixProvider"/> or a <see cref="CodeRefactorings.CodeRefactoringProvider"/>.
/// </summary>
internal sealed class RefactorOrFixAllCodeAction(
    IRefactorOrFixAllState refactorOrFixAllState,
    bool showPreviewChangesDialog,
    string? title = null,
    string? message = null) : CodeAction
{
    private static readonly ISet<string> s_predefinedProviderNames =
        typeof(PredefinedCodeFixProviderNames).GetTypeInfo().DeclaredFields.Concat(typeof(PredefinedCodeRefactoringProviderNames).GetTypeInfo().DeclaredFields)
            .Where(field => field.IsStatic)
            .Select(field => (string)field.GetValue(null)!)
            .ToSet();

    private bool _showPreviewChangesDialog = showPreviewChangesDialog;

    public IRefactorOrFixAllState RefactorOrFixAllState { get; } = refactorOrFixAllState;

    // We don't need to post process changes here as the inner code action created for Fix multiple code fix already executes.
    internal sealed override CodeActionCleanup Cleanup => CodeActionCleanup.None;

    /// <summary>
    /// Creates a new <see cref="IRefactorOrFixAllContext"/> with the given parameters.
    /// </summary>
    private static IRefactorOrFixAllContext CreateFixAllContext(IRefactorOrFixAllState state, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        return state switch
        {
            FixAllState fixAllState => new FixAllContext(fixAllState, progressTracker, cancellationToken),
            RefactorAllState refactorAllState => new RefactorAllContext(refactorAllState, progressTracker, cancellationToken),
            _ => throw ExceptionUtilities.UnexpectedValue(state),
        };
    }

    public override string Title
        => title ?? (this.RefactorOrFixAllState.Scope switch
        {
            FixAllScope.Document => FeaturesResources.Document,
            FixAllScope.Project => FeaturesResources.Project,
            FixAllScope.Solution => FeaturesResources.Solution,
            FixAllScope.ContainingMember => FeaturesResources.Containing_Member,
            FixAllScope.ContainingType => FeaturesResources.Containing_Type,
            _ => throw ExceptionUtilities.UnexpectedValue(this.RefactorOrFixAllState.Scope),
        });

    internal override string Message => message ?? FeaturesResources.Computing_fix_all_occurrences_code_fix;

    protected sealed override Task<ImmutableArray<CodeActionOperation>> ComputeOperationsAsync(
        IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        FixAllLogger.LogState(RefactorOrFixAllState, IsInternalProvider(RefactorOrFixAllState));

        var service = RefactorOrFixAllState.Project.Solution.Services.GetRequiredService<IFixAllGetFixesService>();

        var fixAllContext = CreateFixAllContext(RefactorOrFixAllState, progressTracker, cancellationToken);
        progressTracker.Report(CodeAnalysisProgress.Description(fixAllContext.GetDefaultTitle()));

        return service.GetFixAllOperationsAsync(fixAllContext, _showPreviewChangesDialog);
    }

    protected sealed override Task<Solution?> GetChangedSolutionAsync(
        IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        FixAllLogger.LogState(RefactorOrFixAllState, IsInternalProvider(RefactorOrFixAllState));

        var service = RefactorOrFixAllState.Project.Solution.Services.GetRequiredService<IFixAllGetFixesService>();

        var fixAllContext = CreateFixAllContext(RefactorOrFixAllState, progressTracker, cancellationToken);
        progressTracker.Report(CodeAnalysisProgress.Description(fixAllContext.GetDefaultTitle()));

        return service.GetFixAllChangedSolutionAsync(fixAllContext);
    }

    /// <summary>
    /// Determine if the <see cref="IRefactorOrFixAllState.Provider"/> is an internal first-party provider or not.
    /// </summary>
    private static bool IsInternalProvider(IRefactorOrFixAllState fixAllState)
    {
        var exportAttributes = fixAllState.Provider.GetType().GetTypeInfo().GetCustomAttributes(typeof(ExportCodeFixProviderAttribute), false);
        if (exportAttributes?.FirstOrDefault() is ExportCodeFixProviderAttribute codeFixAttribute)
        {
            return !string.IsNullOrEmpty(codeFixAttribute.Name)
                && s_predefinedProviderNames.Contains(codeFixAttribute.Name);
        }

        exportAttributes = fixAllState.Provider.GetType().GetTypeInfo().GetCustomAttributes(typeof(ExportCodeRefactoringProviderAttribute), false);
        if (exportAttributes?.FirstOrDefault() is ExportCodeRefactoringProviderAttribute codeRefactoringAttribute)
        {
            return !string.IsNullOrEmpty(codeRefactoringAttribute.Name)
                && s_predefinedProviderNames.Contains(codeRefactoringAttribute.Name);
        }

        return false;
    }

    // internal for testing purposes.
    internal TestAccessor GetTestAccessor()
        => new(this);

    // internal for testing purposes.
    internal readonly struct TestAccessor
    {
        private readonly RefactorOrFixAllCodeAction _fixAllCodeAction;

        internal TestAccessor(RefactorOrFixAllCodeAction fixAllCodeAction)
            => _fixAllCodeAction = fixAllCodeAction;

        /// <summary>
        /// Gets a reference to <see cref="_showPreviewChangesDialog"/>, which can be read or written by test code.
        /// </summary>
        public ref bool ShowPreviewChangesDialog
            => ref _fixAllCodeAction._showPreviewChangesDialog;
    }
}
