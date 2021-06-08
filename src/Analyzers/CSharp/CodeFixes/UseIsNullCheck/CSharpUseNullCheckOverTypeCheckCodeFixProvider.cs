// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseIsNullCheckOverIsObject), Shared]
    internal sealed class CSharpUseNullCheckOverTypeCheckCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpUseNullCheckOverTypeCheckCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(IDEDiagnosticIds.UseNullCheckOverTypeCheckDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                new MyCodeAction(c => FixAsync(context.Document, diagnostic, c)),
                context.Diagnostics);

            return Task.CompletedTask;
        }

        protected override Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            foreach (var diagnostic in diagnostics)
            {
                var node = diagnostic.Location.FindNode(getInnermostNodeForTie: true, cancellationToken: cancellationToken);
                SyntaxNode? replacement;
                // Replace 'x is object' with 'x is not null'
                if (node is BinaryExpressionSyntax binary)
                {
                    replacement = SyntaxFactory.IsPatternExpression(
                        expression: binary.Left,
                        pattern: SyntaxFactory.UnaryPattern(SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));
                }
                else if (node is UnaryPatternSyntax)
                {
                    replacement = SyntaxFactory.ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression));   
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(node.Kind());
                }

                editor.ReplaceNode(node, replacement);
            }

            return Task.CompletedTask;
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(CSharpAnalyzersResources.Use_is_null_check, createChangedDocument, nameof(CSharpAnalyzersResources.Use_is_null_check))
            {
            }
        }
    }
}
