// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.OrderModifiers
{
    internal abstract class AbstractOrderModifiersDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly Option<CodeStyleOption<string>> _option;
        private readonly AbstractOrderModifiersHelpers _helpers;

        protected AbstractOrderModifiersDiagnosticAnalyzer(
            ISyntaxFactsService syntaxFacts,
            Option<CodeStyleOption<string>> option,
            AbstractOrderModifiersHelpers helpers,
            string language)
            : base(IDEDiagnosticIds.OrderModifiersDiagnosticId,
                   option,
                   language,
                   new LocalizableResourceString(nameof(FeaturesResources.Order_modifiers), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Modifiers_are_not_ordered), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _syntaxFacts = syntaxFacts;
            _option = option;
            _helpers = helpers;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var syntaxTree = context.Tree;
            var root = syntaxTree.GetRoot(cancellationToken);

            var option = context.Options.GetOptionAsync(_option, syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (!_helpers.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
            {
                return;
            }

            Recurse(context, preferredOrder, option.Notification.Severity, root);
        }

        protected abstract void Recurse(
            SyntaxTreeAnalysisContext context,
            Dictionary<int, int> preferredOrder,
            ReportDiagnostic severity,
            SyntaxNode root);

        protected void CheckModifiers(
            SyntaxTreeAnalysisContext context,
            Dictionary<int, int> preferredOrder,
            ReportDiagnostic severity,
            SyntaxNode memberDeclaration)
        {
            var modifiers = _syntaxFacts.GetModifiers(memberDeclaration);
            if (!AbstractOrderModifiersHelpers.IsOrdered(preferredOrder, modifiers))
            {
                if (severity.WithDefaultSeverity(DiagnosticSeverity.Hidden) == ReportDiagnostic.Hidden)
                {
                    // If the severity is hidden, put the marker on all the modifiers so that the
                    // user can bring up the fix anywhere in the modifier list.
                    context.ReportDiagnostic(
                        Diagnostic.Create(Descriptor, context.Tree.GetLocation(
                            TextSpan.FromBounds(modifiers.First().SpanStart, modifiers.Last().Span.End))));
                }
                else
                {
                    // If the Severity is not hidden, then just put the user visible portion on the
                    // first token.  That way we don't 
                    context.ReportDiagnostic(
                        DiagnosticHelper.Create(Descriptor, modifiers.First().GetLocation(), severity, additionalLocations: null, properties: null));
                }
            }
        }
    }
}
