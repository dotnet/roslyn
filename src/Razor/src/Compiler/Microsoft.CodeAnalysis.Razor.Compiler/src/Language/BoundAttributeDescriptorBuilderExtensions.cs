// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public static class BoundAttributeDescriptorBuilderExtensions
{
    public static void AsDictionary(
        this BoundAttributeDescriptorBuilder builder,
        string attributeNamePrefix,
        string valueTypeName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.IsDictionary = true;
        builder.IndexerAttributeNamePrefix = attributeNamePrefix;
        builder.IndexerValueTypeName = valueTypeName;
    }
}
