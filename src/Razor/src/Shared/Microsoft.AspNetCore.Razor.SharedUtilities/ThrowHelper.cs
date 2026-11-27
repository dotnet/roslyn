// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Razor;

internal static class ThrowHelper
{
    /// <summary>
    ///  Throws an <see cref="ArgumentException"/> with a parameter name and a message.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <param name="message">
    ///  The message to include in the exception.
    /// </param>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentException(string? paramName, string message)
        => throw new ArgumentException(message, paramName);

    /// <summary>
    ///  Throws an <see cref="ArgumentException"/> with a parameter name and a message.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <param name="message">
    ///  The message to include in the exception.
    /// </param>
    /// <returns>
    ///  This method does not return because it always throws an exception, but it is defined to return a
    ///  <typeparamref name="T"/> value. This is useful for control flow scenarios where it is necessary to
    ///  throw an exception and return from a method.
    /// </returns>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowArgumentException<T>(string? paramName, string message)
        => throw new ArgumentException(message, paramName);

    /// <summary>
    ///  Throws an <see cref="ArgumentNullException"/> with a parameter name.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentNullException(string? paramName)
        => throw new ArgumentNullException(paramName);

    /// <summary>
    ///  Throws an <see cref="ArgumentNullException"/> with a parameter name.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <returns>
    ///  This method does not return because it always throws an exception, but it is defined to return a
    ///  <typeparamref name="T"/> value. This is useful for control flow scenarios where it is necessary to
    ///  throw an exception and return from a method.
    /// </returns>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowArgumentNullException<T>(string? paramName)
        => throw new ArgumentNullException(paramName);

    /// <summary>
    ///  Throws an <see cref="ArgumentOutOfRangeException"/> with a parameter name, message, and
    ///  the actual invalid value.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <param name="actualValue">
    ///  The actual invalid value to include in the exception.
    /// </param>
    /// <param name="message">
    ///  The message to include in the exception.
    /// </param>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeException(string? paramName, object? actualValue, string message)
        => throw new ArgumentOutOfRangeException(paramName, actualValue, message);

    /// <summary>
    ///  Throws an <see cref="ArgumentOutOfRangeException"/> with a parameter name and message.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <param name="message">
    ///  The message to include in the exception.
    /// </param>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeException(string? paramName, string message)
        => throw new ArgumentOutOfRangeException(paramName, message);

    /// <summary>
    ///  Throws an <see cref="ArgumentOutOfRangeException"/> with a parameter name.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowArgumentOutOfRangeException(string? paramName)
        => throw new ArgumentOutOfRangeException(paramName);

    /// <summary>
    ///  Throws an <see cref="ArgumentOutOfRangeException"/> with a parameter name, message, and
    ///  the actual invalid value.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <param name="actualValue">
    ///  The actual invalid value to include in the exception.
    /// </param>
    /// <param name="message">
    ///  The message to include in the exception.
    /// </param>
    /// <returns>
    ///  This method does not return because it always throws an exception, but it is defined to return a
    ///  <typeparamref name="T"/> value. This is useful for control flow scenarios where it is necessary to
    ///  throw an exception and return from a method.
    /// </returns>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowArgumentOutOfRangeException<T>(string? paramName, object? actualValue, string message)
        => throw new ArgumentOutOfRangeException(paramName, actualValue, message);

    /// <summary>
    ///  Throws an <see cref="ArgumentOutOfRangeException"/> with a parameter name and message.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <param name="message">
    ///  The message to include in the exception.
    /// </param>
    /// <returns>
    ///  This method does not return because it always throws an exception, but it is defined to return a
    ///  <typeparamref name="T"/> value. This is useful for control flow scenarios where it is necessary to
    ///  throw an exception and return from a method.
    /// </returns>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowArgumentOutOfRangeException<T>(string? paramName, string message)
        => throw new ArgumentOutOfRangeException(paramName, message);

    /// <summary>
    ///  Throws an <see cref="ArgumentOutOfRangeException"/> with a parameter name.
    /// </summary>
    /// <param name="paramName">
    ///  The parameter name to include in the exception.
    /// </param>
    /// <returns>
    ///  This method does not return because it always throws an exception, but it is defined to return a
    ///  <typeparamref name="T"/> value. This is useful for control flow scenarios where it is necessary to
    ///  throw an exception and return from a method.
    /// </returns>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowArgumentOutOfRangeException<T>(string? paramName)
        => throw new ArgumentOutOfRangeException(paramName);

    /// <summary>
    ///  Throws an <see cref="InvalidOperationException"/> with a message.
    /// </summary>
    /// <param name="message">
    ///  The message to include in the exception.
    /// </param>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ThrowInvalidOperationException(string message)
        => throw new InvalidOperationException(message);

    /// <summary>
    ///  Throws an <see cref="InvalidOperationException"/> with a message.
    /// </summary>
    /// <param name="message">
    ///  The message to include in the exception.
    /// </param>
    /// <returns>
    ///  This method does not return because it always throws an exception, but it is defined to return a
    ///  <typeparamref name="T"/> value. This is useful for control flow scenarios where it is necessary to
    ///  throw an exception and return from a method.
    /// </returns>
    /// <remarks>
    ///  This helps the JIT inline methods that need to throw an exceptions.
    /// </remarks>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T ThrowInvalidOperationException<T>(string message)
        => throw new InvalidOperationException(message);
}
