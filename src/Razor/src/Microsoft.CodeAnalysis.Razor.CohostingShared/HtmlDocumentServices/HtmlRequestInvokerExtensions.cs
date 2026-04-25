// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Cohost;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal static class HtmlRequestInvokerExtensions
{
    public static Task<TResponse?> MakeHtmlLspRequestAsync<TRequest, TResponse>(this IHtmlRequestInvoker requestInvoker, TextDocument razorDocument, string method, TRequest request, CancellationToken cancellationToken)
        where TRequest : notnull
    {
        return requestInvoker.MakeHtmlLspRequestAsync<TRequest, TResponse>(razorDocument, method, request, threshold: TimeSpan.Zero, correlationId: Guid.Empty, cancellationToken);
    }
}
