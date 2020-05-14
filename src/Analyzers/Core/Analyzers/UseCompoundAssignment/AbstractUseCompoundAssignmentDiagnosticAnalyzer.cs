// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseCompoundAssignment
{
    internal abstract class AbstractUseCompoundAssignmentDiagnosticAnalyzer<
        TSyntaxKind,
        TAssignmentSyntax,
        TBinaryExpressionSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TAssignmentSyntax : SyntaxNode
        where TBinaryExpressionSyntax : SyntaxNode
    {
        private readonly ISyntaxFacts _syntaxFacts;

        /// <summary>
        /// Maps from a binary expression kind (like AddExpression) to the corresponding assignment
        /// form (like AddAssignmentExpression).
        /// </summary>
        private readonly ImmutableDictionary<TSyntaxKind, TSyntaxKind> _binaryToAssignmentMap;

        protected AbstractUseCompoundAssignmentDiagnosticAnalyzer(
            ISyntaxFacts syntaxFacts,
            ImmutableArray<(TSyntaxKind exprKind, TSyntaxKind assignmentKind, TSyntaxKind tokenKind)> kinds)
            : base(IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId,
                   CodeStyleOptions2.PreferCompoundAssignment,
                   new LocalizableResourceString(
                       nameof(AnalyzersResources.Use_compound_assignment), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
            _syntaxFacts = syntaxFacts;
            UseCompoundAssignmentUtilities.GenerateMaps(kinds, out _binaryToAssignmentMap, out _);
        }

        protected abstract TSyntaxKind GetAnalysisKind();
        protected abstract bool IsSupported(TSyntaxKind assignmentKind, ParseOptions options);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeAssignment, GetAnalysisKind());

        private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (TAssignmentSyntax)context.Node;
            var cancellationToken = context.CancellationToken;

            var syntaxTree = assignment.SyntaxTree;
            var option = context.GetOption(CodeStyleOptions2.PreferCompoundAssignment, assignment.Language);
            if (!option.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            _syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(assignment,
                out var assignmentLeft, out var assignmentToken, out var assignmentRight);

            assignmentRight = _syntaxFacts.WalkDownParentheses(assignmentRight);

            // has to be of the form:  a = b op c
            // op has to be a form we could convert into op=
            if (!(assignmentRight is TBinaryExpressionSyntax binaryExpression))
            {
                return;
            }

            var binaryKind = _syntaxFacts.SyntaxKinds.Convert<TSyntaxKind>(binaryExpression.RawKind);
            if (!_binaryToAssignmentMap.ContainsKey(binaryKind))
            {
                return;
            }

            // Requires at least C# 8 for Coalesce compound expression
            if (!IsSupported(binaryKind, syntaxTree.Options))
            {
                return;
            }

            _syntaxFacts.GetPartsOfBinaryExpression(binaryExpression,
                out var binaryLeft, out var binaryRight);

            // has to be of the form:   expr = expr op ...
            if (!_syntaxFacts.AreEquivalent(assignmentLeft, binaryLeft))
            {
                return;
            }

            // Don't offer if this is `x = x + 1` inside an obj initializer like:
            // `new Point { x = x + 1 }`
            if (_syntaxFacts.IsObjectInitializerNamedAssignmentIdentifier(assignmentLeft))
            {
                return;
            }

            // Don't offer if this is `x = x ?? throw new Exception()`
            if (_syntaxFacts.IsThrowExpression(binaryRight))
            {
                return;
            }

            // Syntactically looks promising.  But we can only safely do this if 'expr'
            // is side-effect-free since we will be changing the number of times it is
            // executed from twice to once.
            var semanticModel = context.SemanticModel;
            if (!UseCompoundAssignmentUtilities.IsSideEffectFree(
                    _syntaxFacts, assignmentLeft, semanticModel, cancellationToken))
            {
                return;
            }

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                assignmentToken.GetLocation(),
                option.Notification.Severity,
                additionalLocations: ImmutableArray.Create(assignment.GetLocation()),
                properties: null));
        }
    }
}
