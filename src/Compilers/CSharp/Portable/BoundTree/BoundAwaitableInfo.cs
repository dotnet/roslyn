// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp;

partial class BoundAwaitableInfo
{
    private partial void Validate()
    {
        if (RuntimeAsyncAwaitMethod is not null)
        {
            Debug.Assert(RuntimeAsyncAwaitMethod.ContainingType.ExtendedSpecialType == InternalSpecialType.System_Runtime_CompilerServices_AsyncHelpers);

            switch (RuntimeAsyncAwaitMethod.Name)
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
                    Debug.Fail($"Unexpected RuntimeAsyncAwaitMethod: {RuntimeAsyncAwaitMethod.Name}");
                    break;
            }
        }
    }
}
