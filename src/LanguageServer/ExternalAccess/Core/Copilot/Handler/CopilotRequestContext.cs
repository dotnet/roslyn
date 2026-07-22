// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.LanguageServer.ExternalAccess.Copilot;

/// <summary>
/// Context for requests handled by <see cref="AbstractCopilotLspServiceDocumentRequestHandler{TRequest, TResponse}"/>
/// </summary>
internal readonly struct CopilotRequestContext(RequestContext context)
{
    /// <summary>
    /// The solution state that the request should operate on.
    /// </summary>
    public Solution Solution => context.Solution ?? throw new InvalidOperationException();

    /// <inheritdoc cref="RequestContext.Document"/>
    public Document? Document => context.Document;

    public T GetRequiredService<T>() where T : class => context.GetRequiredService<T>();
}
