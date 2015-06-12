// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    internal partial class CrefCompletionProvider : AbstractCompletionProvider
    {
        public static readonly SymbolDisplayFormat CrefFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly,
                propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeType,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        public override bool IsCommitCharacter(CompletionItem completionItem, char ch, string textTypedSoFar)
        {
            if (ch == '{' && completionItem.DisplayText.Contains('{'))
            {
                return false;
            }

            if (ch == '(' && completionItem.DisplayText.Contains('('))
            {
                return false;
            }

            return CompletionUtilities.IsCommitCharacter(completionItem, ch, textTypedSoFar);
        }

        public override bool SendEnterThroughToEditor(CompletionItem completionItem, string textTypedSoFar)
        {
            return CompletionUtilities.SendEnterThroughToEditor(completionItem, textTypedSoFar);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return CompletionUtilities.IsTriggerCharacter(text, characterPosition, options);
        }

        protected override Task<bool> IsExclusiveAsync(Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            return SpecializedTasks.True;
        }

        protected override async Task<IEnumerable<CompletionItem>> GetItemsWorkerAsync(Document document, int position, CompletionTriggerInfo triggerInfo, System.Threading.CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (!tree.IsEntirelyWithinCrefSyntax(position, cancellationToken))
            {
                return null;
            }

            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            token = token.GetPreviousTokenIfTouchingWord(position);
            if (token.Kind() == SyntaxKind.None)
            {
                return null;
            }

            var result = SpecializedCollections.EmptyEnumerable<ISymbol>();

            // To get a Speculative SemanticModel (which is much faster), we need to 
            // walk up to the node the DocumentationTrivia is attached to.
            var parentNode = token.GetAncestor<DocumentationCommentTriviaSyntax>().ParentTrivia.Token.Parent;
            var semanticModel = await document.GetSemanticModelForNodeAsync(parentNode, cancellationToken).ConfigureAwait(false);

            // cref ""|, ""|"", ""a|""
            if (token.IsKind(SyntaxKind.DoubleQuoteToken, SyntaxKind.SingleQuoteToken) && token.Parent.IsKind(SyntaxKind.XmlCrefAttribute))
            {
                result = semanticModel.LookupSymbols(token.SpanStart)
                                        .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);

                result = result.Concat(GetOperatorsAndIndexers(token, semanticModel, cancellationToken));
            }
            else if (IsSignatureContext(token))
            {
                result = semanticModel.LookupNamespacesAndTypes(token.SpanStart)
                                        .FilterToVisibleAndBrowsableSymbols(document.ShouldHideAdvancedMembers(), semanticModel.Compilation);
            }
            else if (token.IsKind(SyntaxKind.DotToken) && token.Parent.IsKind(SyntaxKind.QualifiedCref))
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

            return await CreateItemsAsync(document.Project.Solution.Workspace, semanticModel,
                position, result, token, cancellationToken).ConfigureAwait(false);
        }

        private bool IsSignatureContext(SyntaxToken token)
        {
            return token.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken, SyntaxKind.RefKeyword, SyntaxKind.OutKeyword)
                && token.Parent.IsKind(SyntaxKind.CrefParameterList, SyntaxKind.CrefBracketedParameterList);
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

        private async Task<IEnumerable<CompletionItem>> CreateItemsAsync(
            Workspace workspace, SemanticModel semanticModel, int textChangeSpanPosition, IEnumerable<ISymbol> symbols, SyntaxToken token, CancellationToken cancellationToken)
        {
            var items = new List<CompletionItem>();

            foreach (var symbol in symbols)
            {
                var item = await CreateItemAsync(workspace, semanticModel, textChangeSpanPosition, symbol, token, cancellationToken).ConfigureAwait(false);
                items.Add(item);
            }

            return items;
        }

        private async Task<CompletionItem> CreateItemAsync(
            Workspace workspace, SemanticModel semanticModel, int textChangeSpanPosition, ISymbol symbol, SyntaxToken token, CancellationToken cancellationToken)
        {
            int tokenPosition = token.SpanStart;
            string symbolText = string.Empty;

            if (symbol is INamespaceOrTypeSymbol && token.IsKind(SyntaxKind.DotToken))
            {
                symbolText = symbol.Name.EscapeIdentifier();

                if (symbol.GetArity() > 0)
                {
                    symbolText += "{";
                    symbolText += string.Join(", ", ((INamedTypeSymbol)symbol).TypeParameters);
                    symbolText += "}";
                }
            }
            else
            {
                symbolText = symbol.ToMinimalDisplayString(semanticModel, tokenPosition, CrefFormat);
                var parameters = symbol.GetParameters().Select(p =>
                    {
                        var displayName = p.Type.ToMinimalDisplayString(semanticModel, tokenPosition);

                        if (p.RefKind == RefKind.Out)
                        {
                            return "out " + displayName;
                        }

                        if (p.RefKind == RefKind.Ref)
                        {
                            return "ref " + displayName;
                        }

                        return displayName;
                    });

                var parameterList = !symbol.IsIndexer() ? string.Format("({0})", string.Join(", ", parameters))
                                                        : string.Format("[{0}]", string.Join(", ", parameters));
                symbolText += parameterList;
            }

            var insertionText = symbolText
                .Replace('<', '{')
                .Replace('>', '}')
                .Replace("()", "");

            var text = await semanticModel.SyntaxTree.GetTextAsync(cancellationToken).ConfigureAwait(false);
            return new CrefCompletionItem(
                workspace,
                completionProvider: this,
                displayText: insertionText,
                insertionText: insertionText,
                textSpan: GetTextChangeSpan(text, textChangeSpanPosition),
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

        public override TextChange GetTextChange(CompletionItem selectedItem, char? ch = null, string textTypedSoFar = null)
        {
            return new TextChange(selectedItem.FilterSpan, ((CrefCompletionItem)selectedItem).InsertionText);
        }
    }
}
