﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Scripting
{
    internal static class ScriptStateTaskExtensions
    {
        internal async static Task<T> CastAsync<S, T>(this Task<S> task) where S : T
        {
            return await task.ConfigureAwait(true);
        }

        internal async static Task<T> GetEvaluationResultAsync<T>(this Task<ScriptState<T>> task)
        {
            return (await task.ConfigureAwait(true)).ReturnValue;
        }
    }
}
