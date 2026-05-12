// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor;

internal static partial class Assumed
{
    public static void NotNull<T>(
        [NotNull] this T? value,
        string? message = null,
        [CallerArgumentExpression(nameof(value))] string? valueExpression = null,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
        where T : class
    {
        if (value is null)
        {
            ThrowInvalidOperation(message ?? SR.FormatExpected_0_to_be_non_null(valueExpression), path, line);
        }
    }

    public static void NotNull<T>(
        [NotNull] this T? value,
        [InterpolatedStringHandlerArgument(nameof(value))] ThrowIfNullInterpolatedStringHandler<T> message,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
        where T : class
    {
        if (value is null)
        {
            ThrowInvalidOperation(message.GetFormattedText(), path, line);
        }
    }

    public static void NotNull<T>(
        [NotNull] this T? value,
        string? message = null,
        [CallerArgumentExpression(nameof(value))] string? valueExpression = null,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
        where T : struct
    {
        if (value is null)
        {
            ThrowInvalidOperation(message ?? SR.FormatExpected_0_to_be_non_null(valueExpression), path, line);
        }
    }

    public static void NotNull<T>(
        [NotNull] this T? value,
        [InterpolatedStringHandlerArgument(nameof(value))] ThrowIfNullInterpolatedStringHandler<T> message,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
        where T : struct
    {
        if (value is null)
        {
            ThrowInvalidOperation(message.GetFormattedText(), path, line);
        }
    }

    public static void False(
        [DoesNotReturnIf(true)] bool condition,
        string? message = null,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
    {
        if (condition)
        {
            ThrowInvalidOperation(message ?? SR.Expected_condition_to_be_false, path, line);
        }
    }

    public static void False(
        [DoesNotReturnIf(true)] bool condition,
        [InterpolatedStringHandlerArgument(nameof(condition))] ThrowIfFalseInterpolatedStringHandler message,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
    {
        if (condition)
        {
            ThrowInvalidOperation(message.GetFormattedText(), path, line);
        }
    }

    [DebuggerStepThrough]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void True(
        [DoesNotReturnIf(false)] bool condition,
        string? message = null,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
    {
        if (!condition)
        {
            ThrowInvalidOperation(message ?? SR.Expected_condition_to_be_true, path, line);
        }
    }

    /// <summary>
    ///  Can be called at points that are assumed to be unreachable at runtime.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    [DoesNotReturn]
    public static void Unreachable(
        string? message = null,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
        => ThrowInvalidOperation(message ?? SR.This_program_location_is_thought_to_be_unreachable, path, line);

    /// <summary>
    ///  Can be called at points that are assumed to be unreachable at runtime.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    [DoesNotReturn]
    public static void Unreachable(
        UnreachableInterpolatedStringHandler message,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
        => ThrowInvalidOperation(message.GetFormattedText(), path, line);

    /// <summary>
    ///  Can be called at points that are assumed to be unreachable at runtime.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    [DoesNotReturn]
    public static T Unreachable<T>(
        string? message = null,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
        => ThrowInvalidOperation<T>(message ?? SR.This_program_location_is_thought_to_be_unreachable, path, line);

    /// <summary>
    ///  Can be called at points that are assumed to be unreachable at runtime.
    /// </summary>
    /// <exception cref="InvalidOperationException"/>
    [DoesNotReturn]
    public static T Unreachable<T>(
        UnreachableInterpolatedStringHandler message,
        [CallerFilePath] string? path = null,
        [CallerLineNumber] int line = 0)
        => ThrowInvalidOperation<T>(message.GetFormattedText(), path, line);

    [DebuggerHidden]
    [DoesNotReturn]
    private static void ThrowInvalidOperation(string message, string? path, int line)
        => ThrowHelper.ThrowInvalidOperationException(message + Environment.NewLine + SR.FormatFile_0_Line_1(path, line));

    [DebuggerHidden]
    [DoesNotReturn]
    private static T ThrowInvalidOperation<T>(string message, string? path, int line)
        => ThrowHelper.ThrowInvalidOperationException<T>(message + Environment.NewLine + SR.FormatFile_0_Line_1(path, line));
}
