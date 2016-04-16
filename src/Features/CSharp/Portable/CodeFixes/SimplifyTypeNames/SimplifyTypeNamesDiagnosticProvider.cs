// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.SimplifyTypeNames
{
    [ExportDiagnosticProvider(PredefinedDiagnosticProviderNames.SimplifyTypeNames, LanguageNames.CSharp)]
    internal sealed class SimplifyTypeNamesDiagnosticProvider : ScopedDiagnosticProvider
    {
        internal const string DiagnosticId = "SimplifyTypeNames";
        internal static readonly DiagnosticDescriptor DiagnosticMD = new DiagnosticDescriptor(DiagnosticId,
                                                                                              DiagnosticKind.Unnecessary,
                                                                                              CSharpFeaturesResources.SimplifyTypeName,
                                                                                              CSharpFeaturesResources.NameCanBeSimplified,
                                                                                              "Internal",
                                                                                              DiagnosticSeverity.None);

        public override IEnumerable<DiagnosticDescriptor> GetSupportedDiagnostics()
        {
            return SpecializedCollections.SingletonEnumerable(DiagnosticMD);
        }

        protected override async Task<IEnumerable<Diagnostic>> GetDiagnosticsAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            if (!document.IsOpen())
            {
                return null;
            }

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span);

            List<Diagnostic> diagnostics = null;
            diagnostics = ProcessNode(document, model, node, diagnostics, cancellationToken);
            return diagnostics;
        }

        private List<Diagnostic> ProcessNode(Document document, SemanticModel model, SyntaxNode node, List<Diagnostic> result, CancellationToken cancellationToken)
        {
            Diagnostic diagnostic;
            Func<SyntaxNode, bool> descendIntoChildren = n =>
            {
                if (!IsRegularCandidate(n) ||
                    !TrySimplifyTypeNameExpression(document, model, n, out diagnostic, cancellationToken))
                {
                    return true;
                }

                result = result ?? new List<Diagnostic>();
                result.Add(diagnostic);
                return false;
            };

            // find regular node first - search from top to down. once found one, don't get into its children
            foreach (var candidate in node.DescendantNodesAndSelf(descendIntoChildren))
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            // now search structure trivia
            foreach (var candidate in node.DescendantNodesAndSelf(descendIntoChildren: n => !IsCrefCandidate(n), descendIntoTrivia: true))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (IsCrefCandidate(candidate) &&
                    TrySimplifyTypeNameExpression(document, model, candidate, out diagnostic, cancellationToken))
                {
                    result = result ?? new List<Diagnostic>();
                    result.Add(diagnostic);
                }
            }

            return result;
        }

        private bool TrySimplifyTypeNameExpression(
            Document document, SemanticModel model, SyntaxNode node, out Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            diagnostic = default(Diagnostic);

            var optionSet = document.Project.Solution.Workspace.GetOptions();

            TextSpan issueSpan;
            if (!CanSimplifyTypeNameExpression(model, node, optionSet, out issueSpan, cancellationToken))
            {
                return false;
            }

            if (model.SyntaxTree.OverlapsHiddenPosition(issueSpan, cancellationToken))
            {
                return false;
            }

            var tree = model.SyntaxTree;

            diagnostic = Diagnostic.Create(DiagnosticMD, tree.GetLocation(issueSpan));
            return true;
        }

        internal static bool IsCandidate(SyntaxNode node)
        {
            return IsRegularCandidate(node) || IsCrefCandidate(node);
        }

        private static bool IsRegularCandidate(SyntaxNode node)
        {
            return node is QualifiedNameSyntax ||
                   node is AliasQualifiedNameSyntax ||
                   node is MemberAccessExpressionSyntax ||
                   node is IdentifierNameSyntax ||
                   node is GenericNameSyntax ||
                   node is BinaryExpressionSyntax;
        }

        private static bool IsCrefCandidate(SyntaxNode node)
        {
            return node is QualifiedCrefSyntax;
        }

        internal static bool CanSimplifyTypeNameExpression(SemanticModel model, SyntaxNode node, OptionSet optionSet, out TextSpan issueSpan, CancellationToken cancellationToken)
        {
            issueSpan = default(TextSpan);

            // For Crefs, currently only Qualified Crefs needs to be handled separately
            if (node.Kind() == SyntaxKind.QualifiedCref)
            {
                if (((QualifiedCrefSyntax)node).ContainsDiagnostics)
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
            }

            return true;
        }
    }
}
