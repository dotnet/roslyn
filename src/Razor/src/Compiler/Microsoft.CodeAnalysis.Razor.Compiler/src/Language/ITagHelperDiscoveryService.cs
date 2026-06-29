// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Razor.Language;

internal interface ITagHelperDiscoveryService : IRazorEngineFeature
{
    TagHelperCollection GetTagHelpers(Compilation compilation, TagHelperDiscoveryOptions options, CancellationToken cancellationToken = default);
    TagHelperCollection GetTagHelpers(Compilation compilation, CancellationToken cancellationToken = default);

    bool TryGetDiscoverer(Compilation compilation, TagHelperDiscoveryOptions options, [NotNullWhen(true)] out TagHelperDiscoverer? discoverer);
    bool TryGetDiscoverer(Compilation compilation, [NotNullWhen(true)] out TagHelperDiscoverer? discoverer);
}
