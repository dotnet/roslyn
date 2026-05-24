// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.TagHelpers;
using Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

internal sealed partial class ViewComponentTagHelperProducer
{
    public sealed class Factory : FactoryBase
    {
        public override bool TryCreate(
            Compilation compilation,
            bool includeDocumentation,
            bool excludeHidden,
            [NotNullWhen(true)] out TagHelperProducer? result)
        {
            if (!compilation.TryGetTypeByMetadataName(ViewComponentTypes.ViewComponentAttribute, out var viewComponentAttributeType) ||
                viewComponentAttributeType.TypeKind == TypeKind.Error)
            {
                result = null;
                return false;
            }

            var nonViewComponentAttributeType = compilation.GetTypeByMetadataName(ViewComponentTypes.NonViewComponentAttribute);

            var factory = new ViewComponentTagHelperDescriptorFactory(compilation);
            result = new ViewComponentTagHelperProducer(factory, viewComponentAttributeType, nonViewComponentAttributeType);
            return true;
        }
    }
}
