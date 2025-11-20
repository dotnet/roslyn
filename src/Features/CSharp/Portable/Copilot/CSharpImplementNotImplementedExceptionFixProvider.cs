// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Copilot;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Tags;
using Roslyn.Utilities;
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
        var document = context.Document;
        var cancellationToken = context.CancellationToken;

        // Checks for feature flag
        if (document.GetLanguageService<ICopilotOptionsService>() is not { } optionsService ||
            await optionsService.IsImplementNotImplementedExceptionEnabledAsync().ConfigureAwait(false) is false)
        {
            return;
        }

        // Checks for service availability
        if (document.GetLanguageService<ICopilotCodeAnalysisService>() is not { } copilotService ||
            await copilotService.IsImplementNotImplementedExceptionsAvailableAsync(cancellationToken).ConfigureAwait(false) is false)
        {
            return;
        }

        var diagnosticNode = context.Diagnostics[0].Location.FindNode(getInnermostNodeForTie: true, cancellationToken);

        // Preliminary analysis before registering fix
        var methodOrProperty = diagnosticNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();

        if (methodOrProperty is BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax)
        {
            // Pull out the computation into a lazy computation here.  That way if we compute (and thus cache) the
            // result for the preview window, we'll produce the same value when the fix is actually applied.
            var lazy = AsyncLazy.Create(GetDocumentUpdater(context));

            var codeAction = new CopilotCodeAction(
                    title: CSharpAnalyzersResources.Implement,
                    (progress, cancellationToken) => lazy.GetValueAsync(cancellationToken),
                    equivalenceKey: nameof(CSharpAnalyzersResources.Implement));

            context.RegisterCodeFix(
                codeAction,
                context.Diagnostics);

            Logger.Log(FunctionId.Copilot_Implement_NotImplementedException_Fix_Registered, logLevel: LogLevel.Information);
        }
    }

    protected override async Task FixAllAsync(
        Document document, ImmutableArray<Diagnostic> diagnostics,
        SyntaxEditor editor, CancellationToken cancellationToken)
    {
        var memberReferencesBuilder = ImmutableDictionary.CreateBuilder<SyntaxNode, ImmutableArray<ReferencedSymbol>>();
        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        foreach (var diagnostic in diagnostics)
        {
            var diagnosticNode = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken);
            var methodOrProperty = diagnosticNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();

            Contract.ThrowIfFalse(methodOrProperty is BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax);

            if (!memberReferencesBuilder.ContainsKey(methodOrProperty))
            {
                var memberSymbol = semanticModel.GetRequiredDeclaredSymbol(methodOrProperty, cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();
                var searchOptions = FindReferencesSearchOptions.GetFeatureOptionsForStartingSymbol(memberSymbol);
                var references = await SymbolFinder.FindReferencesAsync(memberSymbol, document.Project.Solution, searchOptions, cancellationToken).ConfigureAwait(false);
                memberReferencesBuilder.Add(methodOrProperty, references);
            }
        }

        var copilotService = document.GetRequiredLanguageService<ICopilotCodeAnalysisService>();
        var memberImplementationDetails = await copilotService.ImplementNotImplementedExceptionsAsync(document, memberReferencesBuilder.ToImmutable(), cancellationToken).ConfigureAwait(false);

        foreach (var node in memberReferencesBuilder.Keys)
        {
            var methodOrProperty = (MemberDeclarationSyntax)node;

            Contract.ThrowIfFalse(memberImplementationDetails.TryGetValue(methodOrProperty, out var implementationDetails));

            var replacement = implementationDetails.ReplacementNode;
            if (replacement is BasePropertyDeclarationSyntax or BaseMethodDeclarationSyntax)
            {
                replacement = replacement
                    .WithTriviaFrom(methodOrProperty)
                    .WithAdditionalAnnotations(Formatter.Annotation, WarningAnnotation, Simplifier.Annotation);
            }
            else
            {
                Contract.ThrowIfTrue(string.IsNullOrWhiteSpace(implementationDetails.Message));
                replacement = AddErrorComment(methodOrProperty, implementationDetails.Message);
            }

            editor.ReplaceNode(methodOrProperty, replacement);
        }

        editor.ReplaceNode(editor.OriginalRoot, editor.GetChangedRoot());
        Logger.Log(FunctionId.Copilot_Implement_NotImplementedException_Completed, logLevel: LogLevel.Information);
    }

    private static MemberDeclarationSyntax AddErrorComment(MemberDeclarationSyntax member, string errorMessage)
    {
        Logger.Log(FunctionId.Copilot_Implement_NotImplementedException_Failed, errorMessage, logLevel: LogLevel.Error);

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

    /// <summary>
    /// Custom code action that wraps the document change with Copilot tag to display the Sparkle icon.
    /// </summary>
    private sealed class CopilotCodeAction(
        string title,
        Func<IProgress<CodeAnalysisProgress>, CancellationToken, Task<Document>> createChangedDocument,
        string? equivalenceKey) : DocumentChangeAction(title, createChangedDocument, equivalenceKey)
    {
        public override ImmutableArray<string> Tags { get; } = [WellKnownTags.Copilot];
    }
}
