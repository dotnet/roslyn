// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentPageDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        var @namespace = documentNode.FindPrimaryNamespace();
        var @class = documentNode.FindPrimaryClass();
        if (@namespace == null || @class == null)
        {
            return;
        }

        var directives = documentNode.FindDirectiveReferences(ComponentPageDirective.Directive);
        if (directives.Length == 0)
        {
            return;
        }

        // We don't allow @page directives in imports
        foreach (var directive in directives)
        {
            if (codeDocument.FileKind.IsComponentImport() || directive.Node.IsImported)
            {
                directive.Node.AddDiagnostic(ComponentDiagnosticFactory.CreatePageDirective_CannotBeImported(directive.Node.Source.GetValueOrDefault()));
            }
        }

        // Insert the attributes 'on-top' of the class declaration, since classes don't directly support attributes.
        var index = 0;
        for (; index < @namespace.Children.Count; index++)
        {
            if (object.ReferenceEquals(@class, @namespace.Children[index]))
            {
                break;
            }
        }

        foreach (var directive in directives)
        {
            var pageDirective = directive.Node;

            // The parser also adds errors for invalid syntax, we just need to not crash.
            var routeToken = pageDirective.Tokens.First();

            if (routeToken is not { Content: ['"', '/', .., '"'] })
            {
                pageDirective.AddDiagnostic(ComponentDiagnosticFactory.CreatePageDirective_MustSpecifyRoute(pageDirective.Source));
            }

            if (!codeDocument.CodeGenerationOptions.DesignTime || !pageDirective.HasDiagnostics)
            {
                @namespace.Children.Insert(index++, new RouteAttributeExtensionNode(routeToken.Content) { Source = routeToken.Source });
            }
        }
    }
}
