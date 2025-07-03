// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp;

partial class BoundAwaitableInfo
{
    private partial void Validate()
    {
        if (RuntimeAsyncAwaitCall is not null)
        {
            Debug.Assert(RuntimeAsyncAwaitCall.Method.ContainingType.ExtendedSpecialType == InternalSpecialType.System_Runtime_CompilerServices_AsyncHelpers);
            Debug.Assert(RuntimeAsyncAwaitCallPlaceholder is not null);

            switch (RuntimeAsyncAwaitCall.Method.Name)
            {
                case "Await":
                    Debug.Assert(GetAwaiter is null);
                    Debug.Assert(IsCompleted is null);
                    Debug.Assert(GetResult is null);
                    break;

                case "AwaitAwaiter":
                case "UnsafeAwaitAwaiter":
                    Debug.Assert(GetAwaiter is not null);
                    Debug.Assert(IsCompleted is not null);
                    Debug.Assert(GetResult is not null);
                    break;

                default:
                    Debug.Fail($"Unexpected RuntimeAsyncAwaitCall: {RuntimeAsyncAwaitCall.Method.Name}");
                    break;
            }
        }

        Debug.Assert(GetAwaiter is not null || RuntimeAsyncAwaitCall is not null || IsDynamic || HasErrors);
    }
}
