// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class RoslynDebug
    {
        /// <inheritdoc cref="Debug.Assert(bool)"/>
        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)]bool b) => Debug.Assert(b);

        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)]bool b, string message)
            => Debug.Assert(b, message);
    }
}
