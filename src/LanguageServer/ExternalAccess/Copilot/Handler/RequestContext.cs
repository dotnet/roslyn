// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using LspRequestContext = Microsoft.CodeAnalysis.LanguageServer.Handler.RequestContext;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Copilot;

internal readonly struct RequestContext(LspRequestContext context)
{
    /// <inheritdoc cref="LspRequestContext.Solution"/>
    public Solution? Solution => context.Solution;

    /// <inheritdoc cref="LspRequestContext.Document"/>
    public Document? Document => context.Document;

    public T GetRequiredService<T>() where T : class => context.GetRequiredService<T>();
}
