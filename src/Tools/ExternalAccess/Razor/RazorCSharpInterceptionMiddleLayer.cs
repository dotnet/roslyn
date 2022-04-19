// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Newtonsoft.Json.Linq;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    [Export(typeof(AbstractLanguageClientMiddleLayer))]
    [Shared]
    internal class RazorCSharpInterceptionMiddleLayerWrapper : AbstractLanguageClientMiddleLayer
    {
        private readonly IRazorCSharpInterceptionMiddleLayer _razorCSharpInterceptionMiddleLayer;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public RazorCSharpInterceptionMiddleLayerWrapper(IRazorCSharpInterceptionMiddleLayer razorCSharpInterceptionMiddleLayer)
        {
            _razorCSharpInterceptionMiddleLayer = razorCSharpInterceptionMiddleLayer;
        }

        public override bool CanHandle(string methodName)
            => _razorCSharpInterceptionMiddleLayer.CanHandle(methodName);

        public override Task HandleNotificationAsync(string methodName, JToken methodParam, Func<JToken, Task> sendNotification)
            => _razorCSharpInterceptionMiddleLayer.HandleNotificationAsync(methodName, methodParam, sendNotification);

        public override Task<JToken?> HandleRequestAsync(string methodName, JToken methodParam, Func<JToken, Task<JToken?>> sendRequest)
            => _razorCSharpInterceptionMiddleLayer.HandleRequestAsync(methodName, methodParam, sendRequest);
    }
}
