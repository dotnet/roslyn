// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyTypeNamesDiagnosticAnalyzer
        : SimplifyTypeNamesDiagnosticAnalyzerBase<SyntaxKind>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest =
            ImmutableArray.Create(
                SyntaxKind.QualifiedName,
                SyntaxKind.AliasQualifiedName,
                SyntaxKind.GenericName,
                SyntaxKind.IdentifierName,
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.QualifiedCref);

        public CSharpSimplifyTypeNamesDiagnosticAnalyzer()
            : base(s_kindsOfInterest)
        {
        }

        protected override void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            // Only analyze the topmost name/member-access/qualified-cref.  We'll handle recursing
            // into the node ourselves.  That way, in general, we report the largest/highest issue,
            // and we don't recurse and report another diagnostics for sub-spans of that issue.
            if (context.Node.Ancestors(ascendOutOfTrivia: false).Any(n => IsCandidate(n)))
            {
                return;
            }

            var options = context.Options;
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;
            var node = context.Node;

            // Handle the topmost qualified-cref slightly differently.  Unlike names/member-accesses,
            // we do want to recurse into these even if we are able to simplify it.
            if (node.IsKind(SyntaxKind.QualifiedCref, out QualifiedCrefSyntax qualifiedCref))
            {
                AnalyzeQualifiedCref(context, qualifiedCref);
            }
            else
            {
                // Otherwise, just recurse into the topmost name/member-access normally.
                RecurseAndAnalyzeNode(context, context.Node);
            }
        }

        private void AnalyzeQualifiedCref(SyntaxNodeAnalysisContext context, QualifiedCrefSyntax qualifiedCref)
        {
            var options = context.Options;
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;

            // First, just try to simplify the top-most qualified-cref alone. If we're able to do
            // this, then there's no need to process it's container.  i.e.
            //
            // if we have <see cref="A.B.C"/> and we simplify that to <see cref="C"/> there's no
            // point looking at `A.B`.
            if (TrySimplifyTypeNameExpression(semanticModel, qualifiedCref, options, out var diagnostic, cancellationToken))
            {
                context.ReportDiagnostic(diagnostic);

                // found a match on the qualified cref itself. report it and keep processing.
            }
            else
            {
                // couldn't simplify the qualified cref itself.  descend into the container portion
                // as that might have portions that can be simplified.
                RecurseAndAnalyzeNode(context, qualifiedCref.Container);
            }

            // unilaterally process the member portion of the qualified cref.  These may have things
            // like parameters that could be simplified.  i.e. if we have:
            //
            //      <see cref="A.B.C(X.Y)"/>
            //
            // We can simplify both the qualified portion to just `C` and we can simplify the
            // parameter to just `Y`.
            RecurseAndAnalyzeNode(context, qualifiedCref.Member);
        }

        private void RecurseAndAnalyzeNode(SyntaxNodeAnalysisContext context, SyntaxNode node)
        {
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            foreach (var candidate in node.DescendantNodesAndSelf(DescendIntoChildren))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            return;

            bool DescendIntoChildren(SyntaxNode n)
            {
                if (IsCandidate(n) &&
                    TrySimplifyTypeNameExpression(context.SemanticModel, n, options, out var diagnostic, cancellationToken))
                {
                    // found a match. report is and stop processing.
                    context.ReportDiagnostic(diagnostic);
                    return false;
                }

                // descend further.
                return true;
            }
        }

        internal override bool IsCandidate(SyntaxNode node)
            => node != null && s_kindsOfInterest.Contains(node.Kind());

        protected sealed override bool CanSimplifyTypeNameExpressionCore(
            SemanticModel model, SyntaxNode node, OptionSet optionSet,
            out TextSpan issueSpan, out string diagnosticId, out bool inDeclaration,
            CancellationToken cancellationToken)
        {
            return CanSimplifyTypeNameExpression(
                model, node, optionSet,
                out issueSpan, out diagnosticId, out inDeclaration,
                cancellationToken);
        }

        internal override bool CanSimplifyTypeNameExpression(
            SemanticModel model, SyntaxNode node, OptionSet optionSet,
            out TextSpan issueSpan, out string diagnosticId, out bool inDeclaration,
            CancellationToken cancellationToken)
        {
            inDeclaration = false;
            issueSpan = default;
            diagnosticId = IDEDiagnosticIds.SimplifyNamesDiagnosticId;

            if (node is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression.IsKind(SyntaxKind.ThisExpression))
            {
                // don't bother analyzing "this.Goo" expressions.  They will be analyzed by
                // the CSharpSimplifyThisOrMeDiagnosticAnalyzer.
                return false;
            }

            if (node.ContainsDiagnostics)
            {
                return false;
            }

            SyntaxNode replacementSyntax;
            if (node.IsKind(SyntaxKind.QualifiedCref, out QualifiedCrefSyntax crefSyntax))
            {
                if (!crefSyntax.TryReduceOrSimplifyExplicitName(model, out var replacement, out issueSpan, optionSet, cancellationToken))
                    return false;

                replacementSyntax = replacement;
            }
            else
            {
                var expression = (ExpressionSyntax)node;

                // in case of an As or Is expression we need to handle the binary expression, because it might be 
                // required to add parenthesis around the expression. Adding the parenthesis is done in the CSharpNameSimplifier.Rewriter
                var expressionToCheck = expression.Kind() == SyntaxKind.AsExpression || expression.Kind() == SyntaxKind.IsExpression
                    ? ((BinaryExpressionSyntax)expression).Right
                    : expression;
                if (!expressionToCheck.TryReduceOrSimplifyExplicitName(model, out var replacement, out issueSpan, optionSet, cancellationToken))
                    return false;

                replacementSyntax = replacement;
            }

            // set proper diagnostic ids.
            if (replacementSyntax.HasAnnotations(nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInDeclaration)))
            {
                inDeclaration = true;
                diagnosticId = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId;
            }
            else if (replacementSyntax.HasAnnotations(nameof(CodeStyleOptions.PreferIntrinsicPredefinedTypeKeywordInMemberAccess)))
            {
                inDeclaration = false;
                diagnosticId = IDEDiagnosticIds.PreferBuiltInOrFrameworkTypeDiagnosticId;
            }
            else if (node.Kind() == SyntaxKind.SimpleMemberAccessExpression)
            {
                diagnosticId = IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId;
            }

            return true;
        }

        protected override string GetLanguageName()
        {
            return LanguageNames.CSharp;
        }
    }
}
