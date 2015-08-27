// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Diagnostics.SimplifyTypeNames;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Diagnostics.SimplifyTypeNames
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpSimplifyTypeNamesDiagnosticAnalyzer : SimplifyTypeNamesDiagnosticAnalyzerBase<SyntaxKind>
    {
        private static readonly ImmutableArray<SyntaxKind> s_kindsOfInterest = ImmutableArray.Create(SyntaxKind.QualifiedName,
            SyntaxKind.AliasQualifiedName,
            SyntaxKind.GenericName,
            SyntaxKind.IdentifierName,
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxKind.QualifiedCref);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.RegisterSyntaxNodeAction(AnalyzeNode, s_kindsOfInterest.ToArray());
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
            Func<SyntaxNode, bool> descendIntoChildren = n =>
            {
                if (!IsRegularCandidate(n) ||
                    !TrySimplifyTypeNameExpression(context.SemanticModel, n, context.Options, out diagnostic, context.CancellationToken))
                {
                    return true;
                }

                context.ReportDiagnostic(diagnostic);
                return false;
            };

            // find regular node first - search from top to down. once found one, don't get into its children
            foreach (var candidate in context.Node.DescendantNodesAndSelf(descendIntoChildren))
            {
                context.CancellationToken.ThrowIfCancellationRequested();
            }

            // now search structure trivia
            foreach (var candidate in context.Node.DescendantNodesAndSelf(descendIntoChildren: n => !IsCrefCandidate(n), descendIntoTrivia: true))
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                if (IsCrefCandidate(candidate) &&
                    TrySimplifyTypeNameExpression(context.SemanticModel, candidate, context.Options, out diagnostic, context.CancellationToken))
                {
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        internal static bool IsCandidate(SyntaxNode node)
        {
            return IsRegularCandidate(node) || IsCrefCandidate(node);
        }

        private static bool IsRegularCandidate(SyntaxNode node)
        {
            return node != null && s_kindsOfInterest.Contains(node.Kind());
        }

        private static bool IsCrefCandidate(SyntaxNode node)
        {
            return node is QualifiedCrefSyntax;
        }

        protected sealed override bool CanSimplifyTypeNameExpressionCore(SemanticModel model, SyntaxNode node, OptionSet optionSet, out TextSpan issueSpan, out string diagnosticId, CancellationToken cancellationToken)
        {
            return CanSimplifyTypeNameExpression(model, node, optionSet, out issueSpan, out diagnosticId, cancellationToken);
        }

        internal static bool CanSimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, OptionSet optionSet, out TextSpan issueSpan, out string diagnosticId, CancellationToken cancellationToken)
        {
            issueSpan = default(TextSpan);
            diagnosticId = IDEDiagnosticIds.SimplifyNamesDiagnosticId;

            // For Crefs, currently only Qualified Crefs needs to be handled separately
            if (node.Kind() == SyntaxKind.QualifiedCref)
            {
                if (node.ContainsDiagnostics)
                {
                    return false;
                }

                var crefSyntax = (CrefSyntax)node;

                CrefSyntax replacementNode;
                if (!crefSyntax.TryReduceOrSimplifyExplicitName(model, out replacementNode, out issueSpan, optionSet, cancellationToken))
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

                ExpressionSyntax replacementSyntax;
                if (!expressionToCheck.TryReduceOrSimplifyExplicitName(model, out replacementSyntax, out issueSpan, optionSet, cancellationToken))
                {
                    return false;
                }

                if (expression.Kind() == SyntaxKind.SimpleMemberAccessExpression)
                {
                    var memberAccess = (MemberAccessExpressionSyntax)expression;
                    diagnosticId = memberAccess.Expression.Kind() == SyntaxKind.ThisExpression ?
                        IDEDiagnosticIds.SimplifyThisOrMeDiagnosticId :
                        IDEDiagnosticIds.SimplifyMemberAccessDiagnosticId;
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
