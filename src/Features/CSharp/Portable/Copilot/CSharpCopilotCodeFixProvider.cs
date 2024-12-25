// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.LanguageService;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

/// <summary>
/// Code fix provider which provides fixes for Copilot diagnostics produced by
/// <see cref="ICopilotCodeAnalysisService"/>.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.CopilotSuggestions), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed partial class CSharpCopilotCodeFixProvider() : CodeFixProvider
{
    private const string CopilotDiagnosticId = "COPILOT";
    private const string FixPropertyName = "Fix";
    private const string PromptTitlePropertyName = "PromptTitle";

    private static SyntaxAnnotation WarningAnnotation { get; }
        = CodeActions.WarningAnnotation.Create(
            CSharpFeaturesResources.Warning_colon_AI_suggestions_might_be_inaccurate);

    /// <summary>
    /// Ensure that fixes for Copilot diagnostics are always towards the bottom of the lightbulb.
    /// </summary>
    /// <returns></returns>
    protected sealed override CodeActionRequestPriority ComputeRequestPriority() => CodeActionRequestPriority.Low;

    /// <summary>
    /// We do not support a FixAll operation for Copilot suggestions.
    /// </summary>
    /// <returns></returns>
    public sealed override FixAllProvider? GetFixAllProvider() => null;

    public sealed override ImmutableArray<string> FixableDiagnosticIds => [CopilotDiagnosticId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        if (document.GetLanguageService<ICopilotOptionsService>() is not { } copilotOptionsService ||
            await copilotOptionsService.IsCodeAnalysisOptionEnabledAsync().ConfigureAwait(false) is false)
        {
            return;
        }

        if (document.GetLanguageService<ICopilotCodeAnalysisService>() is not { } copilotService ||
            await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false) is false)
        {
            return;
        }

        var promptTitles = await copilotService.GetAvailablePromptTitlesAsync(document, cancellationToken).ConfigureAwait(false);
        if (promptTitles.IsEmpty)
            return;

        var hasMultiplePrompts = promptTitles.Length > 1;

        // Find the containing method for each diagnostic, and register a fix if any part of the method interect with context span.
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in context.Diagnostics)
        {
            var containingMethod = CSharpSyntaxFacts.Instance.GetContainingMethodDeclaration(root, diagnostic.Location.SourceSpan.Start, useFullSpan: false);
            if (containingMethod?.Span.IntersectsWith(context.Span) is true)
            {
                var fix = TryGetFix(document, containingMethod, diagnostic, hasMultiplePrompts);
                if (fix != null)
                    context.RegisterCodeFix(fix, diagnostic);
            }
        }
    }

    private static CodeAction? TryGetFix(
        Document document,
        SyntaxNode method,
        Diagnostic diagnostic,
        bool hasMultiplePrompts)
    {
        if (!diagnostic.Properties.TryGetValue(FixPropertyName, out var fix)
            || fix is null
            || !diagnostic.Properties.TryGetValue(PromptTitlePropertyName, out var promptTitle)
            || promptTitle is null)
        {
            return null;
        }

        // Parse the proposed Copilot fix into a method declaration.
        // Guard against failure cases where the proposed fixed code does not parse into a method declaration.
        // TODO: consider do this early when we create the diagnostic and add a flag in the property bag to speedup lightbulb computation
        var memberDeclaration = SyntaxFactory.ParseMemberDeclaration(fix, options: method.SyntaxTree.Options);
        if (memberDeclaration is null || memberDeclaration is not BaseMethodDeclarationSyntax baseMethodDeclaration || baseMethodDeclaration.GetDiagnostics().Count() > 3)
            return null;

        var title = hasMultiplePrompts
            ? $"{CSharpFeaturesResources.Apply_fix_from} {promptTitle}"
            : CSharpFeaturesResources.Apply_Copilot_suggestion;

        return new CopilotDocumentChangeCodeAction(
            title,
            createChangedDocument: (_, cancellationToken) => GetFixedDocumentAsync(method, fix, cancellationToken),
            equivalenceKey: title,
            new CopilotDismissChangesCodeAction(method, diagnostic),
            priority: CodeActionPriority.Low);

        async Task<Document> GetFixedDocumentAsync(SyntaxNode method, string fix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);

            // TODO: Replace all the whitespace trivia with elastic trivia, and any other trivia related improvements
            var newMethod = memberDeclaration
                .WithLeadingTrivia(memberDeclaration.HasLeadingTrivia ? memberDeclaration.GetLeadingTrivia() : method.GetLeadingTrivia())
                .WithTrailingTrivia(memberDeclaration.HasTrailingTrivia ? memberDeclaration.GetTrailingTrivia() : method.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation, WarningAnnotation);

            editor.ReplaceNode(method, newMethod);
            return editor.GetChangedDocument();
        }
    }
}
