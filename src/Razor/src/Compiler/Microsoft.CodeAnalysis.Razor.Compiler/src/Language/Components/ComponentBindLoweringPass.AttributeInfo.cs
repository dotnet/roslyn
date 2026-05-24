// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal partial class ComponentBindLoweringPass
{
    private struct AttributeInfo
    {
        public IntermediateNode Node { get; }
        public int Index { get; }
        public TagHelperDescriptor TagHelper { get; }
        public string AttributeName { get; }
        public string OriginalAttributeName { get; }

        private bool? _isFallbackBindTagHelper;
        private bool? _isBindTagHelper;
        private bool? _isInputElementFallbackBindTagHelper;
        private bool? _isInputElementBindTagHelper;

        public bool IsFallbackBindTagHelper
            => _isFallbackBindTagHelper ??= TagHelper.IsFallbackBindTagHelper();

        public bool IsBindTagHelper
            => _isBindTagHelper ??= TagHelper.Kind == TagHelperKind.Bind;

        public bool IsInputElementFallbackBindTagHelper
            => _isInputElementFallbackBindTagHelper ??= TagHelper.IsInputElementFallbackBindTagHelper();

        public bool IsInputElementBindTagHelper
            => _isInputElementBindTagHelper ??= TagHelper.IsInputElementBindTagHelper();

        public bool IsFallback => IsFallbackBindTagHelper || IsInputElementFallbackBindTagHelper;

        private AttributeInfo(
            IntermediateNode node,
            int index,
            TagHelperDescriptor tagHelper,
            string attributeName,
            string originalAttributeName)
        {
            Node = node;
            Index = index;
            TagHelper = tagHelper;
            AttributeName = attributeName;
            OriginalAttributeName = originalAttributeName;
        }

        public AttributeInfo(TagHelperDirectiveAttributeIntermediateNode node, int index)
            : this(node, index, node.TagHelper, node.AttributeName, node.OriginalAttributeName)
        {
        }

        public AttributeInfo(TagHelperDirectiveAttributeParameterIntermediateNode node, int index)
            : this(node, index, node.TagHelper, node.AttributeName, node.OriginalAttributeName)
        {
        }
    }
}
