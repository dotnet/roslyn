// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language.TagHelpers.Producers;

internal interface ITagHelperProducerFactory : IRazorEngineFeature
{
    bool TryCreate(
        Compilation compilation,
        bool includeDocumentation,
        bool excludeHidden,
        [NotNullWhen(true)] out TagHelperProducer? result);
}
