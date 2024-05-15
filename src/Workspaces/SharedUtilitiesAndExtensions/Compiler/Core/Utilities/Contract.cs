// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Roslyn.Utilities;

internal static partial class Contract
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
    public static void ThrowIfNull<T>([NotNull] T value, [CallerLineNumber] int lineNumber = 0) where T : class?
    {
        if (value is null)
        {
            Fail("Unexpected null", lineNumber);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is null.  This method executes in
    /// all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>([NotNull] T? value, [CallerLineNumber] int lineNumber = 0) where T : struct
    {
        if (value is null)
        {
            Fail("Unexpected null", lineNumber);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is null.  This method executes in
    /// all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>([NotNull] T value, string message, [CallerLineNumber] int lineNumber = 0)
    {
        if (value is null)
        {
            Fail(message, lineNumber);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is null.  This method executes in
    /// all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>([NotNull] T value, [InterpolatedStringHandlerArgument("value")] ThrowIfNullInterpolatedStringHandler<T> message, [CallerLineNumber] int lineNumber = 0)
    {
        if (value is null)
        {
            Fail(message.GetFormattedText(), lineNumber);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is false.  This method executes
    /// in all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition, [CallerLineNumber] int lineNumber = 0)
    {
        if (!condition)
        {
            Fail("Unexpected false", lineNumber);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is false.  This method executes
    /// in all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition, string message, [CallerLineNumber] int lineNumber = 0)
    {
        if (!condition)
        {
            Fail(message, lineNumber);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is false.  This method executes
    /// in all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition, [InterpolatedStringHandlerArgument("condition")] ThrowIfFalseInterpolatedStringHandler message, [CallerLineNumber] int lineNumber = 0)
    {
        if (!condition)
        {
            Fail(message.GetFormattedText(), lineNumber);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is true. This method executes in
    /// all builds.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfTrue([DoesNotReturnIf(parameterValue: true)] bool condition, [CallerLineNumber] int lineNumber = 0)
    {
        if (condition)
        {
            Fail("Unexpected true", lineNumber);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is true. This method executes in
    /// all builds.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfTrue([DoesNotReturnIf(parameterValue: true)] bool condition, string message, [CallerLineNumber] int lineNumber = 0)
    {
        if (condition)
        {
            Fail(message, lineNumber);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is true. This method executes in
    /// all builds.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfTrue([DoesNotReturnIf(parameterValue: true)] bool condition, [InterpolatedStringHandlerArgument("condition")] ThrowIfTrueInterpolatedStringHandler message, [CallerLineNumber] int lineNumber = 0)
    {
        if (condition)
        {
            Fail(message.GetFormattedText(), lineNumber);
        }
    }

    [DebuggerHidden]
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Fail(string message = "Unexpected", [CallerLineNumber] int lineNumber = 0)
        => throw new InvalidOperationException($"{message} - line {lineNumber}");
}
