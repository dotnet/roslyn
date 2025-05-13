// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

#if !MICROSOFT_CODEANALYSIS_CONTRACTS_NO_CONTRACT

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis;

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
    public static void ThrowIfNull<T>([NotNull] T value, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null) where T : class?
    {
        if (value is null)
        {
            Fail("Unexpected null", lineNumber, filePath);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is null.  This method executes in
    /// all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>([NotNull] T? value, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null) where T : struct
    {
        if (value is null)
        {
            Fail("Unexpected null", lineNumber, filePath);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is null.  This method executes in
    /// all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>([NotNull] T value, string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
    {
        if (value is null)
        {
            Fail(message, lineNumber, filePath);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is null.  This method executes in
    /// all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>([NotNull] T value, [InterpolatedStringHandlerArgument("value")] ThrowIfNullInterpolatedStringHandler<T> message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
    {
        if (value is null)
        {
            Fail(message.GetFormattedText(), lineNumber, filePath);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is false.  This method executes
    /// in all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
    {
        if (!condition)
        {
            Fail("Unexpected false", lineNumber, filePath);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is false.  This method executes
    /// in all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition, string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
    {
        if (!condition)
        {
            Fail(message, lineNumber, filePath);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is false.  This method executes
    /// in all builds
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition, [InterpolatedStringHandlerArgument("condition")] ThrowIfFalseInterpolatedStringHandler message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
    {
        if (!condition)
        {
            Fail(message.GetFormattedText(), lineNumber, filePath);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is true. This method executes in
    /// all builds.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfTrue([DoesNotReturnIf(parameterValue: true)] bool condition, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
    {
        if (condition)
        {
            Fail("Unexpected true", lineNumber, filePath);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is true. This method executes in
    /// all builds.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfTrue([DoesNotReturnIf(parameterValue: true)] bool condition, string message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
    {
        if (condition)
        {
            Fail(message, lineNumber, filePath);
        }
    }

    /// <summary>
    /// Throws a non-accessible exception if the provided value is true. This method executes in
    /// all builds.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfTrue([DoesNotReturnIf(parameterValue: true)] bool condition, [InterpolatedStringHandlerArgument("condition")] ThrowIfTrueInterpolatedStringHandler message, [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
    {
        if (condition)
        {
            Fail(message.GetFormattedText(), lineNumber, filePath);
        }
    }

    [DebuggerHidden]
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Fail(string message = "Unexpected", [CallerLineNumber] int lineNumber = 0, [CallerFilePath] string? filePath = null)
    {
        var fileName = filePath is null ? null : Path.GetFileName(filePath);
        throw new InvalidOperationException($"{message} - file {fileName} line {lineNumber}");
    }
}

#endif
