// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.Extensions.Internal;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal partial class ComponentBindLoweringPass
{
    private readonly record struct BindEntryKey(IntermediateNode Parent, string AttributeName)
    {
        public BindEntryKey(IntermediateNode parent, TagHelperDirectiveAttributeIntermediateNode node)
            : this(parent, node.AttributeName)
        {
        }

        public BindEntryKey(IntermediateNode parent, TagHelperDirectiveAttributeParameterIntermediateNode node)
            : this(parent, node.AttributeNameWithoutParameter)
        {
        }

        public bool Equals(BindEntryKey other)
            => ReferenceEquals(Parent, other.Parent) &&
               AttributeName == other.AttributeName;

        public override int GetHashCode()
        {
            var hash = HashCodeCombiner.Start();

            hash.Add(Parent);
            hash.Add(AttributeName);

            return hash.CombinedHash;
        }
    }
}
