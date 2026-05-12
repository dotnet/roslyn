// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal struct TagHelperAttributeMatch(
    string name,
    BoundAttributeDescriptor attribute,
    BoundAttributeParameterDescriptor? parameter = null)
{
    public string Name { get; } = name;
    public BoundAttributeDescriptor Attribute { get; } = attribute;
    public BoundAttributeParameterDescriptor? Parameter { get; } = parameter;

    private bool? _isIndexerMatch;

    public bool IsIndexerMatch
        => _isIndexerMatch ??= TagHelperMatchingConventions.SatisfiesBoundAttributeIndexer(Attribute, Name.AsSpan());

    [MemberNotNullWhen(true, nameof(Parameter))]
    public readonly bool IsParameterMatch => Parameter is not null;

    public bool ExpectsStringValue
    {
        get
        {
            if (IsParameterMatch)
            {
                return Parameter.IsStringProperty;
            }

            return Attribute.IsStringProperty || (IsIndexerMatch && Attribute.IsIndexerStringProperty);
        }
    }

    public bool ExpectsBooleanValue
    {
        get
        {
            if (IsParameterMatch)
            {
                return Parameter.IsBooleanProperty;
            }

            return Attribute.IsBooleanProperty || (IsIndexerMatch && Attribute.IsIndexerBooleanProperty);
        }
    }
}
