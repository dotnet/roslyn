// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

internal readonly struct RazorRequestContext(RequestContext context)
{
    internal string Method => context.Method;
    internal Uri? Uri => context.TextDocument?.GetURI().ParsedUri;
    /// <inheritdoc cref="RequestContext.Workspace"/>
    internal Workspace? Workspace => context.Workspace;
    /// <inheritdoc cref="RequestContext.Solution"/>
    internal Solution? Solution => context.Solution;
    /// <inheritdoc cref="RequestContext.Document"/>
    internal TextDocument? TextDocument => context.TextDocument;

    internal T GetRequiredService<T>() where T : class => context.GetRequiredService<T>();
}
