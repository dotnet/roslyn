// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;

/// <summary>
/// Intercepts an LSP message and applies changes to the payload.
/// </summary>
[Obsolete("Please move to GenericInterceptionMiddleLayer and generic interceptors.")]
public abstract class MessageInterceptor
{
    /// <summary>
    /// Applies changes to the message token, and signals if the document path has been changed.
    /// </summary>
    /// <param name="message">The message payload</param>
    /// <param name="containedLanguageName">The name of the content type for the contained language.</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns></returns>
    public abstract Task<InterceptionResult> ApplyChangesAsync(JToken message, string containedLanguageName, CancellationToken cancellationToken);
}
