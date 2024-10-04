// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Serialization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote;

/// <summary>
/// Provides solution assets present locally (in the current process) to a remote process where the solution is being replicated to.
/// </summary>
internal sealed class SolutionAssetProvider(SolutionServices services) : ISolutionAssetProvider
{
    public const string ServiceName = "SolutionAssetProvider";

    internal static ServiceDescriptor ServiceDescriptor { get; } = ServiceDescriptor.CreateInProcServiceDescriptor(ServiceDescriptors.ComponentName, ServiceName, suffix: "", ServiceDescriptors.GetFeatureDisplayName);

    private readonly SolutionAssetStorage _assetStorage = services.GetRequiredService<ISolutionAssetStorageProvider>().AssetStorage;
    private readonly ISerializerService _serializer = services.GetRequiredService<ISerializerService>();

    public ValueTask WriteAssetsAsync(
        PipeWriter pipeWriter,
        Checksum solutionChecksum,
        AssetPath assetPath,
        ReadOnlyMemory<Checksum> checksums,
        CancellationToken cancellationToken)
    {
        // Suppress ExecutionContext flow for asynchronous operations operate on the pipe. In addition to avoiding
        // ExecutionContext allocations, this clears the LogicalCallContext and avoids the need to clone data set by
        // CallContext.LogicalSetData at each yielding await in the task tree.
        //
        // ⚠ DO NOT AWAIT INSIDE THE USING BLOCK LEXICALLY (it's fine to await within the call to
        // WriteAssetsSuppressedFlowAsync). The Dispose method that restores ExecutionContext flow must run on the same
        // thread where SuppressFlow was originally run.
        using var _ = FlowControlHelper.TrySuppressFlow();
        return WriteAssetsSuppressedFlowAsync(pipeWriter, solutionChecksum, assetPath, checksums, cancellationToken);

        async ValueTask WriteAssetsSuppressedFlowAsync(PipeWriter pipeWriter, Checksum solutionChecksum, AssetPath assetPath, ReadOnlyMemory<Checksum> checksums, CancellationToken cancellationToken)
        {
            // The responsibility is on us (as per the requirements of RemoteCallback.InvokeAsync) to Complete the
            // pipewriter.  This will signal to streamjsonrpc that the writer passed into it is complete, which will
            // allow the calling side know to stop reading results.
            Exception? exception = null;
            try
            {
                var scope = _assetStorage.GetScope(solutionChecksum);
                var writer = new RemoteHostAssetWriter(pipeWriter, scope, assetPath, checksums, _serializer);
                await writer.WriteDataAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when ((exception = ex) == null)
            {
                throw ExceptionUtilities.Unreachable();
            }
            finally
            {
                await pipeWriter.CompleteAsync(exception).ConfigureAwait(false);
            }
        }
    }
}
