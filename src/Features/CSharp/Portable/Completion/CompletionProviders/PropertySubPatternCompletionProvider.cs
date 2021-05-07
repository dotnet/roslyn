// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(PropertySubpatternCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(InternalsVisibleToCompletionProvider))]
    [Shared]
    internal class PropertySubpatternCompletionProvider : LSPCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public PropertySubpatternCompletionProvider()
        {
        }

        // Examples:
        // is { $$
        // is { Property.$$
        // is { Property.Property2.$$
        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);

            var (propertyPatternClause, memberNameAccess) = TryGetPropertyPatternClause(tree, position, cancellationToken);
            if (propertyPatternClause is null)
            {
                return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var pattern = (PatternSyntax)propertyPatternClause.Parent;
            var type = semanticModel.GetTypeInfo(pattern, cancellationToken).ConvertedType;

            if (memberNameAccess is not null)
            {
                // We have to figure out the type of the extended property ourselves, because
                // the semantic model could not provide the answer we want in incomplete syntax:
                // `c is { X. }`

                type = GetMemberAccessType(type, memberNameAccess.Expression, document, semanticModel, position);
            }

            if (type is null)
            {
                return;
            }

            // Find the members that can be tested.
            var members = GetCandidatePropertiesAndFields(document, position, semanticModel, type);

            if (propertyPatternClause is not null)
            {
                // Filter out those members that have already been typed as simple (not extended) properties
                var alreadyTestedMembers = new HashSet<string>(propertyPatternClause.Subpatterns.Select(
                    p => p.NameColon?.Name.Identifier.ValueText).Where(s => !string.IsNullOrEmpty(s)));

                members = members.Where(m => !alreadyTestedMembers.Contains(m.Name));
            }

            foreach (var member in members)
            {
                const string ColonString = ":";
                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText: member.Name.EscapeIdentifier(),
                    displayTextSuffix: ColonString,
                    insertionText: null,
                    symbols: ImmutableArray.Create(member),
                    contextPosition: context.Position,
                    rules: s_rules));
            }

            return;

            static ITypeSymbol GetMemberAccessType(ITypeSymbol type, ExpressionSyntax expression, Document document, SemanticModel semanticModel, int position)
            {
                string name;
                if (expression is MemberAccessExpressionSyntax memberAccess)
                {
                    type = GetMemberAccessType(type, memberAccess.Expression, document, semanticModel, position);
                    name = memberAccess.Name.Identifier.ValueText;
                }
                else if (expression is IdentifierNameSyntax identifier)
                {
                    name = identifier.Identifier.ValueText;
                }
                else
                {
                    return null;
                }

                return GetMemberType(type, name, document, semanticModel, position);
            }

            static ITypeSymbol GetMemberType(ITypeSymbol type, string name, Document document, SemanticModel semanticModel, int position)
            {
                var members = GetCandidatePropertiesAndFields(document, position, semanticModel, type);
                var matches = members.Where(m => m.Name == name).ToArray();
                if (matches.Length is 0 or > 1)
                {
                    return null;
                }

                type = matches[0] switch
                {
                    IPropertySymbol property => property.Type,
                    IFieldSymbol field => field.Type,
                    _ => null
                };
                return type;
            }

            static IEnumerable<ISymbol> GetCandidatePropertiesAndFields(Document document, int position, SemanticModel semanticModel, ITypeSymbol type)
            {
                IEnumerable<ISymbol> members = semanticModel.LookupSymbols(position, type);
                members = members.Where(m => m.CanBeReferencedByName &&
                    IsFieldOrReadableProperty(m) &&
                    !m.IsImplicitlyDeclared &&
                    !m.IsStatic &&
                    m.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation));
                return members;
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

        public override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
            => CompletionUtilities.IsTriggerCharacter(text, characterPosition, options) || text[characterPosition] == ' ';

        public override ImmutableHashSet<char> TriggerCharacters { get; } = CompletionUtilities.CommonTriggerCharacters.Add(' ');

        private static (PropertyPatternClauseSyntax, MemberAccessExpressionSyntax) TryGetPropertyPatternClause(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            if (tree.IsInNonUserCode(position, cancellationToken))
            {
                return default;
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.IsKind(SyntaxKind.CommaToken, SyntaxKind.OpenBraceToken))
            {
                return token.Parent.IsKind(SyntaxKind.PropertyPatternClause) ? makeResult(token) : default;
            }

            if (token.IsKind(SyntaxKind.DotToken))
            {
                var memberNameAccess = token.Parent;
                if (!memberNameAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                    || !memberNameAccess.Parent.Parent.IsKind(SyntaxKind.Subpattern)
                    || !memberNameAccess.Parent.Parent.Parent.IsKind(SyntaxKind.PropertyPatternClause))
                {
                    return default;
                }

                return ((PropertyPatternClauseSyntax)memberNameAccess.Parent.Parent.Parent, (MemberAccessExpressionSyntax)memberNameAccess);
            }

            return default;

            (PropertyPatternClauseSyntax, MemberAccessExpressionSyntax) makeResult(SyntaxToken token)
            {
                if (token.Parent.Parent is PatternSyntax)
                {
                    return ((PropertyPatternClauseSyntax)token.Parent, null);
                }

                return default;
            }
        }
    }
}
