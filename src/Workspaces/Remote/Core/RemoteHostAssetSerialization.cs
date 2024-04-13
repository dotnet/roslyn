// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Serialization;
using Nerdbank.Streams;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class RemoteHostAssetSerialization
    {
        public static ValueTask ReadDataAsync<T, TArg>(
            PipeReader pipeReader, Checksum solutionChecksum, int objectCount, ISerializerService serializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
        {
            // Suppress ExecutionContext flow for asynchronous operations operate on the pipe. In addition to avoiding
            // ExecutionContext allocations, this clears the LogicalCallContext and avoids the need to clone data set by
            // CallContext.LogicalSetData at each yielding await in the task tree.
            //
            // ⚠ DO NOT AWAIT INSIDE THE USING. The Dispose method that restores ExecutionContext flow must run on the
            // same thread where SuppressFlow was originally run.
            using var _ = FlowControlHelper.TrySuppressFlow();
            return ReadDataSuppressedFlowAsync(pipeReader, solutionChecksum, objectCount, serializerService, callback, arg, cancellationToken);

            static async ValueTask ReadDataSuppressedFlowAsync(
                PipeReader pipeReader, Checksum solutionChecksum, int objectCount, ISerializerService serializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
            {
                using var stream = await pipeReader.AsPrebufferedStreamAsync(cancellationToken).ConfigureAwait(false);
                ReadData(stream, solutionChecksum, objectCount, serializerService, callback, arg, cancellationToken);
            }
        }

        public static void ReadData<T, TArg>(
            Stream stream, Checksum solutionChecksum, int objectCount, ISerializerService serializerService, Action<Checksum, T, TArg> callback, TArg arg, CancellationToken cancellationToken)
        {
            using var reader = ObjectReader.GetReader(stream, leaveOpen: true, cancellationToken);

            // Ensure that no invariants were broken and that both sides of the communication channel are talking about
            // the same pinned solution.
            var responseSolutionChecksum = Checksum.ReadFrom(reader);
            Contract.ThrowIfFalse(solutionChecksum == responseSolutionChecksum);

            for (int i = 0, n = objectCount; i < n; i++)
            {
                var checksum = Checksum.ReadFrom(reader);
                var kind = (WellKnownSynchronizationKind)reader.ReadInt32();

                // in service hub, cancellation means simply closed stream
                var result = serializerService.Deserialize(kind, reader, cancellationToken);
                Contract.ThrowIfNull(result);
                callback(checksum, (T)result, arg);
            }
        }
    }
}
