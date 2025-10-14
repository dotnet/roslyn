// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler.InlayHint;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost.Handlers;

/// <summary>
/// Provides a wrapper around the <see cref="InlayHintCache"/> so that Razor can control the lifecycle.
/// </summary>
internal sealed class InlayHintCacheWrapper
{
    private readonly InlayHintCache _cache = new();

    public InlayHintCache GetCache() => _cache;
}
