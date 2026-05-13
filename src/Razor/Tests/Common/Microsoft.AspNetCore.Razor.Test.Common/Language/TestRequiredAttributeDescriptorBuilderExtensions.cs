// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

public static class TestRequiredAttributeDescriptorBuilderExtensions
{
    public static RequiredAttributeDescriptorBuilder Name(
        this RequiredAttributeDescriptorBuilder builder, string name, RequiredAttributeNameComparison? nameComparison = null)
    {
        builder.Name = name;

        if (nameComparison is RequiredAttributeNameComparison nameComparisonValue)
        {
            builder.NameComparison = nameComparisonValue;
        }

        return builder;
    }

    public static RequiredAttributeDescriptorBuilder NameComparison(
        this RequiredAttributeDescriptorBuilder builder, RequiredAttributeNameComparison nameComparison)
    {
        builder.NameComparison = nameComparison;

        return builder;
    }

    public static RequiredAttributeDescriptorBuilder Value(
        this RequiredAttributeDescriptorBuilder builder, string? value, RequiredAttributeValueComparison? valueComparison = null)
    {
        builder.Value = value;

        if (valueComparison is RequiredAttributeValueComparison valueComparisonValue)
        {
            builder.ValueComparison = valueComparisonValue;
        }

        return builder;
    }

    public static RequiredAttributeDescriptorBuilder ValueComparison(
        this RequiredAttributeDescriptorBuilder builder, RequiredAttributeValueComparison valueComparison)
    {
        builder.ValueComparison = valueComparison;

        return builder;
    }

    public static RequiredAttributeDescriptorBuilder IsDirectiveAttribute(
        this RequiredAttributeDescriptorBuilder builder, bool isDirectiveAttribute = true)
    {
        builder.IsDirectiveAttribute = isDirectiveAttribute;

        return builder;
    }

    public static RequiredAttributeDescriptorBuilder AddDiagnostic(
        this RequiredAttributeDescriptorBuilder builder, RazorDiagnostic diagnostic)
    {
        builder.Diagnostics.Add(diagnostic);

        return builder;
    }
}
