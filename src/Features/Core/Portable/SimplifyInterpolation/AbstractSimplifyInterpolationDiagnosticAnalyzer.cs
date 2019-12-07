// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.SimplifyInterpolation
{
    internal class AbstractSimplifyInterpolationDiagnosticAnalyzer<
        TInterpolationSyntax,
        TExpressionSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TInterpolationSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        protected AbstractSimplifyInterpolationDiagnosticAnalyzer()
           : base(IDEDiagnosticIds.SimplifyInterpolationId,
                  CodeStyleOptions.PreferSimplifiedInterpolation,
                  new LocalizableResourceString(nameof(FeaturesResources.Simplify_interpolation), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                  new LocalizableResourceString(nameof(FeaturesResources.Interpolation_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterOperationAction(AnalyzeInterpolation, OperationKind.Interpolation);
        }

        private void AnalyzeInterpolation(OperationAnalysisContext context)
        {
            var interpolation = (IInterpolationOperation)context.Operation;

            var syntaxTree = interpolation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var language = interpolation.Language;
            var option = optionSet.GetOption(CodeStyleOptions.PreferSimplifiedInterpolation, language);
            if (!option.Value)
            {
                // No point in analyzing if the option is off.
                return;
            }

            Helpers.UnwrapInterpolation<TInterpolationSyntax, TExpressionSyntax>(
                interpolation, out _, out var alignment, out _, out var formatString);

            if (alignment == null && formatString == null)
            {
                return;
            }

            var locations = ImmutableArray.Create(interpolation.Syntax.GetLocation());

            var severity = option.Notification.Severity;
            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                interpolation.Expression.Syntax.GetFirstToken().GetLocation(),
                severity,
                additionalLocations: locations,
                properties: null));
        }
    }
}
