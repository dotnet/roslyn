// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.CodeAnalysis
{
    public static class UnsafeHelpers
    {
        public static SyntaxToken AsToken<T>(in T hopefullyTokenLike) where T : struct
        {
            return Unsafe.As<T, SyntaxToken>(ref Unsafe.AsRef(hopefullyTokenLike));
        }

        public static SyntaxNode AsNode<T>(T hopefullyNodeLike) where T : class
        {
            return Unsafe.As<SyntaxNode>(hopefullyNodeLike);
        }
    }
}
