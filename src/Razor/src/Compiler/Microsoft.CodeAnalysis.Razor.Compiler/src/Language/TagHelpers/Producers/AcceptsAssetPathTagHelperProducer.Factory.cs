// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class AcceptsAssetPathTagHelperProducer
{
    public sealed class Factory : FactoryBase
    {
        public override bool TryCreate(
            Compilation compilation,
            bool includeDocumentation,
            bool excludeHidden,
            [NotNullWhen(true)] out TagHelperProducer? result)
        {
            if (!compilation.TryGetTypeByMetadataName(ComponentsApi.AcceptsAssetPathAttribute.MetadataName, out var acceptsAssetPathAttributeType))
            {
                // If we can't find AcceptsAssetPathAttribute, then just bail. We won't discover anything.
                result = null;
                return false;
            }

            result = new AcceptsAssetPathTagHelperProducer(acceptsAssetPathAttributeType);
            return true;
        }
    }
}
