// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionDiagnosticAnalyzer<TIfStatementSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TIfStatementSyntax : SyntaxNode
    {
        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected AbstractUseConditionalExpressionDiagnosticAnalyzer(
            string descriptorId,
            EnforceOnBuild enforceOnBuild,
            LocalizableResourceString message,
            PerLanguageOption2<CodeStyleOption2<bool>> option)
            : base(descriptorId,
                   enforceOnBuild,
                   option,
                   new LocalizableResourceString(nameof(AnalyzersResources.Convert_to_conditional_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   message)
        {
        }

        protected abstract ISyntaxFacts GetSyntaxFacts();
        protected abstract (bool matched, bool canSimplify) TryMatchPattern(IConditionalOperation ifOperation, ISymbol containingSymbol);
        protected abstract CodeStyleOption2<bool> GetStylePreference(OperationAnalysisContext context);

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, OperationKind.Conditional);

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var ifOperation = (IConditionalOperation)context.Operation;
            if (ifOperation.Syntax is not TIfStatementSyntax ifStatement)
                return;

            var option = GetStylePreference(context);
            if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
                return;

            var (matched, canSimplify) = TryMatchPattern(ifOperation, context.ContainingSymbol);
            if (!matched)
                return;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                ifStatement.GetFirstToken().GetLocation(),
                option.Notification,
                additionalLocations: ImmutableArray.Create(ifStatement.GetLocation()),
                properties: canSimplify ? UseConditionalExpressionHelpers.CanSimplifyProperties : null));
        }
    }
}
