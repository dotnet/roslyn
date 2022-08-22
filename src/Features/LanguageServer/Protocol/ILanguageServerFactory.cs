// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using MicrosoftaCodeAnalysis.LanguageServer.HandlerHandlerHandler;
using Microsoft.CommonLanguageServerProtocollFrameworkorkork;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal interface ILanguageServerFactory
    {
        public Task<AbstractLanguageServer<RequestContext>> CreateAsync(
            JsonRpc jsonRpc,
            ICapabilitiesProvider capabilitiesProvider,
            IRoslynLspLogger logger);
    }
}
