// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal sealed partial class FormNameTagHelperProducer
{
    public sealed class Factory : FactoryBase
    {
        public override bool TryCreate(
            Compilation compilation,
            bool includeDocumentation,
            bool excludeHidden,
            [NotNullWhen(true)] out TagHelperProducer? result)
        {
            var renderTreeBuilderTypes = compilation.GetTypesByMetadataName(ComponentsApi.RenderTreeBuilder.FullTypeName)
                .Where(IsValidRenderTreeBuilder)
                .Take(2)
                .ToArray();

            if (renderTreeBuilderTypes is not [var renderTreeBuilderType])
            {
                // If we can't find RenderTreeBuilder, then just bail. We won't be able to compile the generated code anyway.
                result = null;
                return false;
            }

            result = new FormNameTagHelperProducer(renderTreeBuilderType);
            return true;

            static bool IsValidRenderTreeBuilder(INamedTypeSymbol type)
            {
                return type.DeclaredAccessibility == Accessibility.Public &&
                       type.GetMembers(ComponentsApi.RenderTreeBuilder.AddNamedEvent)
                           .Any(static m => m.DeclaredAccessibility == Accessibility.Public);
            }
        }
    }
}
