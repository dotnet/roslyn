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
    internal class PropertySubPatternCompletionProvider : CommonCompletionProvider
    {
        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var workspace = document.Project.Solution.Workspace;
            var semanticModel = await document.GetSemanticModelForSpanAsync(new TextSpan(position, length: 0), cancellationToken).ConfigureAwait(false);
            var (type, location) = GetPatternType(document, semanticModel, position, cancellationToken);

            if (type == null)
            {
                return;
            }

            // Find the members that can be tested.
            IEnumerable<ISymbol> members = semanticModel.LookupSymbols(position, type);
            members = members.Where(m => m.CanBeReferencedByName &&
                (m.IsKind(SymbolKind.Property) || m.IsKind(SymbolKind.Field)) &&
                !m.IsImplicitlyDeclared);

            // Filter out those members that have already been typed
            var alreadyTestedMembers = GetTestedMembers(semanticModel.SyntaxTree, position, cancellationToken);
            var untestedMembers = members.Where(m => !alreadyTestedMembers.Contains(m.Name));

            untestedMembers = untestedMembers.Where(m => m.IsEditorBrowsable(document.ShouldHideAdvancedMembers(), semanticModel.Compilation));

            foreach (var untestedProperty in untestedMembers)
            {
                context.AddItem(SymbolCompletionItem.CreateWithSymbolId(
                    displayText: untestedProperty.Name,
                    insertionText: null,
                    symbols: ImmutableArray.Create(untestedProperty),
                    contextPosition: location.SourceSpan.Start,
                    rules: s_rules));
            }
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(enterKeyRule: EnterKeyRule.Never);

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options) || text[characterPosition] == ' ';
        }

        protected (ITypeSymbol type, Location location) GetPatternType(
            Document document, SemanticModel semanticModel, int position, CancellationToken cancellationToken)
        {
            var tree = semanticModel.SyntaxTree;
            if (tree.IsInNonUserCode(position, cancellationToken))
            {
                return default;
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);

            if (token.Kind() != SyntaxKind.CommaToken && token.Kind() != SyntaxKind.OpenBraceToken)
            {
                return default;
            }

            if (token.Parent?.IsKind(SyntaxKind.PropertySubpattern) == false ||
                token.Parent.Parent?.IsKind(SyntaxKind.PropertyPattern) == false)
            {
                return default;
            }

            // is Goo { $$
            // is Goo { P1: 0, $$
            var propertyPattern = (PropertyPatternSyntax)token.Parent.Parent;
            var patternType = propertyPattern.Type;
            if (patternType != null)
            {
                var typeSymbol = (ITypeSymbol)semanticModel.GetSymbolInfo(patternType, cancellationToken).Symbol;
                return (typeSymbol, token.GetLocation());
            }

            // e is { $$
            // e is { P1: 0, $$
            if (propertyPattern.IsParentKind(SyntaxKind.IsPatternExpression))
            {
                var isPattern = (IsPatternExpressionSyntax)propertyPattern.Parent;
                var typeSymbol = semanticModel.GetTypeInfo(isPattern.Expression, cancellationToken).Type;
                if (typeSymbol != null)
                {
                    return (typeSymbol, token.GetLocation());
                }
            }

            // switch (e) { case { $$
            // switch (e) { case { P1: 0, $$
            if (propertyPattern.IsParentKind(SyntaxKind.CasePatternSwitchLabel))
            {
                var casePattern = (CasePatternSwitchLabelSyntax)propertyPattern.Parent;
                var switchStatment = casePattern.Parent?.Parent as SwitchStatementSyntax;
                if (switchStatment != null && switchStatment.Expression != null)
                {
                    var typeSymbol = semanticModel.GetTypeInfo(switchStatment.Expression).Type;
                    if (typeSymbol != null)
                    {
                        return (typeSymbol, token.GetLocation());
                    }
                }
            }

            // e is { P1: { $$
            // e is { P1: { P2: 2, $$
            if (propertyPattern.IsParentKind(SyntaxKind.SubpatternElement))
            {
                var containingProperty = ((SubpatternElementSyntax)propertyPattern.Parent).NameColon?.Name;
                if (containingProperty != null)
                {
                    var symbol = semanticModel.GetSymbolInfo(containingProperty).Symbol;

                    switch (symbol)
                    {
                        case null:
                            break;
                        case IPropertySymbol propertySymbol:
                            return (propertySymbol.Type, token.GetLocation());
                        case IFieldSymbol fieldSymbol:
                            return (fieldSymbol.Type, token.GetLocation());
                    }
                }
            }

            // PROTOTYPE(NullableReferenceTypes): patterns in switch expression

            return default;
        }

        // List the members that are already tested in this property sub-pattern
        protected HashSet<string> GetTestedMembers(SyntaxTree tree, int position, CancellationToken cancellationToken)
        {
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken)
                .GetPreviousTokenIfTouchingWord(position);

            // We should have gotten back a { or ,
            if (token.Kind() == SyntaxKind.CommaToken || token.Kind() == SyntaxKind.OpenBraceToken)
            {
                if (token.Parent != null)
                {
                    if (token.Parent is PropertySubpatternSyntax subpattern)
                    {
                        return new HashSet<string>(subpattern.SubPatterns.Select(
                            p => p.NameColon?.Name?.Identifier.ValueText).Where(s => !string.IsNullOrEmpty(s)));
                    }
                }
            }

            return new HashSet<string>();
        }
    }
}
