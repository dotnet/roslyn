// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.Providers
{
    [ExportCompletionProvider(nameof(AwaitCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(KeywordCompletionProvider))]
    [Shared]
    internal sealed class AwaitCompletionProvider : AbstractAwaitCompletionProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public AwaitCompletionProvider()
        {
        }

        public override ImmutableHashSet<char> TriggerCharacters => CompletionUtilities.CommonTriggerCharactersWithArgumentList;

        private protected override string AsyncKeywordTextWithSpace => "async ";

        /// <summary>
        /// Gets the span start where async keyword should go.
        /// </summary>
        private protected override int GetSpanStart(SyntaxNode declaration)
        {
            return declaration switch
            {
                MethodDeclarationSyntax method => method.ReturnType.SpanStart,
                LocalFunctionStatementSyntax local => local.ReturnType.SpanStart,
                AnonymousMethodExpressionSyntax anonymous => anonymous.DelegateKeyword.SpanStart,
                // If we have an explicit lambda return type, async should go just before it. Otherwise, it should go before parameter list.
                // static [|async|] (a) => ....
                // static [|async|] ExplicitReturnType (a) => ....
                ParenthesizedLambdaExpressionSyntax parenthesizedLambda => (parenthesizedLambda.ReturnType as SyntaxNode ?? parenthesizedLambda.ParameterList).SpanStart,
                SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.SpanStart,
                _ => throw ExceptionUtilities.UnexpectedValue(declaration.Kind())
            };
        }

        private protected override bool ShouldMakeContainerAsync(SyntaxToken token)
        {
            var declaration = GetAsyncSupportingDeclaration(token);
            return declaration is not null && !declaration.GetModifiers().Any(SyntaxKind.AsyncKeyword);
        }

        private protected override CompletionItem GetCompletionItem(SyntaxToken token)
        {
            var shouldMakeContainerAsync = ShouldMakeContainerAsync(token);
            var text = SyntaxFacts.GetText(SyntaxKind.AwaitKeyword);
            return CommonCompletionItem.Create(
                displayText: text,
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Keyword,
                description: RecommendedKeyword.CreateDisplayParts(text, string.Empty),
                inlineDescription: shouldMakeContainerAsync ? CSharpFeaturesResources.Make_container_async : null,
                isComplexTextEdit: shouldMakeContainerAsync);
        }

        private protected override SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token)
            => token.GetAncestor(node => node.IsAsyncSupportingFunctionSyntax());
    }
}
