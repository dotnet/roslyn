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

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class CrefCompletionProvider : CompletionListProvider
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

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;

            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (!tree.IsEntirelyWithinCrefSyntax(position, cancellationToken))
            {
                return;
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);
            if (token.Kind() == SyntaxKind.None)
            {
                return;
            }

            var result = SpecializedCollections.EmptyEnumerable<ISymbol>();

            // To get a Speculative SemanticModel (which is much faster), we need to 
            // walk up to the node the DocumentationTrivia is attached to.
            var parentNode = token.GetAncestor<DocumentationCommentTriviaSyntax>().ParentTrivia.Token.Parent;
            var semanticModel = await document.GetSemanticModelForNodeAsync(parentNode, cancellationToken).ConfigureAwait(false);

            if (IsCrefStartContext(token))
            {
                result = semanticModel.LookupSymbols(token.SpanStart)
                                        .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);

                result = result.Concat(GetOperatorsAndIndexers(token, semanticModel, cancellationToken));
            }
            else if (IsCrefParameterListContext(token))
            {
                result = semanticModel.LookupNamespacesAndTypes(token.SpanStart)
                                        .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);
            }
            else if (IsCrefQualifiedNameContext(token))
            {
                // cref "a.|"
                var parent = token.Parent as QualifiedCrefSyntax;
                var leftType = semanticModel.GetTypeInfo(parent.Container, cancellationToken).Type;
                var leftSymbol = semanticModel.GetSymbolInfo(parent.Container, cancellationToken).Symbol;

                var container = leftSymbol ?? leftType;

                result = semanticModel.LookupSymbols(token.SpanStart, container: (INamespaceOrTypeSymbol)container)
                                        .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);

                if (container is INamedTypeSymbol)
                {
                    result = result.Concat(((INamedTypeSymbol)container).InstanceConstructors);
                }
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var filterSpan = GetTextChangeSpan(text, position);

            var items = CreateItems(document.Project.Solution.Workspace, semanticModel, filterSpan, result, token, cancellationToken);

            if (items.Any())
            {
                context.MakeExclusive(true);
                context.AddItems(items);
            }
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

        // LookupSymbols doesn't return indexers or operators because they can't be referred to by name, so we'll have to try to 
        // find the innermost type declaration and return its operators and indexers
        private IEnumerable<ISymbol> GetOperatorsAndIndexers(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var typeDeclaration = token.GetAncestor<TypeDeclarationSyntax>();

            var result = new List<ISymbol>();

            if (typeDeclaration != null)
            {
                var type = semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken);

                result.AddRange(type.GetMembers().OfType<IPropertySymbol>().Where(p => p.IsIndexer));
                result.AddRange(type.GetAccessibleMembersInThisAndBaseTypes<IMethodSymbol>(type)
                                    .Where(m => m.MethodKind == MethodKind.UserDefinedOperator));
            }

            return result;
        }

        private IEnumerable<CompletionItem> CreateItems(
            Workspace workspace, SemanticModel semanticModel, TextSpan filterSpan, IEnumerable<ISymbol> symbols, SyntaxToken token, CancellationToken cancellationToken)
        {
            var builder = SharedPools.Default<StringBuilder>().AllocateAndClear();
            try
            {
                foreach (var symbol in symbols)
                {
                    builder.Clear();
                    yield return CreateItem(workspace, semanticModel, filterSpan, symbol, token, builder, cancellationToken);
                }
            }
            finally
            {
                SharedPools.Default<StringBuilder>().ClearAndFree(builder);
            }
        }

        private CompletionItem CreateItem(
            Workspace workspace, SemanticModel semanticModel, TextSpan filterSpan, ISymbol symbol, SyntaxToken token, StringBuilder builder, CancellationToken cancellationToken)
        {
            int tokenPosition = token.SpanStart;

            if (symbol is INamespaceOrTypeSymbol && token.IsKind(SyntaxKind.DotToken))
            {
                // Handle qualified namespace and type names.

                builder.Append(symbol.ToDisplayString(QualifiedCrefFormat));
            }
            else
            {
                // Handle unqualified namespace and type names, or member names.

                builder.Append(symbol.ToMinimalDisplayString(semanticModel, tokenPosition, CrefFormat));

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

                        builder.Append(parameter.Type.ToMinimalDisplayString(semanticModel, tokenPosition));
                    }

                    builder.Append(symbol.IsIndexer() ? ']' : ')');
                }
            }

            var symbolText = builder.ToString();

            var insertionText = builder
                .Replace('<', '{')
                .Replace('>', '}')
                .ToString();

            return new Item(
                completionProvider: this,
                displayText: insertionText,
                insertionText: insertionText,
                textSpan: filterSpan,
                descriptionFactory: CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, tokenPosition, symbol),
                glyph: symbol.GetGlyph(),
                sortText: symbolText);
        }

        private TextSpan GetTextChangeSpan(SourceText text, int position)
        {
            return CommonCompletionUtilities.GetTextChangeSpan(
                text,
                position,
                (ch) => CompletionUtilities.IsTextChangeSpanStartCharacter(ch) || ch == '{',
                (ch) => CompletionUtilities.IsWordCharacter(ch) || ch == '{' || ch == '}');
        }

        private string CreateParameters(IEnumerable<ITypeSymbol> arguments, SemanticModel semanticModel, int position)
        {
            return string.Join(", ", arguments.Select(t => t.ToMinimalDisplayString(semanticModel, position)));
        }
    }
}
