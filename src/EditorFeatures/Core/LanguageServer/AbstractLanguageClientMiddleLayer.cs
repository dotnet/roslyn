// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;

internal abstract class AbstractLanguageClientMiddleLayer : ILanguageClientMiddleLayer2<JsonElement>
{
    public abstract bool CanHandle(string methodName);

    public abstract Task HandleNotificationAsync(string methodName, JsonElement methodParam, Func<JsonElement, Task> sendNotification);

    public abstract Task<JsonElement> HandleRequestAsync(string methodName, JsonElement methodParam, Func<JsonElement, Task<JsonElement>> sendRequest);
}
