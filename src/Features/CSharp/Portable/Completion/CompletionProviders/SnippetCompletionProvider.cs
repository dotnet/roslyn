// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class SnippetCompletionProvider : CompletionListProvider
    {
        // If null, the document's language service will be used.
        private readonly ISnippetInfoService _snippetInfoService;

        public SnippetCompletionProvider(ISnippetInfoService snippetInfoService = null)
        {
            _snippetInfoService = snippetInfoService;
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            if (!options.GetOption(CSharpCompletionOptions.IncludeSnippets))
            {
                return false;
            }

            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            using (Logger.LogBlock(FunctionId.Completion_SnippetCompletionProvider_GetItemsWorker_CSharp, cancellationToken))
            {
                // TODO (https://github.com/dotnet/roslyn/issues/5107): Enable in Interactive.
                var workspace = document.Project.Solution.Workspace;
                if (!workspace.CanApplyChange(ApplyChangesKind.ChangeDocument) ||
                     workspace.Kind == WorkspaceKind.Debugger ||
                     workspace.Kind == WorkspaceKind.Interactive)
                {
                    return;
                }

                if (!options.GetOption(CSharpCompletionOptions.IncludeSnippets))
                {
                    return;
                }

                var snippetCompletionItems = await document.GetUnionItemsFromDocumentAndLinkedDocumentsAsync(
                    UnionCompletionItemComparer.Instance,
                    (d, c) => GetSnippetsForDocumentAsync(d, position, workspace, c),
                    cancellationToken).ConfigureAwait(false);

                context.AddItems(snippetCompletionItems);
            }
        }

        private async Task<IEnumerable<CompletionItem>> GetSnippetsForDocumentAsync(Document document, int position, Workspace workspace, CancellationToken cancellationToken)
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken) ||
                syntaxTree.IsRightOfDotOrArrowOrColonColon(position, cancellationToken) ||
                syntaxFacts.GetContainingTypeDeclaration(await syntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false), position) is EnumDeclarationSyntax)
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            var span = new TextSpan(position, 0);
            var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
            if (semanticFacts.IsPreProcessorDirectiveContext(semanticModel, position, cancellationToken))
            {
                var directive = syntaxTree.GetRoot(cancellationToken).FindTokenOnLeftOfPosition(position, includeDirectives: true).GetAncestor<DirectiveTriviaSyntax>();
                if (directive.DirectiveNameToken.IsKind(
                    SyntaxKind.IfKeyword,
                    SyntaxKind.RegionKeyword,
                    SyntaxKind.ElseKeyword,
                    SyntaxKind.ElifKeyword,
                    SyntaxKind.ErrorKeyword,
                    SyntaxKind.LineKeyword,
                    SyntaxKind.PragmaKeyword,
                    SyntaxKind.EndIfKeyword,
                    SyntaxKind.UndefKeyword,
                    SyntaxKind.EndRegionKeyword,
                    SyntaxKind.WarningKeyword))
                {
                    return SpecializedCollections.EmptyEnumerable<CompletionItem>();
                }

                return await GetSnippetCompletionItemsAsync(workspace, semanticModel, position, isPreProcessorContext: true, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            if (semanticFacts.IsGlobalStatementContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsExpressionContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsStatementContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsTypeContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsTypeDeclarationContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsNamespaceContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsMemberDeclarationContext(semanticModel, position, cancellationToken) ||
                semanticFacts.IsLabelContext(semanticModel, position, cancellationToken))
            {
                return await GetSnippetCompletionItemsAsync(workspace, semanticModel, position, isPreProcessorContext: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }

        private async Task<IEnumerable<CompletionItem>> GetSnippetCompletionItemsAsync(Workspace workspace, SemanticModel semanticModel, int position, bool isPreProcessorContext, CancellationToken cancellationToken)
        {
            var service = _snippetInfoService ?? workspace.Services.GetLanguageServices(semanticModel.Language).GetService<ISnippetInfoService>();
            if (service == null)
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            var snippets = service.GetSnippetsIfAvailable();
            if (isPreProcessorContext)
            {
                snippets = snippets.Where(snippet => snippet.Shortcut.StartsWith("#", StringComparison.Ordinal));
            }

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return snippets.Select(snippet => new CompletionItem(
                this,
                displayText: isPreProcessorContext ? snippet.Shortcut.Substring(1) : snippet.Shortcut,
                sortText: isPreProcessorContext ? snippet.Shortcut.Substring(1) : snippet.Shortcut,
                descriptionFactory: c => Task.FromResult((snippet.Title + Environment.NewLine + snippet.Description).ToSymbolDisplayParts()),
                filterSpan: CompletionUtilities.GetTextChangeSpan(text, position),
                glyph: Glyph.Snippet,
                shouldFormatOnCommit: service.ShouldFormatSnippet(snippet)));
        }
    }
}
