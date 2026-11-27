// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentLayoutDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
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

        var directives = documentNode.FindDirectiveReferences(ComponentLayoutDirective.Directive);
        if (directives.Length == 0)
        {
            return;
        }

        var token = directives[0].Node.Tokens.FirstOrDefault();
        if (token == null)
        {
            return;
        }

        // Earlier Razor versions keep treating the directive token as a layout type.
        var isEmptyLayout = token.Content == "null" &&
            codeDocument.ParserOptions.LanguageVersion >= RazorLanguageVersion.Version_12_0;
        if (isEmptyLayout)
        {
            // Report the requirement on the directive instead of emitting an unsupported file-local type.
            if (codeDocument.ParserOptions.CSharpParseOptions.LanguageVersion < CodeAnalysis.CSharp.LanguageVersion.CSharp11)
            {
                documentNode.AddDiagnostic(ComponentDiagnosticFactory.CreateLayoutNull_UnsupportedCSharpVersion(token.Source));
                return;
            }

            // Omitting the attribute would leave router defaults and inherited layouts in effect.
            // The file-local helper must stay with its attribute in the declaration file.
            @namespace.Children.Add(new EmptyLayoutIntermediateNode());
        }

        var attributeNode = new CSharpCodeIntermediateNode();
        attributeNode.Children.AddRange([
            IntermediateNodeFactory.CSharpToken($"[global::{ComponentsApi.LayoutAttribute.FullTypeName}(typeof("),
            isEmptyLayout
                ? IntermediateNodeFactory.CSharpToken(EmptyLayoutIntermediateNode.TypeName)
                : IntermediateNodeFactory.CSharpToken(token.Content, token.Source),
            IntermediateNodeFactory.CSharpToken("))]")
        ]);

        // Insert the new attribute on top of the class
        for (var i = 0; i < @namespace.Children.Count; i++)
        {
            if (object.ReferenceEquals(@namespace.Children[i], @class))
            {
                @namespace.Children.Insert(i, attributeNode);
                break;
            }
        }
    }
}
