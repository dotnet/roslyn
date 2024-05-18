// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;

#pragma warning disable CS0618 // Type or member is obsolete - blocked on Razor switching to new APIs for STJ - https://github.com/dotnet/roslyn/issues/73317
internal abstract class AbstractLanguageClientMiddleLayer : ILanguageClientMiddleLayer
#pragma warning restore CS0618 // Type or member is obsolete
{
    public abstract bool CanHandle(string methodName);

    public abstract Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification);

    public abstract Task<JToken?> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken?>> sendRequest);
}
