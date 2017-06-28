// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.OrderModifiers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpOrderModifiersDiagnosticAnalyzer : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpOrderModifiersDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.OrderModifiers,
                   new LocalizableResourceString(nameof(FeaturesResources.Order_modifiers), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Modifiers_are_not_ordered), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
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

            var option = optionSet.GetOption(CSharpCodeStyleOptions.PreferredModifierOrder);
            if (!OrderModifiersHelper.TryGetOrComputePreferredOrder(option.Value, out var preferredOrder))
            {
                return;
            }

            var descriptor = GetDescriptorWithSeverity(option.Notification.Value);
            Recurse(context, preferredOrder, descriptor, root);
        }

        private void Recurse(
            SyntaxTreeAnalysisContext context,
            Dictionary<int, int> preferredOrder,
            DiagnosticDescriptor descriptor,
            SyntaxNode root)
        {
            foreach (var child in root.ChildNodesAndTokens())
            {
                if (child.IsNode)
                {
                    var node = child.AsNode();
                    if (node is MemberDeclarationSyntax memberDeclaration)
                    {
                        CheckModifiers(context, preferredOrder, descriptor, memberDeclaration);

                        // Recurse and check children.  Note: we only do this if we're on an actual 
                        // member declaration.  Once we hit something that isn't, we don't need to 
                        // keep recursing.  This prevents us from actually entering things like method 
                        // bodies.
                        Recurse(context, preferredOrder, descriptor, node);
                    }
                    else if (node is AccessorListSyntax accessorList)
                    {
                        foreach (var accessor in accessorList.Accessors)
                        {
                            CheckModifiers(context, preferredOrder, descriptor, accessor);
                        }
                    }
                }
            }
        }

        private void CheckModifiers(
            SyntaxTreeAnalysisContext context, 
            Dictionary<int, int> preferredOrder,
            DiagnosticDescriptor descriptor,
            SyntaxNode memberDeclaration)
        {
            var modifiers = memberDeclaration.GetModifiers();
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

            return true;
        }
    }
}