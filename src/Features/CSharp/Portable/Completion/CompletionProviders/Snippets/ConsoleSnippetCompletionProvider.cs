// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets
{
    [ExportCompletionProvider(nameof(ConsoleSnippetCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(FirstBuiltInCompletionProvider))]
    [Shared]
    internal class ConsoleSnippetCompletionProvider : CommonCompletionProvider
    {
        internal override string Language => LanguageNames.CSharp;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConsoleSnippetCompletionProvider()
        {
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = null, CancellationToken cancellationToken = default)
        {
            if (!item.Properties.TryGetValue("SnippetText", out var snippetText))
            {
                return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
            }

            return CompletionChange.Create(new TextChange(item.Span, snippetText), newPosition: 5);
        }

        private static int GetSpanStart(SyntaxNode declaration)
        {
            return declaration switch
            {
                MethodDeclarationSyntax method => method.ReturnType.SpanStart,
                LocalFunctionStatementSyntax local => local.ReturnType.SpanStart,
                AnonymousMethodExpressionSyntax anonymous => anonymous.DelegateKeyword.SpanStart,
                ParenthesizedLambdaExpressionSyntax parenthesizedLambda => (parenthesizedLambda.ReturnType as SyntaxNode ?? parenthesizedLambda.ParameterList).SpanStart,
                SimpleLambdaExpressionSyntax simpleLambda => simpleLambda.Parameter.SpanStart,
                _ => throw ExceptionUtilities.UnexpectedValue(declaration.Kind())
            };
        }

        private static SyntaxNode? GetAsyncSupportingDeclaration(SyntaxToken token)
        {
            var node = token.GetAncestor(node => node.IsAsyncSupportingFunctionSyntax());
            if (node is LocalFunctionStatementSyntax { ExpressionBody: null, Body: null })
            {
                return node.Parent?.FirstAncestorOrSelf<SyntaxNode>(node => node.IsAsyncSupportingFunctionSyntax());
            }

            return node;
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var cancellationToken = context.CancellationToken;
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (syntaxFacts.IsInNonUserCode(syntaxTree, position, cancellationToken))
            {
                return;
            }

            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = (CSharpSyntaxContext)document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);
            var isInsideMethod = syntaxContext.LeftToken.GetAncestors<SyntaxNode>()
                .Any(node => node.IsKind(SyntaxKind.MethodDeclaration) ||
                             node.IsKind(SyntaxKind.LocalFunctionStatement) ||
                             node.IsKind(SyntaxKind.AnonymousMethodExpression) ||
                             node.IsKind(SyntaxKind.ParenthesizedLambdaExpression)) || syntaxContext.IsGlobalStatementContext;

            if (!isInsideMethod)
            {
                return;
            }

            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();
            var completionItem = GetCompletionItem(syntaxContext.TargetToken, generator, syntaxContext.IsGlobalStatementContext);
            context.AddItem(completionItem);
        }

        private static CompletionItem GetCompletionItem(SyntaxToken token, SyntaxGenerator generator, bool isGlobalStatementContext)
        {
            var snippetText = "Console.WriteLine();";

            if (!isGlobalStatementContext)
            {
                var declaration = GetAsyncSupportingDeclaration(token);
                var isAsync = generator.GetModifiers(declaration).IsAsync;

                if (isAsync)
                {
                    snippetText = "await Console.Out.WriteLineAsync();";
                }
            }

            return CommonCompletionItem.Create(
                displayText: "Write to the Console",
                displayTextSuffix: "",
                rules: CompletionItemRules.Default,
                Glyph.Snippet,
                inlineDescription: "",
                properties: ImmutableDictionary.Create<string, string>().Add("SnippetText", snippetText));
        }
    }
}
