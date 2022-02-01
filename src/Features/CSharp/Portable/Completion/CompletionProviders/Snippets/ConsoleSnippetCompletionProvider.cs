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
using Microsoft.CodeAnalysis.Completion.Providers.Snippets;
using Microsoft.CodeAnalysis.CSharp.Completion.Providers;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Completion.CompletionProviders.Snippets
{
    [ExportCompletionProvider(nameof(ConsoleSnippetCompletionProvider), LanguageNames.CSharp)]
    [ExtensionOrder(After = nameof(FirstBuiltInCompletionProvider))]
    [Shared]
    internal class ConsoleSnippetCompletionProvider : AbstractSnippetCompletionProvider
    {
        internal override string Language => LanguageNames.CSharp;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ConsoleSnippetCompletionProvider()
        {
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

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var generator = SyntaxGenerator.GetGenerator(document);
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();
            var completionItem = GetCompletionItem(syntaxContext.TargetToken, generator, text, position, syntaxContext.IsGlobalStatementContext);
            context.AddItem(completionItem);
        }

        private static CompletionItem GetCompletionItem(SyntaxToken token, SyntaxGenerator generator, SourceText text, int position, bool isGlobalStatementContext)
        {
            return SnippetCompletionItem.Create(
                displayText: "Write to the Console",
                displayTextSuffix: "",
                line: text.Lines.IndexOf(position),
                token: token,
                glyph: Glyph.Snippet);
        }

        protected override int GetTargetCaretPosition(SyntaxNode caretTarget)
        {
            return caretTarget.GetLocation().SourceSpan.End;
        }

        protected override SyntaxToken GetToken(CompletionItem completionItem, SyntaxTree tree, CancellationToken cancellationToken)
        {
            var tokenSpanEnd = SnippetCompletionItem.GetTokenSpanEnd(completionItem);
            return tree.FindTokenOnLeftOfPosition(tokenSpanEnd, cancellationToken);
        }

        protected override SyntaxNode GetSyntax(SyntaxToken token)
        {
            return token.GetAncestor<MethodDeclarationSyntax>()
                ?? token.GetAncestor<LocalFunctionStatementSyntax>()
                ?? token.GetAncestor<AnonymousMethodExpressionSyntax>()
                ?? token.GetAncestor<ParenthesizedLambdaExpressionSyntax>()
                ?? (SyntaxNode?)token.GetAncestor<GlobalStatementSyntax>()
                ?? throw ExceptionUtilities.UnexpectedValue(token);
        }

        protected override async Task<Document> GenerateDocumentWithSnippetAsync(Document document, CompletionItem completionItem, TextLine line, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, generator);
            var invocation = generator.InvocationExpression(generator.MemberAccessExpression(generator.IdentifierName("Console"), generator.IdentifierName("WriteLine")));
            var priorNode = root.FindNode(new TextSpan(line.Start, 0));
            editor.InsertAfter(priorNode, invocation);
            return document.WithSyntaxRoot(editor.GetChangedRoot());
        }
    }
}
