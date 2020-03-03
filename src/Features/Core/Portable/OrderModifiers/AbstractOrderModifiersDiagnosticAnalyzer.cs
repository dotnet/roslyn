﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private readonly ISyntaxFacts _syntaxFacts;
        private readonly Option<CodeStyleOption<string>> _option;
        private readonly AbstractOrderModifiersHelpers _helpers;

        protected AbstractOrderModifiersDiagnosticAnalyzer(
            ISyntaxFacts syntaxFacts,
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
            var option = context.GetOption(_option);
            if (!_helpers.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
            {
                return;
            }

            var root = context.Tree.GetRoot(context.CancellationToken);
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
