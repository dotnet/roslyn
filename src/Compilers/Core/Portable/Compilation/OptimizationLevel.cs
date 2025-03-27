// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// <para>
        /// The compiler prefers debuggability over performance. Do not use for code running in a production environment.
        /// </para>
        /// <list type="bullet">
        /// <item><description>JIT optimizations are disabled via assembly level attribute (<see cref="DebuggableAttribute"/>).</description></item>
        /// <item><description>Edit and Continue is enabled.</description></item>
        /// <item><description>Slots for local variables are not reused, lifetime of local variables is extended to make the values available during debugging.</description></item>
        /// </list>
        /// <para>
        /// Corresponds to command line argument /optimize-.
        /// </para>
        /// </summary>
        Debug = 0,

        /// <summary>
        /// Enables all optimizations, debugging experience might be degraded.
        /// <para>
        /// The compiler prefers performance over debuggability. Use for code running in a production environment.
        /// </para>
        /// <list type="bullet">
        /// <item><description>JIT optimizations are enabled via assembly level attribute (<see cref="DebuggableAttribute"/>).</description></item>
        /// <item><description>Edit and Continue is disabled.</description></item>
        /// <item><description>Sequence points may be optimized away. As a result it might not be possible to place or hit a breakpoint.</description></item>
        /// <item><description>User-defined locals might be optimized away. They might not be available while debugging.</description></item>
        /// </list>
        /// <para>
        /// Corresponds to command line argument /optimize+.
        /// </para>
        /// </summary>
        Release = 1
    }

    internal static class OptimizationLevelFacts
    {
        internal static (OptimizationLevel OptimizationLevel, bool DebugPlus) DefaultValues => (OptimizationLevel.Debug, false);

        public static string ToPdbSerializedString(this OptimizationLevel optimization, bool debugPlusMode)
            => (optimization, debugPlusMode) switch
            {
                (OptimizationLevel.Release, true) => "release-debug-plus",
                (OptimizationLevel.Release, false) => "release",
                (OptimizationLevel.Debug, true) => "debug-plus",
                (OptimizationLevel.Debug, false) => "debug",
                _ => throw ExceptionUtilities.UnexpectedValue(optimization)
            };

        public static bool TryParsePdbSerializedString(string value, out OptimizationLevel optimizationLevel, out bool debugPlusMode)
        {
            switch (value)
            {
                case "release-debug-plus":
                    optimizationLevel = OptimizationLevel.Release;
                    debugPlusMode = true;
                    return true;
                case "release":
                    optimizationLevel = OptimizationLevel.Release;
                    debugPlusMode = false;
                    return true;
                case "debug-plus":
                    optimizationLevel = OptimizationLevel.Debug;
                    debugPlusMode = true;
                    return true;
                case "debug":
                    optimizationLevel = OptimizationLevel.Debug;
                    debugPlusMode = false;
                    return true;
                default:
                    optimizationLevel = OptimizationLevel.Debug;
                    debugPlusMode = false;
                    return false;
            }
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
