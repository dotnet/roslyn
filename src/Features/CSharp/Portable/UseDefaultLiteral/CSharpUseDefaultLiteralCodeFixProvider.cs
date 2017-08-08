// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseDefaultLiteralCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.UseDefaultLiteralDiagnosticId);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return SpecializedTasks.EmptyTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            // Fix-All for this feature is somewhat complicated.  Each time we fix one case, it
            // may make the next case unfixable.  For example:
            //
            //    'var v = x ? default(string) : default(string)'.
            //
            // Here, we can replace either of the default expressions, but not both. So we have 
            // to replace one at a time, and only actually replace if it's still safe to do so.

            var parseOptions = (CSharpParseOptions)document.Project.ParseOptions;
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            var workspace = document.Project.Solution.Workspace;
            var originalRoot = editor.OriginalRoot;

            var originalNodes = diagnostics.SelectAsArray(
                d => (DefaultExpressionSyntax)originalRoot.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true));

            // This code fix will not make changes that affect the semantics of a statement or declaration. Therefore,
            // we can skip the expensive verification step in cases where only one default expression appears within the
            // group.
            var nodesBySemanticBoundary = originalNodes.GroupBy(node => GetSemanticBoundary(node));
            var nodesToVerify = nodesBySemanticBoundary.Where(group => group.Skip(1).Any()).Flatten().ToSet();

            // We're going to be continually editing this tree.  Track all the nodes we
            // care about so we can find them across each edit.
            document = document.WithSyntaxRoot(originalRoot.TrackNodes(originalNodes));
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var currentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            foreach (var originalDefaultExpression in originalNodes)
            {
                var defaultExpression = currentRoot.GetCurrentNode(originalDefaultExpression);
                var skipVerification = !nodesToVerify.Contains(originalDefaultExpression);

                if (skipVerification || defaultExpression.CanReplaceWithDefaultLiteral(parseOptions, options, semanticModel, cancellationToken))
                {
                    var replacement = SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression)
                                                   .WithTriviaFrom(defaultExpression);

                    document = document.WithSyntaxRoot(currentRoot.ReplaceNode(defaultExpression, replacement));

                    semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    currentRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            editor.ReplaceNode(originalRoot, currentRoot);
        }

        private static SyntaxNode GetSemanticBoundary(DefaultExpressionSyntax node)
        {
            // Notes:
            // 1. Syntax which doesn't fall into one of the "safe buckets" will get placed into a single group keyed off
            //    the root of the tree. If more than one such node exists in the document, all will be verified.
            // 2. Cannot include ArgumentSyntax because it could affect generic argument inference.
            return node.FirstAncestorOrSelf<SyntaxNode>(n =>
                n is StatementSyntax
                || n is ParameterSyntax
                || n is VariableDeclaratorSyntax
                || n.Parent == null);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(FeaturesResources.Simplify_default_expression, createChangedDocument, FeaturesResources.Simplify_default_expression)
            {
            }
        }
    }
}
