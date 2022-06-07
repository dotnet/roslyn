// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;

namespace Roslyn.Test.Utilities
{
    public static class ScriptTaskExtensions
    {
        public static async Task<ScriptState<object>> ContinueWith(this Task<ScriptState> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync(code, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<ScriptState<object>> ContinueWith(this Task<ScriptState<object>> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync(code, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<ScriptState<T>> ContinueWith<T>(this Task<ScriptState> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync<T>(code, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<ScriptState<T>> ContinueWith<T>(this Task<ScriptState<object>> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync<T>(code, options, cancellationToken).ConfigureAwait(false);
        }

        public static async Task<ScriptState<object>> ContinueWith<S>(this Task<ScriptState<S>> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync(code, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
