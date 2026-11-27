// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class BindTagHelperProducer
{
    public sealed class Factory : FactoryBase
    {
        public override bool TryCreate(
            Compilation compilation,
            bool includeDocumentation,
            bool excludeHidden,
            [NotNullWhen(true)] out TagHelperProducer? result)
        {
            if (!compilation.TryGetTypeByMetadataName(ComponentsApi.BindConverter.FullTypeName, out var bindConverterType))
            {
                result = null;
                return false;
            }

            var bindElementAttributeType = compilation.GetTypeByMetadataName(ComponentsApi.BindElementAttribute.FullTypeName);
            var bindInputElementAttributeType = compilation.GetTypeByMetadataName(ComponentsApi.BindInputElementAttribute.FullTypeName);

            result = new BindTagHelperProducer(bindConverterType, bindElementAttributeType, bindInputElementAttributeType);
            return true;
        }
    }
}
