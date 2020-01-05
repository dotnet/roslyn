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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseDefaultLiteral
{
    [ExportCodeFixProvider(LanguageNames.CSharp), Shared]
    internal partial class CSharpUseDefaultLiteralCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        public CSharpUseDefaultLiteralCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds { get; }
            = ImmutableArray.Create(IDEDiagnosticIds.UseDefaultLiteralDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
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

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, originalNodes,
                (semanticModel, defaultExpression) => defaultExpression.CanReplaceWithDefaultLiteral(parseOptions, options, semanticModel, cancellationToken),
                (_, currentRoot, defaultExpression) => currentRoot.ReplaceNode(
                    defaultExpression,
                    SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression).WithTriviaFrom(defaultExpression)),
                cancellationToken).ConfigureAwait(false);
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
