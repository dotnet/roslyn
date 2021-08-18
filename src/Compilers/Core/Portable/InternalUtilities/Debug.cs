﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal static class RoslynDebug
    {
        /// <inheritdoc cref="Debug.Assert(bool)"/>
        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool b) => Debug.Assert(b);

        /// <inheritdoc cref="Debug.Assert(bool, string)"/>
        [Conditional("DEBUG")]
        public static void Assert([DoesNotReturnIf(false)] bool b, string message)
            => Debug.Assert(b, message);

        [Conditional("DEBUG")]
        public static void AssertNotNull<T>([NotNull] T value)
        {
            Assert(value is object, "Unexpected null reference");
        }

        /// <summary>
        /// Generally <see cref="Debug.Assert(bool)"/> is a sufficient method for enforcing DEBUG 
        /// only invariants in our code. When it triggers that providse a nice stack trace for 
        /// investigation. Generally that is enough.
        /// 
        /// <para>There are cases for which a stack is not enough and we need a full heap dump to 
        /// investigate the failure. This method takes care of that. The behavior is that when running
        /// in our CI environment if the assert triggers we will rudely crash the process and 
        /// produce a heap dump for investigation.</para>
        /// </summary>
        [Conditional("DEBUG")]
        internal static void AssertOrFailFast([DoesNotReturnIf(false)] bool condition, string? message = null)
        {
#if NET20 || NETSTANDARD1_3
            Debug.Assert(condition);
#else
            if (!condition)
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER")))
                {
                    message ??= $"{nameof(AssertOrFailFast)} failed";
                    var stackTrace = new StackTrace();
                    Console.WriteLine(message);
                    Console.WriteLine(stackTrace);

                    // Use FailFast so that the process fails rudely and goes through 
                    // windows error reporting (on Windows at least). This will allow our 
                    // Helix environment to capture crash dumps for future investigation
                    Environment.FailFast(message);
                }
                else
                {
                    Debug.Assert(false, message);
                }
            }
#endif
        }
    }
}
