// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentLayoutDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    private const string EmptyLayoutTypeName = "__EmptyLayout";

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
            var emptyLayout = new ClassDeclarationIntermediateNode
            {
                Name = EmptyLayoutTypeName,
                BaseType = new BaseTypeWithModel($"global::{ComponentsApi.LayoutComponentBase.FullTypeName}"),
                Modifiers = CommonModifiers.FileSealed,
            };
            emptyLayout.Children.Add(new MethodDeclarationIntermediateNode
            {
                Name = ComponentsApi.ComponentBase.BuildRenderTree,
                ReturnType = "void",
                Modifiers = CommonModifiers.ProtectedOverride,
                Parameters = [new(
                    name: ComponentsApi.RenderTreeBuilder.BuilderParameter,
                    type: $"global::{ComponentsApi.RenderTreeBuilder.FullTypeName}")],
                Children =
                {
                    new CSharpCodeIntermediateNode
                    {
                        Children =
                        {
                            IntermediateNodeFactory.CSharpToken(
                                $"{ComponentsApi.RenderTreeBuilder.BuilderParameter}.{ComponentsApi.RenderTreeBuilder.AddContent}(0, {ComponentsApi.LayoutComponentBase.Body});")
                        }
                    }
                }
            });
            @namespace.Children.Add(emptyLayout);
        }

        var attributeNode = new CSharpCodeIntermediateNode();
        attributeNode.Children.AddRange([
            IntermediateNodeFactory.CSharpToken($"[global::{ComponentsApi.LayoutAttribute.FullTypeName}(typeof("),
            IntermediateNodeFactory.CSharpToken(
                isEmptyLayout ? EmptyLayoutTypeName : token.Content,
                isEmptyLayout ? null : token.Source),
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
