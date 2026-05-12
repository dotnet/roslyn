// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentRenderModeDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    private const string GeneratedRenderModeAttributeName = "__PrivateComponentRenderModeAttribute";

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

        var directives = documentNode.FindDirectiveReferences(ComponentRenderModeDirective.Directive);
        if (directives.Length == 0)
        {
            return;
        }

        // We don't need to worry about duplicate attributes as we have already replaced any multiples with MalformedDirective
        Debug.Assert(directives.Length == 1);

        var child = directives[0].Node.Children.FirstOrDefault();
        if (child == null)
        {
            return;
        }

        // If the user is Razor 10 or higher, C# 11 or higher, and has a generic compoment, then we can use a file-scoped class for the generated attribute
        // so everything compiles correctly.
        var useFileScopedClass = codeDocument.ParserOptions.CSharpParseOptions.LanguageVersion >= CodeAnalysis.CSharp.LanguageVersion.CSharp11 &&
            codeDocument.ParserOptions.LanguageVersion >= RazorLanguageVersion.Version_11_0 &&
            @class.TypeParameters.Length > 0;

        // generate the inner attribute class
        var classDecl = new ClassDeclarationIntermediateNode
        {
            Name = GeneratedRenderModeAttributeName,
            BaseType = new BaseTypeWithModel($"global::{ComponentsApi.RenderModeAttribute.FullTypeName}"),
            Modifiers = useFileScopedClass
                ? CommonModifiers.FileSealed
                : CommonModifiers.PrivateSealed
        };

        classDecl.Children.Add(new CSharpCodeIntermediateNode()
        {
            Children =
            {
                IntermediateNodeFactory.CSharpToken($"private static global::{ComponentsApi.IComponentRenderMode.FullTypeName} ModeImpl => "),
                new CSharpCodeIntermediateNode()
                {
                    Source = child.Source,
                    Children =
                    {
                         child is not DirectiveTokenIntermediateNode directiveToken
                             ? child
                             : IntermediateNodeFactory.CSharpToken(
                                 content: directiveToken.Content,
                                 // To avoid breaking hot reload, we don't map the content back to the source unless we're on Razor 11 or higher
                                 source: codeDocument.ParserOptions.LanguageVersion >= RazorLanguageVersion.Version_11_0
                                    ? directiveToken.Source
                                    : null)
                    }
                },
                IntermediateNodeFactory.CSharpToken(";")
            }
        });

        classDecl.Children.Add(new CSharpCodeIntermediateNode()
        {
            Children =
            {
                IntermediateNodeFactory.CSharpToken($"public override global::{ComponentsApi.IComponentRenderMode.FullTypeName} Mode => ModeImpl;")
            }
        });

        if (useFileScopedClass)
        {
            @namespace.Children.Add(classDecl);
        }
        else
        {
            @class.Children.Add(classDecl);
        }

        // generate the attribute usage on top of the class
        var attributeNode = new CSharpCodeIntermediateNode();
        var namespaceSeparator = string.IsNullOrEmpty(@namespace.Name) ? string.Empty : ".";
        var attributeContents = useFileScopedClass
            ? GeneratedRenderModeAttributeName
            : $"global::{@namespace.Name}{namespaceSeparator}{@class.Name}.{GeneratedRenderModeAttributeName}";
        attributeNode.Children.Add(
            IntermediateNodeFactory.CSharpToken($"[{attributeContents}]"));

        // Insert the new attribute on top of the class
        var childCount = @namespace.Children.Count;
        for (var i = 0; i < childCount; i++)
        {
            if (object.ReferenceEquals(@namespace.Children[i], @class))
            {
                @namespace.Children.Insert(i, attributeNode);
                break;
            }
        }

        Debug.Assert(@namespace.Children.Count == childCount + 1);
    }
}
