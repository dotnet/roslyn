// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentChildContentDiagnosticPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Runs after components/eventhandlers/ref/bind/templates. We want to validate every component
    // and it's usage of ChildContent.
    public override int Order => 160;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        var visitor = new Visitor();
        visitor.Visit(documentNode);
    }

    private sealed class Visitor : IntermediateNodeWalker
    {
        public override void VisitComponent(ComponentIntermediateNode node)
        {
            // Check for properties that are set by both element contents (body) and the attribute itself.
            foreach (var childContent in node.ChildContents)
            {
                foreach (var attribute in node.Attributes)
                {
                    if (attribute.AttributeName == childContent.AttributeName)
                    {
                        node.AddDiagnostic(ComponentDiagnosticFactory.Create_ChildContentSetByAttributeAndBody(
                            attribute.Source,
                            attribute.AttributeName));
                    }
                }
            }

            VisitDefault(node);
        }

        public override void VisitComponentChildContent(ComponentChildContentIntermediateNode node)
        {
            // Check that each child content has a unique parameter name within its scope. This is important
            // because the parameter name can be implicit, and it doesn't work well when nested.
            if (node.IsParameterized)
            {
                var ancestors = Ancestors;
                var parentComponent = (ComponentIntermediateNode)ancestors[0];

                // Skip the immediate parent component as we've already validated against it.
                // Loop to ancestors.Length - 1 because we're always checking pairs.

                for (var i = 1; i < ancestors.Length - 1; i++)
                {
                    if (ancestors[i] is ComponentChildContentIntermediateNode { IsParameterized: true } ancestor &&
                        ancestor.ParameterName == node.ParameterName &&
                        ancestors[i + 1] is ComponentIntermediateNode ancestorParentComponent)
                    {
                        // Duplicate name. We report an error because this will almost certainly also lead to an error
                        // from the C# compiler that's way less clear.
                        node.AddDiagnostic(ComponentDiagnosticFactory.Create_ChildContentRepeatedParameterName(
                            node.Source,
                            childContent1: node,
                            component1: parentComponent,
                            childContent2: ancestor,
                            component2: ancestorParentComponent));
                    }
                }
            }

            VisitDefault(node);
        }
    }
}
