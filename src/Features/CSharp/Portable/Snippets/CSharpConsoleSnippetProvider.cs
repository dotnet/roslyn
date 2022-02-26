// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AddImports;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Simplification;
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
            var isInNamespace = token.GetAncestors<SyntaxNode>()
                .Any(node => node.IsKind(SyntaxKind.NamespaceDeclaration) ||
                             node.IsKind(SyntaxKind.FileScopedNamespaceDeclaration));

            if (context.IsStatementContext || (context.IsGlobalStatementContext && !isInNamespace))
            {
                return true;
            }

            return false;
            /*var token = context.LeftToken;
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
            }*/
        }

        protected override string GetSnippetDisplayName()
        {
            return FeaturesResources.Write_to_the_Console;
        }

        protected override async Task<ImmutableArray<TextChange>> GenerateSnippetTextChangesAsync(Document document, int position, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<TextChange>.GetInstance(out var arrayBuilder);
            var snippetTextChange = await GenerateSnippetAsync(document, position, cancellationToken).ConfigureAwait(false);
            arrayBuilder.AddRange(snippetTextChange);
            return arrayBuilder.ToImmutableArray();
        }

        private static async Task<TextChange> GenerateSnippetAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var token = tree.FindTokenOnLeftOfPosition(position, cancellationToken);
            var declaration = GetAsyncSupportingDeclaration(token);
            var isAsync = generator.GetModifiers(declaration).IsAsync;
            SyntaxNode? invocation;
            invocation = isAsync
                ? generator.ExpressionStatement(generator.AwaitExpression(generator.InvocationExpression(
                    generator.MemberAccessExpression(generator.MemberAccessExpression(generator.IdentifierName("Console"),
                    generator.IdentifierName("Out")), generator.IdentifierName("WriteLineAsync")))))
                : generator.ExpressionStatement(generator.InvocationExpression(generator.MemberAccessExpression(
                    generator.IdentifierName("Console"), generator.IdentifierName("WriteLine"))));
            return new TextChange(TextSpan.FromBounds(position, position), invocation.NormalizeWhitespace().ToFullString());
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
            var argumentListNode = caretTarget.DescendantNodes().OfType<ArgumentListSyntax>().First();
            return argumentListNode.GetLocation().SourceSpan.End - 1;
        }

        protected override async Task<SyntaxNode> AnnotateNodesToReformatAsync(Document document,
            SyntaxAnnotation findSnippetAnnotation, SyntaxAnnotation cursorAnnotation, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var snippetExpressionNode = GetConsoleExpressionStatement(root, position);
            if (snippetExpressionNode is null)
            {
                return root;
            }

            var compilation = await document.Project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            var symbol = compilation.GetBestTypeByMetadataName("System.Console");
            if (symbol is null)
            {
                return root;
            }

            var reformatSnippetNode = snippetExpressionNode.WithAdditionalAnnotations(findSnippetAnnotation, cursorAnnotation, Simplifier.Annotation, SymbolAnnotation.Create(symbol), Formatter.Annotation);
            return root.ReplaceNode(snippetExpressionNode, reformatSnippetNode);
        }

        private static SyntaxNode? GetConsoleExpressionStatement(SyntaxNode root, int position)
        {
            var closestNode = root.FindNode(TextSpan.FromBounds(position, position));
            return closestNode.GetAncestorOrThis<ExpressionStatementSyntax>();
        }

        protected override Task<ImmutableArray<TextSpan>> GetRenameLocationsAsync(Document document, int position, CancellationToken cancellationToken)
        {
            return Task.FromResult(ImmutableArray<TextSpan>.Empty);
        }
    }
}
