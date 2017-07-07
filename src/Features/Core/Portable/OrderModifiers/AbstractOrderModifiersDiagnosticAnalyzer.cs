﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.OrderModifiers
{
    internal abstract class AbstractOrderModifiersDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        private readonly ISyntaxFactsService _syntaxFacts;
        private readonly Option<CodeStyleOption<string>> _option;
        private readonly AbstractOrderModifiersHelpers _helpers;

        protected AbstractOrderModifiersDiagnosticAnalyzer(
            ISyntaxFactsService syntaxFacts,
            Option<CodeStyleOption<string>> option,
            AbstractOrderModifiersHelpers helpers)
            : base(IDEDiagnosticIds.OrderModifiers,
                   new LocalizableResourceString(nameof(FeaturesResources.Order_modifiers), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Modifiers_are_not_ordered), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
            _syntaxFacts = syntaxFacts;
            _option = option;
            _helpers = helpers;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SyntaxAnalysis;
        public override bool OpenFileOnly(Workspace workspace) => false;

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;
            var syntaxTree = context.Tree;
            var root = syntaxTree.GetRoot(cancellationToken);

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(_option);
            if (!_helpers.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
            {
                return;
            }

            var descriptor = GetDescriptorWithSeverity(option.Notification.Value);
            Recurse(context, preferredOrder, descriptor, root);
        }

        protected abstract void Recurse(
            SyntaxTreeAnalysisContext context,
            Dictionary<int, int> preferredOrder,
            DiagnosticDescriptor descriptor,
            SyntaxNode root);

        protected void CheckModifiers(
            SyntaxTreeAnalysisContext context,
            Dictionary<int, int> preferredOrder,
            DiagnosticDescriptor descriptor,
            SyntaxNode memberDeclaration)
        {
            var modifiers = _syntaxFacts.GetModifiers(memberDeclaration);
            if (!IsOrdered(preferredOrder, modifiers))
            {
                if (descriptor.DefaultSeverity == DiagnosticSeverity.Hidden)
                {
                    // If the severity is hidden, put the marker on all the modifiers so that the
                    // user can bring up the fix anywhere in the modifier list.
                    context.ReportDiagnostic(
                        Diagnostic.Create(descriptor, context.Tree.GetLocation(
                            TextSpan.FromBounds(modifiers.First().SpanStart, modifiers.Last().Span.End))));
                }
                else
                {
                    // If the Severity is not hidden, then just put the user visible portion on the
                    // first token.  That way we don't 
                    context.ReportDiagnostic(
                        Diagnostic.Create(descriptor, modifiers.First().GetLocation()));
                }
            }
        }

        private bool IsOrdered(Dictionary<int, int> preferredOrder, SyntaxTokenList modifiers)
        {
            if (modifiers.Count >= 2)
            {
                var lastOrder = int.MinValue;
                foreach (var modifier in modifiers)
                {
                    var currentOrder = preferredOrder.TryGetValue(modifier.RawKind, out var value) ? value : int.MaxValue;
                    if (currentOrder < lastOrder)
                    {
                        return false;
                    }

                    lastOrder = currentOrder;
                }
            }

            return true;
        }
    }
}
