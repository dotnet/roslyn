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
            if (parentNode == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(parentNode, cancellationToken).ConfigureAwait(false);

            var symbols = GetSymbols(token, semanticModel, cancellationToken);

            symbols = symbols.FilterToVisibleAndBrowsableSymbols(options.GetOption(CompletionOptions.HideAdvancedMembers, semanticModel.Language), semanticModel.Compilation);

            if (!symbols.Any())
            {
                return;
            }

            context.MakeExclusive(true);

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var filterSpan = GetTextChangeSpan(text, position);

            var items = CreateCompletionItems(document.Project.Solution.Workspace, semanticModel, symbols, token, filterSpan);
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

        private static IEnumerable<ISymbol> GetSymbols(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
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

            return SpecializedCollections.EmptyEnumerable<ISymbol>();
        }

        private static IEnumerable<ISymbol> GetUnqualifiedSymbols(SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (var symbol in semanticModel.LookupSymbols(token.SpanStart))
            {
                yield return symbol;
            }

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
                                yield return member;
                            }
                        }
                    }
                }
            }
        }

        private static IEnumerable<ISymbol> GetQualifiedSymbols(QualifiedCrefSyntax parent, SyntaxToken token, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var leftType = semanticModel.GetTypeInfo(parent.Container, cancellationToken).Type;
            var leftSymbol = semanticModel.GetSymbolInfo(parent.Container, cancellationToken).Symbol;

            var container = (leftSymbol ?? leftType) as INamespaceOrTypeSymbol;

            foreach (var symbol in semanticModel.LookupSymbols(token.SpanStart, container))
            {
                yield return symbol;
            }

            var namedTypeContainer = container as INamedTypeSymbol;
            if (namedTypeContainer != null)
            {
                foreach (var instanceConstructor in namedTypeContainer.InstanceConstructors)
                {
                    yield return instanceConstructor;
                }
            }
        }

        private static TextSpan GetTextChangeSpan(SourceText text, int position)
        {
            return CommonCompletionUtilities.GetTextChangeSpan(
                text,
                position,
                ch => CompletionUtilities.IsTextChangeSpanStartCharacter(ch) || ch == '{',
                ch => CompletionUtilities.IsWordCharacter(ch) || ch == '{' || ch == '}');
        }

        private IEnumerable<CompletionItem> CreateCompletionItems(
            Workspace workspace, SemanticModel semanticModel, IEnumerable<ISymbol> symbols, SyntaxToken token, TextSpan filterSpan)
        {
            var builder = SharedPools.Default<StringBuilder>().Allocate();
            try
            {
                foreach (var symbol in symbols)
                {
                    builder.Clear();
                    yield return CreateItem(workspace, semanticModel, symbol, token, filterSpan, builder);
                }
            }
            finally
            {
                SharedPools.Default<StringBuilder>().ClearAndFree(builder);
            }
        }

        private CompletionItem CreateItem(
            Workspace workspace, SemanticModel semanticModel, ISymbol symbol, SyntaxToken token, TextSpan filterSpan, StringBuilder builder)
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

            return new Item(
                completionProvider: this,
                displayText: insertionText,
                insertionText: insertionText,
                textSpan: filterSpan,
                descriptionFactory: CommonCompletionUtilities.CreateDescriptionFactory(workspace, semanticModel, position, symbol),
                glyph: symbol.GetGlyph(),
                sortText: symbolText);
        }
    }
}
