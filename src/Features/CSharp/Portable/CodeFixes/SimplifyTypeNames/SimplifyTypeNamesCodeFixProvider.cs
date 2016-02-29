// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames;
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
                    IDEDiagnosticIds.RemoveQualificationDiagnosticId);
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
            var optionSet = document.Project.Solution.Workspace.Options;
            string diagnosticId;
            var node = GetNodeToSimplify(root, model, span, optionSet, out diagnosticId, cancellationToken);
            if (node == null)
            {
                return;
            }

            var id = GetCodeActionId(diagnosticId, node.ToString());
            var title = id;
            var codeAction = new SimplifyTypeNameCodeAction(title,
                    (c) => SimplifyTypeNameAsync(document, node, c),
                    id);

            context.RegisterCodeFix(codeAction, context.Diagnostics);
        }

        // internal for testing purpose
        internal static string GetCodeActionId(string diagnosticId, string nodeText)
        {
            switch (diagnosticId)
            {
                case IDEDiagnosticIds.SimplifyNamesDiagnosticId:
                    return string.Format(CSharpFeaturesResources.SimplifyName, nodeText);

                case IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId:
                    return string.Format(CSharpFeaturesResources.SimplifyMemberAccess, nodeText);

                case IDEDiagnosticIds.RemoveQualificationDiagnosticId:
                    return CSharpFeaturesResources.RemoveThisQualification;

                default:
                    throw ExceptionUtilities.Unreachable;
            }
        }

        private static bool CanSimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, OptionSet optionSet, TextSpan span, out string diagnosticId, CancellationToken cancellationToken)
        {
            diagnosticId = null;
            TextSpan issueSpan;
            if (!CSharpSimplifyTypeNamesDiagnosticAnalyzer.IsCandidate(node) ||
                !CSharpSimplifyTypeNamesDiagnosticAnalyzer.CanSimplifyTypeNameExpression(model, node, optionSet, out issueSpan, out diagnosticId, cancellationToken))
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
