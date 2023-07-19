// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.OrderModifiers
{
    internal abstract class AbstractOrderModifiersDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly AbstractOrderModifiersHelpers _helpers;

        protected AbstractOrderModifiersDiagnosticAnalyzer(
            ISyntaxFacts syntaxFacts,
            Option2<CodeStyleOption2<string>> option,
            AbstractOrderModifiersHelpers helpers)
            : base(IDEDiagnosticIds.OrderModifiersDiagnosticId,
                   EnforceOnBuildValues.OrderModifiers,
                   option,
                   new LocalizableResourceString(nameof(AnalyzersResources.Order_modifiers), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Modifiers_are_not_ordered), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
            _syntaxFacts = syntaxFacts;
            _helpers = helpers;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

        protected abstract CodeStyleOption2<string> GetPreferredOrderStyle(SyntaxTreeAnalysisContext context);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var option = GetPreferredOrderStyle(context);
            if (!_helpers.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
            {
                return;
            }

            Recurse(context, preferredOrder, option.Notification.Severity, context.GetAnalysisRoot(findInTrivia: false));
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
