// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Completion.Providers;
using System;
using Microsoft.CodeAnalysis.ErrorReporting;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal sealed class CrefCompletionProvider : AbstractCrefCompletionProvider
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

        // When creating items for SpecialTypes (eg. `UInt32`), create an item
        // that uses the intrinsic type keyword and an item that uses the
        // name of the special type 
        public static readonly SymbolDisplayFormat CrefFormatForSpecialTypes =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

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
            try
            {
                var document = context.Document;
                var position = context.Position;
                var options = context.Options;
                var cancellationToken = context.CancellationToken;

                var (token, semanticModel, symbols) = await GetSymbolsAsync(document, position, options, cancellationToken).ConfigureAwait(false);

                if (symbols.Length == 0)
                {
                    return;
                }

                context.IsExclusive = true;

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var span = GetCompletionItemSpan(text, position);
                var hideAdvancedMembers = options.GetOption(CompletionOptions.HideAdvancedMembers, semanticModel.Language);
                var serializedOptions = ImmutableDictionary<string, string>.Empty.Add(HideAdvancedMembers, hideAdvancedMembers.ToString());

                var items = CreateCompletionItems(document.Project.Solution.Workspace,
                    semanticModel, symbols, token, span, position, serializedOptions);

                context.AddItems(items);
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // nop
            }
        }

        protected override async Task<(SyntaxToken, SemanticModel, ImmutableArray<ISymbol>)> GetSymbolsAsync(
            Document document, int position, OptionSet options, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (!tree.IsEntirelyWithinCrefSyntax(position, cancellationToken))
            {
                return (default, null, ImmutableArray<ISymbol>.Empty);
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true)
                            .GetPreviousTokenIfTouchingWord(position);

            // To get a Speculative SemanticModel (which is much faster), we need to 
            // walk up to the node the DocumentationTrivia is attached to.
            var parentNode = token.Parent.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>()?.ParentTrivia.Token.Parent;
            _testSpeculativeNodeCallbackOpt?.Invoke(parentNode);
            if (parentNode == null)
            {
                return (default, null, ImmutableArray<ISymbol>.Empty);
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(
                parentNode, cancellationToken).ConfigureAwait(false);

            var symbols = GetSymbols(token, semanticModel, cancellationToken)
                .FilterToVisibleAndBrowsableSymbols(
                    options.GetOption(CompletionOptions.HideAdvancedMembers, semanticModel.Language),
                    semanticModel.Compilation);

            return (token, semanticModel, symbols);
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

            if (container is INamedTypeSymbol namedTypeContainer)
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
            Workspace workspace, SemanticModel semanticModel, ImmutableArray<ISymbol> symbols, SyntaxToken token, TextSpan itemSpan, int position, ImmutableDictionary<string, string> options)
        {
            var builder = SharedPools.Default<StringBuilder>().Allocate();
            try
            {
                foreach (var symbol in symbols)
                {
                    yield return CreateItem(workspace, semanticModel, symbol, token, position, builder, options);
                    if (TryCreateSpecialTypeItem(workspace, semanticModel, symbol, token, position, builder, options, out var item))
                    {
                        yield return item;
                    }
                }
            }
            finally
            {
                SharedPools.Default<StringBuilder>().ClearAndFree(builder);
            }
        }

        private bool TryCreateSpecialTypeItem(
            Workspace workspace, SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, int position, StringBuilder builder,
            ImmutableDictionary<string, string> options, out CompletionItem item)
        {
            // If the type is a SpecialType, create an additional item using 
            // its actual name (as opposed to intrinsic type keyword)
            var typeSymbol = symbol as ITypeSymbol;
            if (typeSymbol.IsSpecialType())
            {
                item = CreateItem(workspace, semanticModel, symbol, token, position, builder, options, CrefFormatForSpecialTypes);
                return true;
            }

            item = null;
            return false;
        }

        private CompletionItem CreateItem(
            Workspace workspace, SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, int position, StringBuilder builder, ImmutableDictionary<string, string> options)
        {
            // For every symbol, we create an item that uses the regular CrefFormat,
            // which uses intrinsic type keywords
            return CreateItem(workspace, semanticModel, symbol, token, position, builder, options, CrefFormat);
        }

        private CompletionItem CreateItem(
            Workspace workspace, SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, int position, StringBuilder builder, ImmutableDictionary<string, string> options,
            SymbolDisplayFormat unqualifiedCrefFormat)
        {
            builder.Clear();
            if (symbol is INamespaceOrTypeSymbol && token.IsKind(SyntaxKind.DotToken))
            {
                // Handle qualified namespace and type names.
                builder.Append(symbol.ToDisplayString(QualifiedCrefFormat));
            }
            else
            {
                // Handle unqualified namespace and type names, or member names.

                builder.Append(symbol.ToMinimalDisplayString(semanticModel, token.SpanStart, unqualifiedCrefFormat));

                var parameters = symbol.GetParameters();
                if (!parameters.IsDefaultOrEmpty)
                {
                    // Note: we intentionally don't add the "params" modifier for any parameters.

                    builder.Append(symbol.IsIndexer() ? '[' : '(');

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(", ");
                        }

                        var parameter = parameters[i];

                        switch (parameter.RefKind)
                        {
                            case RefKind.Ref:
                                builder.Append("ref ");
                                break;
                            case RefKind.Out:
                                builder.Append("out ");
                                break;
                            case RefKind.In:
                                builder.Append("in ");
                                break;
                        }

                        builder.Append(parameter.Type.ToMinimalDisplayString(semanticModel, position));
                    }

                    builder.Append(symbol.IsIndexer() ? ']' : ')');
                }
            }

            return CreateItemFromBuilder(symbol, position, builder, options);
        }

        private CompletionItem CreateItemFromBuilder(ISymbol symbol, int position, StringBuilder builder, ImmutableDictionary<string, string> options)
        {
            var symbolText = builder.ToString();

            var insertionText = builder
                .Replace('<', '{')
                .Replace('>', '}')
                .ToString();

            return SymbolCompletionItem.CreateWithNameAndKind(
                displayText: insertionText,
                displayTextSuffix: "",
                insertionText: insertionText,
                symbols: ImmutableArray.Create(symbol),
                contextPosition: position,
                sortText: symbolText,
                properties: options,
                rules: GetRules(insertionText));
        }

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

        private const string InsertionTextProperty = "insertionText";

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            if (!selectedItem.Properties.TryGetValue(InsertionTextProperty, out var insertionText))
            {
                insertionText = selectedItem.DisplayText;
            }

            return Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, insertionText));
        }
    }
}
