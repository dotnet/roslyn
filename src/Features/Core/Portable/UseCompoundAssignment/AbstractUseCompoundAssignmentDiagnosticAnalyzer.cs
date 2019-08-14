// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
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
            ImmutableArray<(TSyntaxKind exprKind, TSyntaxKind assignmentKind, TSyntaxKind tokenKind)> kinds)
            : base(IDEDiagnosticIds.UseCompoundAssignmentDiagnosticId,
                   CodeStyleOptions.PreferCompoundAssignment,
                   new LocalizableResourceString(
                       nameof(FeaturesResources.Use_compound_assignment), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _syntaxFacts = syntaxFacts;
            Utilities.GenerateMaps(kinds, out _binaryToAssignmentMap, out _assignmentToTokenMap);
        }

        protected abstract TSyntaxKind GetKind(int rawKind);
        protected abstract TSyntaxKind GetAnalysisKind();
        protected abstract bool IsSupported(TSyntaxKind assignmentKind, ParseOptions options);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeAssignment, GetAnalysisKind());

        private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var assignment = (TAssignmentSyntax)context.Node;

            var syntaxTree = assignment.SyntaxTree;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
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

            _syntaxFacts.GetPartsOfAssignmentExpressionOrStatement(assignment,
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
            if (!IsSideEffectFree(assignmentLeft, semanticModel, isTopLevel: true, cancellationToken))
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
            SyntaxNode expr, SemanticModel semanticModel, bool isTopLevel, CancellationToken cancellationToken)
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
                return IsSideEffectFreeSymbol(expr, semanticModel, isTopLevel, cancellationToken);
            }

            if (_syntaxFacts.IsParenthesizedExpression(expr))
            {
                _syntaxFacts.GetPartsOfParenthesizedExpression(expr,
                    out _, out var expression, out _);

                return IsSideEffectFree(expression, semanticModel, isTopLevel, cancellationToken);
            }

            if (_syntaxFacts.IsSimpleMemberAccessExpression(expr))
            {
                _syntaxFacts.GetPartsOfMemberAccessExpression(expr,
                    out var subExpr, out _);
                return IsSideEffectFree(subExpr, semanticModel, isTopLevel: false, cancellationToken) &&
                       IsSideEffectFreeSymbol(expr, semanticModel, isTopLevel, cancellationToken);
            }

            if (_syntaxFacts.IsConditionalAccessExpression(expr))
            {
                _syntaxFacts.GetPartsOfConditionalAccessExpression(expr,
                    out var expression, out var whenNotNull);
                return IsSideEffectFree(expression, semanticModel, isTopLevel: false, cancellationToken) &&
                       IsSideEffectFree(whenNotNull, semanticModel, isTopLevel: false, cancellationToken);
            }

            // Something we don't explicitly handle.  Assume this may have side effects.
            return false;
        }

        private static bool IsSideEffectFreeSymbol(
            SyntaxNode expr, SemanticModel semanticModel, bool isTopLevel, CancellationToken cancellationToken)
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

            if (symbol.Kind == SymbolKind.Property && isTopLevel)
            {
                // If we have `this.Prop = this.Prop * 2`, then that's just a single read/write of
                // the prop and we can safely make that `this.Prop *= 2` (since it will still be a
                // single read/write).  However, if we had `this.prop.x = this.prop.x * 2`, then
                // that's multiple reads of `this.prop`, and it's not safe to convert that to
                // `this.prop.x *= 2` in the case where calling 'prop' may have side effects.
                //
                // Note, this doesn't apply if the property is a ref-property.  In that case, we'd
                // go from a read and a write to to just a read (and a write to it's returned ref
                // value).
                var property = (IPropertySymbol)symbol;
                if (property.RefKind == RefKind.None)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
