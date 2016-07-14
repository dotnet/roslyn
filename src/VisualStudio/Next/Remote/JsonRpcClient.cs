// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal class JsonRpcClient : IDisposable
    {
        private readonly Stream _stream;
        public readonly JsonRpc Rpc;

        public JsonRpcClient(Stream stream, object target = null)
        {
            _stream = stream;

            Rpc = JsonRpc.Attach(stream, target ?? this);
        }

        public Task InvokeAsync(string targetName, params object[] arguments)
        {
            return Rpc.InvokeAsync(targetName, arguments);
        }

        public Task<Result> InvokeAsync<Result>(string targetName, params object[] arguments)
        {
            return Rpc.InvokeAsync<Result>(targetName, arguments);
        }

        public void Dispose()
        {
            Rpc.Dispose();
            _stream.Dispose();
        }
    }
}
