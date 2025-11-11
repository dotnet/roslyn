// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers;

[ExportCompletionProvider(nameof(CrefCompletionProvider), LanguageNames.CSharp), Shared]
[ExtensionOrder(After = nameof(EnumAndCompletionListTagCompletionProvider))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CrefCompletionProvider(
    KeywordCompletionProvider keywordCompletionProvider) : AbstractCrefCompletionProvider
{
    private static readonly SymbolDisplayFormat QualifiedCrefFormat =
        new(globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            parameterOptions: SymbolDisplayParameterOptions.None,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.ExpandValueTuple);

    private static readonly SymbolDisplayFormat CrefFormat =
        QualifiedCrefFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static readonly SymbolDisplayFormat MinimalParameterTypeFormat =
        SymbolDisplayFormat.MinimallyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.ExpandValueTuple);

    private readonly KeywordCompletionProvider _keywordCompletionProvider = keywordCompletionProvider;
    private Action<SyntaxNode?>? _testSpeculativeNodeCallback;

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
            if (symbols.IsDefaultOrEmpty)
                return;

            context.IsExclusive = true;

            Contract.ThrowIfNull(semanticModel);

            var serializedOptions = ImmutableArray.Create(KeyValuePair.Create(HideAdvancedMembers, options.MemberDisplayOptions.HideAdvancedMembers.ToString()));

            context.AddItems(CreateCompletionItems(semanticModel, symbols, token, position, serializedOptions));

            // Because we took over completion entirely as an exclusive provider, we have to ensure that appropriate
            // keywords are provided ourselves.
            await _keywordCompletionProvider.ProvideCompletionsAsync(context).ConfigureAwait(false);
        }
        catch (Exception e) when (FatalError.ReportAndCatchUnlessCanceled(e, ErrorSeverity.General))
        {
        }
    }

    protected override async Task<(SyntaxToken, SemanticModel?, ImmutableArray<ISymbol>)> GetSymbolsAsync(
        Document document, int position, CompletionOptions options, CancellationToken cancellationToken)
    {
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        if (!tree.IsEntirelyWithinCrefSyntax(position, cancellationToken))
            return default;

        var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken, includeDocumentationComments: true)
                        .GetPreviousTokenIfTouchingWord(position);

        // To get a Speculative SemanticModel (which is much faster), we need to 
        // walk up to the node the DocumentationTrivia is attached to.
        var parentNode = token.Parent?.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>()?.ParentTrivia.Token.Parent;
        _testSpeculativeNodeCallback?.Invoke(parentNode);
        if (parentNode == null)
            return default;

        var semanticModel = await document.ReuseExistingSpeculativeModelAsync(
            parentNode, cancellationToken).ConfigureAwait(false);

        var symbols = GetSymbols(token, semanticModel, cancellationToken)
            .FilterToVisibleAndBrowsableSymbols(options.MemberDisplayOptions.HideAdvancedMembers, semanticModel.Compilation, inclusionFilter: static s => true);

        return (token, semanticModel, symbols);
    }

    private static bool IsCrefStartContext(SyntaxToken token)
    {
        // cases:
        //   <see cref="|
        //   <see cref='|

        return token.Kind() is SyntaxKind.DoubleQuoteToken or SyntaxKind.SingleQuoteToken &&
               token.Parent.IsKind(SyntaxKind.XmlCrefAttribute);
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

        if (token.Parent?.Kind() is not (SyntaxKind.CrefParameterList or SyntaxKind.CrefBracketedParameterList))
            return false;

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

        return token is (kind: SyntaxKind.CommaToken or SyntaxKind.RefKeyword or SyntaxKind.OutKeyword);
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
            return GetUnqualifiedSymbols(token, semanticModel, cancellationToken);

        if (IsCrefParameterListContext(token))
            return semanticModel.LookupNamespacesAndTypes(token.SpanStart);

        if (IsCrefQualifiedNameContext(token))
            return GetQualifiedSymbols((QualifiedCrefSyntax)token.Parent!, token, semanticModel, cancellationToken);

        return [];
    }

    private static ImmutableArray<ISymbol> GetUnqualifiedSymbols(
        SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);
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

        return result.ToImmutableAndClear();
    }

    private static ImmutableArray<ISymbol> GetQualifiedSymbols(
        QualifiedCrefSyntax parent, SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var leftType = semanticModel.GetTypeInfo(parent.Container, cancellationToken).Type;
        var leftSymbol = semanticModel.GetSymbolInfo(parent.Container, cancellationToken).Symbol;

        var container = (leftSymbol ?? leftType) as INamespaceOrTypeSymbol;

        using var _ = ArrayBuilder<ISymbol>.GetInstance(out var result);
        result.AddRange(semanticModel.LookupSymbols(token.SpanStart, container));

        if (container is INamedTypeSymbol namedTypeContainer)
            result.AddRange(namedTypeContainer.InstanceConstructors);

        return result.ToImmutableAndClear();
    }

    private static IEnumerable<CompletionItem> CreateCompletionItems(
        SemanticModel semanticModel, ImmutableArray<ISymbol> symbols, SyntaxToken token, int position, ImmutableArray<KeyValuePair<string, string>> options)
    {
        using var _ = SharedPools.Default<StringBuilder>().GetPooledObject(out var builder);

        foreach (var group in symbols.GroupBy(s => s.Name))
        {
            var groupCount = group.Count();
            foreach (var symbol in group.OrderBy(s => s.GetArity()))
            {
                // Include the arity in the sort text so that we show types/methods from least arity to most arity.
                var sortText = $"{symbol.Name}`{symbol.GetArity():000}";

                // For every symbol, we create an item that uses the regular CrefFormat,
                // which uses intrinsic type keywords
                yield return CreateItem(semanticModel, symbol, groupCount, token, position, builder, sortText, options, CrefFormat);
                if (TryCreateSpecialTypeItem(semanticModel, symbol, token, position, builder, options, out var item))
                    yield return item;
            }
        }
    }

    private static bool TryCreateSpecialTypeItem(
        SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, int position, StringBuilder builder,
        ImmutableArray<KeyValuePair<string, string>> options, [NotNullWhen(true)] out CompletionItem? item)
    {
        // If the type is a SpecialType, create an additional item using 
        // its actual name (as opposed to intrinsic type keyword)
        var typeSymbol = symbol as ITypeSymbol;
        if (typeSymbol.IsSpecialType())
        {
            item = CreateItem(semanticModel, symbol, groupCount: 1, token, position, builder, builder.ToString(), options, QualifiedCrefFormat);
            return true;
        }

        item = null;
        return false;
    }

    private static CompletionItem CreateItem(
        SemanticModel semanticModel,
        ISymbol symbol,
        int groupCount,
        SyntaxToken token,
        int position,
        StringBuilder builder,
        string sortText,
        ImmutableArray<KeyValuePair<string, string>> options,
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

            // if this has parameters, then add them here.  Otherwise, if this is a method without parameters, but
            // there are overloads of it, then also add the parameters to disambiguate.
            if (parameters.Length > 0 ||
                (symbol is IMethodSymbol && groupCount >= 2))
            {
                // Note: we intentionally don't add the "params" modifier for any parameters.

                builder.Append(symbol.IsIndexer() ? '[' : '(');
                builder.AppendJoinedValues(", ", parameters,
                    (p, builder) =>
                    {
                        builder.Append(p.RefKind switch
                        {
                            RefKind.Ref => "ref ",
                            RefKind.Out => "out ",
                            RefKind.In => "in ",
                            RefKind.RefReadOnlyParameter => "ref readonly ",
                            _ => "",
                        });
                        builder.Append(p.Type.ToMinimalDisplayString(semanticModel, position, MinimalParameterTypeFormat));
                    });
                builder.Append(symbol.IsIndexer() ? ']' : ')');
            }
        }

        return CreateItemFromBuilder(symbol, position, builder, sortText, options);
    }

    private static CompletionItem CreateItemFromBuilder(
        ISymbol symbol, int position, StringBuilder builder, string sortText, ImmutableArray<KeyValuePair<string, string>> options)
    {
        var insertionText = builder
            .Replace('<', '{')
            .Replace('>', '}')
            .ToString();

        return SymbolCompletionItem.CreateWithNameAndKind(
            displayText: insertionText,
            displayTextSuffix: "",
            insertionText: insertionText,
            symbols: [symbol],
            contextPosition: position,
            sortText: sortText,
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

    internal readonly struct TestAccessor(CrefCompletionProvider crefCompletionProvider)
    {
        private readonly CrefCompletionProvider _crefCompletionProvider = crefCompletionProvider;

        public void SetSpeculativeNodeCallback(Action<SyntaxNode?> value)
            => _crefCompletionProvider._testSpeculativeNodeCallback = value;
    }
}
