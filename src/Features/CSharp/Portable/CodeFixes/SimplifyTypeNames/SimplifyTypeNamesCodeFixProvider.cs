// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.SimplifyTypeNames
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.SimplifyNames), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.RemoveUnnecessaryCast)]
    internal partial class SimplifyTypeNamesCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(
                    IDEDiagnosticIds.SimplifyNamesDiagnosticId,
                    IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId,
                    IDEDiagnosticIds.RemoveQualificationDiagnosticId,
                    IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId,
                    IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return SimplifyTypeNamesFixAllProvider.Instance;
        }

        internal static SyntaxNode GetNodeToSimplify(SyntaxNode root, SemanticModel model, TextSpan span, OptionSet optionSet, out string diagnosticId, CancellationToken cancellationToken)
        {
            diagnosticId = null;
            var token = root.FindToken(span.Start, findInsideTrivia: true);
            if (!token.Span.IntersectsWith(span))
            {
                return null;
            }

            foreach (var n in token.GetAncestors<SyntaxNode>())
            {
                if (n.Span.IntersectsWith(span) && CanSimplifyTypeNameExpression(model, n, optionSet, span, out diagnosticId, cancellationToken))
                {
                    return n;
                }
            }

            return null;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var documentOptions = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var node = GetNodeToSimplify(root, model, span, documentOptions, out var diagnosticId, cancellationToken);
            if (node == null)
            {
                return;
            }

            var title = GetTitle(diagnosticId, node.ConvertToSingleLine().ToString());
            var codeAction = new SimplifyTypeNameCodeAction(
                title, c => SimplifyTypeNameAsync(document, node, c), diagnosticId);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        private static string GetTitle(string diagnosticId, string nodeText)
        {
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.SimplifyNamesDiagnosticId:
                case IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInDeclarationsDiagnosticId:
                    return string.Format(CSharpFeaturesResources.Simplify_name_0, nodeText);

                case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId:
                case IDEDiagnosticIds.PreferIntrinsicPredefinedTypeInMemberAccessDiagnosticId:
                    return string.Format(CSharpFeaturesResources.Simplify_member_access_0, nodeText);

                case IDEDiagnosticIds.RemoveQualificationDiagnosticId:
                    return CSharpFeaturesResources.Remove_this_qualification;

                default:
                    throw ExceptionUtilities.UnexpectedValue(diagnosticId);
            }
        }

        private static bool CanSimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, OptionSet optionSet, TextSpan span, out string diagnosticId, CancellationToken cancellationToken)
        {
            diagnosticId = null;
            if (!CSharpSimplifyTypeNamesDiagnosticAnalyzer.IsCandidate(node) ||
                !CSharpSimplifyTypeNamesDiagnosticAnalyzer.CanSimplifyTypeNameExpression(model, node, optionSet, out var issueSpan, out diagnosticId, cancellationToken))
            {
                return false;
            }

            return issueSpan.Equals(span);
        }

        private async Task<Document> SimplifyTypeNameAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var expressionSyntax = node;
            var annotatedexpressionSyntax = expressionSyntax.WithAdditionalAnnotations(Simplifier.Annotation, Formatter.Annotation);

            if (annotatedexpressionSyntax.Kind() == SyntaxKind.IsExpression || annotatedexpressionSyntax.Kind() == SyntaxKind.AsExpression)
            {
                var right = ((BinaryExpressionSyntax)annotatedexpressionSyntax).Right;
                annotatedexpressionSyntax = annotatedexpressionSyntax.ReplaceNode(right, right.WithAdditionalAnnotations(Simplifier.Annotation));
            }

            SyntaxNode oldNode = expressionSyntax;
            SyntaxNode newNode = annotatedexpressionSyntax;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(oldNode, newNode);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}
