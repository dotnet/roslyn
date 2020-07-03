// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Determines the level of optimization of the generated code.
    /// </summary>
    public enum OptimizationLevel
    {
        /// <summary>
        /// Disables all optimizations and instruments the generated code to improve debugging experience.
        /// </summary>
        /// <remarks>
        /// The compiler prefers debuggability over performance. Do not use for code running in a production environment.
        /// <list type="bullet">
        /// <item><description>JIT optimizations are disabled via assembly level attribute (<see cref="DebuggableAttribute"/>).</description></item>
        /// <item><description>Edit and Continue is enabled.</description></item>
        /// <item><description>Slots for local variables are not reused, lifetime of local variables is extended to make the values available during debugging.</description></item>
        /// </list>
        /// <para>
        /// Corresponds to command line argument /optimize-.
        /// </para>
        /// </remarks>
        Debug = 0,

        /// <summary>
        /// Enables all optimizations, debugging experience might be degraded.
        /// </summary>
        /// <remarks>
        /// The compiler prefers performance over debuggability. Use for code running in a production environment.
        /// <list type="bullet">
        /// <item><description>JIT optimizations are enabled via assembly level attribute (<see cref="DebuggableAttribute"/>).</description></item>
        /// <item><description>Edit and Continue is disabled.</description></item>
        /// <item><description>Sequence points may be optimized away. As a result it might not be possible to place or hit a breakpoint.</description></item>
        /// <item><description>User-defined locals might be optimized away. They might not be available while debugging.</description></item>
        /// </list>
        /// <para>
        /// Corresponds to command line argument /optimize+.
        /// </para>
        /// </remarks>
        Release = 1
    }

    internal static class OptimizationLevelFacts
    {
        public static string ToPdbSerializedString(this OptimizationLevel optimization, bool debugPlusMode)
        => optimization switch
        {
            OptimizationLevel.Release => "release",
            OptimizationLevel.Debug => debugPlusMode ? "debug-plus" : "debug",
            _ => throw ExceptionUtilities.UnexpectedValue(optimization)
        };

        public static bool TryParsePdbSerializedString(string value, out OptimizationLevel optimizationLevel, out bool debugPlusMode)
        {
            if (value == "release")
            {
                optimizationLevel = OptimizationLevel.Release;
                debugPlusMode = false;
                return true;
            }
            else if (value == "debug")
            {
                optimizationLevel = OptimizationLevel.Debug;
                debugPlusMode = false;
                return true;
            }
            else if (value == "debug-plus")
            {
                optimizationLevel = OptimizationLevel.Debug;
                debugPlusMode = true;
                return true;
            }

            optimizationLevel = default;
            debugPlusMode = default;
            return false;
        }
    }


    internal static partial class EnumBounds
    {
        internal static bool IsValid(this OptimizationLevel value)
        {
            return value >= OptimizationLevel.Debug && value <= OptimizationLevel.Release;
        }
    }
}
