// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
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

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class ExplicitInterfaceCompletionProvider : CompletionListProvider
    {
        private static readonly SymbolDisplayFormat s_signatureDisplayFormat =
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

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return text[characterPosition] == '.';
        }

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            var span = new TextSpan(position, length:0);
            var semanticModel = await document.GetSemanticModelForSpanAsync(span, cancellationToken).ConfigureAwait(false);
            var syntaxTree = semanticModel.SyntaxTree;

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();

            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken) ||
                semanticFacts.IsPreProcessorDirectiveContext(semanticModel, position, cancellationToken))
            {
                return;
            }

            if (!syntaxTree.IsRightOfDotOrArrowOrColonColon(position, cancellationToken))
            {
                return;
            }

            var node = syntaxTree.FindTokenOnLeftOfPosition(position, cancellationToken)
                                 .GetPreviousTokenIfTouchingWord(position)
                                 .Parent;

            if (node.Kind() != SyntaxKind.ExplicitInterfaceSpecifier)
            {
                return;
            }

            // Bind the interface name which is to the left of the dot
            var name = ((ExplicitInterfaceSpecifierSyntax)node).Name;

            var symbol = semanticModel.GetSymbolInfo(name, cancellationToken).Symbol as ITypeSymbol;
            if (symbol?.TypeKind != TypeKind.Interface)
            {
                return;
            }

            var members = semanticModel.LookupSymbols(
                position: name.SpanStart,
                container: symbol)
                    .Where(s => !s.IsStatic)
                    .FilterToVisibleAndBrowsableSymbols(options.GetOption(CompletionOptions.HideAdvancedMembers, semanticModel.Language), semanticModel.Compilation);

            // We're going to create a entry for each one, including the signature
            var namePosition = name.SpanStart;

            var text = await syntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var textChangeSpan = CompletionUtilities.GetTextChangeSpan(text, position);

            foreach (var member in members)
            {
                var displayText = member.ToMinimalDisplayString(semanticModel, namePosition, s_signatureDisplayFormat);
                var insertionText = displayText;

                context.AddItem(new SymbolCompletionItem(
                    this,
                    displayText,
                    insertionText: insertionText,
                    filterSpan: textChangeSpan,
                    position: position,
                    symbols: new List<ISymbol> { member },
                    context: CSharpSyntaxContext.CreateContext(document.Project.Solution.Workspace, semanticModel, position, cancellationToken),
                    rules: ItemRules.Instance));
            }
        }
    }
}
