// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;

[Export(typeof(InterceptorManager))]
internal sealed class DefaultInterceptorManager : InterceptorManager
{
    [Obsolete]
    private readonly IReadOnlyList<Lazy<MessageInterceptor, IInterceptMethodMetadata>> _lazyInterceptors;
    private readonly IReadOnlyList<Lazy<GenericMessageInterceptor, IInterceptMethodMetadata>> _lazyGenericInterceptors;

    [ImportingConstructor]
    public DefaultInterceptorManager(
#pragma warning disable CS0618 // Type or member is obsolete
        [ImportMany] IEnumerable<Lazy<MessageInterceptor, IInterceptMethodMetadata>> lazyInterceptors,
#pragma warning restore CS0618 // Type or member is obsolete
        [ImportMany] IEnumerable<Lazy<GenericMessageInterceptor, IInterceptMethodMetadata>> lazyGenericInterceptors)
    {
        _ = lazyInterceptors ?? throw new ArgumentNullException(nameof(lazyInterceptors));
#pragma warning disable CS0612 // Type or member is obsolete
        _lazyInterceptors = lazyInterceptors.ToList().AsReadOnly();
#pragma warning restore CS0612 // Type or member is obsolete

        _ = lazyGenericInterceptors ?? throw new ArgumentNullException(nameof(lazyGenericInterceptors));
        _lazyGenericInterceptors = lazyGenericInterceptors.ToList().AsReadOnly();
    }

    public override bool HasInterceptor(string methodName, string contentType)
    {
        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("Cannot be empty", nameof(methodName));
        }

        foreach (var interceptor in _lazyGenericInterceptors)
        {
            if (interceptor.Metadata.ContentTypes.Any(ct => contentType.Equals(ct, StringComparison.Ordinal)))
            {
                foreach (var method in interceptor.Metadata.InterceptMethods)
                {
                    if (method.Equals(methodName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

#pragma warning disable CS0612 // Type or member is obsolete
        foreach (var interceptor in _lazyInterceptors)
#pragma warning restore CS0612 // Type or member is obsolete
        {
            if (interceptor.Metadata.ContentTypes.Any(ct => contentType.Equals(ct, StringComparison.Ordinal)))
            {
                foreach (var method in interceptor.Metadata.InterceptMethods)
                {
                    if (method.Equals(methodName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    public override async Task<TJsonToken?> ProcessGenericInterceptorsAsync<TJsonToken>(string methodName, TJsonToken message, string contentType, CancellationToken cancellationToken)
        where TJsonToken : default
    {
        _ = message ?? throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("Cannot be empty", nameof(methodName));
        }

        if (string.IsNullOrEmpty(contentType))
        {
            throw new ArgumentException("Cannot be empty", nameof(contentType));
        }

        for (var i = 0; i < _lazyGenericInterceptors.Count; i++)
        {
            var interceptor = _lazyGenericInterceptors[i];
            if (CanInterceptMessage(methodName, contentType, interceptor.Metadata))
            {
                var result = await interceptor.Value.ApplyChangesAsync(message, contentType, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (result.UpdatedToken is null ||
                    EqualityComparer<TJsonToken>.Default.Equals(result.UpdatedToken, default!))
                {
                    // The interceptor has blocked this message
                    return default;
                }

                message = result.UpdatedToken;

                if (result.ChangedDocumentUri)
                {
                    // If the DocumentUri changes, we need to restart the loop
                    i = -1;
                    continue;
                }
            }
        }

        return message;
    }

    [Obsolete("Please move to GenericInterceptionMiddleLayer and generic interceptors.")]
    public override async Task<JToken?> ProcessInterceptorsAsync(string methodName, JToken message, string contentType, CancellationToken cancellationToken)
    {
        _ = message ?? throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrEmpty(methodName))
        {
            throw new ArgumentException("Cannot be empty", nameof(methodName));
        }

        if (string.IsNullOrEmpty(contentType))
        {
            throw new ArgumentException("Cannot be empty", nameof(contentType));
        }

        for (var i = 0; i < _lazyInterceptors.Count; i++)
        {
            var interceptor = _lazyInterceptors[i];
            if (CanInterceptMessage(methodName, contentType, interceptor.Metadata))
            {
                var result = await interceptor.Value.ApplyChangesAsync(message, contentType, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                if (result.UpdatedToken is null)
                {
                    // The interceptor has blocked this message
                    return null;
                }

                message = result.UpdatedToken;

                if (result.ChangedDocumentUri)
                {
                    // If the DocumentUri changes, we need to restart the loop
                    i = -1;
                    continue;
                }
            }
        }

        return message;
    }

    private static bool CanInterceptMessage(string methodName, string contentType, IInterceptMethodMetadata metadata)
    {
        var handledMessages = metadata.InterceptMethods;
        var contentTypes = metadata.ContentTypes;

        return handledMessages.Any(m => methodName.Equals(m, StringComparison.Ordinal))
            && contentTypes.Any(ct => contentType.Equals(ct, StringComparison.Ordinal));
    }
}
