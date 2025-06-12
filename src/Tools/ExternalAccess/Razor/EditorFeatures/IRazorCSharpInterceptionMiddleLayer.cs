// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal interface IRazorCSharpInterceptionMiddleLayer
    {
        bool CanHandle(string methodName);

        Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification);

        Task<JToken?> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken?>> sendRequest);
    }
}
