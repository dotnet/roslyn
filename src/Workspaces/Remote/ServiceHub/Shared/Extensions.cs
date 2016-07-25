// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using StreamJsonRpc;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Remote
{
    internal static class Extensions
    {
        public static async Task InvokeAsync(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (var stream = new ServerDirectStream())
            {
                // send request by adding direct stream name to end of arguments
                var task = rpc.InvokeAsync(targetName, arguments.Concat(stream.Name).ToArray());

                // wait for asset source to respond
                await stream.WaitForDirectConnectionAsync(cancellationToken).ConfigureAwait(false);

                // run user task with direct stream
                await funcWithDirectStreamAsync(stream, cancellationToken).ConfigureAwait(false);

                // wait task to finish
                await task.ConfigureAwait(false);
            }
        }

        public static async Task<T> InvokeAsync<T>(
            this JsonRpc rpc, string targetName, IEnumerable<object> arguments, Func<Stream, CancellationToken, Task<T>> funcWithDirectStreamAsync, CancellationToken cancellationToken)
        {
            using (var stream = new ServerDirectStream())
            {
                // send request to asset source
                var task = rpc.InvokeAsync(targetName, arguments.Concat(stream.Name).ToArray());

                // wait for asset source to respond
                await stream.WaitForDirectConnectionAsync(cancellationToken).ConfigureAwait(false);

                // run user task with direct stream
                var result = await funcWithDirectStreamAsync(stream, cancellationToken).ConfigureAwait(false);

                // wait task to finish
                await task.ConfigureAwait(false);

                return result;
            }
        }
    }
}
