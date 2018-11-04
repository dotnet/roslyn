// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.RemoveUnnecessaryParentheses
{
    internal abstract class AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer<
        TLanguageKindEnum,
        TConstructSyntax>
        : AbstractParenthesesDiagnosticAnalyzer
        where TLanguageKindEnum : struct
        where TConstructSyntax : SyntaxNode
    {
        private readonly string _kind;

        protected AbstractRemoveUnnecessaryParenthesesDiagnosticAnalyzer(string kind)
            : base(IDEDiagnosticIds.RemoveUnnecessaryParenthesesDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Remove_unnecessary_parentheses), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Parentheses_can_be_removed), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _kind = kind;
        }

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
  
        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public sealed override bool OpenFileOnly(Workspace workspace)
            => false;

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxNodeKind());

        protected abstract TLanguageKindEnum GetSyntaxNodeKind();
        protected abstract bool CanRemoveParentheses(
            TConstructSyntax construct, SemanticModel semanticModel,
            out PrecedenceKind precedence, out bool clarifiesPrecedence);
        protected abstract bool ShouldNotRemoveParentheses(TConstructSyntax construct, PrecedenceKind precedence);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var syntaxTree = context.SemanticModel.SyntaxTree;
            var cancellationTokan = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationTokan).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var construct = (TConstructSyntax)context.Node;

            if (!CanRemoveParentheses(construct, context.SemanticModel,
                    out var precedence, out var clarifiesPrecedence))
            {
                return;
            }

            if (ShouldNotRemoveParentheses(construct, precedence))
            {
                return;
            }

            var option = GetLanguageOption(precedence);
            var preference = optionSet.GetOption(option, construct.Language);

            if (preference.Notification.Severity == ReportDiagnostic.Suppress)
            {
                // User doesn't care about these parens.  So nothing for us to do.
                return;
            }

            if (preference.Value == ParenthesesPreference.AlwaysForClarity &&
                clarifiesPrecedence)
            {
                // User wants these parens if they clarify precedence, and these parens
                // clarify precedence.  So keep these around.
                return;
            }

            // either they don't want unnecessary parentheses, or they want them only for
            // clarification purposes and this does not make things clear.
            Debug.Assert(preference.Value == ParenthesesPreference.NeverIfUnnecessary ||
                         !clarifiesPrecedence);

            var severity = preference.Notification.Severity;

            var additionalLocations = ImmutableArray.Create(construct.GetLocation());
            var properties = ImmutableDictionary<string, string>.Empty.Add("Kind", _kind);

            context.ReportDiagnostic(DiagnosticHelper.Create(
                UnnecessaryWithSuggestionDescriptor,
                construct.GetFirstToken().GetLocation(),
                severity,
                additionalLocations,
                properties));

            context.ReportDiagnostic(Diagnostic.Create(
                UnnecessaryWithoutSuggestionDescriptor,
                construct.GetLastToken().GetLocation(), additionalLocations));
        }
    }
}
