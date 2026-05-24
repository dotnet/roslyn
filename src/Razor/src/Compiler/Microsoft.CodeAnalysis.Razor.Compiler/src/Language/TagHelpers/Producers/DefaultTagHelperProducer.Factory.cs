// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class DefaultTagHelperProducer
{
    public sealed class Factory : FactoryBase
    {
        public override bool TryCreate(
            Compilation compilation,
            bool includeDocumentation,
            bool excludeHidden,
            [NotNullWhen(true)] out TagHelperProducer? result)
        {
            if (!compilation.TryGetTypeByMetadataName(TagHelperTypes.ITagHelper, out var iTagHelperType) ||
                iTagHelperType.TypeKind == TypeKind.Error)
            {
                // If we can't find ITagHelper, then just bail. We won't discover anything.
                result = null;
                return false;
            }

            var factory = new DefaultTagHelperDescriptorFactory(includeDocumentation, excludeHidden);

            result = new DefaultTagHelperProducer(factory, iTagHelperType);
            return true;
        }
    }
}
