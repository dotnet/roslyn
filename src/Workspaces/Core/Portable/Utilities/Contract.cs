// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class Contract
    {
        /// <summary>
        /// Throws a non-accessible exception if the provided value is null.  This method executes in
        /// all builds
        /// </summary>
        public static void ThrowIfNull<T>([NotNull] T value, string? message = null) where T : class?
        {
            if (value == null)
            {
                message ??= "Unexpected Null";
                Fail(message);
            }
        }

        [return: NotNull]
        public static T NotNull<T>(T value, string? message = null) where T : class?
        {
            ThrowIfNull(value);
            return value;
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is false.  This method executes
        /// in all builds
        /// </summary>
        public static void ThrowIfFalse([DoesNotReturnIf(parameterValue: false)] bool condition, string? message = null)
        {
            if (!condition)
            {
                message ??= "Unexpected false";
                Fail(message);
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is true. This method executes in
        /// all builds.
        /// </summary>
        public static void ThrowIfTrue([DoesNotReturnIf(parameterValue: true)] bool condition, string? message = null)
        {
            if (condition)
            {
                message ??= "Unexpected true";
                Fail(message);
            }
        }

        [DebuggerHidden]
        [DoesNotReturn]
        public static void Fail(string message = "Unexpected")
        {
            throw new InvalidOperationException(message);
        }

        [DebuggerHidden]
        [DoesNotReturn]
        public static T FailWithReturn<T>(string message = "Unexpected")
        {
            throw new InvalidOperationException(message);
        }

        public static void InvalidEnumValue<T>(T value)
        {
            Fail(string.Format("Invalid Enumeration value {0}", value));
        }
    }
}
