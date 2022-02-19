// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Snippets
{
    [ExportSnippetProvider(nameof(ISnippetProvider), LanguageNames.CSharp), Shared]
    internal class CSharpConsoleSnippetProvider : AbstractSnippetProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpConsoleSnippetProvider()
        {
        }

        protected override async Task<bool> IsValidSnippetLocationAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var semanticModel = await document.ReuseExistingSpeculativeModelAsync(position, cancellationToken).ConfigureAwait(false);
            var syntaxContext = (CSharpSyntaxContext)document.GetRequiredLanguageService<ISyntaxContextService>().CreateContext(document, semanticModel, position, cancellationToken);

            if (!ShouldDisplaySnippet(syntaxContext))
            {
                return false;
            }

            return true;
        }

        private static bool ShouldDisplaySnippet(CSharpSyntaxContext context)
        {
            var token = context.LeftToken;
            var isDirectlyInUndesirableLocation = token.GetAncestors<SyntaxNode>()
                .Any(node => node.IsKind(SyntaxKind.ParameterList) ||
                             node.IsKind(SyntaxKind.SimpleLambdaExpression) ||
                             node.IsKind(SyntaxKind.ArgumentList) ||
                             node.IsKind(SyntaxKind.RecordDeclaration) ||
                             node.IsKind(SyntaxKind.ObjectCreationExpression) ||
                             node.IsKind(SyntaxKind.SwitchExpression));

            if (isDirectlyInUndesirableLocation)
            {
                return false;
            }

            var isExpressionInVariable = token.GetAncestors<SyntaxNode>()
                .Any(node => node.IsKind(SyntaxKind.ParenthesizedLambdaExpression) || node.IsKind(SyntaxKind.AnonymousMethodExpression));
            var isInVariableDeclaration = token.GetAncestors<SyntaxNode>().Any(node => node.IsKind(SyntaxKind.VariableDeclaration));

            if (isInVariableDeclaration && !isExpressionInVariable)
            {
                return false;
            }

            var isInsideMethod = token.GetAncestors<SyntaxNode>()
               .Any(node => node.IsKind(SyntaxKind.MethodDeclaration) ||
                            node.IsKind(SyntaxKind.ConstructorDeclaration) ||
                            node.IsKind(SyntaxKind.LocalFunctionStatement) ||
                            node.IsKind(SyntaxKind.AnonymousMethodExpression) ||
                            node.IsKind(SyntaxKind.ParenthesizedLambdaExpression));

            var isInNamespace = token.GetAncestors<SyntaxNode>()
                .Any(node => node.IsKind(SyntaxKind.NamespaceDeclaration) ||
                             node.IsKind(SyntaxKind.FileScopedNamespaceDeclaration));

            if (isInNamespace && !isInsideMethod)
            {
                return false;
            }

            if (!isInsideMethod && !context.IsGlobalStatementContext)
            {
                return false;
            }

            return true;
        }

        protected override string GetSnippetDisplayName()
        {
            return FeaturesResources.Write_to_the_Console;
        }

        protected override async Task<TextChange> GenerateSnippetTextChangeAsync(Document document, TextSpan span, int tokenSpanStart, int tokenSpanEnd, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindTokenOnLeftOfPosition(tokenSpanEnd, cancellationToken);
            var declaration = GetAsyncSupportingDeclaration(token);
            var isAsync = generator.GetModifiers(declaration).IsAsync;
            SyntaxNode? invocation;
            invocation = isAsync
                ? generator.ExpressionStatement(generator.AwaitExpression(generator.InvocationExpression(
                    generator.MemberAccessExpression(generator.MemberAccessExpression(generator.IdentifierName("Console"),
                    generator.IdentifierName("Out")), generator.IdentifierName("WriteLineAsync")))))
                : generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(
                    generator.IdentifierName("Console"), generator.IdentifierName("WriteLine"))));
            var textChange = new TextChange(span, invocation.NormalizeWhitespace().ToFullString());
            return textChange;
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

        protected override int GetTargetCaretPosition(SyntaxNode caretTarget)
        {
            return caretTarget.GetLocation().SourceSpan.End - 1;
        }

        protected override async Task<SyntaxNode> AnnotateRootForReformattingAsync(Document document, TextSpan span,
            SyntaxAnnotation reformatAnnotation, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var closestNode = root.FindNode(span);
            var snippetExpressionNode = closestNode.GetAncestorOrThis<ExpressionStatementSyntax>();
            if (snippetExpressionNode is null)
            {
                return root;
            }

            var reformatSnippetNode = snippetExpressionNode.WithAdditionalAnnotations(reformatAnnotation);
            return root.ReplaceNode(snippetExpressionNode, reformatSnippetNode);
        }

        protected override async Task<SyntaxNode> AnnotateRootForCursorAsync(Document document, TextSpan span,
            SyntaxAnnotation cursorAnnotation, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var closestNode = root.FindNode(span);
            var snippetExpressionNode = closestNode.GetAncestorOrThis<ExpressionStatementSyntax>();
            if (snippetExpressionNode is null)
            {
                return root;
            }

            // Get the argument list to annotate so we can place the cursor in the list
            var argumentListNode = snippetExpressionNode.DescendantNodes().OfType<ArgumentListSyntax>().First();
            var annotedSnippet = argumentListNode.WithAdditionalAnnotations(cursorAnnotation);
            return root.ReplaceNode(argumentListNode, annotedSnippet);
        }

        protected override Task<ImmutableArray<TextSpan>> GetRenameLocationsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray<TextSpan>.Empty);
        }
    }
}
