// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.InlineRename
{
    internal static class InlineRenameSessionOptionsStorage
    {
        public static readonly Option2<bool> RenameOverloads = new("dotnet_rename_overloads", defaultValue: false);
        public static readonly Option2<bool> RenameInStrings = new("dotnet_rename_in_strings", defaultValue: false);
        public static readonly Option2<bool> RenameInComments = new("dotnet_rename_in_comments", defaultValue: false);
        public static readonly Option2<bool> RenameFile = new("dotnet_rename_file", defaultValue: true);
        public static readonly Option2<bool> PreviewChanges = new("dotnet_preview_inline_rename_changes", defaultValue: false);
        public static readonly Option2<bool> RenameAsynchronously = new("dotnet_rename_asynchronously", defaultValue: true);
    }
}
