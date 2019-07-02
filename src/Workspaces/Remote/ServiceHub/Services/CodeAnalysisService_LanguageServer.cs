using System;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json.Linq;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class CodeAnalysisService
    {
        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(JToken arg)
        {
            var capabilities = new ServerCapabilities();
            capabilities.WorkspaceSymbolProvider = true;
            var result = new InitializeResult();
            result.Capabilities = capabilities;

            return result;
        }
    }
}
