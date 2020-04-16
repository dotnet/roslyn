// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UseConditionalExpression
{
    internal abstract class AbstractUseConditionalExpressionDiagnosticAnalyzer<
        TIfStatementSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TIfStatementSyntax : SyntaxNode
    {
        private readonly PerLanguageOption2<CodeStyleOption2<bool>> _option;

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected AbstractUseConditionalExpressionDiagnosticAnalyzer(
            string descriptorId,
            LocalizableResourceString message,
            PerLanguageOption2<CodeStyleOption2<bool>> option)
            : base(descriptorId,
                   option,
                   new LocalizableResourceString(nameof(AnalyzersResources.Convert_to_conditional_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   message)
        {
            _option = option;
        }

        protected abstract ISyntaxFacts GetSyntaxFacts();
        protected abstract bool TryMatchPattern(IConditionalOperation ifOperation, ISymbol containingSymbol);

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeOperation, OperationKind.Conditional);

        private void AnalyzeOperation(OperationAnalysisContext context)
        {
            var ifOperation = (IConditionalOperation)context.Operation;
            if (!(ifOperation.Syntax is TIfStatementSyntax ifStatement))
            {
                return;
            }

            var language = ifStatement.Language;

            var option = context.GetOption(_option, language);
            if (!option.Value)
            {
                return;
            }

            if (!TryMatchPattern(ifOperation, context.ContainingSymbol))
            {
                return;
            }

            var additionalLocations = ImmutableArray.Create(ifStatement.GetLocation());
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                ifStatement.GetFirstToken().GetLocation(),
                option.Notification.Severity,
                additionalLocations,
                properties: null));
        }
    }
}
