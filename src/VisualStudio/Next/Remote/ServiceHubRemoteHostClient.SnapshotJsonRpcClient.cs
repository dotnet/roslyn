// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;
using StreamJsonRpc;

namespace Microsoft.VisualStudio.LanguageServices.Remote
{
    internal partial class ServiceHubRemoteHostClient
    {
        /// <summary>
        /// Communication channel between remote host client and remote host.
        /// 
        /// this is framework's back channel to talk to remote host
        /// 
        /// for example, this will be used to deliver missing assets in remote host.
        /// 
        /// each remote host client will have its own back channel so that it can work isolated
        /// with other clients.
        /// </summary>
        private class SnapshotJsonRpcClient : JsonRpcClient
        {
            private readonly ISolutionChecksumService _checksumService;
            private readonly CancellationTokenSource _source;

            public SnapshotJsonRpcClient(ISolutionChecksumService checksumService, Stream stream) :
                base(stream)
            {
                _checksumService = checksumService;
                _source = new CancellationTokenSource();
            }

            protected override object GetCallbackTarget() => this;

            /// <summary>
            /// this is callback from remote host side to get asset associated with checksum from VS.
            /// </summary>
            public async Task RequestAssetAsync(int serviceId, byte[] checksum, string streamName)
            {
                try
                {
                    using (var stream = await DirectStream.GetAsync(streamName, _source.Token).ConfigureAwait(false))
                    {
                        using (var writer = new ObjectWriter(stream))
                        {
                            writer.WriteInt32(serviceId);
                            writer.WriteArray(checksum);

                            var checksumObject = _checksumService.GetChecksumObject(new Checksum(checksum), _source.Token);
                            writer.WriteString(checksumObject.Kind);

                            await checksumObject.WriteToAsync(writer, _source.Token).ConfigureAwait(false);
                        }

                        await stream.FlushAsync(_source.Token).ConfigureAwait(false);
                    }
                }
                catch (IOException)
                {
                    // remote host side is cancelled (client stream connection is closed)
                    // can happen if pinned solution scope is disposed
                }
                catch (OperationCanceledException)
                {
                    // rpc connection is closed. 
                    // can happen if pinned solution scope is disposed
                }
            }

            protected override void OnDisconnected(object sender, JsonRpcDisconnectedEventArgs e)
            {
                _source.Cancel();
            }
        }
    }
}
