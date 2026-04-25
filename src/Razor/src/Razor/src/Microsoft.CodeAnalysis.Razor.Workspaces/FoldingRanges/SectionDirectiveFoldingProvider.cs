// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;

namespace Microsoft.CodeAnalysis.Razor.FoldingRanges;

internal class SectionDirectiveFoldingProvider : AbstractSyntaxNodeFoldingProvider<RazorDirectiveSyntax>
{
    protected override string GetCollapsedText(RazorDirectiveSyntax node)
    {
        Debug.Assert(node.HasDirectiveDescriptor);
        return $"@{node.DirectiveDescriptor.Directive}{GetSectionName(node)}";

        static string GetSectionName(RazorDirectiveSyntax node)
        {
            if (node.DirectiveBody.CSharpCode.Children is [_, { } name, ..])
            {
                return $" {name.GetContent()}";
            }

            return "";
        }
    }

    protected override ImmutableArray<RazorDirectiveSyntax> GetFoldableNodes(RazorSyntaxTree syntaxTree)
    {
        return syntaxTree.GetSectionDirectives();
    }
}
