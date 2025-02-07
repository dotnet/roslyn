// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.StackTraceExplorer;

namespace Microsoft.CodeAnalysis.Remote;

internal sealed class RemoteStackTraceExplorerService : BrokeredServiceBase, IRemoteStackTraceExplorerService
{
    internal sealed class Factory : FactoryBase<IRemoteStackTraceExplorerService>
    {
        protected override IRemoteStackTraceExplorerService CreateService(in ServiceConstructionArguments arguments)
            => new RemoteStackTraceExplorerService(arguments);
    }

    public RemoteStackTraceExplorerService(in ServiceConstructionArguments arguments) : base(arguments)
    {
    }

    public ValueTask<SerializableDefinitionItem?> TryFindDefinitionAsync(Checksum solutionChecksum, string frameString, StackFrameSymbolPart symbolPart, CancellationToken cancellationToken)
    {
        return RunServiceAsync(solutionChecksum, async solution =>
        {
            var result = await StackTraceAnalyzer.AnalyzeAsync(frameString, cancellationToken).ConfigureAwait(false);
            if (result.ParsedFrames.Length != 1 || result.ParsedFrames[0] is not ParsedStackFrame parsedFrame)
            {
                throw new InvalidOperationException();
            }

            var definition = await StackTraceExplorerUtilities.GetDefinitionAsync(solution, parsedFrame.Root, symbolPart, cancellationToken).ConfigureAwait(false);
            if (definition is null)
            {
                return (SerializableDefinitionItem?)null;
            }

            return SerializableDefinitionItem.Dehydrate(id: 0, definition);
        }, cancellationToken);
    }
}
