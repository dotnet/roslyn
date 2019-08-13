// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
}
