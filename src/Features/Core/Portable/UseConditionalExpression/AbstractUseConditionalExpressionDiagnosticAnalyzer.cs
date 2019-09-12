// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly PerLanguageOption<CodeStyleOption<bool>> _option;

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected AbstractUseConditionalExpressionDiagnosticAnalyzer(
            string descriptorId,
            LocalizableResourceString message,
            PerLanguageOption<CodeStyleOption<bool>> option)
            : base(descriptorId,
                   option,
                   new LocalizableResourceString(nameof(FeaturesResources.Convert_to_conditional_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   message)
        {
            _option = option;
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract bool TryMatchPattern(IConditionalOperation ifOperation);

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
            var syntaxTree = ifStatement.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            var optionSet = context.Options.GetAnalyzerOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(_option, language);
            if (!option.Value)
            {
                return;
            }

            if (!TryMatchPattern(ifOperation))
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
