// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
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
        // Build a dictionary to track method/property nodes and their references
        var inputMethodOrPropertyItems = new Dictionary<MemberDeclarationSyntax, ImmutableArray<ReferencedSymbol>>();

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        foreach (var diagnostic in diagnostics)
        {
            var throwNode = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            var methodOrProperty = throwNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();

            Contract.ThrowIfNull(methodOrProperty);
            Contract.ThrowIfFalse(methodOrProperty is BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax);

            // Skip recomputing refs if we've already checked the same methodOrProperty
            if (!inputMethodOrPropertyItems.ContainsKey(methodOrProperty))
            {
                var memberSymbol = semanticModel.GetRequiredDeclaredSymbol(methodOrProperty, cancellationToken);
                var references = await FindReferencesAsync(document, memberSymbol, cancellationToken).ConfigureAwait(false);

                // Store references in the input dictionary for copilot request
                inputMethodOrPropertyItems.Add(methodOrProperty, references);
            }
        }

        var copilotService = document.GetRequiredLanguageService<ICopilotCodeAnalysisService>();
        // Assume the new API in copilotService to handle multiple methods/properties at once
        // This line would need to be updated once the actual API is implemented
        //var resultingMethodOrPropertyItems = await copilotService.ImplementNotImplementedExceptionAsync(document, inputMethodOrPropertyItems, cancellationToken).ConfigureAwait(false);

        var resultingMethodOrPropertyItems = new Dictionary<MemberDeclarationSyntax, ImplementationDetails>();
        foreach (var item in inputMethodOrPropertyItems)
        {
            var responseFromOldApi = await copilotService.ImplementNotImplementedExceptionAsync(document, item.Key, item.Value, cancellationToken).ConfigureAwait(false);
            if (responseFromOldApi == null)
            {
                continue;
            }

            var implementationDetails = new ImplementationDetails
            {
                ReplacementNode = responseFromOldApi.ReplacementNode,
                Message = responseFromOldApi.Message
            };

            resultingMethodOrPropertyItems.Add(item.Key, implementationDetails);
        }

        foreach (var methodOrProperty in inputMethodOrPropertyItems.Keys)
        {
            SyntaxNode? replacement;
            if (!resultingMethodOrPropertyItems.TryGetValue(methodOrProperty, out var implementationDetails))
            {
                replacement = AddErrorComment(methodOrProperty);
            }
            else
            {
                replacement = implementationDetails.ReplacementNode;
                if (replacement != null && (replacement is BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax))
                {
                    replacement = replacement
                        .WithLeadingTrivia(methodOrProperty.GetLeadingTrivia())
                        .WithTrailingTrivia(methodOrProperty.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation, WarningAnnotation, Simplifier.Annotation);
                }
                else
                {
                    replacement = AddErrorComment(methodOrProperty, implementationDetails.Message);
                }
            }

            editor.ReplaceNode(methodOrProperty, replacement);
        }

        var changedRoot = editor.GetChangedRoot();
        var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);
        var spansToFormat = FormattingExtensions.GetAnnotatedSpans(changedRoot, new());
        var formattedRoot = CSharpSyntaxFormatting.Instance.GetFormattingResult(changedRoot, spansToFormat, formattingOptions, new(), cancellationToken).GetFormattedRoot(cancellationToken);
        changedRoot = formattedRoot;

        editor.ReplaceNode(editor.OriginalRoot, changedRoot);
    }

    private static async Task<ImmutableArray<ReferencedSymbol>> FindReferencesAsync(Document document, ISymbol symbol, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var searchOptions = FindReferencesSearchOptions.GetFeatureOptionsForStartingSymbol(symbol);
        return await SymbolFinder.FindReferencesAsync(symbol, document.Project.Solution, searchOptions, cancellationToken).ConfigureAwait(false);
    }

    private static MemberDeclarationSyntax AddErrorComment(MemberDeclarationSyntax member, string? message = null)
    {
        var errorMessage = string.IsNullOrWhiteSpace(message) ? CSharpFeaturesResources.Failed_to_receive_implementation_from_Copilot_service : message;
        var comment = SyntaxFactory.TriviaList(
            SyntaxFactory.Comment($"/* {errorMessage} */"),
            SyntaxFactory.CarriageReturnLineFeed);
        var leadingTrivia = member.GetLeadingTrivia();

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
