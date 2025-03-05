// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
                title: CSharpAnalyzersResources.Implement_with_Copilot,
                createChangedDocument: async (_, cancellationToken) => await GetDocumentUpdater(context: context, diagnostic: null)(cancellationToken).ConfigureAwait(false),
                createChangedDocumentPreview: (_, _) => Task.FromResult(context.Document),
                equivalenceKey: nameof(CSharpAnalyzersResources.Implement_with_Copilot));
            context.RegisterCodeFix(fix, context.Diagnostics[0]);
        }
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
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var spansToFormat = FormattingExtensions.GetAnnotatedSpans(changedRoot, new());
        var formattedRoot = CSharpSyntaxFormatting.Instance.GetFormattingResult(changedRoot, spansToFormat, formattingOptions, [], cancellationToken).GetFormattedRoot(cancellationToken);
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
        var implementationDetails = await copilotService.ImplementNotImplementedExceptionAsync(document, throwNode, references, cancellationToken).ConfigureAwait(false);

        var replacement = implementationDetails.ReplacementNode;
        if (implementationDetails.IsQuotaExceeded)
        {
            replacement = AddCommentToMember(methodOrProperty, CSharpFeaturesResources.Error_colon_Quota_exceeded);
        }
        else if (replacement is BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax)
        {
            replacement = replacement
                .WithLeadingTrivia(methodOrProperty.GetLeadingTrivia())
                .WithTrailingTrivia(methodOrProperty.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation, WarningAnnotation, Simplifier.Annotation);
        }
        else
        {
            replacement = AddCommentToMember(methodOrProperty, string.Format(CSharpFeaturesResources.Error_colon_Failed_to_parse_into_a_method_or_property, implementationDetails.Message));
        }

        editor.ReplaceNode(methodOrProperty, replacement);
    }

    private static async Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var searchOptions = FindReferencesSearchOptions.GetFeatureOptionsForStartingSymbol(symbol);
        return await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, searchOptions, cancellationToken).ConfigureAwait(false);
    }

    private static MemberDeclarationSyntax AddCommentToMember(MemberDeclarationSyntax member, string message)
    {
        var leadingTrivia = member.GetLeadingTrivia();
        var comment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment($"/* {message} */"),
            SyntaxFactory.CarriageReturnLineFeed);

        // Find the last EndOfLineTrivia
        var syntaxTrivia = leadingTrivia.LastOrDefault(static trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
        var lastEndOfLineIndex = leadingTrivia.IndexOf(syntaxTrivia);

        // Insert the comment after the last EndOfLineTrivia or at the start if none found
        var insertIndex = lastEndOfLineIndex >= 0 ? lastEndOfLineIndex + 1 : 0;
        var newLeadingTrivia = leadingTrivia.InsertRange(insertIndex, comment);

        return member
            .WithLeadingTrivia(newLeadingTrivia)
            .WithAdditionalAnnotations(Formatter.Annotation, WarningAnnotation, Simplifier.Annotation);
    }
}
