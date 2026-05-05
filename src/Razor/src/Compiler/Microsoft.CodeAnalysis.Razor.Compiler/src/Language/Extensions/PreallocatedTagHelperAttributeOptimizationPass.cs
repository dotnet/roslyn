// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal sealed class PreallocatedTagHelperAttributeOptimizationPass : IntermediateNodePassBase, IRazorOptimizationPass
{
    // We want to run after the passes that 'lower' tag helpers. We also want this to run after DefaultTagHelperOptimizationPass.
    public override int Order => DefaultFeatureOrder + 1010;

    protected override void ExecuteCore(
        RazorCodeDocument codeDocument,
        DocumentIntermediateNode documentNode,
        CancellationToken cancellationToken)
    {
        // There's no value in executing this pass at design time, it just prevents some allocations.
        if (documentNode.Options.DesignTime)
        {
            return;
        }

        var walker = new PreallocatedTagHelperWalker();
        walker.VisitDocument(documentNode);
    }

    internal class PreallocatedTagHelperWalker :
        IntermediateNodeWalker,
        IExtensionIntermediateNodeVisitor<DefaultTagHelperHtmlAttributeIntermediateNode>,
        IExtensionIntermediateNodeVisitor<DefaultTagHelperPropertyIntermediateNode>
    {
        private const string PreAllocatedAttributeVariablePrefix = "__tagHelperAttribute_";

        private ClassDeclarationIntermediateNode? _classDeclaration;
        private int _variableCountOffset;
        private int _preallocatedDeclarationCount;

        public override void VisitClassDeclaration(ClassDeclarationIntermediateNode node)
        {
            _classDeclaration = node;
            _variableCountOffset = node.Children.Count;

            VisitDefault(node);
        }

        public void VisitExtension(DefaultTagHelperHtmlAttributeIntermediateNode node)
        {
            if (node.Children is not [HtmlContentIntermediateNode htmlContentNode])
            {
                return;
            }

            var plainTextValue = GetContent(htmlContentNode);

            PreallocatedTagHelperHtmlAttributeValueIntermediateNode? declaration = null;

            foreach (var current in _classDeclaration.AssumeNotNull().Children)
            {
                if (current is PreallocatedTagHelperHtmlAttributeValueIntermediateNode existingDeclaration)
                {
                    if (string.Equals(existingDeclaration.AttributeName, node.AttributeName, StringComparison.Ordinal) &&
                        string.Equals(existingDeclaration.Value, plainTextValue, StringComparison.Ordinal) &&
                        existingDeclaration.AttributeStructure == node.AttributeStructure)
                    {
                        declaration = existingDeclaration;
                        break;
                    }
                }
            }

            if (declaration == null)
            {
                var variableCount = _classDeclaration.Children.Count - _variableCountOffset;
                var preAllocatedAttributeVariableName = PreAllocatedAttributeVariablePrefix + variableCount;
                declaration = new PreallocatedTagHelperHtmlAttributeValueIntermediateNode
                {
                    VariableName = preAllocatedAttributeVariableName,
                    AttributeName = node.AttributeName,
                    Value = plainTextValue,
                    AttributeStructure = node.AttributeStructure,
                };
                _classDeclaration.Children.Insert(_preallocatedDeclarationCount++, declaration);
            }

            var addPreAllocatedAttribute = new PreallocatedTagHelperHtmlAttributeIntermediateNode
            {
                VariableName = declaration.VariableName,
            };

            var nodeIndex = Parent!.Children.IndexOf(node);
            Parent.Children[nodeIndex] = addPreAllocatedAttribute;
        }

        public void VisitExtension(DefaultTagHelperPropertyIntermediateNode node)
        {
            if (!(node.BoundAttribute.IsStringProperty || (node.IsIndexerNameMatch && node.BoundAttribute.IsIndexerStringProperty)) ||
                node.Children is not [HtmlContentIntermediateNode htmlContentNode])
            {
                return;
            }

            var plainTextValue = GetContent(htmlContentNode);

            PreallocatedTagHelperPropertyValueIntermediateNode? declaration = null;

            foreach (var current in _classDeclaration.AssumeNotNull().Children)
            {
                if (current is PreallocatedTagHelperPropertyValueIntermediateNode existingDeclaration)
                {
                    if (string.Equals(existingDeclaration.AttributeName, node.AttributeName, StringComparison.Ordinal) &&
                        string.Equals(existingDeclaration.Value, plainTextValue, StringComparison.Ordinal) &&
                        existingDeclaration.AttributeStructure == node.AttributeStructure)
                    {
                        declaration = existingDeclaration;
                        break;
                    }
                }
            }

            if (declaration == null)
            {
                var variableCount = _classDeclaration.Children.Count - _variableCountOffset;
                var preAllocatedAttributeVariableName = PreAllocatedAttributeVariablePrefix + variableCount;
                declaration = new PreallocatedTagHelperPropertyValueIntermediateNode()
                {
                    VariableName = preAllocatedAttributeVariableName,
                    AttributeName = node.AttributeName,
                    Value = plainTextValue,
                    AttributeStructure = node.AttributeStructure,
                };
                _classDeclaration.Children.Insert(_preallocatedDeclarationCount++, declaration);
            }

            var setPreallocatedProperty = new PreallocatedTagHelperPropertyIntermediateNode(node)
            {
                VariableName = declaration.VariableName,
            };

            var nodeIndex = Parent!.Children.IndexOf(node);
            Parent.Children[nodeIndex] = setPreallocatedProperty;
        }

        private string GetContent(HtmlContentIntermediateNode node)
        {
            return string.Build(node, (ref builder, node) =>
            {
                foreach (var child in node.Children)
                {
                    if (child is HtmlIntermediateToken token)
                    {
                        builder.Append(token.Content);
                    }
                }
            });
        }
    }
}
