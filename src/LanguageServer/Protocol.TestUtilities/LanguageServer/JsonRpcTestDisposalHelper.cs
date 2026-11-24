// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using StreamJsonRpc;

namespace Roslyn.Test.Utilities;

internal static class JsonRpcTestDisposalHelper
{
    internal static async Task DisposeAndWaitForCompletionAsync(JsonRpc jsonRpc)
    {
        jsonRpc.Dispose();

        try
        {
            await jsonRpc.Completion.ConfigureAwait(false);
        }
        catch (ConnectionLostException)
        {
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }
}
