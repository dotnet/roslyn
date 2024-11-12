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
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Snippets;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(SnippetCompletionProvider), LanguageNames.CSharp)]
[ExtensionOrder(After = nameof(CrefCompletionProvider))]
[Shared]
internal sealed class SnippetCompletionProvider : LSPCompletionProvider
{
    private static readonly HashSet<string> s_snippetsWithReplacements =
    [
        CSharpSnippetIdentifiers.Class,
        CommonSnippetIdentifiers.ConsoleWriteLine,
        CommonSnippetIdentifiers.Constructor,
        CSharpSnippetIdentifiers.Do,
        CSharpSnippetIdentifiers.Else,
        CSharpSnippetIdentifiers.Enum,
        CSharpSnippetIdentifiers.For,
        CSharpSnippetIdentifiers.ReversedFor,
        CSharpSnippetIdentifiers.ForEach,
        CSharpSnippetIdentifiers.If,
        CSharpSnippetIdentifiers.Interface,
        CSharpSnippetIdentifiers.Lock,
        CommonSnippetIdentifiers.Property,
        CommonSnippetIdentifiers.GetOnlyProperty,
        CSharpSnippetIdentifiers.StaticIntMain,
        CSharpSnippetIdentifiers.Struct,
        CSharpSnippetIdentifiers.StaticVoidMain,
        CSharpSnippetIdentifiers.While
    ];

    internal override bool IsSnippetProvider => true;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SnippetCompletionProvider()
    {
    }

    internal override string Language => LanguageNames.CSharp;

    public override bool IsInsertionTrigger(SourceText text, int characterPosition, CompletionOptions options)
        => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);

    public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters;

    public override async Task ProvideCompletionsAsync(CompletionContext context)
    {
        try
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            using (Logger.LogBlock(FunctionId.Completion_SnippetCompletionProvider_GetItemsWorker_CSharp, cancellationToken))
            {
                // TODO (https://github.com/dotnet/roslyn/issues/5107): Enable in Interactive.
                var solution = document.Project.Solution;
                if (!solution.CanApplyChange(ApplyChangesKind.ChangeDocument) ||
                     solution.WorkspaceKind is WorkspaceKind.Debugger or WorkspaceKind.Interactive)
                {
                    return;
                }

                context.AddItems(await document.GetUnionItemsFromDocumentAndLinkedDocumentsAsync(
                    UnionCompletionItemComparer.Instance,
                    d => GetSnippetsForDocumentAsync(d, context, cancellationToken)).ConfigureAwait(false));
            }
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
            // nop
        }
    }

    private static async Task<ImmutableArray<CompletionItem>> GetSnippetsForDocumentAsync(
        Document document, CompletionContext completionContext, CancellationToken cancellationToken)
    {
        var position = completionContext.Position;
        var syntaxTree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();

        var root = syntaxTree.GetRoot(cancellationToken);
        var leftToken = root.FindTokenOnLeftOfPosition(position, includeDirectives: true);
        var targetToken = leftToken.GetPreviousTokenIfTouchingWord(position);

        if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken) ||
            syntaxTree.IsRightOfDotOrArrowOrColonColon(position, targetToken, cancellationToken) ||
            syntaxFacts.GetContainingTypeDeclaration(root, position) is EnumDeclarationSyntax ||
            syntaxTree.IsPossibleTupleContext(leftToken, position))
        {
            return [];
        }

        var context = await completionContext.GetSyntaxContextWithExistingSpeculativeModelAsync(document, cancellationToken).ConfigureAwait(false);
        var semanticModel = context.SemanticModel;

        if (syntaxFacts.IsPreProcessorDirectiveContext(syntaxTree, position, cancellationToken))
        {
            var directive = leftToken.GetAncestor<DirectiveTriviaSyntax>();
            Contract.ThrowIfNull(directive);

            if (directive.DirectiveNameToken.Kind() is not (
                    SyntaxKind.IfKeyword or
                    SyntaxKind.RegionKeyword or
                    SyntaxKind.ElseKeyword or
                    SyntaxKind.ElifKeyword or
                    SyntaxKind.ErrorKeyword or
                    SyntaxKind.LineKeyword or
                    SyntaxKind.PragmaKeyword or
                    SyntaxKind.EndIfKeyword or
                    SyntaxKind.UndefKeyword or
                    SyntaxKind.EndRegionKeyword or
                    SyntaxKind.WarningKeyword))
            {
                return GetSnippetCompletionItems(
                    completionContext, document.Project.Solution.Services, semanticModel, isPreProcessorContext: true);
            }
        }
        else
        {
            if (semanticFacts.IsGlobalStatementContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsExpressionContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsStatementContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsTypeContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsTypeDeclarationContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsNamespaceContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsNamespaceDeclarationNameContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsMemberDeclarationContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsLabelContext(semanticModel, position, cancellationToken))
            {
                return GetSnippetCompletionItems(
                    completionContext, document.Project.Solution.Services, semanticModel, isPreProcessorContext: false);
            }
        }

        return [];
    }

    private static ImmutableArray<CompletionItem> GetSnippetCompletionItems(
        CompletionContext context, SolutionServices services, SemanticModel semanticModel, bool isPreProcessorContext)
    {
        var service = services.GetLanguageServices(semanticModel.Language).GetService<ISnippetInfoService>();
        if (service == null)
            return [];

        var snippets = service.GetSnippetsIfAvailable();
        if (context.CompletionOptions.ShouldShowNewSnippetExperience(context.Document))
        {
            snippets = snippets.Where(snippet => !s_snippetsWithReplacements.Contains(snippet.Shortcut));
        }

        if (isPreProcessorContext)
        {
            snippets = snippets.Where(snippet => snippet.Shortcut != null && snippet.Shortcut.StartsWith("#", StringComparison.Ordinal));
        }

        return snippets.SelectAsArray(snippet =>
        {
            var rules = CompletionItemRules.Default.WithFormatOnCommit(service.ShouldFormatSnippet(snippet));

            return CommonCompletionItem.Create(
                displayText: isPreProcessorContext ? snippet.Shortcut[1..] : snippet.Shortcut,
                displayTextSuffix: "",
                sortText: isPreProcessorContext ? snippet.Shortcut[1..] : snippet.Shortcut,
                description: (snippet.Title + Environment.NewLine + snippet.Description).ToSymbolDisplayParts(),
                glyph: Glyph.Snippet,
                rules: rules);
        });
    }
}
