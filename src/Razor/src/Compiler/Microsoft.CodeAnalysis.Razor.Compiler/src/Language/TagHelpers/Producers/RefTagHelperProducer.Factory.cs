// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class RefTagHelperProducer
{
    public sealed class Factory : FactoryBase
    {
        public override bool TryCreate(
            Compilation compilation,
            bool includeDocumentation,
            bool excludeHidden,
            [NotNullWhen(true)] out TagHelperProducer? result)
        {
            if (!compilation.TryGetTypeByMetadataName(ComponentsApi.ElementReference.FullTypeName, out var elementReferenceType))
            {
                // If we can't find ElementRef, then just bail. We won't be able to compile the generated code anyway.
                result = null;
                return false;
            }

            result = new RefTagHelperProducer(elementReferenceType);
            return true;
        }
    }
}
