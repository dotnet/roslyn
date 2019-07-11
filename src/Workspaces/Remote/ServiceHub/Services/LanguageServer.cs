using System;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Microsoft.CodeAnalysis.Remote
{
    internal partial class LanguageServer : ServiceHubServiceBase
    {
        public LanguageServer(Stream stream, IServiceProvider serviceProvider)
            : base(serviceProvider, stream)
        {
            StartService();
        }

        [JsonRpcMethod(Methods.InitializeName)]
        public object Initialize(int? processId, string rootPath, Uri rootUri, ClientCapabilities capabilities, TraceSetting trace, CancellationToken cancellationToken)
        {
            return new InitializeResult()
            {
                Capabilities = new MSLSPServerCapabilities()
                {
                    WorkspaceStreamingSymbolProvider = true
                }
            };
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        public void Shutdown(CancellationToken cancellationToken)
        {
        }
    }
}
