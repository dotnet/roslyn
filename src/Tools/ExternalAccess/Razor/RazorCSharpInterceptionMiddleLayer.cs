// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Editor.Implementation.LanguageClient;
using Newtonsoft.Json.Linq;
using System.Text.Json;

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

        public override Task HandleNotificationAsync(string methodName, JsonElement methodParam, Func<JsonElement, Task> sendNotification)
        {
            // Razor only ever looks at the method name, so it is safe to pass null for all the Newtonsoft JToken params.
            return _razorCSharpInterceptionMiddleLayer.HandleNotificationAsync(methodName, null!, null!);
        }

        public override Task<JsonElement> HandleRequestAsync(string methodName, JsonElement methodParam, Func<JsonElement, Task<JsonElement>> sendRequest)
        {
            // Razor only implements a middlelayer for smeantic tokens refresh, which is a notification.
            // Cohosting makes all this unnecessary, so keeping this as minimal as possible until then.
            throw new NotImplementedException();
        }
    }
}
