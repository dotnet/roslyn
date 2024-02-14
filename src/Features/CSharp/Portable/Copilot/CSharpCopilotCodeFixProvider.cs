// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
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
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.CopilotSuggestions), Shared]
internal sealed partial class CSharpCopilotCodeFixProvider : CopilotCodeFixProvider
{
    private const string FixPropertyName = "Fix";
    private const string PromptTitlePropertyName = "PromptTitle";

    private static SyntaxAnnotation WarningAnnotation { get; }
        = CodeActions.WarningAnnotation.Create(
            CSharpFeaturesResources.Warning_colon_AI_suggestions_might_be_inaccurate);

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CSharpCopilotCodeFixProvider()
    {
    }

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        var copilotService = document.Project.Solution.Services.GetService<ICopilotCodeAnalysisService>();
        if (copilotService is null || !copilotService.IsCodeAnalysisOptionEnabled(document))
            return;

        var isAvailable = await copilotService.IsAvailableAsync(document, cancellationToken).ConfigureAwait(false);
        if (!isAvailable)
            return;

        var promptTitles = await copilotService.GetAvailablePromptTitlesAsync(document, cancellationToken).ConfigureAwait(false);
        if (promptTitles.IsEmpty)
            return;

        var hasMultiplePrompts = promptTitles.Length > 1;

        // Find the containing method, if any, and also update the fix span to the entire method.
        // TODO: count location in doc-comment as part of the method.
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var containingMethod = CSharpSyntaxFacts.Instance.GetContainingMethodDeclaration(root, context.Span.Start, useFullSpan: false);
        if (containingMethod is not BaseMethodDeclarationSyntax)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            Debug.Assert(containingMethod.FullSpan.IntersectsWith(diagnostic.Location.SourceSpan));

            var fix = TryGetFix(document, containingMethod, diagnostic, hasMultiplePrompts);
            if (fix != null)
                context.RegisterCodeFix(fix, diagnostic);
        }
    }

    private static CodeAction? TryGetFix(
        Document document,
        SyntaxNode method,
        Diagnostic diagnostic,
        bool hasMultiplePrompts)
    {
        if (diagnostic.Properties.TryGetValue(FixPropertyName, out var fix) is false ||
            diagnostic.Properties.TryGetValue(PromptTitlePropertyName, out var promptTitle) is false)
        {
            return null;
        }

        var title = hasMultiplePrompts
            ? $"{CSharpFeaturesResources.Apply_fix_from} {promptTitle}"
            : CSharpFeaturesResources.Apply_Copilot_suggestion;

        return new CopilotDocumentChangeCodeAction(
            title,
            createChangedDocument: (_, cancellationToken) => GetFixedDocumentAsync(method, fix!, cancellationToken),
            equivalenceKey: title,
            new CopilotDismissChangesCodeAction(method, diagnostic),
            priority: CodeActionPriority.Low);

        async Task<Document> GetFixedDocumentAsync(SyntaxNode method, string fix, CancellationToken cancellationToken)
        {
            var editor = await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(false);
            var fixMethod = SyntaxFactory.ParseMemberDeclaration(fix, options: editor.SemanticModel.SyntaxTree.Options)!;
            var newMethod = fixMethod
                                .WithLeadingTrivia(fixMethod.HasLeadingTrivia ? fixMethod.GetLeadingTrivia() : method.GetLeadingTrivia())
                                .WithTrailingTrivia(fixMethod.HasTrailingTrivia ? fixMethod.GetTrailingTrivia() : method.GetTrailingTrivia())
                                .WithAdditionalAnnotations(Formatter.Annotation, WarningAnnotation);

            editor.ReplaceNode(method, newMethod);
            return editor.GetChangedDocument();
        }
    }
}
