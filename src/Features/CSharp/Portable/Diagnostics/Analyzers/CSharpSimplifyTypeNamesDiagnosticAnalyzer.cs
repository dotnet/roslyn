// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            if (context.Node.Ancestors(ascendOutOfTrivia: false).Any(n => !n.IsKind(SyntaxKind.QualifiedCref) && s_kindsOfInterest.Contains(n.Kind())))
            {
                // Bail out early because we have already simplified an ancestor of this node (except in the QualifiedCref case).
                // We need to keep going in case this node is under a QualifiedCref because it is possible to have multiple simplifications within the same QualifiedCref.
                // For example, consider <see cref="A.M(Nullable{int})"/>. The 'A.M(Nullable{int})' here is represented by a single QualifiedCref node in the syntax tree.
                // It is possible to have a simplification to remove the 'A.' qualification for the QualifiedCref itself as well as another simplification to change 'Nullable{int}'
                // to 'int?' in the GenericName for the 'Nullable{T}' that is nested inside this QualifiedCref. We need to keep going so that the latter simplification can be
                // made available.
                return;
            }

            Diagnostic diagnostic;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;
            bool descendIntoChildren(SyntaxNode n)
            {
                if (!IsRegularCandidate(n) ||
                    !TrySimplifyTypeNameExpression(context.SemanticModel, n, options, out diagnostic, cancellationToken))
                {
                    return true;
                }

                context.ReportDiagnostic(diagnostic);
                return false;
            }

            // find regular node first - search from top to down. once found one, don't get into its children
            foreach (var candidate in context.Node.DescendantNodesAndSelf(descendIntoChildren))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            // now search structure trivia
            foreach (var candidate in context.Node.DescendantNodesAndSelf(descendIntoChildren: n => !IsCrefCandidate(n), descendIntoTrivia: true))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsCrefCandidate(candidate) &&
                    TrySimplifyTypeNameExpression(context.SemanticModel, candidate, options, out diagnostic, cancellationToken))
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        internal override bool IsCandidate(SyntaxNode node)
            => IsRegularCandidate(node) || IsCrefCandidate(node);

        private static bool IsRegularCandidate(SyntaxNode node)
        {
            return node != null && s_kindsOfInterest.Contains(node.Kind());
        }

        private static bool IsCrefCandidate(SyntaxNode node)
        {
            return node is QualifiedCrefSyntax;
        }

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

            // For Crefs, currently only Qualified Crefs needs to be handled separately
            if (node.Kind() == SyntaxKind.QualifiedCref)
            {
                if (node.ContainsDiagnostics)
                {
                    return false;
                }

                var crefSyntax = (CrefSyntax)node;
                if (!crefSyntax.TryReduceOrSimplifyExplicitName(model, out var replacementNode, out issueSpan, optionSet, cancellationToken))
                {
                    return false;
                }
            }
            else
            {
                var expression = (ExpressionSyntax)node;
                if (expression.ContainsDiagnostics)
                {
                    return false;
                }

                // in case of an As or Is expression we need to handle the binary expression, because it might be 
                // required to add parenthesis around the expression. Adding the parenthesis is done in the CSharpNameSimplifier.Rewriter
                var expressionToCheck = expression.Kind() == SyntaxKind.AsExpression || expression.Kind() == SyntaxKind.IsExpression
                    ? ((BinaryExpressionSyntax)expression).Right
                    : expression;
                if (!expressionToCheck.TryReduceOrSimplifyExplicitName(model, out var replacementSyntax, out issueSpan, optionSet, cancellationToken))
                {
                    return false;
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
                else if (expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                {
                    diagnosticId = IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId;
                }
            }

            return true;
        }

        protected override string GetLanguageName()
        {
            return LanguageNames.CSharp;
        }
    }
}
