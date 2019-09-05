// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;
using RoslynLogger = Microsoft.CodeAnalysis.Internal.Log.Logger;

namespace Microsoft.CodeAnalysis.Remote
{
    /// <summary>
    /// Snapshot service in service hub side.
    /// 
    /// this service will be used to move over snapshot data from client to service hub
    /// </summary>
    internal partial class SnapshotService
    {
        private class JsonRpcAssetSource : AssetSource
        {
            private readonly SnapshotService _owner;

            public JsonRpcAssetSource(SnapshotService owner) : base(owner.AssetStorage)
            {
                _owner = owner;
            }

            public override async Task<IList<(Checksum, object)>> RequestAssetsAsync(int scopeId, ISet<Checksum> checksums, ISerializerService serializerService, CancellationToken cancellationToken)
            {
                using (RoslynLogger.LogBlock(FunctionId.SnapshotService_RequestAssetAsync, GetRequestLogInfo, scopeId, checksums, cancellationToken))
                {
                    // Surround this with a try/catch to report exceptions because we want to report any crashes here.
                    try
                    {
                        return await _owner.RunServiceAsync(() =>
                        {
                            return _owner.InvokeAsync(WellKnownServiceHubServices.AssetService_RequestAssetAsync,
                                new object[] { scopeId, checksums.ToArray() },
                                (s, c) => ReadAssets(s, scopeId, checksums, serializerService, c), cancellationToken);
                        }, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (ReportUnlessCanceled(ex, cancellationToken))
                    {
                        throw ExceptionUtilities.Unreachable;
                    }
                }
            }

            public override async Task<bool> IsExperimentEnabledAsync(string experimentName, CancellationToken cancellationToken)
            {
                using (RoslynLogger.LogBlock(FunctionId.SnapshotService_IsExperimentEnabledAsync, experimentName, cancellationToken))
                {
                    return await _owner.RunServiceAsync(() =>
                    {
                        return _owner.InvokeAsync<bool>(WellKnownServiceHubServices.AssetService_IsExperimentEnabledAsync,
                            new object[] { experimentName }, cancellationToken);
                    }, cancellationToken).ConfigureAwait(false);
                }
            }

            private bool ReportUnlessCanceled(Exception ex, CancellationToken cancellationToken)
            {
                if (!cancellationToken.IsCancellationRequested && _owner.IsDisposed)
                {
                    // kill OOP if snapshot service got disconnected due to this exception.
                    FailFast.OnFatalException(ex);
                }

                return false;
            }

            private IList<(Checksum, object)> ReadAssets(
                Stream stream,
                int scopeId,
                ISet<Checksum> checksums,
                ISerializerService serializerService,
                CancellationToken cancellationToken)
            {
                var results = new List<(Checksum, object)>();

                using var reader = ObjectReader.TryGetReader(stream, cancellationToken);

                Debug.Assert(reader != null,
@"We only ge a reader for data transmitted between live processes.
This data should always be correct as we're never persisting the data between sessions.");

                var responseScopeId = reader.ReadInt32();
                Contract.ThrowIfFalse(scopeId == responseScopeId);

                var count = reader.ReadInt32();
                Contract.ThrowIfFalse(count == checksums.Count);

                for (var i = 0; i < count; i++)
                {
                    var responseChecksum = Checksum.ReadFrom(reader);
                    Contract.ThrowIfFalse(checksums.Contains(responseChecksum));

                    var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                    // in service hub, cancellation means simply closed stream
                    var @object = serializerService.Deserialize<object>(kind, reader, cancellationToken);

                    results.Add((responseChecksum, @object));
                }

                return results;
            }

            private static string GetRequestLogInfo(int serviceId, IEnumerable<Checksum> checksums)
            {
                return $"{serviceId} - {Checksum.GetChecksumsLogInfo(checksums)}";
            }
        }
    }
}
