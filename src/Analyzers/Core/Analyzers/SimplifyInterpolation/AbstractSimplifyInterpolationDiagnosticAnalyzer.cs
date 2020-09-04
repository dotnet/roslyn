// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.SimplifyInterpolation
{
    internal abstract class AbstractSimplifyInterpolationDiagnosticAnalyzer<
        TInterpolationSyntax,
        TExpressionSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TInterpolationSyntax : SyntaxNode
        where TExpressionSyntax : SyntaxNode
    {
        protected AbstractSimplifyInterpolationDiagnosticAnalyzer()
           : base(IDEDiagnosticIds.SimplifyInterpolationId,
                  CodeStyleOptions2.PreferSimplifiedInterpolation,
                  new LocalizableResourceString(nameof(AnalyzersResources.Simplify_interpolation), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                  new LocalizableResourceString(nameof(AnalyzersResources.Interpolation_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                  isUnnecessary: true)
        {
        }

        protected abstract IVirtualCharService GetVirtualCharService();

        protected abstract ISyntaxFacts GetSyntaxFacts();

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterOperationAction(AnalyzeInterpolation, OperationKind.Interpolation);

        private void AnalyzeInterpolation(OperationAnalysisContext context)
        {
            var interpolation = (IInterpolationOperation)context.Operation;

            var syntaxTree = interpolation.Syntax.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetAnalyzerOptionSet(syntaxTree, cancellationToken);
            if (optionSet == null)
            {
                return;
            }

            var language = interpolation.Language;
            var option = optionSet.GetOption(CodeStyleOptions2.PreferSimplifiedInterpolation, language);
            if (!option.Value)
            {
                // No point in analyzing if the option is off.
                return;
            }

            Helpers.UnwrapInterpolation<TInterpolationSyntax, TExpressionSyntax>(
                GetVirtualCharService(), GetSyntaxFacts(), interpolation, out _, out var alignment, out _,
                out var formatString, out var unnecessaryLocations);

            if (alignment == null && formatString == null)
            {
                return;
            }

            // The diagnostic itself fades the first unnecessary location, and the remaining locations are passed as
            // additional unnecessary locations.
            var firstUnnecessaryLocation = unnecessaryLocations[0];
            var remainingUnnecessaryLocations = unnecessaryLocations.RemoveAt(0);

            context.ReportDiagnostic(DiagnosticHelper.CreateWithLocationTags(
                Descriptor,
                firstUnnecessaryLocation,
                option.Notification.Severity,
                additionalLocations: ImmutableArray.Create(interpolation.Syntax.GetLocation()),
                additionalUnnecessaryLocations: remainingUnnecessaryLocations));
        }
    }
}
