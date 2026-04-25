// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentInjectDirectivePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
{
    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        var visitor = new Visitor();
        visitor.Visit(documentNode);

        var properties = new HashSet<string>(StringComparer.Ordinal);
        var classNode = documentNode.FindPrimaryClass();

        for (var i = visitor.Directives.Count - 1; i >= 0; i--)
        {
            var directive = visitor.Directives[i];
            var tokens = directive.Children.OfType<DirectiveTokenIntermediateNode>().ToArray();
            var isMalformed = directive is MalformedDirectiveIntermediateNode;

            var hasType = tokens.Length > 0 && !string.IsNullOrWhiteSpace(tokens[0].Content);
            Debug.Assert(hasType || isMalformed);
            var typeName = hasType ? tokens[0].Content : string.Empty;
            var typeSpan = hasType ? tokens[0].Source : directive.Source?.GetZeroWidthEndSpan();

            var hasMemberName = tokens.Length > 1 && !string.IsNullOrWhiteSpace(tokens[1].Content);
            Debug.Assert(hasMemberName || isMalformed);
            var memberName = hasMemberName ? tokens[1].Content : null;
            var memberSpan = hasMemberName ? tokens[1].Source : null;

            if (hasMemberName && !properties.Add(memberName!))
            {
                continue;
            }

            classNode!.Children.Add(new ComponentInjectIntermediateNode(typeName, memberName, typeSpan, memberSpan, isMalformed));
        }
    }

    private class Visitor : IntermediateNodeWalker
    {
        public IList<IntermediateNode> Directives { get; } = [];

        public override void VisitDirective(DirectiveIntermediateNode node)
        {
            if (node.Directive == ComponentInjectDirective.Directive)
            {
                Directives.Add(node);
            }
        }

        public override void VisitMalformedDirective(MalformedDirectiveIntermediateNode node)
        {
            if (node.Directive == ComponentInjectDirective.Directive)
            {
                Directives.Add(node);
            }
        }
    }
}
