// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentTemplateDiagnosticPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Runs after components/eventhandlers/ref/bind. We need to check for templates in all of those
    // places.
    public override int Order => 150;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        using var _ = ListPool<IntermediateNodeReference>.GetPooledObject(out var candidates);

        var visitor = new Visitor(candidates);
        visitor.Visit(documentNode);

        foreach (var candidate in candidates)
        {
            var (node, parent) = candidate;

            parent.AddDiagnostic(ComponentDiagnosticFactory.Create_TemplateInvalidLocation(node.Source));

            // Remove the offending node since we don't know how to render it. This means that the user won't get C#
            // completion at this location, which is fine because it's inside an HTML attribute.
            candidate.Remove();
        }
    }

    private sealed class Visitor(List<IntermediateNodeReference> candidates)
        : IntermediateNodeWalker, IExtensionIntermediateNodeVisitor<TemplateIntermediateNode>
    {
        private readonly List<IntermediateNodeReference> _candidates = candidates;

        public void VisitExtension(TemplateIntermediateNode node)
        {
            // We found a template, let's check where it's located.
            foreach (var ancestor in Ancestors)
            {
                if (ancestor is HtmlAttributeIntermediateNode or // Inside markup attribute
                                ComponentAttributeIntermediateNode or // Inside component attribute
                                TagHelperPropertyIntermediateNode or // Inside malformed ref attribute
                                TagHelperDirectiveAttributeIntermediateNode) // Inside a directive attribute
                {
                    _candidates.Add(new IntermediateNodeReference(node, Parent.AssumeNotNull()));

                    // We found a candidate and can stop looking. There's no need to report multiple diagnostics for the same node.
                    break;
                }
            }
        }
    }
}
