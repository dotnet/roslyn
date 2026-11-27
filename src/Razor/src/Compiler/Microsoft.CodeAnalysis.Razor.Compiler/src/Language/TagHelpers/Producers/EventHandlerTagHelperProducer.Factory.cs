// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class EventHandlerTagHelperProducer
{
    public sealed class Factory : FactoryBase
    {
        public override bool TryCreate(
            Compilation compilation,
            bool includeDocumentation,
            bool excludeHidden,
            [NotNullWhen(true)] out TagHelperProducer? result)
        {
            if (!compilation.TryGetTypeByMetadataName(ComponentsApi.EventHandlerAttribute.FullTypeName, out var eventHandlerAttributeType))
            {
                // If we can't find EventHandlerAttribute, then just bail. We won't discover anything.
                result = null;
                return false;
            }

            result = new EventHandlerTagHelperProducer(eventHandlerAttributeType);
            return true;
        }
    }
}
