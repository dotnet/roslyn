// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion.Providers;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class CrefCompletionProvider : CommonCompletionProvider
    {
        public static readonly SymbolDisplayFormat QualifiedCrefFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.None,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        public static readonly SymbolDisplayFormat CrefFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        private readonly Action<SyntaxNode> _testSpeculativeNodeCallbackOpt;

        public CrefCompletionProvider(Action<SyntaxNode> testSpeculativeNodeCallbackOpt = null)
        {
            _testSpeculativeNodeCallbackOpt = testSpeculativeNodeCallbackOpt;
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (!tree.IsEntirelyWithinCrefSyntax(position, cancellationToken))
            {
                return;
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true)
                            .GetPreviousTokenIfTouchingWord(position);

            // To get a Speculative SemanticModel (which is much faster), we need to 
            // walk up to the node the DocumentationTrivia is attached to.
            var parentNode = token.Parent.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>()?.ParentTrivia.Token.Parent;
            _testSpeculativeNodeCallbackOpt?.Invoke(parentNode);
            if (parentNode == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(
                parentNode, cancellationToken).ConfigureAwait(false);

            var symbols = GetSymbols(token, semanticModel, cancellationToken);

            symbols = symbols.FilterToVisibleAndBrowsableSymbols(options.GetOption(CompletionOptions.HideAdvancedMembers, semanticModel.Language), semanticModel.Compilation);

            if (!symbols.Any())
            {
                return;
            }

            context.IsExclusive = true;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var span = GetCompletionItemSpan(text, position);

            var items = CreateCompletionItems(document.Project.Solution.Workspace, semanticModel, symbols, token, span);
            context.AddItems(items);
        }

        private static bool IsCrefStartContext(SyntaxToken token)
        {
            // cases:
            //   <see cref="|
            //   <see cref='|

            return token.IsKind(SyntaxKind.DoubleQuoteToken, SyntaxKind.SingleQuoteToken)
                && token.Parent.IsKind(SyntaxKind.XmlCrefAttribute);
        }

        private static bool IsCrefParameterListContext(SyntaxToken token)
        {
            // cases:
            //   <see cref="M(|
            //   <see cref="M(x, |
            //   <see cref="M(x, ref |
            //   <see cref="M(x, out |
            //   <see cref="M[|
            //   <see cref="M[x, |
            //   <see cref="M[x, ref |
            //   <see cref="M[x, out |

            if (!token.Parent.IsKind(SyntaxKind.CrefParameterList, SyntaxKind.CrefBracketedParameterList))
            {
                return false;
            }

            if (token.IsKind(SyntaxKind.OpenParenToken) &&
                token.Parent.IsKind(SyntaxKind.CrefParameterList))
            {
                return true;
            }

            if (token.IsKind(SyntaxKind.OpenBracketToken) &&
                token.Parent.IsKind(SyntaxKind.CrefBracketedParameterList))
            {
                return true;
            }

            return token.IsKind(SyntaxKind.CommaToken, SyntaxKind.RefKeyword, SyntaxKind.OutKeyword);
        }

        private static bool IsCrefQualifiedNameContext(SyntaxToken token)
        {
            // cases:
            //   <see cref="x.|

            return token.IsKind(SyntaxKind.DotToken)
                && token.Parent.IsKind(SyntaxKind.QualifiedCref);
        }

        private static ImmutableArray<ISymbol> GetSymbols(
            SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (IsCrefStartContext(token))
            {
                return GetUnqualifiedSymbols(token, semanticModel, cancellationToken);
            }
            else if (IsCrefParameterListContext(token))
            {
                return semanticModel.LookupNamespacesAndTypes(token.SpanStart);
            }
            else if (IsCrefQualifiedNameContext(token))
            {
                return GetQualifiedSymbols((QualifiedCrefSyntax)token.Parent, token, semanticModel, cancellationToken);
            }

            return ImmutableArray<ISymbol>.Empty;
        }

        private static ImmutableArray<ISymbol> GetUnqualifiedSymbols(
            SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var result = ArrayBuilder<ISymbol>.GetInstance();
            result.AddRange(semanticModel.LookupSymbols(token.SpanStart));

            // LookupSymbols doesn't return indexers or operators because they can't be referred to by name.
            // So, try to find the innermost type declaration and return its operators and indexers
            var typeDeclaration = token.Parent?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeDeclaration != null)
            {
                var type = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);
                if (type != null)
                {
                    foreach (var baseType in type.GetBaseTypesAndThis())
                    {
                        foreach (var member in baseType.GetMembers())
                        {
                            if ((member.IsIndexer() || member.IsUserDefinedOperator()) &&
                                member.IsAccessibleWithin(type))
                            {
                                result.Add(member);
                            }
                        }
                    }
                }
            }

            return result.ToImmutableAndFree();
        }

        private static ImmutableArray<ISymbol> GetQualifiedSymbols(
            QualifiedCrefSyntax parent, SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var leftType = semanticModel.GetTypeInfo(parent.Container, cancellationToken).Type;
            var leftSymbol = semanticModel.GetSymbolInfo(parent.Container, cancellationToken).Symbol;

            var container = (leftSymbol ?? leftType) as INamespaceOrTypeSymbol;

            var result = ArrayBuilder<ISymbol>.GetInstance();
            result.AddRange(semanticModel.LookupSymbols(token.SpanStart, container));

            var namedTypeContainer = container as INamedTypeSymbol;
            if (namedTypeContainer != null)
            {
                result.AddRange(namedTypeContainer.InstanceConstructors);
            }

            return result.ToImmutableAndFree();
        }

        private static TextSpan GetCompletionItemSpan(SourceText text, int position)
        {
            return CommonCompletionUtilities.GetWordSpan(
                text,
                position,
                ch => CompletionUtilities.IsCompletionItemStartCharacter(ch) || ch == '{',
                ch => CompletionUtilities.IsWordCharacter(ch) || ch == '{' || ch == '}');
        }

        private IEnumerable<CompletionItem> CreateCompletionItems(
            Workspace workspace, SemanticModel semanticModel, IEnumerable<ISymbol> symbols, SyntaxToken token, TextSpan itemSpan)
        {
            var builder = SharedPools.Default<StringBuilder>().Allocate();
            try
            {
                foreach (var symbol in symbols)
                {
                    builder.Clear();
                    yield return CreateItem(workspace, semanticModel, symbol, token, builder);
                }
            }
            finally
            {
                SharedPools.Default<StringBuilder>().ClearAndFree(builder);
            }
        }

        private CompletionItem CreateItem(
            Workspace workspace, SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, StringBuilder builder)
        {
            int position = token.SpanStart;

            if (symbol is INamespaceOrTypeSymbol && token.IsKind(SyntaxKind.DotToken))
            {
                // Handle qualified namespace and type names.

                builder.Append(symbol.ToDisplayString(QualifiedCrefFormat));
            }
            else
            {
                // Handle unqualified namespace and type names, or member names.

                builder.Append(symbol.ToMinimalDisplayString(semanticModel, position, CrefFormat));

                var parameters = symbol.GetParameters();
                if (!parameters.IsDefaultOrEmpty)
                {
                    // Note: we intentionally don't add the "params" modifier for any parameters.

                    builder.Append(symbol.IsIndexer() ? '[' : '(');

                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(", ");
                        }

                        var parameter = parameters[i];

                        if (parameter.RefKind == RefKind.Out)
                        {
                            builder.Append("out ");
                        }
                        else if (parameter.RefKind == RefKind.Ref)
                        {
                            builder.Append("ref ");
                        }

                        builder.Append(parameter.Type.ToMinimalDisplayString(semanticModel, position));
                    }

                    builder.Append(symbol.IsIndexer() ? ']' : ')');
                }
            }

            var symbolText = builder.ToString();

            var insertionText = builder
                .Replace('<', '{')
                .Replace('>', '}')
                .ToString();

            return SymbolCompletionItem.Create(
                displayText: insertionText,
                insertionText: insertionText,
                symbol: symbol,
                contextPosition: position,
                sortText: symbolText,
                rules: GetRules(insertionText));
        }

        protected override Task<CompletionDescription> GetDescriptionWorkerAsync(Document document, CompletionItem item, CancellationToken cancellationToken)
            => SymbolCompletionItem.GetDescriptionAsync(item, document, cancellationToken);

        private static readonly CharacterSetModificationRule s_WithoutOpenBrace = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '{');
        private static readonly CharacterSetModificationRule s_WithoutOpenParen = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '(');

        private CompletionItemRules GetRules(string displayText)
        {
            var commitRules = ImmutableArray<CharacterSetModificationRule>.Empty;

            if (displayText.Contains("{"))
            {
                commitRules = commitRules.Add(s_WithoutOpenBrace);
            }

            if (displayText.Contains("("))
            {
                commitRules = commitRules.Add(s_WithoutOpenParen);
            }

            if (commitRules.IsEmpty)
            {
                return CompletionItemRules.Default;
            }
            else
            {
                return CompletionItemRules.Default.WithCommitCharacterRules(commitRules);
            }
        }


        private static readonly string InsertionTextProperty = "insertionText";

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            string insertionText;
            if (!selectedItem.Properties.TryGetValue(InsertionTextProperty, out insertionText))
            {
                insertionText = selectedItem.DisplayText;
            }

            return Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, insertionText));
        }
    }
}
