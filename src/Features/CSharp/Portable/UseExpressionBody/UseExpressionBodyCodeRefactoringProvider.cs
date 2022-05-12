// Licensed to the .NET Foundation under one or more agreements.
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
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseExpressionBody
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp,
        Name = PredefinedCodeRefactoringProviderNames.UseExpressionBody), Shared]
    internal class UseExpressionBodyCodeRefactoringProvider : CodeRefactoringProvider
    {
        private static readonly ImmutableArray<UseExpressionBodyHelper> _helpers = UseExpressionBodyHelper.Helpers;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public UseExpressionBodyCodeRefactoringProvider()
        {
        }

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (textSpan.Length > 0)
                return;

            var position = textSpan.Start;
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindToken(position).Parent!;

            var containingLambda = node.FirstAncestorOrSelf<LambdaExpressionSyntax>();
            if (containingLambda != null &&
                node.AncestorsAndSelf().Contains(containingLambda.Body))
            {
                // don't offer inside a lambda.  Lambdas can be quite large, and it will be very noisy
                // inside the body of one to be offering to use a block/expression body for the containing
                // class member.
                return;
            }

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var optionSet = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);

            foreach (var helper in _helpers)
            {
                var declaration = TryGetDeclaration(helper, text, node, position);
                if (declaration == null)
                    continue;

                var succeeded = TryComputeRefactoring(context, root, declaration, optionSet, helper);
                if (succeeded)
                    return;
            }
        }

        private static SyntaxNode? TryGetDeclaration(
            UseExpressionBodyHelper helper, SourceText text, SyntaxNode node, int position)
        {
            var declaration = GetDeclaration(node, helper);
            if (declaration == null)
                return null;

            if (position < declaration.SpanStart)
            {
                // The user is allowed to be before the starting point of this node, as long as
                // they're only between the start of the node and the start of the same line the
                // node starts on.  This prevents unnecessarily showing this feature in areas like
                // the comment of a method.
                if (!text.AreOnSameLine(position, declaration.SpanStart))
                    return null;
            }

            return declaration;
        }

        private static bool TryComputeRefactoring(
            CodeRefactoringContext context, SyntaxNode root, SyntaxNode declaration,
            OptionSet optionSet, UseExpressionBodyHelper helper)
        {
            var document = context.Document;

            var succeeded = false;
            if (helper.CanOfferUseExpressionBody(optionSet, declaration, forAnalyzer: false))
            {
                var title = helper.UseExpressionBodyTitle.ToString();
                context.RegisterRefactoring(
                    CodeAction.Create(
                        title,
                        c => UpdateDocumentAsync(
                            document, root, declaration, helper,
                            useExpressionBody: true, cancellationToken: c),
                        title),
                    declaration.Span);
                succeeded = true;
            }

            if (helper.CanOfferUseBlockBody(optionSet, declaration, forAnalyzer: false, out _, out _))
            {
                var title = helper.UseBlockBodyTitle.ToString();
                context.RegisterRefactoring(
                    CodeAction.Create(
                        title,
                        c => UpdateDocumentAsync(
                            document, root, declaration, helper,
                            useExpressionBody: false, cancellationToken: c),
                        title),
                    declaration.Span);
                succeeded = true;
            }

            return succeeded;
        }

        private static SyntaxNode? GetDeclaration(SyntaxNode node, UseExpressionBodyHelper helper)
        {
            for (var current = node; current != null; current = current.Parent)
            {
                if (helper.SyntaxKinds.Contains(current.Kind()))
                    return current;
            }

            return null;
        }

        private static async Task<Document> UpdateDocumentAsync(
            Document document, SyntaxNode root, SyntaxNode declaration,
            UseExpressionBodyHelper helper, bool useExpressionBody,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var updatedDeclaration = helper.Update(semanticModel, declaration, useExpressionBody);

            var parent = declaration is AccessorDeclarationSyntax
                ? declaration.Parent
                : declaration;
            RoslynDebug.Assert(parent is object);
            var updatedParent = parent.ReplaceNode(declaration, updatedDeclaration)
                                      .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(parent, updatedParent);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
