// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseCompoundAssignment
{
    internal abstract class AbstractUseCompoundAssignmentDiagnosticAnalyzer<
        TSyntaxKind,
        TAssignmentSyntax,
        TBinaryExpressionSyntax>
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TAssignmentSyntax : SyntaxNode
        where TBinaryExpressionSyntax : SyntaxNode
    {
        private readonly ISyntaxFactsService _syntaxFacts;

        /// <summary>
        /// Maps from a binary expression kind (like AddExpression) to the corresponding assignment
        /// form (like AddAssignmentExpression).
        /// </summary>
        private readonly ImmutableDictionary<TSyntaxKind, TSyntaxKind> _binaryToAssignmentMap;

        /// <summary>
        /// Maps from an assignment form (like AddAssignmentExpression) to the corresponding
        /// operator type (like PlusEqualsToken).
        /// </summary>
        private readonly ImmutableDictionary<TSyntaxKind, TSyntaxKind> _assignmentToTokenMap;

        protected AbstractUseCompoundAssignmentDiagnosticAnalyzer(
            ISyntaxFactsService syntaxFacts,
            ImmutableDictionary<TSyntaxKind, TSyntaxKind> binaryToAssignmentMap,
            ImmutableDictionary<TSyntaxKind, TSyntaxKind> assignmentToTokenMap)
            : base(IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId,
                   new LocalizableResourceString(
                       nameof(FeaturesResources.Use_compound_assignment), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _syntaxFacts = syntaxFacts;
            _binaryToAssignmentMap = binaryToAssignmentMap;
            _assignmentToTokenMap = assignmentToTokenMap;

            Debug.Assert(_binaryToAssignmentMap.Count == _assignmentToTokenMap.Count);
            Debug.Assert(_binaryToAssignmentMap.Values.All(_assignmentToTokenMap.ContainsKey));
        }

        protected abstract TSyntaxKind GetKind(int rawKind);
        protected abstract TSyntaxKind GetAnalysisKind();

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeAssignment, GetAnalysisKind());

        private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var assignment = (TAssignmentSyntax)context.Node;

            var options = context.Options;
            var syntaxTree = assignment.SyntaxTree;
            var optionSet = options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferCompoundAssignment, assignment.Language);
            if (!option.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            _syntaxFacts.GetPartsOfAssignmentStatement(assignment, 
                out var assignmentLeft, out var assignmentToken, out var assignmentRight);

            // has to be of the form:  a = b op c
            // op has to be a form we could convert into op=
            if (!(assignmentRight is TBinaryExpressionSyntax binaryExpression))
            {
                return;
            }

            var binaryKind = GetKind(binaryExpression.RawKind);
            if (!_binaryToAssignmentMap.ContainsKey(binaryKind))
            {
                return;
            }

            _syntaxFacts.GetPartsOfBinaryExpression(binaryExpression,
                out var binaryLeft, out _);
            
            // has to be of the form:   expr = expr op ...
            if (!_syntaxFacts.AreEquivalent(assignmentLeft, binaryLeft))
            {
                return;
            }

            // Syntactically looks promising.  But we can only safely do this if 'expr'
            // is side-effect-free since we will be changing the number of times it is
            // execute from twice to once.
            var semanticModel = context.SemanticModel;
            if (!IsSideEffectFree(assignmentLeft, semanticModel, cancellationToken))
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

        private bool IsSideEffectFree(
            SyntaxNode expr, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (expr == null)
            {
                return false;
            }

            // it basically has to be of the form "a.b.c", where all components are locals,
            // parameters or fields.  Basically, nothing that can cause arbitrary user code
            // execution when being evaluated by the compiler.

            if (_syntaxFacts.IsThisExpression(expr) ||
                _syntaxFacts.IsBaseExpression(expr))
            {
                // Referencing this/base like  this.a.b.c causes no side effects itself.
                return true;
            }

            if (_syntaxFacts.IsIdentifierName(expr))
            {
                return IsSideEffectFreeSymbol(expr, semanticModel, cancellationToken);
            }

            if (_syntaxFacts.IsParenthesizedExpression(expr))
            {
                _syntaxFacts.GetPartsOfParenthesizedExpression(expr,
                    out _, out var expression, out _);

                return IsSideEffectFree(expression, semanticModel, cancellationToken);
            }

            if (_syntaxFacts.IsSimpleMemberAccessExpression(expr))
            {
                _syntaxFacts.GetPartsOfMemberAccessExpression(expr,
                    out var expression, out _);
                return IsSideEffectFree(expression, semanticModel, cancellationToken) &&
                       IsSideEffectFreeSymbol(expr, semanticModel, cancellationToken);
            }

            if (_syntaxFacts.IsConditionalAccessExpression(expr))
            {
                _syntaxFacts.GetPartsOfConditionalAccessExpression(expr,
                    out var expression, out var whenNotNull);
                return IsSideEffectFree(expression, semanticModel, cancellationToken) &&
                       IsSideEffectFree(whenNotNull, semanticModel, cancellationToken);
            }

            // Something we don't explicitly handle.  Assume this may have side effects.
            return false;
        }

        private bool IsSideEffectFreeSymbol(
            SyntaxNode expr, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expr, cancellationToken);
            if (symbolInfo.CandidateSymbols.Length > 0 ||
                symbolInfo.Symbol == null)
            {
                // couldn't bind successfully, assume that this might have side-effects.
                return false;
            }

            var symbol = symbolInfo.Symbol;
            switch (symbol.Kind)
            {
                case SymbolKind.Namespace:
                case SymbolKind.NamedType:
                case SymbolKind.Field:
                case SymbolKind.Parameter:
                case SymbolKind.Local:
                    return true;
            }

            return false;
        }
    }
}
