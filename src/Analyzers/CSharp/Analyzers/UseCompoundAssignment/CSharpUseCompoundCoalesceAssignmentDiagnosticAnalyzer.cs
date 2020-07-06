// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.LanguageServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseCompoundAssignment;

namespace Microsoft.CodeAnalysis.CSharp.UseCompoundAssignment
{
    /// <summary>
    /// Looks for expressions of the form <c>expr ?? (expr = value)</c> and converts it to
    /// <c>expr ??= value</c>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseCompoundCoalesceAssignmentDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseCompoundCoalesceAssignmentDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseCoalesceCompoundAssignmentDiagnosticId,
                   CodeStyleOptions2.PreferCompoundAssignment,
                   new LocalizableResourceString(nameof(AnalyzersResources.Use_compound_assignment), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeCoalesceExpression, SyntaxKind.CoalesceExpression);

        private void AnalyzeCoalesceExpression(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var semanticModel = context.SemanticModel;
            var options = (CSharpParseOptions)semanticModel.SyntaxTree.Options;
            if (options.LanguageVersion < LanguageVersion.CSharp8)
                return;

            var coalesceExpression = (BinaryExpressionSyntax)context.Node;

            var option = context.GetOption(CodeStyleOptions2.PreferCompoundAssignment, coalesceExpression.Language);

            // Bail immediately if the user has disabled this feature.
            if (!option.Value)
                return;

            var coalesceLeft = coalesceExpression.Left;
            var coalesceRight = coalesceExpression.Right;

            if (!(coalesceRight is ParenthesizedExpressionSyntax parenthesizedExpr))
                return;

            if (!(parenthesizedExpr.Expression is AssignmentExpressionSyntax assignment))
                return;

            if (assignment.Kind() != SyntaxKind.SimpleAssignmentExpression)
                return;

            // have    x ?? (y = z)
            // ensure that 'x' and 'y' are suitably equivalent.
            var syntaxFacts = CSharpSyntaxFacts.Instance;
            if (!syntaxFacts.AreEquivalent(coalesceLeft, assignment.Left))
                return;

            // Syntactically looks promising.  But we can only safely do this if 'expr'
            // is side-effect-free since we will be changing the number of times it is
            // executed from twice to once.
            if (!UseCompoundAssignmentUtilities.IsSideEffectFree(
                    syntaxFacts, coalesceLeft, semanticModel, cancellationToken))
            {
                return;
            }

            // Good match.
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                coalesceExpression.OperatorToken.GetLocation(),
                option.Notification.Severity,
                additionalLocations: ImmutableArray.Create(coalesceExpression.GetLocation()),
                properties: null));
        }
    }
}
