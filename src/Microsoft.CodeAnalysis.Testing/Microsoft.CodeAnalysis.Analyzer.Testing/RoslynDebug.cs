// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class RoslynDebug
    {
        /// <inheritdoc cref="Debug.Assert(bool)"/>
        [Conditional("DEBUG")]
        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1405:Debug.Assert should provide message text", Justification = "This is a wrapper for Debug.Assert.")]
        public static void Assert([DoesNotReturnIf(false)] bool b)
            => Debug.Assert(b);

        /// <inheritdoc cref="Debug.Assert(bool, string)"/>
        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool b, string message)
            => Debug.Assert(b, message);

        [Conditional("DEBUG")]
        public static void AssertNotNull<T>([NotNull] T value)
            where T : class?
        {
            Assert(value is object, "Unexpected null reference");
        }
    }
}
