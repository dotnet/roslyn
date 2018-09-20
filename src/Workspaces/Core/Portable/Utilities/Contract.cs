// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Roslyn.Utilities
{
    internal static class Contract
    {
        /// <summary>
        /// Throws a non-accessible exception if the provided value is null.  This method executes in
        /// all builds
        /// </summary>
        public static void ThrowIfNull<T>(T value, string message = null) where T : class
        {
            if (value == null)
            {
                message = message ?? "Unexpected Null";
                Fail(message);
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is false.  This method executes
        /// in all builds
        /// </summary>
        public static void ThrowIfFalse(bool condition, string message = null)
        {
            if (!condition)
            {
                message = message ?? "Unexpected false";
                Fail(message);
            }
        }

        /// <summary>
        /// Throws a non-accessible exception if the provided value is true. This method executes in
        /// all builds.
        /// </summary>
        public static void ThrowIfTrue(bool condition, string message = null)
        {
            if (condition)
            {
                message = message ?? "Unexpected true";
                Fail(message);
            }
        }

        [DebuggerHidden]
        public static void Fail(string message = "Unexpected")
        {
            throw new InvalidOperationException(message);
        }

        [DebuggerHidden]
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
