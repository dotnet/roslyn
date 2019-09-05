// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal class PropertySubpatternCompletionProvider : CommonCompletionProvider
    {
        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            var token = TryGetOpenBraceOrCommaInPropertyPatternClause(tree, position, cancellationToken);
            if (token == default || !(token.Parent.Parent is PatternSyntax))
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelForSpanAsync(new TextSpan(position, length: 0), cancellationToken).ConfigureAwait(false);
            var pattern = (PatternSyntax)token.Parent.Parent;
            var type = semanticModel.GetTypeInfo(pattern, cancellationToken).ConvertedType;
            if (type == null)
            {
                return;
            }

            // Find the members that can be tested.
            IEnumerable<ISymbol> members = semanticModel.LookupSymbols(position, type);
            members = members.Where(m => m.CanBeReferencedByName &&
                IsFieldOrReadableProperty(m) &&
                !m.IsImplicitlyDeclared &&
                !m.IsStatic);

            // Filter out those members that have already been typed
            var propertyPatternClause = (PropertyPatternClauseSyntax)token.Parent;

            // List the members that are already tested in this property sub-pattern
            var alreadyTestedMembers = new HashSet<string>(propertyPatternClause.Subpatterns.Select(
                p => p.NameColon?.Name.Identifier.ValueText).Where(s => !string.IsNullOrEmpty(s)));

            var untestedMembers = members.Where(m => !alreadyTestedMembers.Contains(m.Name) &&
                m.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation));

            foreach (var untestedMember in untestedMembers)
            {
                const string ColonString = ":";
                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText: untestedMember.Name.EscapeIdentifier(),
                    displayTextSuffix: ColonString,
                    insertionText: null,
                    symbols: ImmutableArray.Create(untestedMember),
                    contextPosition: token.GetLocation().SourceSpan.Start,
                    rules: s_rules));
            }
        }

        private static bool IsFieldOrReadableProperty(ISymbol symbol)
        {
            if (symbol.IsKind(SymbolKind.Field))
            {
                return true;
            }

            if (symbol.IsKind(SymbolKind.Property) && !((IPropertySymbol)symbol).IsWriteOnly)
            {
                return true;
            }

            return false;
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options) || text[characterPosition] == ' ';

        private static SyntaxToken TryGetOpenBraceOrCommaInPropertyPatternClause(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            if (tree.IsInNonUserCode(position, cancellationToken))
            {
                return default;
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (!token.IsKind(SyntaxKind.CommaToken, SyntaxKind.OpenBraceToken))
            {
                return default;
            }

            return token.Parent.IsKind(SyntaxKind.PropertyPatternClause) ? token : default;
        }
    }
}
