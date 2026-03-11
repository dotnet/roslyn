// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor
{
    internal class RazorLanguageServerTargetWrapper : IRazorLanguageServerTarget
    {
        private readonly AbstractLanguageServer<RequestContext> _languageServer;

        public RazorLanguageServerTargetWrapper(AbstractLanguageServer<RequestContext> languageServer)
        {
            _languageServer = languageServer;
        }
    }
}
