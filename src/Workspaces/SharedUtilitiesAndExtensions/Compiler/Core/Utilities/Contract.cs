// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities
{
    internal static class Contract
    {
        // Guidance on inlining:
        // ThrowXxx methods are used heavily across the code base. 
        // Inline their implementation of condition checking but don't inline the code that is only executed on failure.
        // This approach makes the common path efficient (both execution time and code size) 
        // while keeping the rarely executed code in a separate method.

        /// <summary>
        /// Throws a non-accessible exception if the provided value is null.  This method executes in
        /// all builds
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull<T>([NotNull] T value) where T : class?
        {
            if (value is null)
            {
                Fail("Unexpected null");
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is null.  This method executes in
        /// all builds
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfNull<T>([NotNull] T value, string message) where T : class?
        {
            if (value is null)
            {
                Fail(message);
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is false.  This method executes
        /// in all builds
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition)
        {
            if (!condition)
            {
                Fail("Unexpected false");
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is false.  This method executes
        /// in all builds
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition, string message)
        {
            if (!condition)
            {
                Fail(message);
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is true. This method executes in
        /// all builds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfTrue([DoesNotReturnIf(parameterValue: true)] bool condition)
        {
            if (condition)
            {
                Fail("Unexpected true");
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is true. This method executes in
        /// all builds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowIfTrue([DoesNotReturnIf(parameterValue: true)] bool condition, string message)
        {
            if (condition)
            {
                Fail(message);
            }
        }

        [DebuggerHidden]
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Fail(string message = "Unexpected")
            => throw new InvalidOperationException(message);
    }
}
