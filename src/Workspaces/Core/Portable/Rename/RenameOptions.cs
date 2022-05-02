// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Rename
{
    public static class RenameOptions
    {
        public static Option<bool> RenameOverloads { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameOverloads), defaultValue: false);
        public static Option<bool> RenameInStrings { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameInStrings), defaultValue: false);
        public static Option<bool> RenameInComments { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameInComments), defaultValue: false);

        /// <summary>
        /// Set to true if the file name should match the type name after a rename operation
        /// </summary>
        internal static Option<bool> RenameFile { get; } = new Option<bool>(nameof(RenameOptions), nameof(RenameFile), defaultValue: false);

        public static Option<bool> PreviewChanges { get; } = new Option<bool>(nameof(RenameOptions), nameof(PreviewChanges), defaultValue: false);
    }

    internal readonly record struct RenameOptionSet(
        bool RenameOverloads,
        bool RenameInStrings,
        bool RenameInComments,
        bool RenameFile)
    {
        internal static RenameOptionSet From(Solution solution)
            => From(solution, options: null);

        internal static RenameOptionSet From(Solution solution, OptionSet options)
        {
            options ??= solution.Options;

            return new RenameOptionSet(
                options.GetOption(RenameOptions.RenameOverloads),
                options.GetOption(RenameOptions.RenameInStrings),
                options.GetOption(RenameOptions.RenameInComments),
                options.GetOption(RenameOptions.RenameFile));
        }
    }
}
