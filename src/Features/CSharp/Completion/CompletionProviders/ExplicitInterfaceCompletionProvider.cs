// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class ExplicitInterfaceCompletionProvider : AbstractCompletionProvider
    {
        public override bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            return CompletionUtilities.IsCommitCharacter(completionItem, ch, textTypedSoFar);
        }

        public override bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar)
        {
            return CompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return text[characterPosition] == '.';
        }

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(
            Document document,
            int position,
            CompletionTriggerInfo triggerInfo,
            CancellationToken cancellationToken)
        {
            var span = new TextSpan(position, 0);
            var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
            var syntaxTree = semanticModel.SyntaxTree;

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken) ||
                semanticFacts.IsPreProcessorDirectiveContext(semanticModel, position, cancellationToken))
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            if (!syntaxTree.IsRightOfDotOrArrowOrColonColon(position, cancellationToken))
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            var node = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                 .GetPreviousTokenIfTouchingWord(position)
                                 .Parent;

            if (node.Kind() == SyntaxKind.ExplicitInterfaceSpecifier)
            {
                return await GetCompletionsOffOfExplicitInterfaceAsync(
                    document, semanticModel, position, ((ExplicitInterfaceSpecifierSyntax)node).Name, cancellationToken).ConfigureAwait(false);
            }

            return SpecializedCollections.EmptyEnumerable<CompletionItem>();
        }

        private async Task<IEnumerable<CompletionItem>> GetCompletionsOffOfExplicitInterfaceAsync(
            Document document, SemanticModel semanticModel, int position, NameSyntax name, CancellationToken cancellationToken)
        {
            // Bind the interface name which is to the left of the dot
            var syntaxTree = semanticModel.SyntaxTree;
            var nameBinding = semanticModel.GetSymbolInfo(name, cancellationToken);
            var context = CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken);

            var symbol = nameBinding.Symbol as ITypeSymbol;
            if (symbol == null || symbol.TypeKind != TypeKind.Interface)
            {
                return SpecializedCollections.EmptyEnumerable<CompletionItem>();
            }

            var members = semanticModel.LookupSymbols(
                position: name.SpanStart,
                container: symbol)
                    .Where(s => !s.IsStatic)
                    .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);

            // We're going to create a entry for each one, including the signature
            var completions = new List<CompletionItem>();

            var signatureDisplayFormat =
                new SymbolDisplayFormat(
                    genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                    memberOptions:
                        SymbolDisplayMemberOptions.IncludeParameters,
                    parameterOptions:
                        SymbolDisplayParameterOptions.IncludeName |
                        SymbolDisplayParameterOptions.IncludeType |
                        SymbolDisplayParameterOptions.IncludeParamsRefOut,
                    miscellaneousOptions:
                        SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                        SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var namePosition = name.SpanStart;

            var text = await context.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textChangeSpan = CompletionUtilities.GetTextChangeSpan(text, context.Position);

            foreach (var member in members)
            {
                var displayString = member.ToMinimalDisplayString(semanticModel, namePosition, signatureDisplayFormat);
                var memberCopied = member;
                var insertionText = displayString;

                completions.Add(new SymbolCompletionItem(
                    this,
                    displayString,
                    insertionText: insertionText,
                    filterSpan: textChangeSpan,
                    position: position,
                    symbols: new List<ISymbol> { member },
                    context: context));
            }

            return completions;
        }

        public override TextChange GetTextChange(CompletionItem selectedItem, char? ch = default(char), string textTypedSoFar = null)
        {
            if (ch.HasValue && ch.Value == '(')
            {
                return new TextChange(selectedItem.FilterSpan, ((SymbolCompletionItem)selectedItem).Symbols[0].Name);
            }

            return new TextChange(selectedItem.FilterSpan, selectedItem.DisplayText);
        }
    }
}
