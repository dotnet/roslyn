// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Exposes extension methods for displaying symbol descriptions.
    /// </summary>
    public static class SymbolDisplayExtensions
    {
        /// <summary>
        /// Converts an immutable array of <see cref="SymbolDisplayPart"/>s to a string.
        /// </summary>
        /// <param name="parts">The array of parts.</param>
        /// <returns>The concatenation of the parts into a single string.</returns>
        [PerformanceSensitive("https://github.com/dotnet/roslyn/pull/67203", AllowImplicitBoxing = false)]
        public static string ToDisplayString(this ImmutableArray<SymbolDisplayPart> parts)
        {
            if (parts.IsDefault)
            {
                throw new ArgumentException("parts");
            }

            if (parts.Length == 0)
            {
                return string.Empty;
            }

            if (parts.Length == 1)
            {
                return parts[0].ToString();
            }

            var pool = PooledStringBuilder.GetInstance();
            try
            {
                var actualBuilder = pool.Builder;
                foreach (var part in parts)
                {
                    actualBuilder.Append(part.ToString());
                }

                return actualBuilder.ToString();
            }
            finally
            {
                pool.Free();
            }
        }

        /// <summary>
        /// Determines if a flag is set on the <see cref="SymbolDisplayCompilerInternalOptions"/> enum.
        /// </summary>
        /// <param name="options">The value to check.</param>
        /// <param name="flag">An enum field that specifies the flag.</param>
        /// <returns>Whether the <paramref name="flag"/> is set on the <paramref name="options"/>.</returns>
        internal static bool IncludesOption(this SymbolDisplayCompilerInternalOptions options, SymbolDisplayCompilerInternalOptions flag)
        {
            return (options & flag) == flag;
        }

        /// <summary>
        /// Determines if a flag is set on the <see cref="SymbolDisplayGenericsOptions"/> enum.
        /// </summary>
        /// <param name="options">The value to check.</param>
        /// <param name="flag">An enum field that specifies the flag.</param>
        /// <returns>Whether the <paramref name="flag"/> is set on the <paramref name="options"/>.</returns>
        internal static bool IncludesOption(this SymbolDisplayGenericsOptions options, SymbolDisplayGenericsOptions flag)
        {
            return (options & flag) == flag;
        }

        /// <summary>
        /// Determines if a flag is set on the <see cref="SymbolDisplayMemberOptions"/> enum.
        /// </summary>
        /// <param name="options">The value to check.</param>
        /// <param name="flag">An enum field that specifies the flag.</param>
        /// <returns>Whether the <paramref name="flag"/> is set on the <paramref name="options"/>.</returns>
        internal static bool IncludesOption(this SymbolDisplayMemberOptions options, SymbolDisplayMemberOptions flag)
        {
            return (options & flag) == flag;
        }

        /// <summary>
        /// Determines if a flag is set on the <see cref="SymbolDisplayMiscellaneousOptions"/> enum.
        /// </summary>
        /// <param name="options">The value to check.</param>
        /// <param name="flag">An enum field that specifies the flag.</param>
        /// <returns>Whether the <paramref name="flag"/> is set on the <paramref name="options"/>.</returns>
        internal static bool IncludesOption(this SymbolDisplayMiscellaneousOptions options, SymbolDisplayMiscellaneousOptions flag)
        {
            return (options & flag) == flag;
        }

        /// <summary>
        /// Determines if a flag is set on the <see cref="SymbolDisplayParameterOptions"/> enum.
        /// </summary>
        /// <param name="options">The value to check.</param>
        /// <param name="flag">An enum field that specifies the flag.</param>
        /// <returns>Whether the <paramref name="flag"/> is set on the <paramref name="options"/>.</returns>
        internal static bool IncludesOption(this SymbolDisplayParameterOptions options, SymbolDisplayParameterOptions flag)
        {
            return (options & flag) == flag;
        }

        /// <summary>
        /// Determines if a flag is set on the <see cref="SymbolDisplayKindOptions"/> enum.
        /// </summary>
        /// <param name="options">The value to check.</param>
        /// <param name="flag">An enum field that specifies the flag.</param>
        /// <returns>Whether the <paramref name="flag"/> is set on the <paramref name="options"/>.</returns>
        internal static bool IncludesOption(this SymbolDisplayKindOptions options, SymbolDisplayKindOptions flag)
        {
            return (options & flag) == flag;
        }

        /// <summary>
        /// Determines if a flag is set on the <see cref="SymbolDisplayLocalOptions"/> enum.
        /// </summary>
        /// <param name="options">The value to check.</param>
        /// <param name="flag">An enum field that specifies the flag.</param>
        /// <returns>Whether the <paramref name="flag"/> is set on the <paramref name="options"/>.</returns>
        internal static bool IncludesOption(this SymbolDisplayLocalOptions options, SymbolDisplayLocalOptions flag)
        {
            return (options & flag) == flag;
        }
    }
}
