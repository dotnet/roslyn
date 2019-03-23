// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Scripting;

namespace Roslyn.Test.Utilities
{
    public static class ScriptTaskExtensions
    {
        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Needs review: https://github.com/dotnet/roslyn/issues/34287")]
        public static async Task<ScriptState<object>> ContinueWith(this Task<ScriptState> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync(code, options, cancellationToken).ConfigureAwait(false);
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Needs review: https://github.com/dotnet/roslyn/issues/34287")]
        public static async Task<ScriptState<object>> ContinueWith(this Task<ScriptState<object>> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync(code, options, cancellationToken).ConfigureAwait(false);
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Needs review: https://github.com/dotnet/roslyn/issues/34287")]
        public static async Task<ScriptState<T>> ContinueWith<T>(this Task<ScriptState> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync<T>(code, options, cancellationToken).ConfigureAwait(false);
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Needs review: https://github.com/dotnet/roslyn/issues/34287")]
        public static async Task<ScriptState<T>> ContinueWith<T>(this Task<ScriptState<object>> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync<T>(code, options, cancellationToken).ConfigureAwait(false);
        }

        [SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "Needs review: https://github.com/dotnet/roslyn/issues/34287")]
        public static async Task<ScriptState<object>> ContinueWith<S>(this Task<ScriptState<S>> task, string code, ScriptOptions options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await (await task.ConfigureAwait(false)).ContinueWithAsync(code, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
