// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(CrefCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(EnumAndCompletionListTagCompletionProvider))]
    [Shared]
    internal sealed class CrefCompletionProvider : AbstractCrefCompletionProvider
    {
        public static readonly SymbolDisplayFormat QualifiedCrefFormat =
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.None,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        public static readonly SymbolDisplayFormat CrefFormat =
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
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
            new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions: SymbolDisplayParameterOptions.None,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers);

        private Action<SyntaxNode?>? _testSpeculativeNodeCallback;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CrefCompletionProvider()
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
                var options = context.CompletionOptions;
                var cancellationToken = context.CancellationToken;

                var (token, semanticModel, symbols) = await GetSymbolsAsync(document, position, options, cancellationToken).ConfigureAwait(false);

                if (symbols.Length == 0)
                {
                    return;
                }

                Contract.ThrowIfNull(semanticModel);

                context.IsExclusive = true;

                var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
                var span = GetCompletionItemSpan(text, position);
                var serializedOptions = ImmutableDictionary<string, string>.Empty.Add(HideAdvancedMembers, options.HideAdvancedMembers.ToString());

                var items = CreateCompletionItems(semanticModel, symbols, token, position, serializedOptions);

                context.AddItems(items);
            }
            catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
            {
                // nop
            }
        }

        protected override async Task<(SyntaxToken, SemanticModel?, ImmutableArray<ISymbol>)> GetSymbolsAsync(
            Document document, int position, CompletionOptions options, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (!tree.IsEntirelyWithinCrefSyntax(position, cancellationToken))
            {
                return (default, null, ImmutableArray<ISymbol>.Empty);
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true)
                            .GetPreviousTokenIfTouchingWord(position);

            // To get a Speculative SemanticModel (which is much faster), we need to 
            // walk up to the node the DocumentationTrivia is attached to.
            var parentNode = token.Parent?.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>()?.ParentTrivia.Token.Parent;
            _testSpeculativeNodeCallback?.Invoke(parentNode);
            if (parentNode == null)
            {
                return (default, null, ImmutableArray<ISymbol>.Empty);
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(
                parentNode, cancellationToken).ConfigureAwait(false);

            var symbols = GetSymbols(token, semanticModel, cancellationToken)
                .FilterToVisibleAndBrowsableSymbols(options.HideAdvancedMembers, semanticModel.Compilation);

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
                return GetQualifiedSymbols((QualifiedCrefSyntax)token.Parent!, token, semanticModel, cancellationToken);
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

        private static IEnumerable<CompletionItem> CreateCompletionItems(
            SemanticModel semanticModel, ImmutableArray<ISymbol> symbols, SyntaxToken token, int position, ImmutableDictionary<string, string> options)
        {
            var builder = SharedPools.Default<StringBuilder>().Allocate();
            try
            {
                foreach (var symbol in symbols)
                {
                    yield return CreateItem(semanticModel, symbol, token, position, builder, options);
                    if (TryCreateSpecialTypeItem(semanticModel, symbol, token, position, builder, options, out var item))
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

        private static bool TryCreateSpecialTypeItem(
            SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, int position, StringBuilder builder,
            ImmutableDictionary<string, string> options, [NotNullWhen(true)] out CompletionItem? item)
        {
            // If the type is a SpecialType, create an additional item using 
            // its actual name (as opposed to intrinsic type keyword)
            var typeSymbol = symbol as ITypeSymbol;
            if (typeSymbol.IsSpecialType())
            {
                item = CreateItem(semanticModel, symbol, token, position, builder, options, CrefFormatForSpecialTypes);
                return true;
            }

            item = null;
            return false;
        }

        private static CompletionItem CreateItem(
            SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, int position, StringBuilder builder, ImmutableDictionary<string, string> options)
        {
            // For every symbol, we create an item that uses the regular CrefFormat,
            // which uses intrinsic type keywords
            return CreateItem(semanticModel, symbol, token, position, builder, options, CrefFormat);
        }

        private static CompletionItem CreateItem(
            SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, int position, StringBuilder builder, ImmutableDictionary<string, string> options,
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

        private static CompletionItem CreateItemFromBuilder(ISymbol symbol, int position, StringBuilder builder, ImmutableDictionary<string, string> options)
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
                filterText: insertionText,
                properties: options,
                rules: GetRules(insertionText));
        }

        private static readonly CharacterSetModificationRule s_WithoutOpenBrace = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '{');
        private static readonly CharacterSetModificationRule s_WithoutOpenParen = CharacterSetModificationRule.Create(CharacterSetModificationKind.Remove, '(');

        private static CompletionItemRules GetRules(string displayText)
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

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            if (!SymbolCompletionItem.TryGetInsertionText(selectedItem, out var insertionText))
            {
                insertionText = selectedItem.DisplayText;
            }

            return Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, insertionText));
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor
        {
            private readonly CrefCompletionProvider _crefCompletionProvider;

            public TestAccessor(CrefCompletionProvider crefCompletionProvider)
                => _crefCompletionProvider = crefCompletionProvider;

            public void SetSpeculativeNodeCallback(Action<SyntaxNode?> value)
                => _crefCompletionProvider._testSpeculativeNodeCallback = value;
        }
    }
}
