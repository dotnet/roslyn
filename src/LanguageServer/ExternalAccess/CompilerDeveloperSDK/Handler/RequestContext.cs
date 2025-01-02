// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.ExternalAccess.CompilerDeveloperSdk;

internal readonly struct RequestContext(LspRequestContext context)
{
    /// <inheritdoc cref="LspRequestContext.Workspace"/>
    internal Workspace? Workspace => context.Workspace;
    /// <inheritdoc cref="LspRequestContext.Solution"/>
    internal Solution? Solution => context.Solution;
    /// <inheritdoc cref="LspRequestContext.Document"/>
    internal Document? Document => context.Document;
    /// <inheritdoc cref="LspRequestContext.GetRequiredDocument()"/>
    internal Document GetRequiredDocument() => context.GetRequiredDocument();

    internal T GetRequiredService<T>() where T : class => context.GetRequiredService<T>();
}
