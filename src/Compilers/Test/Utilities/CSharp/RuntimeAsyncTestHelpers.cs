// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    /// <summary>
    /// Helper utilities for runtime async tests.
    /// </summary>
    public static class RuntimeAsyncTestHelpers
    {
        /// <summary>
        /// Gets whether runtime async tests are enabled via the DOTNET_RuntimeAsync environment variable.
        /// </summary>
        public static bool IsRuntimeAsyncEnabled { get; } = Environment.GetEnvironmentVariable("DOTNET_RuntimeAsync") == "1";

        /// <summary>
        /// Determines the expected output for a test that may use runtime async features.
        /// </summary>
        /// <param name="output">The expected output when running normally.</param>
        /// <returns>
        /// The expected output string if the test should execute and validate output, 
        /// or null if the test should not execute the assembly.
        /// </returns>
        public static string? ExpectedOutput(string? output) => ExecutionConditionUtil.IsCoreClr && IsRuntimeAsyncEnabled ? output : null;
    }
}
