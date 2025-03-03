// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.UseConditionalExpression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using static Microsoft.CodeAnalysis.CodeActions.CodeAction;

namespace Microsoft.CodeAnalysis.CSharp.Copilot;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.CopilotImplementNotImplementedException), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpImplementNotImplementedExceptionFixProvider() : SyntaxEditorBasedCodeFixProvider
{
    private static SyntaxAnnotation WarningAnnotation { get; }
        = CodeActions.WarningAnnotation.Create(
            CSharpFeaturesResources.Warning_colon_AI_suggestions_might_be_inaccurate);

    public override ImmutableArray<string> FixableDiagnosticIds { get; }
        = [IDEDiagnosticIds.CopilotImplementNotImplementedExceptionDiagnosticId];

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (context.Diagnostics.Length == 0)
        {
            return;
        }

        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        if (document.GetLanguageService<ICopilotOptionsService>() is not { } optionsService ||
            await optionsService.IsImplementNotImplementedExceptionEnabledAsync().ConfigureAwait(false) is false)
        {
            return;
        }

        if (document.GetLanguageService<ICopilotCodeAnalysisService>() is not { } copilotService ||
            await copilotService.IsAvailableAsync(cancellationToken).ConfigureAwait(false) is false)
        {
            return;
        }

        var throwNode = context.Diagnostics[0].Location.FindNode(getInnermostNodeForTie: true, cancellationToken);

        // Preliminary analysis before registering fix
        var methodOrProperty = throwNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (methodOrProperty is BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax)
        {
            var fix = DocumentChangeAction.New(
                CSharpAnalyzersResources.Implement_with_Copilot,
                async (_, cancellationToken) => await GetDocumentUpdater(context, null)(cancellationToken).ConfigureAwait(false),
                (_, _) => Task.FromResult(context.Document),
                nameof(CSharpAnalyzersResources.Implement_with_Copilot));
            context.RegisterCodeFix(fix, context.Diagnostics[0]);
        }
    }

    private static async Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var progress = new StreamingProgressCollector();
        await SymbolFinder.FindReferencesAsync(
            symbol, document.Project.Solution, progress, documents: null,
            FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);

        return progress.GetReferencedSymbols();
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        foreach (var diagnostic in diagnostics)
        {
            await FixOneAsync(editor, document, diagnostic, cancellationToken).ConfigureAwait(false);
        }

        var changedRoot = editor.GetChangedRoot();

        // Get the language specific rule for formatting this construct and call into the
        // formatter to explicitly format things. Note: all we will format is the new
        // node as that's the only node that has the appropriate annotation on it.
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var rules = ImmutableArray.Create(MultiLineConditionalExpressionFormattingRule.Instance);
        var spansToFormat = FormattingExtensions.GetAnnotatedSpans(changedRoot, new());

        var formattedRoot = CSharpSyntaxFormatting.Instance.GetFormattingResult(changedRoot, spansToFormat, formattingOptions, rules, cancellationToken).GetFormattedRoot(cancellationToken);
        changedRoot = formattedRoot;

        editor.ReplaceNode(editor.OriginalRoot, changedRoot);
    }

    private static async Task FixOneAsync(
        SyntaxEditor editor, Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var throwNode = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
        var methodOrProperty = throwNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        Contract.ThrowIfNull(methodOrProperty);
        Contract.ThrowIfFalse(methodOrProperty is BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax);
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var memberSymbol = semanticModel.GetRequiredDeclaredSymbol(methodOrProperty, cancellationToken);
        var references = await FindReferencesAsync(document, memberSymbol, cancellationToken).ConfigureAwait(false);
        var copilotService = document.GetRequiredLanguageService<ICopilotCodeAnalysisService>();

        var (implementationSuggestion, isQuotaExceeded) = await copilotService.ImplementNotImplementedExceptionAsync(
            document, throwNode.Span, methodOrProperty, memberSymbol, semanticModel, references, cancellationToken).ConfigureAwait(false);

        MemberDeclarationSyntax replacement;
        if (isQuotaExceeded)
        {
            replacement = AddCommentToMember(methodOrProperty, CSharpFeaturesResources.Error_colon_Quota_exceeded);
        }
        else if (implementationSuggestion == null
            || !implementationSuggestion.TryGetValue("Implementation", out var implementation)
            || string.IsNullOrWhiteSpace(implementation))
        {
            var message = implementationSuggestion?.TryGetValue("Message", out var description) == true
                ? description
                : CSharpFeaturesResources.Error_colon_Could_not_complete_this_request;
            replacement = AddCommentToMember(methodOrProperty, message);
        }
        else
        {
            var parseOnce = SyntaxFactory.ParseMemberDeclaration(implementation, options: methodOrProperty.SyntaxTree.Options);
            replacement = parseOnce is BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax
                ? parseOnce
                    .WithLeadingTrivia(methodOrProperty.GetLeadingTrivia())
                    .WithTrailingTrivia(methodOrProperty.GetTrailingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation, WarningAnnotation, Simplifier.Annotation)
                : AddCommentToMember(methodOrProperty, CSharpFeaturesResources.Error_colon_Failed_to_parse_into_a_method_or_property);
        }

        editor.ReplaceNode(methodOrProperty, replacement);
    }

    private static MemberDeclarationSyntax AddCommentToMember(MemberDeclarationSyntax member, string message)
    {
        var leadingTrivia = member.GetLeadingTrivia();
        var comment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment($"/* {message} */"),
            SyntaxFactory.CarriageReturnLineFeed);

        // Find the position after all blank lines
        var insertIndex = 0;
        while (insertIndex < leadingTrivia.Count &&
               leadingTrivia[insertIndex].IsKind(SyntaxKind.EndOfLineTrivia))
        {
            insertIndex++;
        }

        // Insert the comment after any blank lines
        var newLeadingTrivia = leadingTrivia.InsertRange(insertIndex, comment);

        return member
            .WithLeadingTrivia(newLeadingTrivia)
            .WithAdditionalAnnotations(Formatter.Annotation, WarningAnnotation, Simplifier.Annotation);
    }
}
