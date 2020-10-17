﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Simplification.Simplifiers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessaryCast
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.ImplementInterface)]
    internal partial class CSharpRemoveUnnecessaryCastCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpRemoveUnnecessaryCastCodeFixProvider()
        {
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(IDEDiagnosticIds.RemoveUnnecessaryCastDiagnosticId);

        internal sealed override CodeFixCategory CodeFixCategory => CodeFixCategory.CodeStyle;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            context.RegisterCodeFix(new MyCodeAction(
                AnalyzersResources.Remove_Unnecessary_Cast,
                c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                context.Diagnostics);
            return Task.CompletedTask;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var castNodes = diagnostics.SelectAsArray(
                d => (ExpressionSyntax)d.AdditionalLocations[0].FindNode(getInnermostNodeForTie: true, cancellationToken));

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, castNodes,
                (semanticModel, castExpression) => CastSimplifier.IsUnnecessaryCast(castExpression, semanticModel, cancellationToken),
                (_, currentRoot, castExpression) =>
                {
                    var oldParent = castExpression.WalkUpParentheses();
                    var newParent = Recurse(oldParent);

                    return currentRoot.ReplaceNode(oldParent, newParent);
                },
                cancellationToken).ConfigureAwait(false);
        }

        private ExpressionSyntax Recurse(ExpressionSyntax old)
        {
            if (old is ParenthesizedExpressionSyntax parenthesizedExpression)
            {
                // It's common in C# to have to write  ((Goo)expr).Etc(). we don't just want to
                // remove the cast and produce (expr).Etc().  So we mark all parent parenthesized
                // expressions as worthy of simplification.  The simplifier will remove these
                // if possible, or leave them alone if not.
                return parenthesizedExpression.ReplaceNode(parenthesizedExpression.Expression, Recurse(parenthesizedExpression.Expression))
                                              .WithAdditionalAnnotations(Simplifier.Annotation);
            }
            else if (old is CastExpressionSyntax castExpression)
            {
                // parenthesize the uncasted value to help ensure any proper parsing. The excess
                // parens will be removed if unnecessary. 
                return castExpression.Uncast().WithAdditionalAnnotations(Formatter.Annotation)
                                     .Parenthesize();
            }
            else if (old is BinaryExpressionSyntax binaryExpression)
            {
                return binaryExpression.Left.WithTrailingTrivia(binaryExpression.GetTrailingTrivia())
                                       .WithAdditionalAnnotations(Simplifier.Annotation);
            }
            else
            {
                throw ExceptionUtilities.UnexpectedValue(old);
            }
        }

        private class MyCodeAction : CustomCodeActions.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
