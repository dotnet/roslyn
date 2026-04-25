// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.MessageInterception;

/// <summary>
/// Receives notification messages from the server and invokes any applicable message interception layers.
/// </summary>
public class GenericInterceptionMiddleLayer<TJsonToken> : ILanguageClientMiddleLayer2<TJsonToken>
{
    private readonly InterceptorManager _interceptorManager;
    private readonly string _contentType;

    /// <summary>
    /// Create the middle layer
    /// </summary>
    /// <param name="interceptorManager">Interception manager</param>
    /// <param name="contentType">The content type name of the language for the ILanguageClient using this middle layer</param>
    public GenericInterceptionMiddleLayer(InterceptorManager interceptorManager, string contentType)
    {
        _interceptorManager = interceptorManager ?? throw new ArgumentNullException(nameof(interceptorManager));
        _contentType = !string.IsNullOrEmpty(contentType) ? contentType : throw new ArgumentException("Cannot be empty", nameof(contentType));
    }

    public bool CanHandle(string methodName)
    {
        return _interceptorManager.HasInterceptor(methodName, _contentType);
    }

    public async Task HandleNotificationAsync(string methodName, TJsonToken methodParam, Func<TJsonToken, Task> sendNotification)
    {
        var payload = methodParam;
        if (CanHandle(methodName))
        {
            payload = await _interceptorManager.ProcessGenericInterceptorsAsync(methodName, methodParam, _contentType, CancellationToken.None);
        }

        if (payload is not null &&
            !EqualityComparer<TJsonToken>.Default.Equals(payload, default!))
        {
            // this completes the handshake to give the payload back to the client.
            await sendNotification(payload);
        }
    }

    public async Task<TJsonToken?> HandleRequestAsync(string methodName, TJsonToken methodParam, Func<TJsonToken, Task<TJsonToken?>> sendRequest)
    {
        // First send the request through.
        // We don't yet have a scenario where the request needs to be intercepted, but if one does come up, we may need to redesign the interception handshake
        // to handle both request and response interception.
        var response = await sendRequest(methodParam);

        if (response is not null &&
            !EqualityComparer<TJsonToken>.Default.Equals(response, default!) &&
            CanHandle(methodName))
        {
            response = await _interceptorManager.ProcessGenericInterceptorsAsync(methodName, response, _contentType, CancellationToken.None);
        }

        return response;
    }
}
