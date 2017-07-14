// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    // This is a workaround for a limitation in vs-threading.
    // https://github.com/dotnet/roslyn/issues/19042
    internal class JsonRpcMessageHandler : HeaderDelimitedMessageHandler
    {
        public JsonRpcMessageHandler(Stream sendingStream, Stream receivingStream)
            : base(sendingStream, receivingStream)
        {
        }

        protected override void Dispose(bool disposing)
        {
            // Do not call base.Dispose. We do not want the AsyncSemaphore instances to be disposed due to a race
            // condition.

            if (disposing)
            {
                ReceivingStream?.Dispose();
                SendingStream?.Dispose();
            }
        }
    }
}
