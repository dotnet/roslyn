// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        internal static async Task FindLiteralReferencesAsync(
           object value,
           TypeCode typeCode,
           Solution solution,
           IStreamingFindLiteralReferencesProgress progress,
           CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.FindReference, cancellationToken))
            {
                var client = await RemoteHostClient.TryGetClientAsync(solution.Services, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    // Create a callback that we can pass to the server process to hear about the 
                    // results as it finds them.  When we hear about results we'll forward them to
                    // the 'progress' parameter which will then update the UI.
                    var serverCallback = new FindLiteralsServerCallback(solution, progress);

                    _ = await client.TryInvokeAsync<IRemoteSymbolFinderService>(
                        solution,
                        (service, solutionInfo, callbackId, cancellationToken) => service.FindLiteralReferencesAsync(solutionInfo, callbackId, value, typeCode, cancellationToken),
                        serverCallback,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await FindLiteralReferencesInCurrentProcessAsync(value, solution, progress, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        internal static Task FindLiteralReferencesInCurrentProcessAsync(
            object value, Solution solution,
            IStreamingFindLiteralReferencesProgress progress,
            CancellationToken cancellationToken)
        {
            var engine = new FindLiteralsSearchEngine(solution, progress, value);
            return engine.FindReferencesAsync(cancellationToken);
        }
    }
}
