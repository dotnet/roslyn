// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions;

public sealed class ModelExpressionPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    private const string ModelExpressionTypeName = "Microsoft.AspNetCore.Mvc.ViewFeatures.ModelExpression";

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        if (documentNode.DocumentKind != RazorPageDocumentClassifierPass.RazorPageDocumentKind &&
            documentNode.DocumentKind != MvcViewDocumentClassifierPass.MvcViewDocumentKind)
        {
            // Not a MVC file. Skip.
            return;
        }

        var visitor = new Visitor();
        visitor.Visit(documentNode);
    }

    private class Visitor : IntermediateNodeWalker
    {
        public List<TagHelperIntermediateNode> TagHelpers { get; } = new List<TagHelperIntermediateNode>();

        public override void VisitTagHelperProperty(TagHelperPropertyIntermediateNode node)
        {
            if (string.Equals(node.BoundAttribute.TypeName, ModelExpressionTypeName, StringComparison.Ordinal) ||
                (node.IsIndexerNameMatch &&
                 string.Equals(node.BoundAttribute.IndexerTypeName, ModelExpressionTypeName, StringComparison.Ordinal)))
            {
                var expression = new CSharpExpressionIntermediateNode();

                expression.Children.Add(IntermediateNodeFactory.CSharpToken("ModelExpressionProvider.CreateModelExpression(ViewData, __model => "));

                if (node.Children.Count == 1 && node.Children[0] is CSharpIntermediateToken token)
                {
                    // A 'simple' expression will look like __model => __model.Foo

                    expression.Children.Add(IntermediateNodeFactory.CSharpToken("__model."));

                    expression.Children.Add(token);
                }
                else
                {
                    for (var i = 0; i < node.Children.Count; i++)
                    {
                        if (node.Children[i] is CSharpExpressionIntermediateNode nestedExpression)
                        {
                            for (var j = 0; j < nestedExpression.Children.Count; j++)
                            {
                                if (nestedExpression.Children[j] is CSharpIntermediateToken csharpToken)
                                {
                                    expression.Children.Add(csharpToken);
                                }
                            }

                            continue;
                        }
                    }
                }

                expression.Children.Add(IntermediateNodeFactory.CSharpToken(")"));

                node.Children.Clear();

                node.Children.Add(expression);
            }
        }
    }
}
